using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Broker
{
    class BrokerAplication
    {
        static void Main(string[] args)
        {

            char[] delimiter = { ':', '/' };
            string[] arg = args[2].Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Broker Application " + arg[2]);
            Console.WriteLine("Broker Application URL:" + args[3]);
            Console.WriteLine("Broker Application URL:" + args[2]);


            TcpChannel brokerChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(brokerChannel, false);

            Broker broker = new Broker(args[0], args[3], args[2], args[4], args[5], args[6]);
            RemotingServices.Marshal(broker, "broker", typeof(Broker));
            if (!args[3].Equals("null"))
            {

                IBroker parent = (IBroker)Activator.GetObject(
                    typeof(IBroker), 
                    args[3]);

                parent.addChild(args[0], args[2]);
            }

            Console.ReadLine();
        }
    }



    public class Broker : MarshalByRefObject, IBroker
    {
        //Variables that caracterize the Broker or the system.
        private String parentURL;
        private string name;
        private string typeOrder;
        private string typeRouting;
        private bool lightLog;
        private string myUrl;
        private bool isFrozen = false;
        private Dictionary<string, string> childs;
        private Dictionary<string, string> pubs;
        private Dictionary<string, string> subs;

        //Auxiliar list for FIFO,FILTER,etc implementation
        private List<KeyValuePair<string, Event>> events;
        private List<Event> queueEvents;
        private List<KeyValuePair<string,string>> topicSubs;
        private List<KeyValuePair<string, string>> filteringInterest;
        private List<PubInfo> pubTopicSeq;
        private List<FrozenEvent> frozenEvents;
       // private Dictionary<string,int> maxPerTopic;
        private List<PubInfo> maxPerTopic;
      
        public Broker(string name, string parent,string myUrl,string policy, string order, string logLvl) {
            this.parentURL = parent;
            this.name = name;
            this.childs = new Dictionary<string, string>(2);
            this.pubs = new Dictionary<string, string>();
            this.subs = new Dictionary<string, string>();
            this.typeOrder = order;
            this.typeRouting = policy;
            if (logLvl.Equals("light"))
                lightLog = true;
            else
                lightLog = false;
            this.myUrl = myUrl;

            
            this.filteringInterest = new List<KeyValuePair<string, string>>();
            this.topicSubs = new List<KeyValuePair<string, string>>();
            this.events = new List<KeyValuePair<string, Event>>();
            this.queueEvents = new List<Event>();
            this.frozenEvents = new List<FrozenEvent>();
            this.pubTopicSeq = new List<PubInfo>();
            this.maxPerTopic = new List<PubInfo>();
        }

        public bool hasChild()
        {
            if (childs.Count == 0)
                return false;
            else return true;
        }

        public void addChild(string name, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("NEW CHILD", name, URL);
                frozenEvents.Add(fe);
                return;
            }
            this.childs.Add(name, URL);
            Console.WriteLine("Child Added:" + name);
        }

        public void addPublisher(string name, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("NEW PUBLISHER", name, URL);
                frozenEvents.Add(fe);
                return;
            }
            this.pubs.Add(name, URL);
            lock (this) { 
                this.pubTopicSeq.Add(new PubInfo(name));
            }
            Console.WriteLine("Pub Added:" + name);
        }

        public void addSubscriber(string name, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("NEW SUBSCRIPTOR",name,URL);
                frozenEvents.Add(fe);
                return;
            }
            this.subs.Add(name, URL);
            Console.WriteLine("Subscriber Added:" + name);
        }

        public string receivePub(string name, Event e)
        {
            //For freeze implementation
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("EVENT",e);
                frozenEvents.Add(fe);
                return "ACK";
            }

            Console.WriteLine("Received Publish" + "Name: " + name + "eventTopic: " + e.getTopic() + " " + e.getNumber());
            if (!(name.StartsWith("broker")))
            {
                sendToPM("PubEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
            }

            //For adjustment of events in filterings
            if(this.typeRouting.Equals("filtering"))
            {
                updateMax(e);
            }
               propagate(e);
               sentToSub(name, e);


            return "ACK";
        }

        //Function that keeps the max SeqNumber for each topic for adjustments purpose on filtering
        private void updateMax(Event e)
        {
            bool entrei = false;
            foreach(PubInfo maxPubs in this.maxPerTopic)
            {
                if(maxPubs.getName().Equals(e.getSender()))
                {
                    if(e.getNumber() > maxPubs.getSeqNumber(e.getTopic()))
                    {
                        maxPubs.setSeqNumber(e.getTopic(),e.getNumber());
                        entrei = true;
                    }
                }
            }
            if(!entrei)
            {
                PubInfo pubI = new PubInfo(e.getSender());
                pubI.addTopic(e.getTopic());
                pubI.setSeqNumber(e.getTopic(), e.getNumber());
                this.maxPerTopic.Add(pubI);
            }

            
            //if (this.maxPerTopic.ContainsKey(e.getTopic()))
            //{
            //    if (e.getNumber() > this.maxPerTopic[e.getTopic()])
            //        this.maxPerTopic[e.getTopic()] = e.getNumber();
            //}
            //else
            //{
            //    this.maxPerTopic.Add(e.getTopic(), e.getNumber());
            //}
        }

        private void sentToSub(string name, Event e)
        {
            if(typeOrder.Equals("NO")) 
            {
 
               // sendToSubscriber(e);
                Thread thread = new Thread(() => this.sendToSubscriber(e));
                thread.Start();
            }
            if(typeOrder.Equals("FIFO")) 
            {
               Thread thread = new Thread(() => this.sentToSubscriberFIFO(name,e));
               thread.Start();
                //sentToSubscriberFIFO(name, e);
            }
            
        }

        public PubInfo getPubInfo(string name, string topic)
        {

            foreach (PubInfo pubT in this.pubTopicSeq)
            {
                if (pubT.getName().Equals(name) && pubT.hasTopic(topic)) 
                {
                 
                    return pubT;
                }
            }
            return null;
        }

        private void sentToSubscriberFIFO(string name, Event e)
        {

            lock (this)
            {
                bool existsPub = false;
                bool existsTopic = false;
                if (this.pubTopicSeq.Count > 0)
                {

                    foreach (PubInfo pubT in this.pubTopicSeq)
                    {

                        if (pubT.getName().Equals(name))
                        {
                            existsPub = true;
                            pubT.addTopic(e.getTopic());
                            break;
                        }    
                    }
                }
                if (!existsPub)
                {
                    try
                    {
                        PubInfo newPub = new PubInfo(name);
                        newPub.addTopic(e.getTopic());
                        this.pubTopicSeq.Add(newPub);
                      //  lastSeqNumber.Add(name, 0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                Console.WriteLine("Event iD ->" + e.getNumber() + "FROM " + e.getSender() + "TOPIC" + e.getTopic());
                PubInfo pub = getPubInfo(name, e.getTopic());
                foreach (KeyValuePair<string, string> kvp in topicSubs)
                {

                    if (itsForSend(kvp, e.getTopic()))
                    {
                        Console.WriteLine("Found a Subscriber:" + kvp.Value);
                        existsTopic = true;

                        if (pub.getSeqNumber(e.getTopic()) + 1 /*lastSeqNumber[name] + 1*/ == e.getNumber())
                        {

                            ISubscriber sub = (ISubscriber)Activator.GetObject(
                            typeof(ISubscriber),
                            kvp.Value);
                            sub.receiveEvent(e.getSender(), e);
                            events.Add(new KeyValuePair<string, Event>(name, e));                          
                            sendToPM("SubEvent " + sub.getName() + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());

                            Console.WriteLine("SENT EVENT->" + e.getNumber() + "TOPIC:" + e.getTopic() + "PUB->" + e.getSender());

                            if (queueEvents.Contains(e))
                                queueEvents.Remove(e);

                         // pub.getSeqNumber(e.getTopic());
                            pub.addSeqNumber(e.getTopic());
                            getNextFIFOE(name, e);

                           
                        }
                        else
                        {
                                  Console.WriteLine("Go to Priority, Event:" + e.getNumber());
                                  queueEvents.Add(e);
                               //   getNextFIFOE(name, lastSeqNumber[name] + 1);

                        }
                    }
                }
                if (!existsTopic)
                    {
                        if (  (pub.getSeqNumber(e.getTopic())+ 1 == e.getNumber()) && (name.Equals(e.getSender())))
                        {
                            pub.addSeqNumber(e.getTopic());
                            getNextFIFOE(name, e);
                        }
                        else
                        {
                            queueEvents.Add(e);
                            //getNextFIFOE(name, e);
                        }

                    }
            }
        }
        

        private void getNextFIFOE(string name, Event e)
        {
            lock (this) { 
            try
            {
                  foreach (Event queueE in this.queueEvents)
                    {
                        if (queueE.getNumber() == (e.getNumber() + 1) && (queueE.getSender().Equals(e.getSender())) && queueE.getTopic().Equals(e.getTopic()))
                        {
                         //  Console.WriteLine("FOUND NEXT MESSAGE : " + queueE.getNumber());

                          sentToSubscriberFIFO(queueE.getSender(), queueE);
                          //  Thread thread = new Thread(() => this.sentToSubscriberFIFO(queueE.getSender(), queueE));
                           // thread.Start();
                            break;
                        }
                    }
                
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            }
        }

        public string subscribe(string topic, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("SUB", topic, URL);
                frozenEvents.Add(fe);
                return "ACK";
            }
            Console.WriteLine("Received Subscribe");
            this.topicSubs.Add(new KeyValuePair<string,string>(topic, URL));

            //this.maxPerTopic.Add(topic, 0);
            if (typeRouting.Equals("filtering"))
            {
                tellBrokersInterest(this.myUrl,topic);
            }
            return "ACK";
        }

        public string unsubscribe(string topic, string URL)
        {
            lock (this) { 
                if (isFrozen)
                {
                    FrozenEvent fe = new FrozenEvent("UNSUB", topic, URL);
                    frozenEvents.Add(fe);
                    return "ACK";
                }
        //        bool isfilter = false;
                // pode eliminar o errado caso existam 2 ocorrencias , FIX ME
                Console.WriteLine("Received Unsubscribe");
               foreach (PubInfo pub in this.pubTopicSeq)
               {
               
               }
                foreach(KeyValuePair<string,string>  kvp in topicSubs)
	            {

                    if (this.typeRouting.Equals("filtering"))
                    {
                        removeInterestBrokers(topic,myUrl);
                        //if (topic.Equals(kvp.Key))
                        //{
                        //    isfilter = true;
                        //    removeInterestBrokers();
                        //    break;
                        //}
                    }

                    if (kvp.Key.Equals(topic) && kvp.Value.Equals(URL))
                    {
                         topicSubs.Remove(kvp);
                         break;
	                }

                }
                return "ACK";
            }
        }

        private void removeInterestBrokers(string topic, string fromURL)
        {
            Console.WriteLine("FFS");
            if (!(parentURL.Equals("null")))
            {
                if (!parentURL.Equals(fromURL))
                {
                    IBroker parent = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                    this.parentURL);
                    Console.WriteLine("Remove to Parent: " + topic + fromURL);
                    parent.removeInterest(topic, myUrl);

                    sendToPM("BroEvent " + this.name + " , Removing Interest in " + topic);
                }

            }
            if (!(childs.Count == 0))
            {
                foreach (string childurl in childs.Values)
                {

                    if (!childurl.Equals(fromURL))
                    {

                        IBroker child = (IBroker)Activator.GetObject(
                            typeof(IBroker),
                            childurl);
                        Console.WriteLine("Remove to child: " + topic + childurl);
                        child.removeInterest(topic, myUrl);

                        sendToPM("BroEvent " + this.name + " , removing Interest in " + topic);
                    }
                }
            }
        }

        public void removeInterest(string topic, string fromURL)
        {
            Console.WriteLine("REMOVING  LOCK");
            lock (this) { 
                Console.WriteLine("REMOVING WON LOCK");
                bool existsTopic = false;
                KeyValuePair<string, string> kvp1;
                kvp1 = new KeyValuePair<string, string>(topic, fromURL);
                if (filteringInterest.Contains(kvp1))
                {
                    Console.WriteLine("ENTREI E RETIREI DO MEU INTERESSE!");
                    filteringInterest.Remove(kvp1);
                    foreach (KeyValuePair<string, string> t in filteringInterest)
                    {
                        Console.WriteLine("FILTERING TABLE ---->" + t.Key +"-->" + t.Value);
                    }
                }
                foreach (KeyValuePair<string, string> kvp in topicSubs)
                {
                    if (kvp.Key.Equals(topic))
                    {

                        existsTopic = true;
                        break;
                    }
                }
                if (!existsTopic)
                {

                    removeInterestBrokers(topic, fromURL);
                }
            }
        }


        public void crash()
        {
            Environment.Exit(-1);
        }

        public void status()
        {
            int i = 0;
            Console.WriteLine("Making Status");
            Console.WriteLine("ParentURL : " + parentURL);
            foreach (string s in childs.Keys)
        	{
                i++;
		        Console.WriteLine("Child nº"+i+" :"+s);
	        }
            Console.WriteLine("Numero de Childs : " + i);
            i = 0;
            foreach (string s in pubs.Keys)
            {
                i++;
                Console.WriteLine("Pub nº" + i + " :" + s);
            }
            Console.WriteLine("Numero de Pubs : " + i);
            i = 0;
            foreach (string s in subs.Keys)
            {
                i++;
                Console.WriteLine("Subscritor nº" + i + " :" + s);
            }
            Console.WriteLine("Numero de Subs : " + i);
            i = 0;
            foreach (KeyValuePair<string, string> kp in topicSubs)
            {
                i++;
                Console.WriteLine("Subscricao nº" + i + " Name :" + kp.Key + "Topic : "+ kp.Value);
            }
            Console.WriteLine("Numero de Subscricoes : " + i);
            i = 0;
            foreach (KeyValuePair<string, Event> kp in events)
            {
                i++;
                Console.WriteLine("Evento nº" + i + " Name:" + kp.Key + " Topic : " + kp.Value.getTopic() + " Conteudo : " + kp.Value.getContent());
            }
            Console.WriteLine("Numero de eventos : "+i);
        }

        public void propagate(Event e)
        {
            if (this.typeRouting.Equals("flooding"))
            {
                Thread thread = new Thread(() => this.floodNoOrder(e));
                thread.Start();
            }
            if (this.typeRouting.Equals("filtering"))
            {
                Thread thread = new Thread(() => this.floodFiltered(e));
                thread.Start();
            }
        }

        public void floodFiltered(Event e)
        {
            //ok stop it right
            lock (this) { 
                string lastHop = e.getLastHop();
                e.setLastHop(this.myUrl);
                foreach (KeyValuePair<string,string> kvp in filteringInterest)
                {
                   // Console.WriteLine("THIS IS THE FLOOD"+kvp.Value);
                     //&& kvp.Value.Equals(this.myUrl)
                    if (itsForSendInterest(kvp, e.getTopic()))
                    {
                        if (!lastHop.Equals(kvp.Value))
                        {
                           Console.WriteLine("Filtering to:" + kvp.Value);
                           IBroker broker = (IBroker)Activator.GetObject(
                           typeof(IBroker),
                           kvp.Value);
                           broker.receivePub(e.getSender(),e);
                        }
                    }
                }
            }
        }


        public void floodNoOrder(Event e)
        {
            Console.WriteLine("Flooding Started");
            string lastHop = e.getLastHop();
            e.setLastHop(this.myUrl);
            if (lastHop.Equals("null"))
            {
                if (! (parentURL.Equals("null")))
                {

                    IBroker parent = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                    this.parentURL);

                    parent.receivePub(e.getSender(), e);
                    sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());

                }
                if (!(childs.Count == 0))
                {

                    foreach (string childurl in childs.Values)
                    {
                        IBroker child = (IBroker)Activator.GetObject(
                            typeof(IBroker),
                            childurl);

                        child.receivePub(e.getSender(), e);
                        sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
                    }
                }
            }
            else
            {
                if (!(this.parentURL.Equals("null")) && !(this.parentURL.Equals(lastHop)))
                {

                    IBroker parent = (IBroker)Activator.GetObject(
                            typeof(IBroker),
                            this.parentURL);

                    parent.receivePub(e.getSender(), e);
                    sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
                }
                if (!(childs.Count == 0))
                {
                    foreach (string childurl in childs.Values)
                    {
                        if (!(childurl.Equals(lastHop)))
                        {
                          IBroker child = (IBroker)Activator.GetObject(
                                typeof(IBroker),
                               childurl);

                          child.receivePub(e.getSender(), e);
                          sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
                        }
                    }
                }
            }
        }

        private void tellBrokersInterest(string fromURL,string topic)
        {
            lock (this)
            {
                Console.WriteLine("TELLING EVERYONE FROM:" + fromURL);
                if (!(parentURL.Equals("null")))
                {
                    if (!parentURL.Equals(fromURL))
                    {
                        IBroker parent = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                        this.parentURL);
                        Console.WriteLine("Expand to Parent: " + topic + parentURL);
                        parent.receiveInterest(topic, myUrl);

                        sendToPM("BroEvent " + this.name + " , Giving Interest in " + topic);
                    }

                }
                if (!(childs.Count == 0))
                {
                    foreach (string childurl in childs.Values)
                    {
                        if (!childurl.Equals(fromURL))
                        {
                            IBroker child = (IBroker)Activator.GetObject(
                                typeof(IBroker),
                                childurl);
                            Console.WriteLine("Expand to child: " + topic + childurl);
                            child.receiveInterest(topic, myUrl);

                            sendToPM("BroEvent " + this.name + " , Giving Interest in " + topic);
                        }
                    }
                }
            }
        }


        public void receiveInterest(string topic, string url)
        {
            lock (this)
            {
                KeyValuePair<string, string> kvp;
                int max = 0;
                kvp = new KeyValuePair<string, string>(topic, url);
                if (!filteringInterest.Contains(kvp))
                {
                    filteringInterest.Add(kvp);
                    Console.WriteLine("Adicionei interesse from " + url + topic);
                    tellBrokersInterest(url, topic);

                    foreach(PubInfo pubMax in this.maxPerTopic)
                    {
                        Console.WriteLine(pubMax.getName() + "---->" + pubMax.getSeqNumber(topic));
                        if (pubMax.hasTopic(topic))
                        {
                            foreach(string topicAux in pubMax.getValues().Keys)
                            {
                                Console.WriteLine("I have the max ----->" + max);
                                if (pubMax.giveNumber(topic,topicAux) && pubMax.getSeqNumber(topicAux) > 0 )
                                    max = pubMax.getSeqNumber(topicAux);
                                else
                                   max = askNeighboors(topic, url, pubMax.getName());


                            // Vê isto deficiente de merda quando chegares. this.maxPerTopic
                                if(max > 0)
                                {
                                    Console.WriteLine("I'm in SHOULD I? MAX QUE VAI FDP " + max);
                                    if (this.childs.ContainsValue(url))
                                    {
                                        //pubMax.setSeqNumber(topic, max);
                                        foreach (KeyValuePair<string, string> child in childs)
                                        {
                                            if (child.Value.Equals(url))
                                            {
                                                IBroker broker = (IBroker)Activator.GetObject(
                                                 typeof(IBroker),
                                                child.Value);
                                                broker.adjustEvents(topicAux, max, pubMax.getName());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!parentURL.Equals("null") && parentURL.Equals(url))
                                        {
                                            IBroker broker = (IBroker)Activator.GetObject(
                                                    typeof(IBroker),
                                                    parentURL);
                                            broker.adjustEvents(topicAux, max, pubMax.getName());
                                        }
                                    }
                                }                            
                            }
                        }
                        else
                        {
                            Console.WriteLine("Going to ask my neighboors");
                            max = askNeighboors(topic, url, pubMax.getName());
                            Console.WriteLine("Max is ------>" + max);

                            Console.WriteLine("ASKED THE NEIGHBORS");
                            if (!(max <= 0))
                            {

                                Console.WriteLine("I'm in SHOULD I? MAX QUE VAI FDP " + max);
                                if (this.childs.ContainsValue(url))
                                {
                                    //pubMax.setSeqNumber(topic, max);
                                    foreach (KeyValuePair<string, string> child in childs)
                                    {
                                        if (child.Value.Equals(url))
                                        {
                                            IBroker broker = (IBroker)Activator.GetObject(
                                             typeof(IBroker),
                                            child.Value);
                                            broker.adjustEvents(topic, max, pubMax.getName());
                                        }
                                    }
                                }
                                else
                                {
                                    if (!parentURL.Equals("null") && parentURL.Equals(url))
                                    {
                                        IBroker broker = (IBroker)Activator.GetObject(
                                                typeof(IBroker),
                                                parentURL);
                                        broker.adjustEvents(topic, max, pubMax.getName());
                                    }
                                }
                            }

                        }

                    }

                }

            } 
        }

        private int askNeighboors(string topic, string url,string pubName)
        {
            int max = 0;
            int aux = 0;
            lock (maxPerTopic) 
            {
                Console.WriteLine("INSIDE ASK ----->" + url + " :::::" + topic);
                foreach (KeyValuePair<string, string> child in childs) 
                {

                    if(!child.Value.Equals(url))
                    {
                        IBroker broker = (IBroker)Activator.GetObject(
                                typeof(IBroker),
                                child.Value);
                        //Método que devolve um int que devolve o max seq para o topico topic
                        Console.WriteLine("Child: " + child.Value);
                        aux = broker.returnSeqTopic(topic, this.myUrl, pubName);
                        if(aux > max)
                        {
                            max = aux;
                        }
                    }
                }
                if((!parentURL.Equals("null")) && !parentURL.Equals(url))
                {
                    Console.WriteLine("Parent: "+  parentURL);
                    IBroker broker = (IBroker)Activator.GetObject(
                                typeof(IBroker),
                               parentURL);
                    //Método que devolve um int que devolve o max seq para o topico topic
                    aux = broker.returnSeqTopic(topic, this.myUrl, pubName);
                    if(aux > max)
                    {
                        max = aux;
                    }
                }
            }
            return max;
        }

        public int returnSeqTopic(string topic,string url,string pubName)
        {
         //   lock (maxPerTopic) 
           // {
            foreach (PubInfo pubMax in this.maxPerTopic)
            {
                if (pubMax.hasTopic(topic))
                {
                    return pubMax.getSeqNumber(topic);
                }
            }
           return askNeighboors(topic, url, pubName);
           // }
        }

        public void sendToSubscriber(Event e)
        {
            foreach (KeyValuePair<string,string> kvp in topicSubs)
            {
                Console.WriteLine("Event iD ->" + e.getNumber());
                if (itsForSend(kvp , e.getTopic()))
                {

                    ISubscriber sub = (ISubscriber)Activator.GetObject(
                    typeof(ISubscriber),
                    kvp.Value);
                    Console.WriteLine("Sending to : " + kvp.Value);
                    sub.receiveEvent(e.getSender(), e);
                    sendToPM("SubEvent "+sub.getName()+" , "+e.getSender()+" , "+e.getTopic()+" , "+e.getNumber());
                }
            }
        }

        public void adjustEvents(string topic, int max, string pubName)
        {

            lock (maxPerTopic) {
                Console.WriteLine("CORRESPONDING ADJUSTS");
                localPubAdjust(topic,max,pubName);
                localMaxAdjust(topic, max, pubName);
                
            }
        }

        private void localMaxAdjust(string topic, int max, string pubName)
        {
            bool inside = false;
            Console.WriteLine("MAX IS MAAAAAAAAX"+max);
            foreach (PubInfo pub in this.maxPerTopic)
            {
                if (pub.getName().Equals(pubName))
                {
                    inside = true;
                    if (pub.hasTopic(topic))
                    {
                        if (max > pub.getSeqNumber(topic))
                        {
                            pub.setSeqNumber(topic, max );
                        }
                    }
                    else
                    {
                        pub.addTopic(topic);
                        pub.setSeqNumber(topic, max );
                    }
                }
            }

            if (!inside)
            {
                PubInfo pubInfo = new PubInfo(pubName);
                pubInfo.addTopic(topic);
                pubInfo.setSeqNumber(topic, max);
                this.maxPerTopic.Add(pubInfo);
            }
        }

        private void localPubAdjust(string topic, int max, string pubName)
        {
            bool inside = false;
            Console.WriteLine("MAX DO LOCAL PUBAJUST---->" + max);
            foreach (PubInfo pub in this.pubTopicSeq)
            {
                if (pub.getName().Equals(pubName))
                {
                    inside = true;
                    if (pub.hasTopic(topic))
                    {
                        Console.WriteLine("TOPICO DO LOCAL PUBAJUST---->" + max + "--->" + topic);
                        if (max > pub.getSeqNumber(topic))
                        {
                            pub.setSeqNumber(topic, max);
                        }
                    }
                    else
                    {

                        pub.addTopic(topic);
                        pub.setSeqNumber(topic, max);
                    }
                }
            }

            if (!inside)
            {
                PubInfo pubInfo = new PubInfo(pubName);
                pubInfo.addTopic(topic);
                pubInfo.setSeqNumber(topic, max);
                this.pubTopicSeq.Add(pubInfo);
            }
        }


        private bool itsForSend(KeyValuePair<string, string> kvp, string topic)
        {

            string PathSub = kvp.Key;
            string url = kvp.Value;
            string pathEvento = topic;

            if (PathSub.Equals(pathEvento))
            {
                return true;
            }

            char[] delimiter = {'/'};
            string asterisco = "*";
            string[] pathSub = PathSub.Split(delimiter);
            string[] path = pathEvento.Split(delimiter);

            int niveis = pathSub.Count();
            int i = 0;
            bool isForSent = false;



            while (i <= niveis -1 )
	        {

               if (pathSub[i].Equals(path[i]))
                {
                    i++;
                }
               else {
                   if (!(pathSub[i].Equals(path[i])))
                   {
                       if ((i == niveis - 1) && path[i].Equals(asterisco))
                       {

                           isForSent = true;
                           break;
                       }
                       if ((i == niveis - 1) && pathSub[i].Equals(asterisco))
                       {
                           isForSent = true;
                           break;
                       }
                       else
                       {
                           isForSent = false;
                           break;
                       }
                   }
               }
	        }
            return isForSent;
        }

        private bool itsForSendInterest(KeyValuePair<string, string> kvp, string topic)
        {

            string PathSub = kvp.Key;
            string url = kvp.Value;
            string pathEvento = topic;

            if (PathSub.Equals(pathEvento))
            {
                return true;
            }

            char[] delimiter = { '/' };
            string asterisco = "*";
            string[] pathSub = PathSub.Split(delimiter);
            string[] path = pathEvento.Split(delimiter);

            int niveis = pathSub.Count();
            int i = 0;
            bool isForSent = false;



            while (i <= niveis - 1)
            {

                if (pathSub[i].Equals(path[i]))
                {
                    i++;
                }
                else
                {
                    if (!(pathSub[i].Equals(path[i])))
                    {
                        if ((i == niveis - 1) && pathSub[i].Equals(asterisco))
                        {

                            isForSent = true;
                            break;
                        }
                        else
                        {
                            isForSent = false;
                            break;
                        }
                    }
                }
            }
            return isForSent;
        }

        public void printevents()
        {

            //foreach();
        }

        public void sendToPM(string msg)
        {
            if (!(msg.StartsWith("BroEvent") && lightLog))
            {
                string cfgpath = @"..\..\..\cfg.txt";
                StreamReader script = new StreamReader(cfgpath);
                String Line = script.ReadLine();

                IPuppetMaster pm = (IPuppetMaster)Activator.GetObject(
                            typeof(IPuppetMaster),
                            Line);
                pm.toLog(msg);
            }
        }

        public void freeze()
        {
            isFrozen = true;
        }

        public void unfreeze()
        {
            isFrozen = false;
            checkFrozenEvents();
        }

        public void checkFrozenEvents()
        {
            foreach(FrozenEvent fe in frozenEvents)
            {
                switch (fe.getEventType())
                {
                    case"NEW CHILD":
                        this.addChild(fe.getName(), fe.getURL());
                        break;
                    case "NEW PUBLISHER":
                        this.addPublisher(fe.getName(), fe.getURL());
                        break;
                    case "NEW SUBSCRIPTOR":
                        this.addSubscriber(fe.getName(), fe.getURL());
                        break;
                    case "EVENT":
                        this.receivePub(fe.getEvent().getSender(), fe.getEvent());
                        break;
                    case "SUB":
                        this.subscribe(fe.getName(), fe.getURL()); //mesmo o nome dos atributos nao corresponder, vai bater tudo certo.
                        break;
                    case "UNSUB":
                        this.unsubscribe(fe.getName(), fe.getURL()); //mesmo o nome dos atributos nao corresponder, vai bater tudo certo.
                        break;
                }

            }

            frozenEvents = new List<FrozenEvent>();
        }
    }
}

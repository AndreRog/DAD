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
            Console.WriteLine("Broker Application " + " " + args[0] + " "+ arg[2]);
            Console.WriteLine("Broker Application URL:" + args[2]);
            Console.WriteLine("Broker Application Parent URL:" + args[3]);



            TcpChannel brokerChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(brokerChannel, false);

            Broker broker = new Broker(args[0], args[3], args[2], args[4], args[5], args[6]);
            RemotingServices.Marshal(broker, "broker", typeof(Broker));
            
            if (!args[3].Equals("null") && !args[7].Equals("REPLICA"))
            {

                IBroker parent = (IBroker)Activator.GetObject(
                    typeof(IBroker), 
                    args[3]);

                parent.addChild(args[0], args[2]);

            }
            if (args[7].Equals("REPLICA"))
            {
                IBroker parent = (IBroker)Activator.GetObject(
                  typeof(IBroker),
                    args[3]);
                parent.addReplica(args[2], true);
                broker.newReplica(args[3], true);
            }
            else
                broker.setLeader(true);


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
        private bool isLeader;
        private bool isFrozen = false;
        private Dictionary<string, string> childs;
        private Dictionary<string, bool> replicas;
        private Dictionary<string, string> pubs;
        private Dictionary<string, string> subs;

        //Auxiliar list for FIFO,FILTER,etc implementation
        private List<KeyValuePair<string, Event>> events;
        private List<Event> queueEvents;
        private List<KeyValuePair<string,string>> topicSubs;
        private List<KeyValuePair<string, string>> filteringInterest;
        private List<PubInfo> pubTopicSeq;
        private Queue<FrozenEvent> frozenEvents;
        private List<PubInfo> maxPerTopic;
        
        //Variable for totalOrder
        private Dictionary<string, int> adjustTotal;
        private int seqNumber;
        private int nextEvent;
        private Dictionary<string, int> fifoArray;
        private Dictionary<int, Event> totalQueue;
        private Queue<FrozenEvent> frozenQueue; 

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
            this.seqNumber = 0;
            this.nextEvent = 0;
            this.isLeader = false;
            this.replicas = new Dictionary<string,bool>();

            this.frozenQueue = new Queue<FrozenEvent>();

            this.adjustTotal = new Dictionary<string, int>();

            this.filteringInterest = new List<KeyValuePair<string, string>>();
            this.topicSubs = new List<KeyValuePair<string, string>>();
            this.events = new List<KeyValuePair<string, Event>>();
            this.queueEvents = new List<Event>();
            this.frozenEvents = new Queue<FrozenEvent>();
            this.pubTopicSeq = new List<PubInfo>();
            this.maxPerTopic = new List<PubInfo>();
            this.fifoArray = new Dictionary<string, int>(2);
            this.totalQueue =  new Dictionary<int,Event>();
        }

        public void setLeader(bool leader)
        {
            if (leader)
                Console.WriteLine("I am the Leader");
            else
                Console.WriteLine("I am the replica");

            this.isLeader = leader;
        }

        public bool getLeader()
        {
            return this.isLeader;
        }

        public void addReplica(string url, bool alive)
        {
            lock(this.replicas)
            {
            if(!this.replicas.ContainsKey(url))
            {
                this.replicas.Add(url, alive);
            }
            foreach(KeyValuePair<string,bool> brokers in this.replicas)
            {
                if(!(brokers.Key.Equals(url)))
                {
                        IBroker brokerS = (IBroker)Activator.GetObject(
                            typeof(IBroker),
                            brokers.Key);
                        brokerS.newReplica(url, true);
                }
            }
                }
        }

        public void newReplica(string url,bool alive)
        {
            lock (this.replicas)
            {
                if (!this.replicas.ContainsKey(url))
                {
                    this.replicas.Add(url, alive);

                    IBroker brokerS = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                           url);
                    brokerS.newReplica(this.myUrl, true);
                }
            }
        }

        public int SeqNumber()
        {
            return Interlocked.Increment(ref seqNumber);

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
                frozenEvents.Enqueue(fe);
                return;
            }
            foreach(KeyValuePair<string,bool> rep in  replicas)
            {
                if(rep.Value && isLeader)
                {
                    IBroker brokerS = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                            rep.Key);
                    brokerS.addChild(name, URL);
                }
            }
            this.childs.Add(name, URL);
            this.fifoArray.Add(URL, 0);
            Console.WriteLine("Child Added:" + name);
        }

        public void addPublisher(string name, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("NEW PUBLISHER", name, URL);
                frozenEvents.Enqueue(fe);
                return;
            }
            foreach (KeyValuePair<string, bool> rep in replicas)
            {
                if (rep.Value && isLeader)
                {
                    IBroker brokerS = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                            rep.Key);
                    brokerS.addPublisher(name, URL);
                }
            }
            this.pubs.Add(name, URL);
            Console.WriteLine("Pub Added:" + name);
        }

        public void addSubscriber(string name, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("NEW SUBSCRIPTOR",name,URL);
                frozenEvents.Enqueue(fe);
                return;
            }
            foreach (KeyValuePair<string, bool> rep in replicas)
            {
                if (rep.Value && isLeader)
                {
                    IBroker brokerS = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                            rep.Key);
                    brokerS.addSubscriber(name, URL);
                }
            }
            this.subs.Add(name, URL);
            Console.WriteLine("Subscriber Added:" + name);
        }

        public string receivePub(string name, Event e)
        {
            //For freeze implementation
            if (isFrozen)
            { 
                int i = e.getNumber() - e.getAdjustment();
                Console.WriteLine("FROZEN EVENT ---->" + i);
                FrozenEvent fe = new FrozenEvent("EVENT",e);
                frozenEvents.Enqueue(fe);
                return "ACK";
            }
            else { 
            int i = e.getNumber() - e.getAdjustment();
            e.setNumber(i);
            Console.WriteLine("Received Publish " + "Name: " + name + "eventTopic: " + e.getTopic() + " " + e.getNumber());
            if (!(name.StartsWith("broker")))
            {
                sendToPM("PubEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
            }
            
            //For adjustment of events in filterings
            if(this.typeRouting.Equals("filtering") && !(this.typeOrder.Equals("TOTAL")))
            {
                updateMax(e);
            }
            if (this.typeOrder.Equals("TOTAL") && e.getTotalSeq() == 0)
            {
                if (this.typeRouting.Equals("flooding")) { 
                    if (!this.adjustTotal.ContainsKey(e.getTopic()))
                    {
                        adjustTotal.Add(e.getTopic(), 0);
                    }
                    e.setSeqNumber(getSeqNumber());
                }
                else
                {
                    sendToRoot(e);
                }
            }
            if(!(this.typeOrder.Equals("TOTAL")) || !(this.typeRouting.Equals("filtering")))
            {
               propagate(e);
               sentToSub(name, e);
            }
            }
            return "ACK";
        }

        //FILTERING TOTAL
        public void sendToRoot(Event e)
        {
            if (parentURL.Equals("null"))
            {
                Thread thread = new Thread(() => this.totalpropagate(e));
                thread.Start();
            }
            else
            {
                IBroker parent = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                        this.parentURL);
                parent.sendToRoot(e);
            }
        }

        private void totalpropagate(Event e)
        {
            lock(this)
            {
                int i;
                foreach (KeyValuePair<string, string> kvp in filteringInterest) 
                {
                    if (childs.ContainsValue(kvp.Value))
                    {
                        if (itsForSendInterest(kvp, e.getTopic()))
                        { 

                            this.fifoArray[kvp.Value] = this.fifoArray[kvp.Value] + 1;
                            sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + this.fifoArray[kvp.Value]);
                            i = this.fifoArray[kvp.Value];
                            IBroker child = (IBroker)Activator.GetObject(
                                 typeof(IBroker),
                                 kvp.Value);
                            child.receiveFIFOChannel(e, i);
                        }
                    }
                }
                if(parentURL.Equals("null"))
                    sendToSubscriber(e);
            }
        }

        public void receiveFIFOChannel(Event e, int next)
        {
            lock(this)
            {
                if(this.nextEvent + 1 == next )
                {
                    sendToSubscriber(e);
                    this.nextEvent += 1;
                    this.totalpropagate(e);
                    Thread thread = new Thread(() => this.getNextTotal(next));
                    thread.Start();
                  //  getNextTotal(next);
                    if(totalQueue.ContainsKey(next))
                        totalQueue.Remove(next);
                }
                else
                {
                    if(!totalQueue.ContainsKey(next))
                     totalQueue.Add(next, e);
                }
            }
        }

        public void getNextTotal(int i)
        {
            lock (this)
            {
                foreach (KeyValuePair<int, Event> nextE in totalQueue)
                {
                    if (nextE.Key == i + 1)
                    {
                        receiveFIFOChannel(nextE.Value, nextE.Key);
                        break;
                    }
                }
            }
        }

        //TOTAL FLOOD
        public int getSeqNumber()
        {
            int nextSeq = 0;
            if (parentURL.Equals("null"))
            {
                nextSeq = this.SeqNumber();
                Console.WriteLine("ROOT SENT ------>" + nextSeq);
                return nextSeq;
            }
            else
            {
                IBroker parent = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                        this.parentURL);
                        Console.WriteLine("GetSeq from Parent: " + parentURL);
                nextSeq = parent.getSeqNumber();
            }
            Console.WriteLine("ROOT SENT ------>" + nextSeq);
            return nextSeq;
        }

        private void sentToSubscriberTOTAL(string name, Event e)
        {
            lock (this)
            {
                bool existsTopic = false;
                foreach (KeyValuePair<string, string> kvp in topicSubs)
                {

                    if (itsForSend(kvp, e.getTopic()))
                    {
                        Console.WriteLine("Found a Subscriber:" + kvp.Value);
                        existsTopic = true;

                        if (e.getTotalSeq() == this.nextEvent + 1 || (e.getTotalSeq() - (e.getAdjustment()) == this.nextEvent + 1))
                        {

                            ISubscriber sub = (ISubscriber)Activator.GetObject(
                            typeof(ISubscriber),
                            kvp.Value);
                            sub.receiveEvent(e.getSender(), e);
                            events.Add(new KeyValuePair<string, Event>(name, e));
                            sendToPM("SubEvent " + sub.getName() + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());

                            Console.WriteLine("SENT EVENT->" + e.getTotalSeq() + "TOPIC:" + e.getTopic());

                            if (queueEvents.Contains(e))
                                queueEvents.Remove(e);

                            // pub.getSeqNumber(e.getTopic());
                            this.nextEvent = e.getTotalSeq();
                            getNextTOTALE(name, e);


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
                    if (this.nextEvent + 1 == e.getTotalSeq())
                    {
                        this.nextEvent += 1;
                        getNextTOTALE(name, e);
                    }
                    else
                    {
                        queueEvents.Add(e);
                        //getNextFIFOE(name, e);
                    }
                }
            }
        }

        private void getNextTOTALE(string name, Event e)
        {
            lock (this)
            {
                try
                {
                    foreach (Event queueE in this.queueEvents)
                    {
                        if (queueE.getTotalSeq() == (e.getTotalSeq() + 1) || queueE.getTotalSeq() - queueE.getAdjustment() == (e.getTotalSeq() + 1))
                        {
                            //  Console.WriteLine("FOUND NEXT MESSAGE : " + queueE.getNumber());

                            sentToSubscriberTOTAL(queueE.getSender(), queueE);
                            //  Thread thread = new Thread(() => this.sentToSubscriberFIFO(queueE.getSender(), queueE));
                            // thread.Start();
                            break;
                        }
                    }

                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
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
        }

        public PubInfo getPubInfo(string name)
        {

            foreach (PubInfo pubT in this.pubTopicSeq)
            {
                if (pubT.getName().Equals(name))
                {

                    return pubT;
                }
            }
            return null;
        }

        private void sentToSub(string name, Event e)
        {
            if(typeOrder.Equals("NO")) 
            {
 
               // sendToSubscriber(e);
                Thread thread = new Thread(() => this.sendToSubscriber(e));
                thread.Start();
            }
            if(typeOrder.Equals("FIFO") && !(this.typeRouting.Equals("filtering")))
            {
               //Thread thread = new Thread(() => this.sentToSubscriberFIFO(name,e));
               //thread.Start();
                sentToSubscriberFIFO(name, e);
            }
            if (typeOrder.Equals("TOTAL"))
            {
                Thread thread = new Thread(() => this.sentToSubscriberTOTAL(name, e));
                thread.Start();
            }
            
        }

        private Event findNextTotal(Event e)
        {
            lock (this)
            {
                try
                {
                    foreach (Event queueE in this.queueEvents)
                    {
                        if (queueE.getTotalSeq() == (e.getTotalSeq() + 1))
                        {
                            return queueE;
                        }
                    }

                }
                catch (Exception ex) { Console.WriteLine(ex); }

                return null;
            }
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
                            break;
                        }    
                    }
                }
                if (!existsPub)
                {
                    try
                    {
                        PubInfo newPub = new PubInfo(name);
                        this.pubTopicSeq.Add(newPub);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                Console.WriteLine("Event iD ->" + e.getNumber() + "FROM " + e.getSender() + "TOPIC" + e.getTopic());    
                PubInfo pub = getPubInfo(name);
                foreach (KeyValuePair<string, string> kvp in topicSubs)
                {

                    if (itsForSend(kvp, e.getTopic()))
                    {
                        Console.WriteLine("Found a Subscriber:" + kvp.Value);
                        existsTopic = true;

                        if (pub.getFIFOSeq() + 1  == e.getNumber())
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

                            pub.addFIFOSeq();
                            getNextFIFOE(name, e);

                           
                        }
                        else
                        {
                                  Console.WriteLine("Go to Priority, Event:" + e.getNumber());
                                  queueEvents.Add(e);
                        }
                    }
                }
                if (!existsTopic)
                {
                    if ((pub.getFIFOSeq() + 1 == e.getNumber()))
                    {
                        pub.addFIFOSeq();
                        getNextFIFOE(name, e);
                    }
                    else
                    {
                        queueEvents.Add(e);
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
                        if (queueE.getNumber() == (e.getNumber() + 1) && (queueE.getSender().Equals(e.getSender())))
                        {
                         //  Console.WriteLine("FOUND NEXT MESSAGE : " + queueE.getNumber());
                            if(this.typeRouting.Equals("flooding"))
                                sentToSubscriberFIFO(queueE.getSender(), queueE);
                            else
                            { 
                                Thread thread = new Thread(() => this.floodFiltered(queueE));
                                thread.Start();
                            }
                            break;
                        }
                    }
                
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            }
        }

        //NORMAL FUNCS
        public string subscribe(string topic, string URL)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("SUB", topic, URL);
                frozenEvents.Enqueue(fe);
                return "ACK";
            }
            Console.WriteLine("Received Subscribe " + topic);
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
                    frozenEvents.Enqueue(fe);
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
            if (!(parentURL.Equals("null")))
            {
                if (!parentURL.Equals(fromURL))
                {
                    IBroker parent = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                    this.parentURL);
                    Console.WriteLine("Remove to Parent: " + topic + fromURL);
                    parent.removeInterest(topic, myUrl);

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

                    }
                }
            }
        }

        public void removeInterest(string topic, string fromURL)
        {
            lock (this) { 
                bool existsTopic = false;
                KeyValuePair<string, string> kvp1;
                kvp1 = new KeyValuePair<string, string>(topic, fromURL);
                if (filteringInterest.Contains(kvp1))
                {
                    filteringInterest.Remove(kvp1);
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
            foreach(KeyValuePair<string, bool> rep in this.replicas)
            {
                if (rep.Value)
                    Console.WriteLine("Replica:" + rep.Key + " is alive" );
                else
                    Console.WriteLine("Replica:" + rep.Key + " failed");
            }
            
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

        public bool hasPub(string publisher)
        {
            bool pubFound = false;
            foreach(PubInfo pub in this.pubTopicSeq)
            {
                if(pub.getName().Equals(publisher))
                {
                    pubFound = true;
                    return pubFound;
                }
            }
            return pubFound;
        }

        public void floodFiltered(Event e)
        {
            //ok stop it right
            lock (this) { 
                string lastHop = e.getLastHop();
                List<string> sentURL = new List<string>();
                e.setLastHop(this.myUrl);
                if(!hasPub(e.getSender()))
                {
                    addInfo(e.getSender());
                }
                PubInfo pub = getPubInfo(e.getSender());
                if (e.getNumber() == pub.getFIFOSeq() + 1)
                {
                    foreach (KeyValuePair<string,string> kvp in filteringInterest)
                    {
                        if (itsForSendInterest(kvp, e.getTopic()))
                        {
                            if (!lastHop.Equals(kvp.Value) && !sentURL.Contains(kvp.Value))
                            {

                                
                               e.setAdjustment(pub.getAdjusment(kvp.Value));
                               Console.WriteLine("Filtering to:" + kvp.Value +  "  " + pub.getAdjusment(kvp.Value) + " " + e.getTopic());
                               sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
                               IBroker broker = (IBroker)Activator.GetObject(
                               typeof(IBroker),
                               kvp.Value);
                               broker.receivePub(e.getSender(),e);
                               sentURL.Add(kvp.Value);
                            }
                        }
                        else
                        {
                            if (!(presentInterest(e.getTopic(), kvp.Value)) && !lastHop.Equals(kvp.Value) && !sentURL.Contains(kvp.Value))
                            {

                                pub.addAdjustment(kvp.Value);
                                sentURL.Add(kvp.Value);
                            }
                        }
                    }
                    sendToSubscriber(e);
                    pub.addFIFOSeq();
                    getNextFIFOE(e.getSender(),e);
                }
                else
                {
                    this.queueEvents.Add(e);
                }
              }
          }

        private void addInfo(string name)
        {
            PubInfo newPub = new PubInfo(name);
            if (!parentURL.Equals("null"))
                newPub.addURL(parentURL);
            foreach (string url in this.childs.Values)
            {
                newPub.addURL(url);
            }
            this.pubTopicSeq.Add(newPub);
        }

        public bool presentInterest(string p1, string p2)
        {
            bool notPresentInterest = false;
            foreach (KeyValuePair<string, string> kvp in filteringInterest) 
            {
                if (kvp.Value.Equals(p2) && kvp.Key.Equals(p1))
                {
                    notPresentInterest = true;
                    return true;
                }
            }
            return notPresentInterest;
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


                }
                if (!(childs.Count == 0))
                {

                    foreach (string childurl in childs.Values)
                    {
                        IBroker child = (IBroker)Activator.GetObject(
                            typeof(IBroker),
                            childurl);

                        child.receivePub(e.getSender(), e);

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

                        }
                    }
                }
            }
             sendToPM("BroEvent " + name + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());
        }

        private void tellBrokersInterest(string fromURL,string topic)
        {
            lock (this)
            {
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
                kvp = new KeyValuePair<string, string>(topic, url);
                if (!filteringInterest.Contains(kvp))
                {
                    filteringInterest.Add(kvp);
                    Console.WriteLine("Adicionei interesse from " + url + topic);
                    tellBrokersInterest(url, topic);
                }
            }
        }

        public void sendToSubscriber(Event e)
        {
            foreach (KeyValuePair<string,string> kvp in topicSubs)
            {
                Console.WriteLine("Event iD ->" + e.getNumber() + " " + e.getTopic());
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
            Console.WriteLine("Unfreezng the queue");
            checkFrozenEvents();
        }

        public void checkFrozenEvents()
        {
            lock(this)
            {

                while (this.frozenEvents.Count > 0)
            {
                Console.WriteLine("Unfreezng the queue");
                FrozenEvent fe = frozenEvents.Dequeue();
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
            }  
            frozenEvents = new Queue<FrozenEvent>();
        }
    }
}

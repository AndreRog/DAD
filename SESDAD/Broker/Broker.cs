﻿using System;
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

        private String parentURL;
        private string name;
        private List<KeyValuePair<string, Event>> events;
        private List<Event> queueEvents;
        private Dictionary<string,string> childs;
        private Dictionary<string, string> pubs;
        private Dictionary<string, string> subs;
        private List<KeyValuePair<string,string>> topicSubs;
        private List<KeyValuePair<string, string>> filteringInterest;
        private Dictionary<string, int> lastSeqNumber;
        private List<FrozenEvent> frozenEvents;
        private string typeOrder;
        private string typeRouting;
        private bool lightLog;
        private string myUrl;
        private bool isFrozen = false;
      
        public Broker(string name, string parent,string myUrl,string policy, string order, string logLvl) {
            this.parentURL = parent;
            this.name = name;
            this.childs = new Dictionary<string, string>(2);
            this.filteringInterest = new List<KeyValuePair<string, string>>();
            this.pubs = new Dictionary<string, string>();
            this.subs = new Dictionary<string, string>();
            this.topicSubs = new List<KeyValuePair<string, string>>();
            this.events = new List<KeyValuePair<string, Event>>();
            this.queueEvents = new List<Event>();
            this.frozenEvents = new List<FrozenEvent>();
            this.lastSeqNumber = new Dictionary<string, int>();
            this.typeOrder = order;
            this.typeRouting = policy;
            if (logLvl.Equals("light"))
                lightLog = true;
            else
                lightLog = false;
            this.myUrl = myUrl;
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
            this.lastSeqNumber.Add(name,0);
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


               propagate(e);
               sentToSub(name, e);


            return "ACK";
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
               // sentToSubscriberFIFO(name, e);
            }
            
        }

        private void sentToSubscriberFIFO(string name, Event e)
        {

            bool existsTopic = false;
            lock (this)
            {

                //}
                if (!(this.lastSeqNumber.ContainsKey(name)))
                {

                    try
                    {
                        lastSeqNumber.Add(name, 0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
               // Console.WriteLine("Event iD ->" + e.getNumber() + "FROM " + e.getSender());
                foreach (KeyValuePair<string, string> kvp in topicSubs)
                {

                    if (itsForSend(kvp, e.getTopic()))
                    {
                        Console.WriteLine("Found a Subscriber:" + kvp.Value);
                        existsTopic = true;
                        if (lastSeqNumber[name] + 1 == e.getNumber())
                        {

                            ISubscriber sub = (ISubscriber)Activator.GetObject(
                            typeof(ISubscriber),
                            kvp.Value);
                            Console.WriteLine("Sending to : " + kvp.Value + " EVENT :" + e.getNumber() + "FROM:" + name);
                            sub.receiveEvent(e.getSender(), e);
                            events.Add(new KeyValuePair<string, Event>(name, e));
                            sendToPM("SubEvent " + sub.getName() + " , " + e.getSender() + " , " + e.getTopic() + " , " + e.getNumber());



                           //     Console.WriteLine("COME ON IN");
                           if (queueEvents.Contains(e))
                                 queueEvents.Remove(e);
                           lastSeqNumber[name] += 1;
                       //    Console.WriteLine("SENT SUB WITH EVENT NUMBER:" + e.getNumber() + " AND SEQNUMB: " + lastSeqNumber[name] + " PUB:" + e.getSender());
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

                        if ((lastSeqNumber[name] + 1 == e.getNumber()) && (name.Equals(e.getSender())))
                        {
                            lastSeqNumber[name] += 1;
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
                        if (queueE.getNumber() == (e.getNumber() + 1) && (queueE.getSender().Equals(e.getSender())))
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
            Console.WriteLine("HAVE TO REMOVE");
            bool existsTopic = false;
            KeyValuePair<string, string> kvp1;
            kvp1 = new KeyValuePair<string, string>(topic, fromURL);
            if (filteringInterest.Contains(kvp1))
            {
                Console.WriteLine("Entrei");
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
             string lastHop = e.getLastHop();
            e.setLastHop(this.myUrl);
            foreach (KeyValuePair<string,string> kvp in filteringInterest)
            {
                 //&& kvp.Value.Equals(this.myUrl)
                if (kvp.Key.Equals(e.getTopic()))
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
            if (!(parentURL.Equals("null")))
            {
                if (!parentURL.Equals(fromURL))
                {
                    IBroker parent = (IBroker)Activator.GetObject(
                    typeof(IBroker),
                    this.parentURL);

                    parent.receiveInterest(topic, myUrl);
                    Console.WriteLine("Expand to Parent: " + topic + fromURL);
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

                        child.receiveInterest(topic, myUrl);
                        Console.WriteLine("Expand to child: " + topic + fromURL);
                        sendToPM("BroEvent " + this.name + " , Giving Interest in " + topic);
                    }
                }
            }
        }

        public void receiveInterest(string topic, string name)
        {
            KeyValuePair<string,string> kvp;
            kvp = new KeyValuePair<string,string>(topic, name);
            if(!filteringInterest.Contains(kvp))
            {
                filteringInterest.Add(kvp);
                tellBrokersInterest(name, topic);
                Console.WriteLine("Adicionei interesse from " + name + topic);
            }
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

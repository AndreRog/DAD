using System;
using CommonTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Diagnostics;

namespace Subscriber
{
    class SubscriberApplication
    {
        static void Main(string[] args)
        {

            char[] delimiter = { ':', '/' };
            string[] arg = args[2].Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Subscriber Application " + arg[2]);


            TcpChannel subChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(subChannel, false);
            
            //Add Sub to Broker.
            IBroker broker = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                    args[3]);

            Subscriber subscriber = new Subscriber(args[0],args[2]);
            int i = 3;
            while(i < args.Length )
            { 
                subscriber.addBroker(args[i]);
                i++;
            }

            RemotingServices.Marshal(subscriber, "sub", typeof(Subscriber));


     

            broker.addSubscriber(args[0], args[2]);
            Console.ReadLine();

        }
    }

    public class Subscriber : MarshalByRefObject, ISubscriber
    {
        private string name;

        private string adress;

        private Dictionary<string,bool> brokerUrl;

        private string leaderURL;

        private List<KeyValuePair<string, Event>> eventsReceived;

        private bool isFrozen = false;

        private List<FrozenEvent> frozenEvents;

        //private Dictionary<string, Event> eventsReceived;

        public Subscriber(string name, string url)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = new Dictionary<string,bool>();
            this.leaderURL = "null";
            this.eventsReceived = new List<KeyValuePair<string, Event>>();
            this.frozenEvents = new List<FrozenEvent>();
        }

        public void addBroker(string url)
        {
            if (this.leaderURL.Equals("null"))
            {
                this.leaderURL = url;
            }
            else 
            { 
                char[] delimiter = { ':', '/' };
                string[] arg = url.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                int newPort = Int32.Parse(arg[2]);
                foreach(string brokerS in this.brokerUrl.Keys)
                {
                    string[] b = brokerS.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                    int oldPort = Int32.Parse(b[2]);
                    if (newPort < oldPort)
                    {
                        this.leaderURL = url;
                    }
                }
            }
            this.brokerUrl.Add(url, true);
            Console.WriteLine("Leader--->" + this.leaderURL);
        }

        public string getName()
        {
            return name;
        }

        public void subEvent(string topic)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("SUB", topic,adress);
                frozenEvents.Add(fe);
                return;
            }
            try
            {
                IBroker broker = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                        this.leaderURL);

                broker.subscribe(topic, adress);
                Console.WriteLine("Create Subscription on : " + topic);
            }
            catch(Exception e)
            {
                Console.WriteLine("BUM BUM CLAP:" + e.Message);
            }
        }

        public void UnsubEvent(string topic)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("UNSUB", topic, adress);
                frozenEvents.Add(fe);
                return;
            }
            try
            {
                IBroker broker = (IBroker)Activator.GetObject(
                     typeof(IBroker),
                     this.leaderURL);

                broker.unsubscribe(topic, adress);
                Console.WriteLine("Create Unsubscription on : " + topic);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something make bum bum"+ e.Message);
            }
        }

        public void displayEvents()
        {
            foreach (KeyValuePair<string,Event> kvp in eventsReceived)
            {
                Console.WriteLine("Event de : " + kvp.Key+" sobre : "+kvp.Value.getTopic());
            }
        }

        public void receiveEvent(string name, Event e)
        {
            if (isFrozen)
            {
                FrozenEvent fe = new FrozenEvent("EVENT", e);
                frozenEvents.Add(fe);
                return;
            }
            this.eventsReceived.Add(new KeyValuePair<string, Event>(name, e));
            if(e.getTotalSeq() != 0)
            {
                Console.WriteLine("Evento Recebido de " + e.getSender() + " sobre " + e.getTopic() + " EventNumber : " + e.getTotalSeq());
            }
            else
            {
                Console.WriteLine("Evento Recebido de " + e.getSender() + " sobre " + e.getTopic() + " EventNumber : " + e.getNumber());
            }

        }

        public void crash()
        {
            Process.GetCurrentProcess().Kill();
        }

        public void status()
        {
            int i = 0;
            Console.WriteLine("Making Status");
            Console.WriteLine("Name : " + name);
            Console.WriteLine("Address : " + adress);
            Console.WriteLine("BrokerURL : " + brokerUrl);
            Console.WriteLine("Eventos recebidos");
            foreach (KeyValuePair<string, Event> kvp in eventsReceived)
            {
                Console.WriteLine("Event de : " + kvp.Key + " sobre : " + kvp.Value.getTopic());
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
            foreach (FrozenEvent fe in frozenEvents)
            {
                switch (fe.getEventType())
                {
                    case "EVENT":
                        this.receiveEvent(fe.getEvent().getSender(), fe.getEvent());
                        break;
                    case "SUB":
                        this.subEvent(fe.getName()); //mesmo o nome dos atributos nao corresponder, vai bater tudo certo.
                        break;
                    case "UNSUB":
                        this.UnsubEvent(fe.getName()); //mesmo o nome dos atributos nao corresponder, vai bater tudo certo.
                        break;
                }

            }

            frozenEvents = new List<FrozenEvent>();
        }
    }
}

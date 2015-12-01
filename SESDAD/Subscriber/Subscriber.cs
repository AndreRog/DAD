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

            Subscriber subscriber = new Subscriber(args[0],args[2], args[3],broker);
            RemotingServices.Marshal(subscriber, "sub", typeof(Subscriber));


     

            broker.addSubscriber(args[0], args[2]);
            Console.ReadLine();

        }
    }

    public class Subscriber : MarshalByRefObject, ISubscriber
    {
        private string name;

        private string adress;

        private string brokerUrl;

        private IBroker broker;

        private List<KeyValuePair<string, Event>> eventsReceived;
        private bool isFrozen = false;
        private List<FrozenEvent> frozenEvents;
        //private Dictionary<string, Event> eventsReceived;

        public Subscriber(string name, string url, string brokerUrl, IBroker broker)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = brokerUrl;
            this.broker = broker;
            this.eventsReceived = new List<KeyValuePair<string, Event>>();
            this.frozenEvents = new List<FrozenEvent>();
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
                this.broker.subscribe(topic, adress);
                Console.WriteLine("Create Subscription on : " + topic);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something make bum bum" + e.Message);
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
                this.broker.unsubscribe(topic, adress);
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
            Console.WriteLine("Evento Recebido de "+e.getSender()+" sobre " + e.getTopic()+" EventNumber : "+e.getNumber());
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

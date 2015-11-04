using System;
using CommonTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

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
            RemotingServices.Marshal(subscriber, "subscriber", typeof(Subscriber));


     

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

        private Dictionary<string, Event> eventsReceived;

        public Subscriber(string name, string url, string brokerUrl, IBroker broker)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = brokerUrl;
            this.broker = broker;
            eventsReceived = new Dictionary<string, Event>();
        }

        public string getName()
        {
            return name;
        }

        public void subEvent(string topic)
        {
            try
            {
                this.broker.subscribe(topic, adress);
                Console.WriteLine("Create Subscription on : " + topic);
            }
            catch (Exception)
            {
                Console.WriteLine("Something make bum bum");
            }
        }

        public void UnsubEvent(string topic)
        {
            try
            {
                this.broker.unsubscribe(topic, adress);
                Console.WriteLine("Create Unsubscription on : " + topic);
            }
            catch (Exception)
            {
                Console.WriteLine("Something make bum bum");
            }
        }

        public void displayEvents()
        {
            foreach (string s in eventsReceived.Keys)
            {
                Console.WriteLine("Event : " + s);
            }
        }

        public void receiveEvent(string topic, Event e)
        {
            if (!(eventsReceived.ContainsValue(e)))
            {
                this.eventsReceived.Add(topic, e);
                Console.WriteLine("Evento Recebido : " + topic);
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
            Console.WriteLine("Name : " + name);
            Console.WriteLine("Address : " + adress);
            Console.WriteLine("BrokerURL : " + brokerUrl);
            Console.WriteLine("Eventos recebidos");
            foreach (Event e in eventsReceived.Values)
            {
                i++;
                Console.WriteLine("Evento nº " + i + "Topic : " + e.getTopic() + " Content : " + e.getContent());
            }
        }
    }
}

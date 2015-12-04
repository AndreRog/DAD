using System;
using CommonTypes;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Publisher
{
    class PublisherApplication
    {
        static void Main(string[] args)
        {
            char[] delimiter = { ':', '/' };
            string[] arg = args[2].Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Publisher Application " + arg[2]);

            Console.WriteLine(arg[2]);
            TcpChannel subChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(subChannel, false);

            //Add Pub to broker.
            IBroker broker = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                    args[3]);

            Publisher publisher = new Publisher(args[0],args[2], args[3], broker);
            RemotingServices.Marshal(publisher, "pub", typeof(Publisher));



            broker.addPublisher(args[0],args[2]);

            Console.ReadLine();
        }
    }

    public class Publisher : MarshalByRefObject, IPublisher
    {
        private string name;

        private string adress;

        private string brokerUrl;

        private IBroker broker;

        private List<KeyValuePair<string, Event>> events;
        private Dictionary<string, int> pubSeq;

        private int seqNumber ;

        private bool isFrozen = false;

        private List<FrozenEvent> frozenEvents;

      //  private Dictionary<string, Event> events; 

        public Publisher(string name, string url, string brokerUrl, IBroker broker)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = brokerUrl;
            this.broker = broker;
            this.events = new List<KeyValuePair<string, Event>>();
            this.pubSeq = new Dictionary<string,int>();
            this.frozenEvents = new List<FrozenEvent>();
            this.seqNumber = 0;
        }

        //Antes de reestruturação
        public int SeqNumber()
        {
            return Interlocked.Increment(ref seqNumber);

        }

        public void pubEvent(string numberEvents, string topic, string interval)
        {
            Thread thread = new Thread(() => this.sendEvent(numberEvents, topic, interval));
            thread.Start();
        }

        public void sendEvent(string numberEvents, string topic, string interval)
        {
           
            Event e;
            int times = Int32.Parse(numberEvents);
            int sleep = Int32.Parse(interval);
            int i = 0;
            int eventNumber;

            Console.WriteLine(this.brokerUrl);
            if (!this.pubSeq.ContainsKey(topic))
            {
                this.pubSeq.Add(topic, 0);
            }
            for (i = 0; i < times; i++)
            {
                //this.pubSeq[topic] += 1;
                //eventNumber = this.pubSeq[topic];


                if (isFrozen)
                {
                    i--;
                }
                else
                {
                    eventNumber = SeqNumber();
                    e = new Event(topic, "", this.name, eventNumber);
                    Console.WriteLine("Creating Event : " + topic + "EventNumber:" + e.getNumber());
                    this.broker.receivePub(this.name, e);
                    events.Add(new KeyValuePair<string, Event>(name, e));

                    Thread.Sleep(sleep);
                }
            }
        
        }

        public void crash()
        {
            Environment.Exit(-1);
        }

        public void status()
        {
            Console.WriteLine("Making Status");
            Console.WriteLine("Name : " + name);
            Console.WriteLine("Address : " + adress);
            Console.WriteLine("BrokerURL : " + brokerUrl);
            Console.WriteLine("Eventos publicados " + this.seqNumber);
        }

        public void freeze()
        {
            isFrozen = true;
        }

        public void unfreeze()
        {
            isFrozen = false;
            //checkFrozenEvents();
        }

        //public void checkFrozenEvents()
        //{
        //    foreach (FrozenEvent fe in frozenEvents)
        //    {
        //        this.broker.receivePub(this.name,fe.getEvent());                  
        //    }
        //    frozenEvents = new List<FrozenEvent>();
        //}

    }
}

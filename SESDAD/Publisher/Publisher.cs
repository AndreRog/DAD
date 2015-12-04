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

            Publisher publisher = new Publisher(args[0],args[2]);
            int i = 3;
            while(i < args.Length )
            { 
                publisher.addBroker(args[i]);
                i++;
            }
            RemotingServices.Marshal(publisher, "pub", typeof(Publisher));



            broker.addPublisher(args[0],args[2]);

            Console.ReadLine();
        }
    }

    public class Publisher : MarshalByRefObject, IPublisher
    {
        private string name;

        private string adress;

        private Dictionary<string, bool> brokerUrl;

        private List<KeyValuePair<string, Event>> events;

        private Dictionary<string, int> pubSeq;

        private int seqNumber ;

        private bool isFrozen = false;

        private List<FrozenEvent> frozenEvents;

        private string leaderURL;

      //  private Dictionary<string, Event> events; 

        public Publisher(string name, string url)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = new Dictionary<string, bool>();
            this.leaderURL = "null";
            this.events = new List<KeyValuePair<string, Event>>();
            this.pubSeq = new Dictionary<string,int>();
            this.frozenEvents = new List<FrozenEvent>();
            this.seqNumber = 0;
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
                foreach (string brokerS in this.brokerUrl.Keys)
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

            if (!this.pubSeq.ContainsKey(topic))
            {
                this.pubSeq.Add(topic, 0);
            }
            for (i = 0; i < times; i++)
            {
                if (isFrozen)
                {
                    i--;
                }
                else
                {
                    eventNumber = SeqNumber();
                    e = new Event(topic, "", this.name, eventNumber);
                    Console.WriteLine("Creating Event : " + topic + "EventNumber:" + e.getNumber());
                    IBroker broker = (IBroker)Activator.GetObject(
                       typeof(IBroker),
                       this.leaderURL);
                    broker.receivePub(this.name, e);
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
        }

    }
}

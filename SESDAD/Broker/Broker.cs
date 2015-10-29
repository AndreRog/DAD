using System;
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

namespace Broker
{
    class BrokerAplication
    {
        static void Main(string[] args)
        {

            char[] delimiter = { ':', '/' };
            string[] arg = args[2].Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Broker Application " + arg[2]);
  

            TcpChannel brokerChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(brokerChannel, false);

            Broker broker = new Broker(args[3]);
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
        private List<KeyValuePair<string, Event>> events;
        private Dictionary<string,string> childs;
        private Dictionary<string, string> pubs;
        private Dictionary<string, string> subs;
        private Dictionary<string, string> topicSubs;
        public Broker(String parent) {
            this.parentURL = parent;
            this.childs = new Dictionary<string, string>(2);
            this.pubs = new Dictionary<string, string>();
            this.subs = new Dictionary<string, string>();
            this.topicSubs = new Dictionary<string, string>();
            this.events = new List<KeyValuePair<string, Event>>();
        }

        public void addChild(string name, string URL)
        {
            this.childs.Add(name, URL);
            Console.WriteLine("Child Added:" + name);
        }

        public void addPublisher(string name, string URL)
        {
            this.pubs.Add(name, URL);
            Console.WriteLine("Pub Added:" + name);
        }

        public void addSubscriber(string name, string URL)
        {
            this.subs.Add(name, URL);
            Console.WriteLine("Subscriber Added:" + name);
        }

        public string receivePub(string name, Event e)
        {
            Console.WriteLine("Received Publish");
            events.Add( new KeyValuePair<string,Event>(name, e));
            return "ACK";
        }

        public string subscribe(string topic, string URL)
        {
            Console.WriteLine("Received Subscribe");
            this.topicSubs.Add(topic, URL);
            return "ACK";
        }

        public string unsubscribe(string topic, string URL)
        {
            // pode eliminar o errado caso existam 2 ocorrencias , FIX ME
            Console.WriteLine("Received Unsubscribe");
            this.topicSubs.Remove(topic);
            return "ACK";
        }

        public void crash()
        {
            Environment.Exit(-1);
        }

        public void flood()
        {
            int i,j;

            string url;
            string topicName;
            Event e;

            for(i=0; i < events.Count(); i++){
                topicName = events[i].Key;
                e = events[i].Value;
                for (j = 0; j < topicSubs.Count(); j++)
                {

                    if (topicSubs.ContainsKey(topicName))
                    {
                        topicSubs.TryGetValue(topicName,out url);
                        ISubscriber sub = (ISubscriber)Activator.GetObject(
                            typeof(ISubscriber),
                            url);
                        sub.receiveEvent(topicName, e);
                    }
                }
            }
        }
    }
}

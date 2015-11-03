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

            Broker broker = new Broker(args[0],args[3], args[2]);
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
        private Dictionary<string,string> childs;
        private Dictionary<string, string> pubs;
        private Dictionary<string, string> subs;
        private Dictionary<string, string> topicSubs;
        private string typeFlood;
        private string myUrl;
      
        public Broker(string name, string parent,string myUrl) {
            this.parentURL = parent;
            this.name = name;
            this.childs = new Dictionary<string, string>(2);
            this.pubs = new Dictionary<string, string>();
            this.subs = new Dictionary<string, string>();
            this.topicSubs = new Dictionary<string, string>();
            this.events = new List<KeyValuePair<string, Event>>();
            this.typeFlood = "NO";
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
            propagate(e);
            sendToSubscriber(e);
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
            if (this.topicSubs[topic].Equals(URL))
            {
                this.topicSubs.Remove(topic);
            }
            return "ACK";
        }

        public void crash()
        {
            Environment.Exit(-1);
        }

        public void sendAll(Event e)
        {
            foreach (string s in subs.Values)
            {
                ISubscriber sub = (ISubscriber)Activator.GetObject(
                            typeof(ISubscriber),
                            s);
                Console.WriteLine(e.getTopic());
                sub.receiveEvent(e.getTopic(), e);
            }
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
            if (this.typeFlood.Equals("NO")) {
                Thread thread = new Thread(() => this.floodNoOrder(e));
                thread.Start();
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

                    parent.receivePub(this.name, e);

                }
                if (!(childs.Count == 0))
                {

                    foreach (string childurl in childs.Values)
                    {
                        IBroker child = (IBroker)Activator.GetObject(
                            typeof(IBroker),
                            childurl);

                        child.receivePub(this.name, e);
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

                    parent.receivePub(this.name, e);
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

                          child.receivePub(this.name, e);
                        }
                    }
                }
            }
        }

        public void sendToSubscriber(Event e)
        {

            foreach (KeyValuePair<string,string> kvp in topicSubs)
            {
                if (itsForSend(kvp , e.getTopic()))
                {

                    ISubscriber sub = (ISubscriber)Activator.GetObject(
                    typeof(ISubscriber),
                    kvp.Value);
                    Console.WriteLine("Sending to : " + kvp.Key);

                    sub.receiveEvent(e.getTopic(), e);
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
            int i = 1;
            bool isForSent = false;

            while (niveis >= i)
	        {
               if (pathSub[i].Equals(path[i]))
                {
                    i++;
                }
               if (pathSub[i].Equals(asterisco))
                {
                    isForSent = true;
                    break;
                }         
	        }
            return isForSent;
        }

        public void printevents()
        {

            //foreach();
        }
    }
}

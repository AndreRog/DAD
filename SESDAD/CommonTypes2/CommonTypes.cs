using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CommonTypes
{

    public interface IBroker 
    {
        
        void addChild(string name,string url);
        void addPublisher(string name, string url);
        void addSubscriber(string name, string url);
        string receivePub(string name, Event e);
        string subscribe(string topic, string URL);
        string unsubscribe(string topic, string URL);
        void crash();
        void status();
        void unfreeze();
        void freeze();
        void receiveInterest(string topic, string name);
        void removeInterest(string topic, string myUrl);
        void sendToSubscriber(Event e);
        void adjustEvents(string topic, int maxPerTopic, string pubName);
        int returnSeqTopic(string topic,string name,string pubName);
        int getSeqNumber();
        void sendToRoot(Event e);

        void receiveFIFOChannel(Event e, int nextEvent);
    }

    public interface ISubscriber
    {
        string getName();
        void subEvent(string topic);
        void displayEvents();
        void receiveEvent(string topic, Event e);
        void UnsubEvent(string topicName);
        void crash();
        void status();
        void unfreeze();
        void freeze();
    }

    public interface IPublisher
    {
        void pubEvent(string numberEvents, string topicName, string interva);
        void crash();
        void status();
       // int SeqNumber();
        void unfreeze();
        void freeze();
    }

    public interface IPuppetMaster
    {
        void addBroker(string name, string site, string url, string urlbroker);
        void addSubscriber(string name, string site, string url);
        void addSubscriberRemote(string name, string site, string url, string urlBroker);
        void addPublisher(string name, string site, string url);
        void addPublisherRemote(string name, string site, string url,string urlBroker);
        void toLog(string msg);
        void subscribe(string processName, string topicName);
        void publish(string processName, string numberEvents, string topicName, string interval);
        void unsubscribe(string processName, string topicName);
        void crash(string processName);
    }

    [Serializable]
    public class Event
    {
        private string topic;

        private string content;
        private string sender;
        private int number;
        private int totalNumber;
        private int adjustment;
        private string lastHop;
        
       // private int seq;

        public Event(string topic, string content,string sender, int number)
        {
            this.topic = topic;
            this.content = content;
            this.sender = sender;
            this.number = number;
            this.lastHop = "null";
            this.totalNumber = 0;
            this.adjustment = 0;
        }

        public int getAdjustment()
        {
            return this.adjustment;
        }

        public void setAdjustment(int i)
        {
            lock (this) { 
            this.adjustment = i;

            }
        }


        public string getTopic()
        {
            return topic;
        }

        public string getContent()
        {
            return content;
        }

        public string getLastHop() {
           return this.lastHop;
        }

        public void setLastHop(string s){
            this.lastHop = s;
        }

        public int getTotalSeq()
        {
            return this.totalNumber;
        }

        public void setSeqNumber(int i)
        {
            this.totalNumber = i;
        }

        public string getSender()
        {
            return sender;
        }

        public int getNumber()
        {
            return number;
        }
    }


    [Serializable]
    public class PubInfo
    {
        private string name;

        private string url;

        private Dictionary<string, int> topicSeqNumber;

        //private string topic;

        //private int seqNumber;

        public PubInfo(string name) {
            this.name = name;
            this.topicSeqNumber = new Dictionary<string, int>();
            //this.topic = topic;
            //this.seqNumber = seq;
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void printMe()
        {
            foreach(KeyValuePair<string, int> oi in topicSeqNumber)
            {
                Console.WriteLine("KEY: --->" + oi.Key + "VALUE: ---->" + oi.Value );
            }
        }

        public void addTopic(string topic)
        {
            lock(this)
            {
                if (topicSeqNumber.Count > 0) 
                { 
                     if (!topicSeqNumber.ContainsKey(topic)) { 
                        this.topicSeqNumber.Add(topic, 0);
                     }
                }
                else
                {
                    this.topicSeqNumber.Add(topic, 0);
                }
            }
        }

        public Dictionary<string, int> getValues(){
            return this.topicSeqNumber;
        } 

        public bool hasTopic(string topic)
        {
            printMe();
            string PathSub = topic;
            char[] delimiter = { '/' };
            string asterisco = "*";
            string[] pathSub = PathSub.Split(delimiter);

            int niveis = pathSub.Count();
            int i = 0;
            bool isForSent = false;
            if (this.topicSeqNumber.ContainsKey(topic))
            {
                return true;
            }
            foreach(string auxTopic in this.topicSeqNumber.Keys)
            {
                i = 0;
                string[] path = auxTopic.Split(delimiter);
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
                            //PODE DAR ERROS MAIS TARDe
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
            }
            return isForSent;
        }
        

        public int getSeqNumber(string topic) 
        {
            if (this.topicSeqNumber.ContainsKey(topic))
                return this.topicSeqNumber[topic];
            return 0;
        }

        public void setSeqNumber(string topic, int max) 
        {
            if (this.topicSeqNumber.ContainsKey(topic))
            {
                this.topicSeqNumber[topic] = max;
            }
        }

        public void addSeqNumber(string topic)
        {
            this.topicSeqNumber[topic] += 1;
        }

        public string getName()
        {
            return this.name;
        }

        public bool giveNumber(string topic, string topicAux)
        {
            string PathSub = topic;
            string pathEvento = topicAux;

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
    }

    public class FrozenEvent
    {
        private string eventType;
        private string name;
        private string url;
        private Event e;
        private string numberEvents;
        private string interval;

        public FrozenEvent(string eventType, string name, string url)
        {
            this.eventType = eventType;
            this.name = name;
            this.url = url;
        }

        public FrozenEvent(string eventType,Event e)
        {
            this.eventType = eventType;
            this.e = e;
        }

        public string getNumberEvents()
        {
            return numberEvents;
        }

        public string getInterval()
        {
            return interval;
        }

        public string getEventType()
        {
            return eventType;
        }

        public string getName()
        {
            return name;
        }

        public string getURL()
        {
            return url;
        }

        public Event getEvent()
        {
            return e;
        }

        public bool hasEvent(){
            if(this.e.Equals(null)){
                return false;
            }else return true;
        }
    }


    public class Queue
    {
        private int lastSeq;

        private List<Event> waitQ;

        public Queue (){
            this.waitQ = new List<Event>();
            this.lastSeq = -1;
        }
    }
}

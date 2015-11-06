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
        void sendToSubscriber(Event e);
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
        int SeqNumber();
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

        private string lastHop;
        
       // private int seq;

        public Event(string topic, string content,string sender, int number)
        {
            this.topic = topic;
            this.content = content;
            this.sender = sender;
            this.number = number;
            this.lastHop = "null";
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

        public string getSender()
        {
            return sender;
        }

        public int getNumber()
        {
            return number;
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

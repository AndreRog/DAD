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
        void addSubscriber(string name, string site, string url, string urlbroker);
        void addPublisher(string name, string site, string url, string urlbroker);
        void toLog(string msg);
        void subscribe(string processName, string topicName);
        void publish(string processName, string numberEvents, string topicName, string interval);
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



















    public class Node<T>
    {
        // Private member-variables
        private T data;
        private String site;
        private NodeList<T> neighbors = null;
        private Node<T> parent;
        public Node() { }
        public Node(String siteName)
        {
            this.site = siteName;
        }
        public Node(T data, Node<T> parent, String site) : this(data, null, parent, site) { }
        public Node(T data, NodeList<T> neighbors, Node<T> parent, String site)
        {
            this.data = data;
            this.neighbors = neighbors;
            this.parent = parent;
            this.site = site;
        }

        public T Value
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }


        public String Site
        {
            get
            {
                return site;
            }
            set
            {
                site = value;
            }
        }

        public Node<T> Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        public NodeList<T> Neighbors
        {
            get
            {
                return neighbors;
            }
            set
            {
                neighbors = value;
            }
        }
    }



    public class NodeList<T> : Collection<Node<T>>
    {
        public NodeList() : base() { }

        public NodeList(int initialSize)
        {
            // Add the specified number of items
            for (int i = 0; i < initialSize; i++)
                base.Items.Add(default(Node<T>));
        }

        public Node<T> FindByValue(String site)
        {
            // search the list for the value
            foreach (Node<T> node in Items)
                if (node.Site.Equals(site))
                    return node;

            // if we reached here, we didn't find a matching node
            return null;
        }
    }



    public class BinaryTreeNode<T> : Node<T>
    {
        public BinaryTreeNode(String site) : base(site) { }
        public BinaryTreeNode(T data, Node<T> parent, String site) : base(data, null, parent, site) { }
        public BinaryTreeNode(T data, BinaryTreeNode<T> left, BinaryTreeNode<T> right)
        {
            base.Value = data;
            NodeList<T> children = new NodeList<T>(2);
            children[0] = left;
            children[1] = right;

            base.Neighbors = children;
        }

        public BinaryTreeNode<T> Left
        {
            get
            {
                if (base.Neighbors == null)
                    return null;
                else
                    return (BinaryTreeNode<T>)base.Neighbors[0];
            }
            set
            {
                if (base.Neighbors == null)
                    base.Neighbors = new NodeList<T>(2);

                base.Neighbors[0] = value;
                Console.WriteLine(value.Site);
            }
        }

        public BinaryTreeNode<T> Right
        {
            get
            {
                if (base.Neighbors == null)
                    return null;
                else
                    return (BinaryTreeNode<T>)base.Neighbors[1];
            }
            set
            {
                if (base.Neighbors == null)
                    base.Neighbors = new NodeList<T>(2);

                base.Neighbors[1] = value;
                Console.WriteLine(value.Site);
            }
        }

        public void Add(String siteName,String parentName)
        {
            BinaryTreeNode<T> node = new BinaryTreeNode<T>(siteName);
            BinaryTreeNode<T> parentNode = searchByName(parentName, this);
            if (parentNode.Left == null)
                parentNode.Left = node;
            else
            {
                if (parentNode.Right == null)
                    parentNode.Right = node;
            }

            node.Parent = parentNode;
        }

        public BinaryTreeNode<T> searchByName(String siteName,BinaryTreeNode<T> current)
        {
            BinaryTreeNode<T> result = null;
            if (current == null)
                return result;
            if (current.Site.Equals(siteName))
            {
                result = current;
                return result;
            }
            else
            {
                if (current.Neighbors != null)
                {
                    if (current.Neighbors[0] != null)
                    {
                        result = searchByName(siteName, current.Left);
                    }
                    if (current.Neighbors[1] != null && result == null)
                    {
                        result = searchByName(siteName, current.Right);
                    }
                }
            }
            return result;
        }
    }

    public class BinaryTree<T>
    {
        private BinaryTreeNode<T> root;

        public BinaryTree()
        {
            root = null;
        }

        public virtual void Clear()
        {
            root = null;
        }

        public BinaryTreeNode<T> Root
        {
            get
            {
                return root;
            }
            set
            {
                root = value;
            }
        }

    }

}

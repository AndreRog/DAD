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
        string receivePub(string name,Event e);
        string subscribe(string topic, string URL);
        string unsubscribe(string topic, string URL);
        void crash();
    }

    public interface ISubscriber
    {
        void subEvent(string topic);
        void displayEvents();
        void receiveEvent(string topic, Event e);
        void UnsubEvent(string topicName);
        void crash();
    }

    public interface IPublisher
    {
        void pubEvent(string numberEvents, string topicName, string interva);
        void crash();
    }

    public interface IPuppetMaster
    {
        void addBroker(string name, string site, string url, string urlbroker);
    }

    [Serializable]
    public class Event
    {
        private string topic;

        private string content;

        public Event(string topic, string content)
        {
            this.topic = topic;
            this.content = content;
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

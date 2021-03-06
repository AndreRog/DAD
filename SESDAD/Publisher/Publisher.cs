﻿using System;
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

        private static int seqNumber = 0;

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
            this.frozenEvents = new List<FrozenEvent>();
        }

        public int SeqNumber()
        {
            return Interlocked.Increment(ref seqNumber);

        }

        public void pubEvent(string numberEvents, string topic, string interval)
        {
            //thread bad shit see it
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
            for (i = 0; i < times; i++)
            {
                eventNumber = this.SeqNumber();
                e = new Event(topic, "",this.name, eventNumber);

                if (isFrozen)
                {
                    FrozenEvent fe = new FrozenEvent("EVENT", e);
                    frozenEvents.Add(fe);
                    Thread.Sleep(sleep);
                }
                else
                {

                    this.broker.receivePub(this.name, e);
                    events.Add(new KeyValuePair<string, Event>(name, e));
                    Console.WriteLine("Creating Event : " + topic + e.getNumber());
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
            int i = 0;
            Console.WriteLine("Making Status");
            Console.WriteLine("Name : " + name);
            Console.WriteLine("Address : " + adress);
            Console.WriteLine("BrokerURL : " + brokerUrl);
            Console.WriteLine("Eventos publicados");
            foreach (KeyValuePair<string, Event> e in events)
            {
                i++;
                Console.WriteLine("Evento nº " + i + "Topic : " + e.Key + " Content : " + e.Value.getContent());
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
                this.broker.receivePub(this.name,fe.getEvent());                  
            }
            frozenEvents = new List<FrozenEvent>();
        }

    }
}

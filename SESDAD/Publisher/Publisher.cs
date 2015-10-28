using System;
using CommonTypes;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


            TcpChannel subChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(subChannel, false);

            Publisher publisher = new Publisher(args[0],args[2], args[3]);
            RemotingServices.Marshal(publisher, "publisher", typeof(Publisher));

            //Add Pub to broker.
            IBroker broker = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                    args[3]);

            broker.addPublisher(args[0],args[2]);

            Console.ReadLine();
        }
    }

    public class Publisher : MarshalByRefObject, ISubscriber
    {
        private string name;

        private string adress;

        private string brokerUrl;

        public Publisher(string name, string url, string brokerUrl)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = brokerUrl;
        }

        public void pubEvent(string numberEvents, string topic, string interval)
        {

        }

    }
}

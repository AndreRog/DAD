using System;
using CommonTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Subscriber
{
    class SubscriberApplication
    {
        static void Main(string[] args)
        {

            char[] delimiter = { ':', '/' };
            string[] arg = args[2].Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Subscriber Application " + arg[2]);


            TcpChannel subChannel = new TcpChannel(Int32.Parse(arg[2]));
            ChannelServices.RegisterChannel(subChannel, false);

            Subscriber subscriber = new Subscriber(args[0],args[2], args[3]);
            RemotingServices.Marshal(subscriber, "subscriber", typeof(Subscriber));


           //Add Sub to Broker.
            IBroker broker = (IBroker)Activator.GetObject(
                        typeof(IBroker),
                    args[3]);

            broker.addSubscriber(args[0], args[2]);
            Console.ReadLine();

        }
    }

    public class Subscriber : MarshalByRefObject, ISubscriber
    {
        private string name;

        private string adress;

        private string brokerUrl;

        public Subscriber(string name, string url, string brokerUrl)
        {
            this.name = name;
            this.adress = url;
            this.brokerUrl = brokerUrl;
        }


    }
}

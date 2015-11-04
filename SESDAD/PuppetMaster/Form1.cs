using System;
using CommonTypes;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace PuppetMaster
{
    public partial class Form1 : Form
    {
        private PuppetMaster puppet;
        public delegate void UpdateListMessage(string msg);
        public UpdateListMessage myDelegate;
        public bool singleMachine;
        public bool master;

        public Form1(string args)
        {
            InitializeComponent();
            singleMachine = false;
            myDelegate = new UpdateListMessage(add_Message_List);

            
            if (args.Equals("-singleMachine"))
            {
                singleMachine = true;
                puppet = new PuppetMaster(this, 0, true);
            }
            else
            {
                int iD = Int32.Parse(args);
                if (iD == 0)
                {
                    master = true;
                }
                else
                {
                    master = false;
                }
                puppet = new PuppetMaster(this, iD, false);
            }

            scriptbox.Enabled = false;
            textBox2.Enabled = false;
            PMConfig.Enabled = false;
            execute.Enabled = false;
            button2.Enabled = false;
            InitCheck();
        }

        private void InitCheck()
        {
            if (master || singleMachine)
            {
                scriptbox.Enabled = true;
                textBox2.Enabled = true;
                execute.Enabled = true;
                button2.Enabled = true;
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            string comand = textBox2.Text;
            command(comand);
        }

        private void add_Message_List(String msg)
        {
            MsgViewBox.Items.Add(msg);
        }

        private void add_Message_Sub(string processnameS, string processnameP, string topicName, string eventNumber)
        {
            MsgViewBox.Items.Add("SubEvent "+processnameS+" , "+ processnameP+" , "+topicName+" , "+eventNumber);
        }

        private void execute_Click(object sender, EventArgs e)
        {
            string scriptPath = scriptbox.Text;
            StreamReader script = new StreamReader(scriptPath);
            String Line;

            while ((Line = script.ReadLine()) != null)
            {
                command(Line);
            }

        }


        public void command(String commands)
        {
            if (commands.Equals(""))
            {
                return;
            }

            if (!(commands.StartsWith("Site")))
            {
                add_Message_List(commands);
            }
            
            String[] command = commands.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            
            switch (command[0])
            {


                case "Process":
                    if (command[3].Equals("BROKER"))
                    {
                        puppet.addBroker(command[1], command[5], command[7], command[8]);
                    }
                    else if (command[3].Equals("SUBSCRIBER"))
                    {
                        puppet.addSubscriber(command[1], command[5], command[7], command[8]);
                    }
                    else if (command[3].Equals("PUBLISHER"))
                    {
                        puppet.addPublisher(command[1], command[5], command[7], command[8]);
                    }
                    break;
                case "Subscriber":
                    if (command[2].Equals("Subscribe"))
                    {
                        puppet.subscribe(command[1], command[3]);
                    }
                    else if (command[2].Equals("Unsubscribe"))
                    {
                        puppet.unsubscribe(command[1], command[3]);
                    }
                    break;
                case "Publisher":
                    puppet.publish(command[1], command[3], command[5], command[7]);
                    break;
                case "Status":
                    puppet.status();
                    break;
                case "Crash":
                    puppet.crash(command[1]);
                    break;
                case "Freeze":
                    puppet.freeze(command[1]);
                    break;
                case "Unfreeze":
                    puppet.unfreeze(command[1]);
                    break;
                case "Wait":
                    System.Threading.Thread.Sleep(Int32.Parse(command[1]));
                    break;
            }

        }

        private void MsgViewBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Site_Click(object sender, EventArgs e)
        {

        }


    }

    public class PuppetMaster : MarshalByRefObject, IPuppetMaster
    {

        private bool master;
        private bool single;
        private String address;
        private int site;
        private Dictionary<int, string> puppets;
        private Dictionary<string, string> brokers;
        private Dictionary<string, string> pubWithUrl;
        private Dictionary<string, int> pubWithSite;
        private Dictionary<string, string> subsWithUrl;
        private Dictionary<string, int> subsWithSite;
        private Form1 formP;
        public PuppetMaster(Form1 form1, int iD, bool single)
        {
            this.formP = form1;
            this.site = iD;
            this.single = single;
            brokers = new Dictionary<string, string>();
            pubWithUrl = new Dictionary<string, string>();
            pubWithSite = new Dictionary<string, int>();
            subsWithUrl = new Dictionary<string, string>();
            subsWithSite = new Dictionary<string, int>();
            puppets = new Dictionary<int, string>();
            init();
        }

        public void init()
        {
            string cfgpath = @"..\..\..\cfg.txt";
            StreamReader script = new StreamReader(cfgpath);
            String Line;
            int i = 0;
            while ((Line = script.ReadLine()) != null)
            {
                if (i == site)
                {
                    if (i == 0)
                    {
                        master = true;

                    }
                    else
                    {
                        master = false;
                    }
                    this.address = Line;
                    char[] delimiter = { ':', '/' };
                    string[] arg = Line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                    TcpChannel channel = new TcpChannel(Int32.Parse(arg[2]));
                    ChannelServices.RegisterChannel(channel, false);
                    RemotingServices.Marshal(this, "PM", typeof(PuppetMaster));
                    i++;
                }
                else
                {
                    puppets.Add(i, Line);
                    i++;
                }

            }
        }

        public int convertStoI(string s)
        {
            char[] j = new char[1];
            j[0] = s[s.Length - 1];
            string a = new String(j);

            int i = Int32.Parse(a);
            return i;
        }

        public void addBroker(string name, string site, string URL, string URLParent)
        {
            int siteB = convertStoI(site);
            if (this.single || this.site == siteB)
            {

                String arguments = name + " " + site + " " + URL + " " + URLParent;
                String filename = @"..\..\..\Broker\bin\Debug\Broker.exe";
                Process.Start(filename, arguments);
            }
            else
            {
                //Remote PuppetSlave starts the process.
                IPuppetMaster puppetM = (IPuppetMaster)Activator.GetObject(
                    typeof(IPuppetMaster),
                    puppets[siteB]);
                puppetM.addBroker(name, site, URL, URLParent);
            }
            this.brokers.Add(name, URL);
        }

        public void addSubscriber(string name, string site, string url, string urlbroker)
        {


            int siteB = convertStoI(site);
            if (this.single || this.site == siteB)
            {
                String arguments = name + " " + site + " " + url + " " + urlbroker;
                String filename = @"..\..\..\Subscriber\bin\Debug\Subscriber.exe";
                Process.Start(filename, arguments);
            }
            else
            {
                //Remote Puppet Code need to try it yet.
                IPuppetMaster puppetM = (IPuppetMaster)Activator.GetObject(
                    typeof(IPuppetMaster),
                    puppets[siteB]);
                puppetM.addSubscriber(name, site, url, urlbroker);
            }
            this.subsWithUrl.Add(name, url);
            this.subsWithSite.Add(name, siteB);
        }

        public void addPublisher(string name, string site, string url, string urlbroker)
        {

            int siteB = convertStoI(site);
            if (this.single || this.site == siteB)
            {
                String arguments = name + " " + site + " " + url + " " + urlbroker;
                String filename = @"..\..\..\Publisher\bin\Debug\Publisher.exe";
                Process.Start(filename, arguments);
            }
            else
            {
                //Remote Puppet Code need to try it yet.
            }
            this.pubWithUrl.Add(name, url);
            this.pubWithSite.Add(name, siteB);
        }


        public void subscribe(string processName, string topicName)
        {

            //string siteP = this.subsWithSite[processName];


            if (this.single || this.site == this.subsWithSite[processName])
            {
                Console.WriteLine(subsWithUrl[processName]);

                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                      typeof(ISubscriber),
                             this.subsWithUrl[processName]);

                subscriber.subEvent(topicName);
            }
        }

        public void unsubscribe(string processName, string topicName)
        {
            if (this.single || this.site == (this.subsWithSite[processName]))
            {
                Console.WriteLine(subsWithUrl[processName]);

                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                      typeof(ISubscriber),
                             this.subsWithUrl[processName]);

                subscriber.UnsubEvent(topicName);
            }
        }

        public void publish(string processName, string numberEvents, string topicName, string interval)
        {



            if (this.single || this.site == (this.pubWithSite[processName]))
            {
                Console.WriteLine(pubWithUrl[processName]);

                IPublisher publisher = (IPublisher)Activator.GetObject(
                      typeof(IPublisher),
                             this.pubWithUrl[processName]);

                publisher.pubEvent(numberEvents, topicName, interval);
            }
            else
            {
                //IPuppetMaster puppetM = PuppetMaster

            }


        }

        public void crash(string processName)
        {
            if (this.site.Equals(this.pubWithSite[processName]))
            {
                Console.WriteLine(pubWithUrl[processName]);

                formP.BeginInvoke(formP.myDelegate, pubWithUrl[processName]);
                IPublisher publisher = (IPublisher)Activator.GetObject(
                      typeof(IPublisher),
                             this.pubWithUrl[processName]);

                publisher.crash();

            }

            if (this.site.Equals(this.subsWithSite[processName]))
            {
                Console.WriteLine(subsWithUrl[processName]);

                formP.BeginInvoke(formP.myDelegate, subsWithUrl[processName]);
                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                      typeof(ISubscriber),
                             this.subsWithUrl[processName]);

                subscriber.crash();

            }
            if (this.brokers.Equals(this.brokers[processName]))
            {
                Console.WriteLine(brokers[processName]);

                formP.BeginInvoke(formP.myDelegate, brokers[processName]);
                IBroker broker = (IBroker)Activator.GetObject(
                      typeof(IBroker),
                             this.brokers[processName]);

                broker.crash();
            }
        }

        public void status()
        {
            Console.WriteLine("Making Status");
            // int i;
            foreach (string s in brokers.Values)
            {
                Console.WriteLine("Broker : " + s);
                formP.BeginInvoke(formP.myDelegate, s);
                IBroker broker = (IBroker)Activator.GetObject(
                                    typeof(IBroker),
                             s);
                broker.status();
            }
            foreach (string s in pubWithUrl.Values)
            {
                Console.WriteLine("Publisher : " + s);
                formP.BeginInvoke(formP.myDelegate, s);
                IPublisher publisher = (IPublisher)Activator.GetObject(
                      typeof(IPublisher),
                             s);
                publisher.status();
            }
            foreach (string s in subsWithUrl.Values)
            {
                Console.WriteLine("Subscriber : " + s);
                formP.BeginInvoke(formP.myDelegate, s);
                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                      typeof(ISubscriber),
                             s);
                subscriber.status();
            }
        }

        public void freeze(string processName)
        {
            Console.WriteLine("Freezing " + processName);
            throw new NotImplementedException();
        }

        public void unfreeze(string processName)
        {
            Console.WriteLine("Unfreezing " + processName);
            throw new NotImplementedException();
        }

        public void toLog(string msg)
        {
            formP.BeginInvoke(formP.myDelegate, msg);
        }
    }
}



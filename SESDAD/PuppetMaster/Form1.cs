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
        public Dictionary<string, string> sitesT;
        public UpdateListMessage myDelegate;
        public bool singleMachine;
        public bool master;

        public Form1(string args)
        {
            InitializeComponent();
            singleMachine = false;
            myDelegate = new UpdateListMessage(add_Message_List);
            sitesT = new Dictionary<string, string>();

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
            MsgViewBox.Items.Add("SubEvent " + processnameS + " , " + processnameP + " , " + topicName + " , " + eventNumber);
        }

        private void execute_Click(object sender, EventArgs e)
        {
            //string scriptPath = scriptbox.Text;
            string scriptPath = @"..\..\..\script.txt";
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

                case "Site":
                    sitesT.Add(command[1], command[3]);
                    puppet.addSite(command[1], command[3]);
                    break;
                case "Process":
                    if (command[3].Equals("BROKER"))
                    {
                        add_Message_List("");
                        if (sitesT[command[5]].Equals("none"))
                        {
                            puppet.addBroker(command[1], command[5], command[7], "null");
                        }
                        else
                        {
                            puppet.addBroker(command[1], command[5], command[7], puppet.getParent(sitesT[command[5]]));
                        }
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
                    puppet.sleep(command[1]);
                    break;
                case "RoutingPolicy":
                    puppet.changePolicy(command[1]);
                    break;
                case "Ordering":
                    puppet.changeOrdering(command[1]);
                    break;
                case "LoggingLevel":
                    puppet.changeLogLvl(command[1]);
                    break;
            }

        }
    }

    public class PuppetMaster : MarshalByRefObject, IPuppetMaster
    {

        private bool master;
        private bool single;
        private String address;
        private int site;
        private string policy;
        private string order;
        private string logLvl;
        private Dictionary<string, string> sites;
        private Dictionary<int, string> puppets;
        private Dictionary<string, string> brokers;
        private Dictionary<int, string> brokersSite;
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
            brokersSite = new Dictionary<int, string>();
            pubWithUrl = new Dictionary<string, string>();
            pubWithSite = new Dictionary<string, int>();
            subsWithUrl = new Dictionary<string, string>();
            subsWithSite = new Dictionary<string, int>();
            puppets = new Dictionary<int, string>();
            sites = new Dictionary<string, string>();
            policy = "flooding";
            order = "FIFO";
            logLvl = "light";
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

        public void addSite(string site, string parent)
        {
            sites.Add(site, parent);
        }

        public void addBroker(string name, string site, string URL, string URLParent)
        {
            int siteB = convertStoI(site);
            if (this.single || this.site == siteB)
            {

                String arguments = name + " " + site + " " + URL + " " + URLParent + " " + policy + " " + order + " " + logLvl;
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
            this.brokersSite.Add(siteB, URL);
        }

        public string getParent(string siteO)
        {
            int i = convertStoI(siteO);
            return this.brokersSite[i];
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
                //Remote Puppet Code.
                IPuppetMaster puppetM = (IPuppetMaster)Activator.GetObject(
                    typeof(IPuppetMaster),
                    puppets[siteB]);
                puppetM.addPublisher(name, site, url, urlbroker);
            }
            this.pubWithUrl.Add(name, url);
            this.pubWithSite.Add(name, siteB);
        }


        public void subscribe(string processName, string topicName)
        {

            if (this.single || this.site == this.subsWithSite[processName])
            {
                Console.WriteLine(subsWithUrl[processName]);

                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                      typeof(ISubscriber),
                             this.subsWithUrl[processName]);

                subscriber.subEvent(topicName);
            }
            else
            {
                IPuppetMaster puppetM = (IPuppetMaster)Activator.GetObject(
                     typeof(IPuppetMaster),
                     puppets[this.subsWithSite[processName]]);
                puppetM.subscribe(processName, topicName);
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
            else
            {
                IPuppetMaster puppetM = (IPuppetMaster)Activator.GetObject(
                     typeof(IPuppetMaster),
                     puppets[this.subsWithSite[processName]]);
                puppetM.unsubscribe(processName, topicName);
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
                IPuppetMaster puppetM = (IPuppetMaster)Activator.GetObject(
                     typeof(IPuppetMaster),
                     puppets[this.pubWithSite[processName]]);
                puppetM.publish(processName, numberEvents, topicName, interval);

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

            string url = "";
            if (brokers.TryGetValue(processName, out url))
            {
                formP.BeginInvoke(formP.myDelegate, brokers[processName]);
                IBroker broker = (IBroker)Activator.GetObject(
                                    typeof(IBroker),
                             brokers[processName]);
                broker.freeze();
            }
            else if (pubWithUrl.TryGetValue(processName, out url))
            {
                formP.BeginInvoke(formP.myDelegate, pubWithUrl[processName]);
                IPublisher publisher = (IPublisher)Activator.GetObject(
                                    typeof(IPublisher),
                             pubWithUrl[processName]);
                publisher.freeze();
            }
            else
            {
                formP.BeginInvoke(formP.myDelegate, subsWithUrl[processName]);
                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                                    typeof(ISubscriber),
                             subsWithUrl[processName]);
                subscriber.freeze();
            }
        }

        public void unfreeze(string processName)
        {
            string url = "";
            if (brokers.TryGetValue(processName, out url))
            {
                formP.BeginInvoke(formP.myDelegate, brokers[processName]);
                IBroker broker = (IBroker)Activator.GetObject(
                                    typeof(IBroker),
                             brokers[processName]);
                broker.freeze();
            }
            else if (pubWithUrl.TryGetValue(processName, out url))
            {
                formP.BeginInvoke(formP.myDelegate, pubWithUrl[processName]);
                IPublisher publisher = (IPublisher)Activator.GetObject(
                                    typeof(IPublisher),
                             pubWithUrl[processName]);
                publisher.freeze();
            }
            else if (subsWithUrl.TryGetValue(processName, out url))
            {
                formP.BeginInvoke(formP.myDelegate, subsWithUrl[processName]);
                ISubscriber subscriber = (ISubscriber)Activator.GetObject(
                                    typeof(ISubscriber),
                             subsWithUrl[processName]);
                subscriber.freeze();
            }
        }

        public void sleep(string s)
        {
            int sleepTime = Int32.Parse(s);
            System.Threading.Thread.Sleep(sleepTime);
        }

        public void toLog(string msg)
        {
            formP.BeginInvoke(formP.myDelegate, msg);
        }

        public void changePolicy(string p)
        {
            policy = p;
        }

        internal void changeOrdering(string p)
        {
            order = p;
        }

        internal void changeLogLvl(string p)
        {
            logLvl = p;
        }
    }
}



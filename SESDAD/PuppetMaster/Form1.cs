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
        
        public Form1()
        {
            InitializeComponent();
            myDelegate = new UpdateListMessage(add_Message_List);
            puppet = new PuppetMaster(this);

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void add_Message_List(String msg)
        {
            MsgViewBox.Items.Add("[" + msg + "]: ");
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
 
            String[] command = commands.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            
            switch (command[0])
            {

                case "Site":
                    puppet.addSite(command[1], command[3]);
                    break;
                case "Process":
                    if (command[3].Equals("BROKER"))
                    {
                        puppet.addBroker(command[1], command[5], command[7], command[8]);
                    }
                    else if (command[3].Equals("SUBSCRIBER")) {
                        puppet.addSubscriber(command[1], command[5], command[7], command[8]);
                    }
                     else if (command[3].Equals("PUBLISHER")) {
                        puppet.addPublisher(command[1], command[5], command[7], command[8]);
                    }
                    break;
                case "Subscriber":
                    if (command[2].Equals("Subscribe"))
                    {
                        puppet.subscribe(command[1],command[3]);
                    }
                    break;
                case "Publisher":
                    puppet.publish(command[1], command[3], command[5], command[7]);
                    break;
            }

        }

        private void MsgViewBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

       
    }

    public class PuppetMaster : MarshalByRefObject
    {

        private bool master;
        private String address;
        private String site;
        private Dictionary<string, string> brokers;
        private Dictionary<string, string> pubWithUrl;
        private Dictionary<string, string> pubWithSite;
        private Dictionary<string, string> subsWithUrl;
        private Dictionary<string, string> subsWithSite;
        private Form1 formP;
        private BinaryTree<IBroker> sites;
        public PuppetMaster(Form1 form1)
        {
            this.formP = form1;
            sites = null;
            brokers = new Dictionary<string, string>();
            pubWithUrl = new Dictionary<string, string>();
            pubWithSite = new Dictionary<string, string>();
            subsWithUrl = new Dictionary<string, string>();
            subsWithSite = new Dictionary<string, string>();
            init();
        }

        public void init()
        {
            string cfgpath = @"..\..\..\cfg.txt";
            StreamReader script = new StreamReader(cfgpath);
            String Line;

            while ((Line = script.ReadLine()) != null)
            {
                config(Line);
            }
        }


        public void config(String line) {

            if(line.Equals("MASTER") || line.Equals("SLAVE")){
                if(line.Equals("MASTER")) 
                    this.master = true;
                else
                    this.master = false;
            }
            else if(line.Contains("SITE")) {
                string[] s = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                this.site = s[1];
            }
            else {
                this.address = line;
                char[] delimiter = { ':', '/' };
                string[] arg = line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                TcpChannel channel = new TcpChannel(Int32.Parse(arg[2]));
                ChannelServices.RegisterChannel(channel, false);              
                RemotingServices.Marshal(this, "PM", typeof(PuppetMaster));
            }
        }

        public void addBroker(string name, string site, string URL, string URLParent)
        {

            String arguments = name + " " + site + " " + URL + " " +URLParent;
            String filename = @"..\..\..\Broker\bin\Debug\Broker.exe";
            Process.Start(filename, arguments);
            formP.BeginInvoke(formP.myDelegate,  URL);

            this.brokers.Add(name,URL);
//code to build the tree not sure, cause it's done on puppetMaster. ASK
           //sites.Root.searchByName(site, sites.Root).Value = broker;
           //String msg = sites.Root.searchByName(site, sites.Root).Value.Hello();
           //Update the form
           //formP.BeginInvoke(formP.myDelegate, new Object[] { msg });
        }

        public void addSubscriber(string name, string site, string url, string urlbroker)
        {
            String arguments = name + " " + site + " " + url + " " + urlbroker;
            String filename = @"..\..\..\Subscriber\bin\Debug\Subscriber.exe";
            Process.Start(filename, arguments);
            formP.BeginInvoke(formP.myDelegate, url);
            this.subsWithUrl.Add(name, url);
            this.subsWithSite.Add(name, site);
        }

        public void addPublisher(string name, string site, string url, string urlbroker)
        {
            String arguments = name + " " + site + " " + url + " " + urlbroker;
            String filename = @"..\..\..\Publisher\bin\Debug\Publisher.exe";
            Process.Start(filename, arguments);
            formP.BeginInvoke(formP.myDelegate, url);
            this.pubWithUrl.Add(name, url);
            this.pubWithSite.Add(name, site);
        }


        public void subscribe(string processName, string topicName)
        {


        }

        public void publish(string processName, string numberEvents, string topicName, string interval)
        {
            if (this.site.Equals(this.pubWithSite[processName]))
            {

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



        public void addSite(string siteName, string siteParent)
        {
        //    if (sites == null){
        //        sites = new BinaryTree<IBroker>();
        //        sites.Root = new BinaryTreeNode<IBroker>(null, null, siteName);
        //}
        //    else
        //        sites.Root.Add(siteName, siteParent);
        }




    }

}



//Default
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//mine
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

//outer libraries
using LumiSoft.Net.UDP;
using LumiSoft.Net.Codec;
using LumiSoft.Media.Wave;

namespace client
{
    public partial class Vclient : Form
    {
        public Socket serversocket;
        private bool m_IsSendingMic = false;
        private bool m_IsSendingTest = false;
        private UdpServer VUdpServer = null;
        private WaveIn VWaveIn = null;
        private WaveOut VWaveOut = null;
        private int m_Codec = 0;
        private FileStream VRecordStream = null;
        private IPEndPoint VTargetEP = null;
        private System.Windows.Forms.Timer VTimer = null;
        public string chatwith;
        public string myip = null;
        public bool chaton = false;

        byte[] byteData = new byte[1024];
        byte[] b;

        public Vclient()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            loaddevices();

            TextBox.CheckForIllegalCrossThreadCalls = false;
            ComboBox.CheckForIllegalCrossThreadCalls = false;
            ListBox.CheckForIllegalCrossThreadCalls = false;
            Button.CheckForIllegalCrossThreadCalls = false;
            Label.CheckForIllegalCrossThreadCalls = false;

        }

        private void loaddevices()
        {
            //VWaveIn.Dispose();
            VWaveIn = null;
            // VWaveOut.Dispose();
            VWaveOut = null;
            comboBox1.Items.Clear();
            foreach (WavInDevice device in WaveIn.Devices)
            {
                comboBox1.Items.Add(device.Name);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            // Load output devices.
            comboBox2.Items.Clear();
            foreach (WavOutDevice device in WaveOut.Devices)
            {
                comboBox2.Items.Add(device.Name);
            }
            if (comboBox2.Items.Count > 0)
            {
                comboBox2.SelectedIndex = 0;
            }
            if (comboBox3.SelectedIndex != 1 && comboBox3.SelectedIndex != 0)
            {
                comboBox3.SelectedIndex = 0;
            }

            comboBox4.Items.Clear();
            foreach (IPAddress ip in System.Net.Dns.GetHostAddresses(""))
            {
                comboBox4.Items.Add(ip.ToString());
            }

            if (comboBox4.Items.Count > 0)
            {
                comboBox4.SelectedIndex = 0;
            }
            comboBox5.Items.Clear();
            foreach (IPAddress ip in System.Net.Dns.GetHostAddresses(""))
            {
                comboBox5.Items.Add(ip.ToString());
            }

            if (comboBox5.Items.Count > 0)
            {
                comboBox5.SelectedIndex = 0;
            }


            VTimer = new System.Windows.Forms.Timer();
            VTimer.Interval = 1000;
            VTimer.Tick += new EventHandler(VTimer_Tick);


        }

        private void button2_Click(object sender, EventArgs e)
        {
            loaddevices();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Test")
            {
                m_IsSendingTest = true;
                button1.Text = "stop";
                button2.Enabled = false;
                start_work();

                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                m_Codec = comboBox3.SelectedIndex;
                VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
                VWaveOut = new WaveOut(WaveOut.Devices[comboBox1.SelectedIndex], 8000, 16, 1);
                VWaveIn.BufferFull += new BufferFullHandler(audio_BufferFull);
                VWaveIn.Start();

            }
            else
            {
                m_IsSendingTest = false;
                button1.Text = "Test";
                end_work();
                try
                {
                    VWaveIn.Dispose();
                }
                catch { }
                VWaveIn = null;
                try
                {
                    VWaveOut.Dispose();
                }
                catch { }
                VWaveOut = null;
            }

        }

        private void audio_BufferFull(byte[] buffer)
        {
            // Compress data.
            byte[] encodedData = null;
            if (m_Codec == 0)
            {
                encodedData = G711.Encode_aLaw(buffer, 0, buffer.Length);
            }
            else if (m_Codec == 1)
            {
                encodedData = G711.Encode_uLaw(buffer, 0, buffer.Length);
            }

            // We just sent buffer to target end point.

            if (m_IsSendingTest)
            {
                byte[] decodedData = null;
                if (m_Codec == 0)
                {
                    decodedData = G711.Decode_aLaw(encodedData, 0, encodedData.Length);
                }
                else if (m_Codec == 1)
                {
                    decodedData = G711.Decode_uLaw(encodedData, 0, encodedData.Length);
                }

                // We just play received packet.
                VWaveOut.Play(decodedData, 0, decodedData.Length);
                /* 
                VWaveOut.Play(buffer, 0, buffer.Length);
                */
            }
            else //sending to server
            {
                // We just sent buffer to target end point.
                VUdpServer.SendPacket(encodedData, 0, encodedData.Length, VTargetEP);
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Connect")
            {
                if (textBox2.Text == "" || textBox2.Text.Contains('*'))
                {
                    MessageBox.Show(this, "Please Input a valid name !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //try
                //{
                //    VTargetEP = new IPEndPoint(IPAddress.Parse(textBox1.Text), 1500);
                //}
                //catch
                //{
                //    MessageBox.Show(this, "Invalid target IP address or port !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}


                try
                {
                    serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    IPAddress ipAddress = IPAddress.Parse(textBox1.Text);
                    //Server is listening on port 1100
                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 1100);

                    //Connect to the server
                    serversocket.BeginConnect(ipEndPoint, new AsyncCallback(OnConnect), null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


                //VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
                //VWaveIn.BufferFull += new BufferFullHandler(audio_BufferFull);

                //VWaveOut = new WaveOut(WaveOut.Devices[comboBox1.SelectedIndex], 8000, 16, 1);


                //VWaveIn.Start();

                /*
                VWaveIn = new WaveIn(WaveIn.Devices[VInDevices.SelectedIndex], 8000, 16, 1, 400);
                VWaveIn.BufferFull += new BufferFullHandler(VWaveIn_BufferFull);
                VWaveIn.Start();

                VToggleMic.Text = "Stop";
                VSendTestSound.Enabled = false;*/

            }

            else
            {
                end_work();
                //endlisten();
                try
                {
                    string Name = "X" + comboBox3.Text[5];

                    //if (chaton)
                    //{
                    //    string[] ip = chatwith.Split(' ');
                    //    Name += 'y' + ip[1] + ' ' + ip[2];
                    //}
                    //else
                    //{
                    //    Name += 'N';
                    //}

                    sendtoserver(Name);
                    endlisten();

                    //ankitbyteData = new byte[1024];
                    serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                    label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();

                    button7.Enabled = false;//test button
                    //serversocket.Close();

                    /*
                    b = null;
                    b = Encoding.ASCII.GetBytes(Name);

                    //Send the message to the server
                    serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);*/

                    //serversocket.Close();
                    button3.Text = "Connect";
                    listBox1.Items.Clear();
                    listBox1.Items.Add("No client available currently");
                    button4.Enabled = false;
                }
                catch
                {
                    MessageBox.Show("error connecting/disconnecting");
                }

                //VWaveIn.Dispose();
                //VWaveIn = null;
            }
        }

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                serversocket.EndConnect(ar);
                start_work();
                button1.Enabled = false;
                button3.Text = "Disconnect";

                button7.Enabled = true;
                m_Codec = comboBox3.SelectedIndex;

                //We are connected so we login into the server
                string Name = comboBox3.Text[5] + textBox2.Text;

                sendtoserver(Name);


                //ankitbyteData = new byte[1024];
                serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();

                /*
                b = null;
                b = Encoding.ASCII.GetBytes(Name);

                //Send the message to the server
                serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);*/
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "problem connecting server!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                //serversocket.EndSend(ar);
                serversocket.EndSend(ar);
                //byteData = new byte[1024];
                //serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Sending Data from client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void sendtoserver(string msg)
        {
            if (msg != "")
            {
                b = Encoding.ASCII.GetBytes(msg);
            }
            //else re-send whatever there is in bytearray again
            //Send the message to the server
            serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), null);


        }


        byte ini = new byte();
        private void OnReceive(IAsyncResult ar)
        {
            label8.Text = (Convert.ToInt32(label8.Text) - 1).ToString();
            //Socket serversocket = (Socket)ar.AsyncState;
            serversocket.EndReceive(ar);

            //Transform the array of bytes received from the user into an
            //intelligent form of object Data
            string msgReceived = Encoding.ASCII.GetString(byteData);
            msgReceived = msgReceived.Substring(0, msgReceived.IndexOf('\0'));

            for (int i = 0; i < msgReceived.Length; i++)
            {
                byteData[i] = ini;
            }

            if (msgReceived == "")
            {
                MessageBox.Show("empty msg");

            }


            /*
             * L: list recieved
             * I:sending client ip adress for voice chat
             * V:voice chat req/responc
             * */


            try
            {
                if (msgReceived == "resend")
                {
                    //resend coz last sending failed
                    sendtoserver("");
                    //serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);
                }

                //else if (msgReceived == "closed")
                //{
                //    serversocket.Close();
                //    return;
                //}

                else if (msgReceived == "test")
                {
                    //resend coz last sending failed
                    MessageBox.Show("test");
                    //serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);
                }
                else if (msgReceived[0] == 'L')
                {
                    if (!chaton)
                    {
                        msgReceived = msgReceived.Substring(1);
                        // MessageBox.Show(msgReceived);
                        // msgReceived = msgReceived.Substring(1, msgReceived.IndexOf('\0'));

                        button4.Text = "Talk";
                        string[] ppl = msgReceived.Split('*');
                        listBox1.Items.Clear();
                        foreach (string word in ppl)
                        {
                            if (word.Length > 1)
                                listBox1.Items.Add(word);
                        }
                        if (listBox1.Items.Count < 1)
                        {
                            listBox1.Items.Add("No client available currently");
                            button4.Enabled = false;
                        }
                        //else
                        //    button4.Enabled = true;

                    }
                }

                else if (msgReceived[0] == 'I')
                {
                    msgReceived = msgReceived.Substring(1);
                    myip = msgReceived;
                    //MessageBox.Show(msgReceived);

                    startlisten();

                }

                else if (msgReceived[0] == 'V')
                {
                    msgReceived = msgReceived.Substring(1);
                    string[] ip = msgReceived.Split(' ');
                    /*
                     * own ip
                     * request from ip
                     * ip no
                     * ip name
                     * */
                    string msg = "R";

                    //sendtoserver("test");

                    request popup = new request(ip);
                    DialogResult dialogresult = popup.ShowDialog();
                    //  R : response
                    if (dialogresult == DialogResult.OK)
                    {
                        MessageBox.Show("connected to " + ip[1]);

                        startsend(ip[1]);

                        msg += "Y";
                        //Console.WriteLine("You clicked OK");

                        chatwith = null;
                        chatwith = ip[2] + ' ' + ip[3];
                        chaton = true;
                        button4.Text = "stop chat";
                        button4.Enabled = true;
                        button7.Enabled = false;
                        listBox1.Items.Clear();
                        listBox1.Items.Add("No client available currently");
                    }
                    else if (dialogresult == DialogResult.Cancel)
                    {
                        msg += "N";
                        //Console.WriteLine("You clicked either Cancel or X button in the top right corner");
                    }
                    popup.Dispose();
                    msg += comboBox3.Text[5] + msgReceived;

                    sendtoserver(msg);
                    //sendtoserver(msg);
                    /*
                    b = Encoding.ASCII.GetBytes(msg);

                    //Send the message to the server
                    serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);
                    // serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);*/

                }

                else if (msgReceived[0] == 'R')
                {
                    if (msgReceived[1] == 'Y')
                    {
                        msgReceived = msgReceived.Substring(2);
                        string[] ip = msgReceived.Split(' ');
                        startsend(ip[0]);
                        chaton = true;
                        button4.Text = "stop chat";
                        button4.Enabled = true;
                        button7.Enabled = false;
                        listBox1.Items.Clear();
                        listBox1.Items.Add("No client available currently");
                        MessageBox.Show("Request to" + ip[0] + " Accepted!!\r\nChat is Now on :)");

                    }

                    else
                    {
                        msgReceived = msgReceived.Substring(2);
                        string[] ip = msgReceived.Split(' ');
                        MessageBox.Show("Request to" + ip[0] + " Rejected");

                        chatwith = null;
                        chaton = false;
                        button4.Text = "Talk";
                        button4.Enabled = false;
                        button7.Enabled = true;
                        endsend();

                    }
                }

                else if (msgReceived[0] == 'Q')
                {
                    msgReceived = msgReceived.Substring(1);
                    MessageBox.Show(msgReceived + " Left chat, making you available again for Voice chat");

                    chatwith = null;
                    endsend();
                    chaton = false;
                    button4.Text = "Talk";
                    button4.Enabled = false;
                    button7.Enabled = true;
                }

            }
            catch
            {
                MessageBox.Show("error at client recieve");
            }

            //ankitbyteData = new byte[1024];
            serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();
        }


        private void VWaveIn_BufferFull(byte[] buffer)
        {
            // Compress data. 
            byte[] encodedData = null;
            if (m_Codec == 0)
            {
                encodedData = G711.Encode_aLaw(buffer, 0, buffer.Length);
            }
            else if (m_Codec == 1)
            {
                encodedData = G711.Encode_uLaw(buffer, 0, buffer.Length);
            }

            // We just sent buffer to target end point.

            VUdpServer.SendPacket(encodedData, 0, encodedData.Length, VTargetEP);
        }


        private void VUdpServer_PacketReceived(UdpPacket_eArgs e)
        {
            // Decompress data.
            byte[] decodedData = null;
            if (m_Codec == 0)
            {
                decodedData = G711.Decode_aLaw(e.Data, 0, e.Data.Length);
            }
            else if (m_Codec == 1)
            {
                decodedData = G711.Decode_uLaw(e.Data, 0, e.Data.Length);
            }

            // We just play received packet.
            VWaveOut.Play(decodedData, 0, decodedData.Length);

            // Record if recoring enabled.
            if (VRecordStream != null)
            {
                VRecordStream.Write(decodedData, 0, decodedData.Length);
            }
        }



        private void VTimer_Tick(object sender, EventArgs e)
        {
            VPacketsReceived.Text = VUdpServer.PacketsReceived.ToString();
            VBytesReceived.Text = VUdpServer.BytesReceived.ToString();
            VPacketsSent.Text = VUdpServer.PacketsSent.ToString();
            VBytesSent.Text = VUdpServer.BytesSent.ToString();
        }




        private void start_work()
        {
            //button1.Enabled = true;
            button2.Enabled = false;
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            comboBox3.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            tabPage2.Enabled = false;
            //tabControl1.TabIndex(1).
        }

        private void end_work()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            comboBox3.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            tabPage2.Enabled = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serversocket != null)
            {
                string Name = "X" + comboBox3.Text[5];


                sendtoserver(Name);

                endlisten();

                //ankit byteData = new byte[1024];
                serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();

                /*
                b = null;
                b = Encoding.ASCII.GetBytes(Name);

                //Send the message to the server
                serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);

                // serversocket.Close();*/
            }
            this.Dispose();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "Talk")
            {
                try
                {
                    if ((listBox1.SelectedItem.ToString().Length > 1) && (listBox1.SelectedItem.ToString() != "No client available currently"))
                    {
                        //V+codec+requested ip
                        string Name = "V" + comboBox3.Text[5] + listBox1.SelectedItem.ToString();

                        sendtoserver(Name);



                        //ankitbyteData = new byte[1024];
                        serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                        label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();

                        chatwith = null;
                        chatwith = listBox1.SelectedItem.ToString();

                        /*
                        b = null;
                        b = Encoding.ASCII.GetBytes(Name);

                        //Send the message to the server
                        serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);*/

                    }
                }
                catch
                {
                    MessageBox.Show("Please select correct Option");
                }
            }
            else
            {
                string Name = "Q" + comboBox3.Text[5] + chatwith;

                sendtoserver(Name);

                endsend();
                chatwith = null;
                chaton = false;
                button4.Text = "Talk";
                button4.Enabled = false;
                button7.Enabled = true;
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "Listen")
            {
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (comboBox4.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select Your Ip address !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                myip = comboBox4.Text;
                m_Codec = comboBox3.SelectedIndex;
                startlisten();
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
                comboBox3.Enabled = false;
                button2.Enabled = false;
                tabPage1.Enabled = false;
                button5.Text = "stop listen";
                textBox3.Enabled = true;
                button6.Enabled = true;
                button2.Enabled = false;
            }
            else
            {
                endlisten();
                button5.Text = "Listen";

                button6.Text = "Transmit";


                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                button2.Enabled = true;
                tabPage1.Enabled = true;
                textBox3.Enabled = false;
                button6.Enabled = false;
                button2.Enabled = true;
            }
        }

        public void startlisten()
        {
            #region start recieving



            VWaveOut = new WaveOut(WaveOut.Devices[comboBox2.SelectedIndex], 8000, 16, 1);

            VUdpServer = new UdpServer();
            VUdpServer.Bindings = new IPEndPoint[] { new IPEndPoint(IPAddress.Parse(myip), 1200) };
            VUdpServer.PacketReceived += new PacketReceivedHandler(VUdpServer_PacketReceived);
            VUdpServer.Start();
            VTimer.Start();
            //return;
            #endregion

        }

        public void endlisten()
        {
            try
            {
                VUdpServer.Dispose();
                VUdpServer = null;

                VWaveOut.Dispose();
                VWaveOut = null;

                if (VRecordStream != null)
                {
                    VRecordStream.Dispose();
                    VRecordStream = null;
                }

                VTimer.Stop();
                endsend();
            }
            catch { }
            //VTimer = null;


        }

        public void startsend(string s)
        {

            try
            {
                VTargetEP = new IPEndPoint(IPAddress.Parse(s), 1200);
            }
            catch
            {
                MessageBox.Show(this, "Invalid target IP address recieved", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
            VWaveIn.BufferFull += new BufferFullHandler(VWaveIn_BufferFull);
            VWaveIn.Start();

        }

        public void endsend()
        {
            try
            {
                m_IsSendingMic = false;

                VWaveIn.Dispose();
                VWaveIn = null;
            }
            catch { }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (button6.Text == "Transmit")
            {
                startsend(textBox3.Text);
                button6.Text = "stop trans.";
            }
            else
            {
                endsend();
                button6.Text = "Transmit";
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            sendtoserver("test");
            //serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            //label8.Text = (Convert.ToInt32(label8.Text) + 1).ToString();

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                if (!chaton)
                    button4.Enabled = false;
                else
                    button4.Enabled = true;
                return;
            }

            if (listBox1.SelectedItem.ToString() != "No client available currently" && listBox1.SelectedItem.ToString().Length > 1)
            {
                button4.Enabled = true;
            }
            else
                button4.Enabled = false;
        }

        //group cast

        List<IPEndPoint> gpcst = new List<IPEndPoint>();
        private void button9_Click(object sender, EventArgs e)
        {

            if (button9.Text == "Listen")
            {
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (comboBox4.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select Your Ip address !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                myip = comboBox4.Text;
                m_Codec = comboBox3.SelectedIndex;
                startlisten();
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
                comboBox3.Enabled = false;
                button2.Enabled = false;
                tabPage1.Enabled = false;
                tabPage2.Enabled = false;
                textBox4.Enabled = true;
                button8.Enabled = true;
                button9.Text = "stop listen";
            }
            else
            {
                endlisten();
                button9.Text = "Listen";



                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                button2.Enabled = true;
                tabPage1.Enabled = true;
                tabPage2.Enabled = true;
                textBox4.Enabled = false;
                button8.Enabled = false;
                gpcst.Clear();
                checkedListBox1.Items.Clear();
            }



        }
        private void button8_Click(object sender, EventArgs e)
        {

            IPEndPoint x;
            try
            {
                x = new IPEndPoint(IPAddress.Parse(textBox4.Text), 1200);
            }
            catch
            {
                MessageBox.Show(this, "Invalid target IP address recieved", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (checkedListBox1.Items.Contains(x.Address.ToString()) == false)
            {

                checkedListBox1.Items.Add(x.Address.ToString());
                gpcst.Add(x);
            }

            if (gpcst.Count == 1)
            {
                button10.Enabled = true;
                VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
                VWaveIn.BufferFull += new BufferFullHandler(gpcst_BufferFull);
                VWaveIn.Start();
            }

        }

        private void gpcst_BufferFull(byte[] buffer)
        {
            // Compress data. 
            if (gpcst.Count > 0)
            {
                byte[] encodedData = null;
                if (m_Codec == 0)
                {
                    encodedData = G711.Encode_aLaw(buffer, 0, buffer.Length);
                }
                else if (m_Codec == 1)
                {
                    encodedData = G711.Encode_uLaw(buffer, 0, buffer.Length);
                }

                // We just sent buffer to target end point.
                foreach (IPEndPoint k in gpcst)
                {
                    VUdpServer.SendPacket(encodedData, 0, encodedData.Length, k);
                }
            }
            else
            {
                VWaveIn.Stop();
                VWaveIn = null;
                checkedListBox1.Items.Clear();
                gpcst.Clear();
                button10.Enabled = false;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            IPEndPoint temp;
            try
            {
                foreach (string s in checkedListBox1.CheckedItems)
                {
                    temp = new IPEndPoint(IPAddress.Parse(s), 1200);

                    gpcst.Remove(temp);
                }
            }
            catch { }

            while ((checkedListBox1.Items.Count >= 0) && (checkedListBox1.CheckedItems.Count > 0))
            {

                checkedListBox1.Items.Remove(checkedListBox1.CheckedItems[0]);
            }
            if (gpcst.Count < 1)
            {
                VWaveIn.Stop();
                VWaveIn = null;
                checkedListBox1.Items.Clear();
                gpcst.Clear();
                button10.Enabled = false;
            }
            //for (int i = checkedListBox1.Items.Count - 1; i >= 0; i--)
            //{
            //    if(checkedListBox1.Items(i).checked)
            //        {


            //        }

            //}
        }
    }
}
//Default
using System;
using System.Collections.Generic;
using System.Collections;
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

namespace Server
{
    public partial class Vserver : Form
    {

        class ClientInfo
        {
            public int no;
            public Socket socket;   //Socket of the client
            public string strName;  //Name by which the user logged into the chat room
            public bool busy;

            public ClientInfo()
            {
                no = 0;
                socket = null;
                strName = null;
                busy = false;
            }

            public ClientInfo(int a, Socket b, string c)
            {
                no = a;
                socket = b;
                strName = c;
                busy = false;
            }

            public bool available()
            {
                if (busy)
                    return false;
                else
                    return true;
            }

        }

        List<ClientInfo> alaw = new List<ClientInfo>();
        List<ClientInfo> ulaw = new List<ClientInfo>();
        Socket vtcp;
        byte[] byteData = new byte[128];
        byte[] message;

        public Vserver()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;
            textBox1.Text = "<<Server Started>>";
            try
            {
                //We are using TCP sockets
                vtcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //Assign the any IP of the machine and listen on port number 1100
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1100);

                //Bind and listen on the given address
                vtcp.Bind(ipEndPoint);
                vtcp.Listen(4);

                //Accept the incoming clients
                vtcp.BeginAccept(new AsyncCallback(OnAccept), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "voice server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = vtcp.EndAccept(ar);

                //Start listening for more clients
                vtcp.BeginAccept(new AsyncCallback(OnAccept), null);

                //Once the client connects then start receiving the commands from her
                //byteData = new byte[128];
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "voice server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        byte ini = new byte();
        private void OnReceive(IAsyncResult ar)
        {
            Socket clientSocket = (Socket)ar.AsyncState;
            clientSocket.EndReceive(ar);

            string msgReceived = Encoding.ASCII.GetString(byteData);
            msgReceived = msgReceived.Substring(0, msgReceived.IndexOf('\0'));

            for (int i = 0; i < msgReceived.Length; i++)
            {
                byteData[i] = ini;
            }

            // byteData = new byte[128];


            //message = Encoding.ASCII.GetBytes("test");
            //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);


            if (msgReceived == "")
            {
                MessageBox.Show("empty msg");
                
            }

            try
            {
                #region case of new client added
                if (msgReceived[0] == 'a')
                {
                    ClientInfo clientInfo = new ClientInfo();
                    if (alaw.Count > 0)
                        clientInfo.no = ((ClientInfo)alaw[alaw.Count - 1]).no + 1;
                    else
                        clientInfo.no = 1;

                    clientInfo.socket = clientSocket;
                    clientInfo.strName = msgReceived.Substring(1);
                    clientInfo.busy = false;

                    alaw.Add(clientInfo);
                    sendlist('a');//send connected valid clients to client


                    //sending ip adress recorded to client
                    string s = "I";
                    message = Encoding.ASCII.GetBytes(s + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())));
                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);



                    textBox1.Text += "\r\n<<<< " + clientInfo.strName + " CONNECTED with alaw codec >>>>";

                }

                else if (msgReceived[0] == 'u')
                {
                    ClientInfo clientInfo = new ClientInfo();
                    if (ulaw.Count > 0)
                        clientInfo.no = ((ClientInfo)ulaw[ulaw.Count - 1]).no + 1;
                    else
                        clientInfo.no = 1;
                    clientInfo.socket = clientSocket;
                    clientInfo.strName = msgReceived.Substring(1);
                    clientInfo.busy = false;


                    alaw.Add(clientInfo);
                    sendlist('a');//send connected valid clients to client


                    //sending its ip adress recorded to client
                    string s = "I";
                    message = Encoding.ASCII.GetBytes(s + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())));
                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                    textBox1.Text += "\r\n<<<< " + clientInfo.strName + " CONNECTED with ulaw codec >>>>";
                }
                #endregion

                else if (msgReceived == "test")
                {
                    //resend coz last sending failed
                    textBox1.Text += "\r\n<<<< Test >>>>";
                    message = Encoding.ASCII.GetBytes("test");
                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                    //serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);
                }

                else if (msgReceived[0] == 'X')
                {
                    List<ClientInfo> temp = new List<ClientInfo>();

                    //message = Encoding.ASCII.GetBytes("test");
                    //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                    char typ = msgReceived[1];
                    if (typ == 'u')
                        temp = ulaw;
                    else
                        temp = alaw;
                    //Socket sok;
                    int nIndex = 0;
                    foreach (ClientInfo client in temp)
                    {
                        if (client.socket == clientSocket)
                        {

                            //message = Encoding.ASCII.GetBytes("test");
                            //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);
                            //client.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), client.socket);

                            //sok = client.socket;
                            temp.RemoveAt(nIndex);
                            textBox1.Text += "\r\n<<<< " + client.strName + " left with " + typ + "law codec >>>>";

                            //sok.Close();
                            break;
                        }
                        ++nIndex;
                    }


                    //message = Encoding.ASCII.GetBytes("test");
                    //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                    //clientSocket.Close();
                    sendlist(typ);//send connected valid clients to client
                    return;

                }

                else if (msgReceived[0] == 'V')
                {
                    //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);
                    //sendlist('a');
                    char typ = msgReceived[1];
                    msgReceived = msgReceived.Substring(2);

                    string no = msgReceived.Split(' ')[0];
                    string name = msgReceived.Split(' ')[1];

                    ClientInfo a = new ClientInfo();




                    List<ClientInfo> temp = new List<ClientInfo>();
                    if (typ == 'u')
                        temp = ulaw;
                    else
                        temp = alaw;
                    //message = Encoding.ASCII.GetBytes("test");

                    //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                    int i = 0;

                    foreach (ClientInfo c in temp)
                    {
                        if (c.socket == clientSocket)
                        {
                            //message = Encoding.ASCII.GetBytes("test");
                            //c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                            //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);


                            temp[i].busy = true;
                            a = c;

                            //message = Encoding.ASCII.GetBytes("test");
                            //c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);


                            break;
                        }
                        ++i;

                    }

                    textBox1.Text += "\r\n<<<< " + a.no + " busy >>>>";

                    i = 0;
                    foreach (ClientInfo c in temp)
                    {
                        if ((Convert.ToInt32(no) == c.no) && (name == c.strName))
                        {
                            temp[i].busy = true;
                            //Request+req to+ +req from ip + +req from no+ +req from name
                            string str = "V" + (IPAddress.Parse(((IPEndPoint)c.socket.RemoteEndPoint).Address.ToString())) + " " + (IPAddress.Parse(((IPEndPoint)a.socket.RemoteEndPoint).Address.ToString())) + " " + a.no.ToString() + " " + a.strName;
                            message = Encoding.ASCII.GetBytes(str);

                            textBox1.Text += "\r\n<<<< " + temp[i].no + " busy >>>>";
                            c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                            textBox1.Text += "\r\n<<<< sending request from frnd1 to frnd2 >>>>";
                            break;
                        }
                        ++i;
                        //extract from remoteendpoint . address from each socket n send to other
                        //talk over port 1200

                    }
                    sendlist(typ);

                }

                else if (msgReceived[0] == 'Q')
                {
                    char typ = msgReceived[1];

                    List<ClientInfo> temp = new List<ClientInfo>();

                    if (typ == 'u')
                        temp = ulaw;
                    else
                        temp = alaw;
                    msgReceived = msgReceived.Substring(2);
                    string no = msgReceived.Split(' ')[0];
                    string name = msgReceived.Split(' ')[1];
                    int i = 0;
                    //making clientsocket available
                    foreach (ClientInfo c in temp)
                    {
                        if (c.socket == clientSocket)
                        {
                            temp[i].busy = false;
                            break;
                        }
                        ++i;

                    }

                    i = 0;
                    foreach (ClientInfo c in temp)
                    {
                        if ((Convert.ToInt32(no) == c.no) && (name == c.strName))
                        {
                            temp[i].busy = false;
                            string str = "Q" + name;
                            message = Encoding.ASCII.GetBytes(str);

                            c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                            textBox1.Text += "\r\n<<<< frnd on ip" + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())) + "choose to disconnect from " + (IPAddress.Parse(((IPEndPoint)temp[i].socket.RemoteEndPoint).Address.ToString())) + " making both available again >>>>";
                            break;
                        }
                        ++i;

                    }

                    sendlist(typ);

                }
                else if (msgReceived[0] == 'R')
                {
                    char typ;
                    if (msgReceived[1] == 'Y')
                    {

                        //accepted
                        typ = msgReceived[2];

                        List<ClientInfo> temp = new List<ClientInfo>();

                        if (typ == 'u')
                            temp = ulaw;
                        else
                            temp = alaw;

                        msgReceived = msgReceived.Substring(3);
                        string no = msgReceived.Split(' ')[2];
                        string name = msgReceived.Split(' ')[3];
                        textBox1.Text += "\r\n<<<< reply frm frnd 2=yes,making connectn >>>>";

                        foreach (ClientInfo c in temp)
                        {
                            if ((Convert.ToInt32(no) == c.no) && (name == c.strName))
                            {
                                string s = "RY" + msgReceived;
                                message = Encoding.ASCII.GetBytes(s);
                                c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                                break;
                            }

                        }



                    }
                    else if (msgReceived[1] == 'N')
                    {
                        typ = msgReceived[2];
                        textBox1.Text += "\r\n<<<< reply frm frnd 2=no,making frnd 1 n 2 available again >>>>";

                        List<ClientInfo> temp = new List<ClientInfo>();

                        if (typ == 'u')
                            temp = ulaw;
                        else
                            temp = alaw;

                        msgReceived = msgReceived.Substring(3);

                        string no = msgReceived.Split(' ')[2];
                        string name = msgReceived.Split(' ')[3];
                        int k = 0;
                        for (var x = 0; x < temp.Count; x++)
                        {
                            if ((Convert.ToInt32(no) == temp[x].no) && (name == temp[x].strName))
                            {

                                temp[x].busy = false;
                                textBox1.Text += "\r\n<<<< " + temp[x].no + " available again >>>>";
                                k++;
                                string s = "RN";
                                message = Encoding.ASCII.GetBytes(s);
                                temp[x].socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), temp[x].socket);
                            }
                            else if (temp[x].socket == clientSocket)
                            {
                                temp[x].busy = false;
                                textBox1.Text += "\r\n<<<< " + temp[x].no + " available again >>>>";
                                k++;

                            }
                            if (k > 1)
                                break;
                        }

                        sendlist(typ);
                        //refused
                    }
                    textBox1.Text += "\r\n<<<< passing reply to frnd1 >>>>";

                }

            }
            catch (Exception ex)
            {
                if (msgReceived == "")
                {
                    string s = "resend";
                    message = Encoding.ASCII.GetBytes(s);
                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);


                }
                else
                    MessageBox.Show(ex.Message, "voice server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            //ankitbyteData = new byte[128];
            clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
        }


        void sendlist(char typ)
        {
            string s;
            List<ClientInfo> temp = new List<ClientInfo>();

            if (typ == 'u')
                temp = ulaw;
            else
                temp = alaw;


            foreach (ClientInfo j in temp)
            {
                if (j.available())
                {
                    s = "L";
                    foreach (ClientInfo k in temp)
                    {
                        if ((k.socket != j.socket) && k.available())
                            s += k.no + " " + k.strName + "**";
                    }
                    message = Encoding.ASCII.GetBytes(s);

                    j.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), j.socket);
                }
            }
        }


        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Sending Data from server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();

        }


    }
}

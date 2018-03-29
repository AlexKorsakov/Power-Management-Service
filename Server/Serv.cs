using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Timer = System.Timers.Timer;

namespace Server
{
    public partial class Serv : Form
    {
        public static String host = System.Net.Dns.GetHostName();
        int BroadcastLatency = 5000;        //частота рассылки, сек
        Timer operationTimer = new Timer() { Interval = 1000 };
        bool operationTimerflag = false;
        //private List<ResponceParse> Clients = new List<ResponceParse>();        //UDP 



        public Serv()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Отправка широковещательных сообщений, чтобы серверы можно было найти.
        /// Отправка  результатов запросов.
        /// </summary>
        #region UDP-клиент
        public static Socket sendingSocket;
        Thread senderThread = null;

        //отправка Broadcast UDP-сообщения
        public void Broadcast()
        {
            IPEndPoint remotePoint = new IPEndPoint(IPAddress.Broadcast, 2200);  //IPAddress.Broadcast
            sendingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            try
            {
                Options si = new Options();
                string uptime = si.GetUptime();

                string message = "#BROADCAST#" + host + "#INFO#" + uptime + "#END#"; // сообщение для отправки
                sendingSocket.SendTo(Encoding.Unicode.GetBytes(message), remotePoint);
                //Send(remotePoint);
            }
            catch (Exception ex)
            {
                //log
                MessageBox.Show(ex.Message);
            }
        }

        public void Send(IPEndPoint remotePoint, string message)
        {
            byte[] msg = Encoding.Default.GetBytes(message);
            int sended = 0;
            try
            {
                while (sended < message.Length)     
                    sended = sendingSocket.SendTo(Encoding.Unicode.GetBytes(message), remotePoint);
            }
            catch (Exception ex)
            {

            }

        }
        #endregion





        #region UDP-сервер

        ResponceParse parser;
        bool stopReceive = false;
        Thread recieverThread = null;
        public static Socket listeningSocket;


        //Прием UDP-сообщения
        public void ReceiveMessage()
        {
            //IPEndPoint localIP = new IPEndPoint(IPAddress.Any, 2200);    //IPAddress.Parse("255.255.255.255")
            //listeningSocket.Bind(localIP);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, 2201));
            try
            {
                while (true)
                {
                    byte[] data = new byte[256]; // буфер для получаемых данных
                    IPEndPoint localIP = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint remoteIp = (EndPoint)localIP;
                    listeningSocket.ReceiveFrom(data, ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                    if (message != "")
                    {
                        parser = new ResponceParse(message);
                        Options opt = new Options();
                        bool commandResult = false;
                        //var task = new Task { TimeToRun = parser.intime, Interval = new TimeSpan(0, 0, parser.delay) };
                        switch (parser.type)
                        {
                            case "COM":
                                //выполнить по таймеру
                                commandResult = opt.RunCommand(parser.substrings[1], parser.delay, parser.intime);
                                //task.Action = delegate { commandResult = opt.RunCommand(parser.type, parser.delay, parser.intime); };
                                //Send(new IPEndPoint(remoteFullIp.Address, 2200), "#COM-RES#" + commandResult);
                                break;
                            case "INFO":
                                Send(new IPEndPoint(remoteFullIp.Address, 2200), "#INFO#" + opt.GetUptime() + "#FROM#" + host + "#END#");
                                /*
                                task.Action = delegate
                                {
                                    //MessageBox.Show("Task is running");
                                };

                                operationTimer.Elapsed += delegate { task.CheckAndRun(); };
                                operationTimer.Start();
                                */
                                //hostName = substrings[2];
                                break;
                            case "POWERCFG":
                                //Выполнить  POWERCFG с флагом
                                //Send(new IPEndPoint(remoteFullIp.Address, 2200), "#POWERCFG#" + opt.PowerConfiguration(parser.substrings[1]));
                                break;
                        }
                    }
                    if (stopReceive) break;
                }
            }
            catch (Exception ex)
            {
                var w32ex = ex as Win32Exception;
                if (w32ex.ErrorCode != 10004)
                {
                    MessageBox.Show("Ошибка приема сообщения: " + ex.Message);
                }
            }
        }



        /*
        void StopUDPReceive()
        {
            stopReceive = true;
            if (listeningSocket != null) listeningSocket.Close();
            if (senderThread != null) senderThread.Join();
        }
        */
        void StopUDPReceive()
        {
            stopReceive = true;
            timer1.Enabled = false;
            if (listeningSocket != null) sendingSocket.Dispose();
            if (sendingSocket != null) listeningSocket.Dispose();
            if (senderThread != null) senderThread.Join();
            if (recieverThread != null) recieverThread.Join();
            senderThread = null;
            recieverThread = null;
        }
        #endregion





        #region События 

        private void Serv_Load(object sender, EventArgs e)
        {
            this.Text = host;
            timer1.Interval = BroadcastLatency;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            stopReceive = false;
            timer1.Enabled = true;      //Запуск шрассылки
            sendingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            recieverThread = new Thread(new ThreadStart(ReceiveMessage));  //Поток для прослушки по UDP
            recieverThread.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopUDPReceive();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            senderThread = new Thread(new ThreadStart(Broadcast));
            senderThread.Start();
        }
        #endregion


        IPAddress SelectAddress(IPHostEntry currentHost)        //исключить не "192.168"
        {
            IPAddress ipAddress = currentHost.AddressList[0];
            foreach (IPAddress ip in currentHost.AddressList)
            {
                string str = ip.ToString().Substring(0, 7);
                if (str == "192.168")
                {
                    ipAddress = ip;
                    break;
                }
            }
            return ipAddress;
        }
    }

    public class Task
    {
        public TimeSpan Interval { get; set; }
        public DateTime TimeToRun { get; set; }
        public Action Action { get; set; }

        public void CheckAndRun()
        {
            var now = DateTime.Now;
            /*
            if (TimeToRun <= now)
            {
                while (TimeToRun < now)
                    TimeToRun += Interval;
                if (Action != null)
                    try
                    {
                        Action();
                    }
                    catch {
                }
            }
            if (TimeToRun == null || TimeToRun < now)
                TimeToRun = now;

            */
            var date = TimeToRun.AddSeconds(Interval.Seconds);
            var v = date.CompareTo(now);
            bool b = false;


            if (v == 0)
            {
                MessageBox.Show("" + v);
                Action?.Invoke();

                b = true;
            }

        }
    }
}

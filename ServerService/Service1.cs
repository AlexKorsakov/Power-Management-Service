using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Timer = System.Timers.Timer;
using System.Runtime.InteropServices;

namespace ServerService
{
    public partial class My_PowerMgmnt : ServiceBase
    {
        public static String host = System.Net.Dns.GetHostName();
        int BroadcastLatency = 5000;        //частота рассылки, сек
        private EventLog eventLog1;
        private Timer timer1 = new Timer();

        public My_PowerMgmnt(string[] args)
        {
            InitializeComponent();
            if (args.Count() == 1)
            {
                int i = 10;
                if (Int32.TryParse(args[0], out i) && i >= 10)
                    SetBroadcastLatency(i);
            }

            eventLog1 = new EventLog();
            if (!EventLog.SourceExists("My_PowerMgmntSource"))
            {
                EventLog.CreateEventSource(
                    "My_PowerMgmntSource", "My_PowerMgmntLog");
            }
            eventLog1.Source = "My_PowerMgmntSource";
            eventLog1.Log = "My_PowerMgmntLog";
        }


        #region События 

        private void timer1_Tick(object sender, EventArgs e)
        {
            senderThread = new Thread(new ThreadStart(Broadcast));
            senderThread.Start();
        }


        protected override void OnStart(string[] args)
        {
            //Мониторинг состояния службы
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);


            //Запуск таймера рассылки
            this.timer1.Interval = BroadcastLatency;
            this.timer1.Elapsed += new System.Timers.ElapsedEventHandler(this.timer1_Tick);
            this.timer1.Start();

            StartUDPRecieve();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            StopUDPReceive();

            this.timer1.Stop();
            this.timer1 = null;
            eventLog1 = null;
        }

        protected override void OnContinue()
        {
            StartUDPRecieve();
            eventLog1.WriteEntry("Продолжаю служить.");
        }

        protected override void OnPause()
        {
            StopUDPReceive();
            eventLog1.WriteEntry("Работа приостановлена.", EventLogEntryType.Warning);
            base.OnPause();
        }

        protected override void OnShutdown()
        {
            StopUDPReceive();
            eventLog1.WriteEntry("Завершение работы.", EventLogEntryType.Warning);
            base.OnShutdown();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            eventLog1.WriteEntry("Изменение параметров питания.", EventLogEntryType.Warning);
            return base.OnPowerEvent(powerStatus);
        }

        #endregion





        #region States
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public long dwServiceType;
            public ServiceState dwCurrentState;
            public long dwControlsAccepted;
            public long dwWin32ExitCode;
            public long dwServiceSpecificExitCode;
            public long dwCheckPoint;
            public long dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        #endregion

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
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry("Broadcast error!", EventLogEntryType.Error);
            }
        }

        public void Send(IPEndPoint remotePoint, string message)
        {
            byte[] msg = Encoding.Default.GetBytes(message);
            sendingSocket.SendTo(Encoding.Unicode.GetBytes(message), remotePoint);

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
                        switch (parser.type)
                        {
                            case "COM":
                                eventLog1.WriteEntry("Клиент: " +
                                    remoteFullIp.Address.ToString() +
                                    ", прислал команду " + parser.substrings[1] +
                                    ", Delay: " + parser.delay.ToString() +
                                    ", In time: " + parser.intime.ToString() +
                                    ". Начинаю выполнение", EventLogEntryType.Information);

                                commandResult = opt.RunCommand(parser.substrings[1], parser.delay, parser.intime);
                                break;
                            case "INFO":
                                Send(new IPEndPoint(remoteFullIp.Address, 2200), "#INFO#" + opt.GetUptime() + "#FROM#" + host + "#END#");
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
                    eventLog1.WriteEntry("Ошибка приема сообщения: " + ex.Message, EventLogEntryType.Error);
                }
            }
        }


        void StartUDPRecieve()
        {
            stopReceive = false;
            timer1.Enabled = true;      //Запуск шрассылки
            sendingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            recieverThread = new Thread(new ThreadStart(ReceiveMessage));  //Поток для прослушки по UDP
            recieverThread.Start();
            eventLog1.WriteEntry("Service started!");
        }
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
            eventLog1.WriteEntry("Service stopped!");
        }
        #endregion


        
        /// <summary>
        /// Задает частоту шрассылки в мс
        /// </summary>
        /// <param name="value"></param>
        void SetBroadcastLatency(int value)
        {
            BroadcastLatency = value;
        }
    }
}

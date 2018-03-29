using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace KursovoyUDP
{
    class ResponceParse     //класс для парсинга входящих udp-сообщений
    {
        public static List<ResponceParse> Clients = new List<ResponceParse>();         
        public String ResponceMessage = null;
        String[] substrings = null;
        public string hostName;
        public IPAddress ip;
        public string type;
        public int port=2200;
        public string uptime;

        public ResponceParse(string message, IPAddress remoteFullIp)
        {
            ResponceMessage = message;
            char delimeter ='#';
            substrings = message.Split(delimeter);

            type = substrings[1];
            switch (type)
            {
                case "BROADCAST":
                    hostName = substrings[2];
                    uptime = substrings[4];
                    break;
                case "INFO":
                    hostName = substrings[4];
                    uptime = substrings[2];
                    break;
                case "POWERCFG":
                    ResponceMessage = ResponceMessage.Substring(("#POWERCFG#Q#END#").Length);
                    break;
            }
            ip = remoteFullIp;
        }
        public ResponceParse(string name)
        {
            hostName = name;
        }
        public ResponceParse()
        {

        }
        public void ParsePort()
        {
            int i = ResponceMessage.IndexOf(":");
            int z = ResponceMessage.IndexOf("#END#");
            port = Int32.Parse(ResponceMessage.Substring(i + 1, z - i - 1));
        }

        public static bool ContainsClient(IPEndPoint ip)
        {
            foreach (ResponceParse client in Clients)
            {
                if (client.ip.Equals(ip.Address))
                    return true;
            }
            return false;
        }

        public static IPEndPoint GetIp_fromName(string hostname)
        {
            foreach (ResponceParse client in Clients)
            {
                if (client.hostName == hostname)
                    return new IPEndPoint(client.ip, 2201);
            }
            return new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2201);
        }

        public static void Remove(string hostname)
        {
            foreach (ResponceParse client in Clients)
            {
                if (client.hostName == hostname)
                {
                    Clients.Remove(client);
                    break;
                }
            }
        }

        public static void Remove(string[] hostname)
        {
            foreach (ResponceParse client in Clients)
            {
                foreach (string host in hostname)
                {
                    if (client.hostName == host)
                    {
                        Clients.Remove(client);
                        break;
                    }
                }
            }
        }
    }
}

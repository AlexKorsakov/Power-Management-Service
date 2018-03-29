using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace ServerService
{
    class ResponceParse     //класс для парсинга входящих udp-сообщений
    {
        public String ResponceMessage = null;
        public string hostName;
        public string type;
        public int delay = 0;
        public DateTime intime;
        public String[] substrings;
        bool commandResult = false;         //Результат выполнения RunCommand()

        public ResponceParse(string message)
        {
            ResponceMessage = message;
            char delimeter = '#';
            substrings = splitArray(message.Split(delimeter));
            type = substrings[0];

            int subindex = Array.IndexOf(substrings, "DELAY");
            if (subindex != -1)
                delay = Int32.Parse(substrings[subindex + 1]);

            intime = DateTime.Now;
            subindex = Array.IndexOf(substrings, "INTIME");
            if (subindex != -1 && DateTime.TryParse(substrings[subindex + 1], out intime))
            {
                if (DateTime.Now > intime)
                    intime = intime.AddDays(1);
            }
        }

        string[] splitArray(string[] array)
        {
            string[] newarray = new string[array.Length - 2];
            for (int i = 0; i < newarray.Length + 1; i++)
                if (i != 0)
                    newarray[i - 1] = array[i];
            return newarray;
        }
    }
}

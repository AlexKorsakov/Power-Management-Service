using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace Server
{
    class Options
    {
        public Options()
        {

        }

        public bool RunCommand(string command, int delay, DateTime intime)
        {
            switch (command)
            {
                case "SUSPEND":
                    return GoSuspend();
                case "EXIT":
                    return GoExit();
                case "SHUTDOWN":
                    return GoShutdown(delay, intime);
                case "REBOOT":
                    return GoReboot(delay, intime);
                case "HIBERNATE":
                    return GoHibernate();

            }
            return false;
        }

        #region Get

        /// <summary>
        /// Возвращает аптайм компа
        /// </summary>
        /// <returns></returns>
        public string GetUptime()
        {
            TimeSpan t = TimeSpan.FromMilliseconds(Environment.TickCount);
            return string.Format("{0:D2}d:{1:D2}h:{2:D2}m:{3:D2}s",
                                    t.Days,
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds);
        }

        public string PowerConfiguration(string param)
        {
            string output = "";
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "POWERCFG",
                    Arguments = param,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(866)            
                }
            };
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                output += proc.StandardOutput.ReadLine();
            }
            /*
            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = "POWERCFG /"+ param;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                //p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
                p.Start();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            */
            return output;
        }

        public bool GoHibernate()
        {
            return Application.SetSuspendState(PowerState.Hibernate, true, true);
        }

        public bool GoSuspend()
        {
            return Application.SetSuspendState(PowerState.Suspend, true, true);
        }

        private void button1_Click(System.Object sender, EventArgs e)
        {
        
        }

        [DllImport("user32")]
        public static extern void LockWorkStation();
        public bool GoExit()
        {
            MessageBox.Show("sdfsd");
            LockWorkStation();
            return true;
        }
        public bool GoShutdown(int t, DateTime intime)
        {
            var diff = intime.AddMinutes(t) - DateTime.Now;
            if (diff.TotalSeconds < 0)
                diff = new TimeSpan(0, 0, 0);
            string s = "/s /t " + ((int)diff.TotalSeconds+1).ToString();
            //Process.Start("shutdown", "/s /t " + ((int)diff.TotalSeconds).ToString());
            return true;
        }
        public bool GoReboot(int t, DateTime intime)
        {
            var diff = intime.AddMinutes(t) - DateTime.Now;
            if (diff.TotalSeconds < 0)
                diff = new TimeSpan(0, 0, 0);
            Process.Start("shutdown", "/r /t " + ((int)diff.TotalSeconds + 1).ToString());
            return true;
        }
        #endregion





    }
}

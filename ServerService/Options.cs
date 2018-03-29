using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace ServerService
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

        #endregion


        #region Operations

        public bool GoHibernate()
        {
            return Application.SetSuspendState(PowerState.Hibernate, true, true);
        }

        public bool GoSuspend()
        {
            return Application.SetSuspendState(PowerState.Suspend, true, true);
        }

        public bool GoExit()
        {
            return Application.SetSuspendState(PowerState.Suspend, true, true);
        }
        public bool GoShutdown(int t, DateTime intime)
        {
            var diff = intime.AddMinutes(t) - DateTime.Now;
            if (diff.TotalSeconds < 0)
                diff = new TimeSpan(0, 0, 0);
            Process.Start("shutdown", "/s /t " + ((int)diff.TotalSeconds+1).ToString());
            return true;
        }
        public bool GoReboot(int t, DateTime intime)
        {
            var diff = intime.AddMinutes(t) - DateTime.Now;
            if (diff.TotalSeconds < 0)
                diff = new TimeSpan(0, 0, 0);
            Process.Start("shutdown", "/s /t " + ((int)diff.TotalSeconds+1).ToString());        //изменить на /r
            return true;
        }
        #endregion
    }
}

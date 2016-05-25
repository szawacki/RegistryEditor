using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RegEditor
{
    public class Command
    {
        private string result;
        public int exitCode = 0;

        /// <summary>
        /// Check host availability
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public bool checkRemoteHostAvailablity(string hostname)
        {
            this.CMD("ping -n 1 -w 250 " + hostname, false);

            if (result.Contains("TTL="))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Run cmd command
        /// </summary>
        /// <param name="commandline"></param>
        /// <param name="visible"></param>
        public void CMD(string commandline, bool visible)
        {
            this.result = "";
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + commandline);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            this.result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(120);
        }

        /// <summary>
        /// Get result
        /// </summary>
        /// <returns></returns>
        public string getResult()
        {
            return result;
        }

        /// <summary>
        /// Search for expression in string
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public bool expressionFoundInResult(string expression)
        {
                if (this.result.Contains(expression))
                    return true;

            return false;
        }
    }
}

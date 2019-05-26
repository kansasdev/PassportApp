using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSetup
{
    public class Prereq
    {
        public bool CheckJavaPrerequisite(ref string msgBack)
        {

            bool ok = false;
            List<String> output = new List<string>();
            Process process = new Process();
            try
            {
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = "/c \"" + "java -version " + "\"";

                process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        output.Add((string)e.Data);
                    }
                });
                process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        output.Add((String)e.Data);
                    }
                });

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                ok = (process.ExitCode == 0);
            }
            catch (Exception ex)
            {
                msgBack = ex.Message;
            }
            string temp = string.Empty;
            output.ForEach(q => temp = temp + Environment.NewLine + q);
            msgBack = temp;
            return ok;

        }

        public bool CheckPythonForSetup(ref string back)
        {
            //do setupu - ustaw zmienną w local computer, a nie current user
            //to do - popróbuj dla everyone/just me

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"cmd.exe"; // Specify exe name.
            start.Arguments = "/c mrz.exe --version";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = false;
            string result = string.Empty;

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    result = reader.ReadToEnd();
                    process.WaitForExit();
                }
            }

            back = result;

            if (result.Contains("PassportEye"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool FirewallRuleExists(ref string msgBack)
        {
            bool ok = false;
            List<String> output = new List<string>();
            Process process = new Process();
            try
            {
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = "Get-NetFirewallRule -DisplayName PassportService";

                process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        output.Add((string)e.Data);
                    }
                });
                process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        output.Add((String)e.Data);
                    }
                });

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                ok = (process.ExitCode == 0);
            }
            catch (Exception ex)
            {
                msgBack = ex.Message;
            }
            string temp = string.Empty;
            output.ForEach(q => temp = temp + Environment.NewLine + q);
            msgBack = temp;
            if (ok)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool RemoveFirewallRules(ref string msgBack)
        {
            bool ok = false;
            List<String> output = new List<string>();
            Process process = new Process();
            try
            {
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = "Remove-NetFirewallRule -DisplayName PassportService";

                process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        output.Add((string)e.Data);
                    }
                });
                process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        output.Add((String)e.Data);
                    }
                });

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                ok = (process.ExitCode == 0);
            }
            catch (Exception ex)
            {
                msgBack = ex.Message;
            }
            string temp = string.Empty;
            output.ForEach(q => temp = temp + Environment.NewLine + q);
            msgBack = temp;
            if (ok && temp == "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomSetup
{
    [RunInstaller(true)]

    public partial class Install : System.Configuration.Install.Installer
    {
        public Install()
        {
            InitializeComponent();
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            //check Java
            Prereq p = new Prereq();
            string msg = string.Empty;
            bool JavaCheck = p.CheckJavaPrerequisite(ref msg);
            if (!JavaCheck)
            {
                throw new InstallException("No Java installed" + Environment.NewLine + @"Check https://github.com/kansasdev for instructions");

            }
            else
            {
                bool pythonCheck = p.CheckPythonForSetup(ref msg);
                if (pythonCheck)
                {
                    base.OnBeforeInstall(savedState);
                }
                else
                {
                    throw new InstallException("No Python with Tesseract and PassportEye installed" + Environment.NewLine + msg + Environment.NewLine + @"Check https://github.com/kansasdev for instructions");
                }
            }


        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            string installationProg = this.Context.Parameters["assemblypath"];
            string installationPath = installationProg.Replace("CustomSetup.dll", "");

            if (File.Exists(installationPath + "PassportService.exe.config"))
            {
                List<string> lst = File.ReadAllLines(installationPath + "PassportService.exe.config").ToList();
                List<string> lstNew = new List<string>();
                foreach (string s in lst)
                {
                    if (s.Trim().StartsWith("<add key=\"JavaReaderPath\""))
                    {
                        lstNew.Add("<add key=\"JavaReaderPath\" value = \"" + installationPath + "\"/>");
                    }
                    else
                    {
                        lstNew.Add(s);
                    }
                }

                File.Delete(installationPath + "\\PassportService.exe.config");
                File.WriteAllLines(installationPath + "\\PassportService.exe.config", lstNew.ToArray());
            }
            if (File.Exists(installationPath + "config.properties"))
            {
                List<string> lst = File.ReadAllLines(installationPath + "config.properties").ToList();
                List<string> lstNew = new List<string>();
                string input = Interaction.InputBox("Enter reader name (Configuration Panel/System/Smart Cards)", "Enter reader name", "ACS ACR1281 1S Dual Reader PICC 0", -1, -1);
                foreach (string s in lst)
                {
                    if (s.StartsWith("reader="))
                    {
                        lstNew.Add("reader=" + input);
                    }
                    else
                    {
                        lstNew.Add(s);
                    }
                }

                File.Delete(installationPath + "config.properties");
                File.WriteAllLines(installationPath + "config.properties", lstNew.ToArray());

            }
        }



        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            string installationProg = this.Context.Parameters["assemblypath"];
            string installationPath = installationProg.Replace("CustomSetup.dll", "");
            //Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(installationPath, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents);
            Directory.GetFiles(installationPath, "*.txt").ToList().ForEach(q => File.Delete(q));
            Directory.GetFiles(installationPath, "*.bin").ToList().ForEach(q => File.Delete(q));
            Directory.GetFiles(installationPath, "*.jpeg").ToList().ForEach(q => File.Delete(q));
            Directory.GetFiles(installationPath, "*.jp2").ToList().ForEach(q => File.Delete(q));

            Prereq p = new Prereq();
            string msg = string.Empty;
            bool res = p.FirewallRuleExists(ref msg);
            if (res == true)
            {
                msg = string.Empty;
                res = p.RemoveFirewallRules(ref msg);
                if (!res)
                {
                    MessageBox.Show("Couldn't remove firewall rules for PassportService" + Environment.NewLine + "Uninstallation will fail,try to remove it manually and then try again" + Environment.NewLine + msg);
                }
            }

            base.OnBeforeUninstall(savedState);
        }

    }
}

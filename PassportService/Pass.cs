using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace PassportService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Pass" in both code and config file together.

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Pass : IPass
    {
        private IPassCallback callback;
        private PersonalDataVIZ pdv;
        public Pass()
        {
            callback = OperationContext.Current.GetCallbackChannel<IPassCallback>();
        }

        public Pass(bool noCallback)
        {

        }
        public PersonalDataVIZ ExtractMrzData(byte[] image)
        {
            pdv = new PersonalDataVIZ();
            try
            {
                File.WriteAllBytes(ConfigurationSettings.AppSettings["JavaReaderPath"]+"pass.jpeg", image);

                string msgBack = string.Empty;
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
                    process.StartInfo.WorkingDirectory = ConfigurationSettings.AppSettings["JavaReaderPath"];
                    process.StartInfo.Arguments = "/c \"" + "mrz pass.jpeg" + "\"";

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
                            msgBack = msgBack + e.Data;
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
                if (ok&&string.IsNullOrEmpty(msgBack))
                {
                    string temp = string.Empty;
                    output.ForEach(new Action<string>(ParseMrzOcr)
                    );
                    msgBack = string.Empty;
                    pdv.Errors = msgBack;
                    return pdv;
                }
                else
                {
                    pdv.Errors = msgBack;
                    return pdv;
                }

            }
            catch (Exception ex)
            {
                throw new FaultException("Error analyzing MRZ: " + ex.Message);

            }
        }

        private void ParseMrzOcr(string line)
        {
            if(pdv == null)
            {
                pdv = new PersonalDataVIZ();
            }
            if(line.StartsWith("number"))
            {
                pdv.DocumentNumber = line.Replace("number", "").Trim();
            }
            if(line.StartsWith("country"))
            {
                pdv.IssuuingCountry = line.Replace("country", "").Trim();
            }
            if(line.StartsWith("date_of_birth"))
            {
                pdv.DateOfBirth = line.Replace("date_of_birth", "").Trim();
            }
            if(line.StartsWith("expiration_date"))
            {
                pdv.ExpirationDate = line.Replace("expiration_date", "").Trim();
            }
            if(line.StartsWith("nationality"))
            {
                pdv.Citizenship = line.Replace("nationality", "").Trim();
            }
            if (line.StartsWith("sex"))
            {
                pdv.Sex = line.Replace("sex", "").Trim();
            }
            if(line.StartsWith("names"))
            {
                pdv.Firstname = line.Replace("names", "").Replace(">","").Trim();
                
                pdv.Firstname = pdv.Firstname.Replace("<", " ");

                pdv.Firstname = pdv.Firstname.Trim();

            }

            if (line.StartsWith("surname"))
            {
                pdv.Lastname = line.Replace("surname", "").Replace(">","").Trim();
                
                pdv.Lastname = pdv.Lastname.Replace("<", " ");

                pdv.Lastname = pdv.Lastname.Trim();
            }
            if(line.StartsWith("valid_number"))
            {
                if(line.Contains("True"))
                {
                    pdv.DocNumberChecksum = true;
                }
                if (line.Contains("False"))
                {
                    pdv.DocNumberChecksum = false;
                }
            }
            if (line.StartsWith("valid_date_of_birth"))
            {
                if (line.Contains("True"))
                {
                    pdv.DOBChecksum = true;
                }
                if (line.Contains("False"))
                {
                    pdv.DOBChecksum = false;
                }
            }
            if (line.StartsWith("valid_expiration_date"))
            {
                if (line.Contains("True"))
                {
                    pdv.ExpirationChecksum = true;
                }
                if (line.Contains("False"))
                {
                    pdv.ExpirationChecksum = false;
                }
            }
        }

        public void ReadChip(KeyData kd)
        {
            string msgBack = string.Empty;
            bool ok = false;
            List<String> output = new List<string>();
            Process process = new Process();
            ChipData cd = new ChipData();
            try
            {
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WorkingDirectory = ConfigurationSettings.AppSettings["JavaReaderPath"];
                process.StartInfo.Arguments = "/c \"" + "java -jar mrtd.jar -doc:" + kd.DocNumber + " -dob:" + kd.DOB + " -val:" + kd.ValidityDate + "\"";



                process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        try
                        {
                            if (!e.Data.StartsWith("WARN"))
                            {
                                if (e.Data.Contains("OK session opened"))
                                {
                                    callback.SessionOpened();
                                }
                                if (e.Data.Contains("OK PACE"))
                                {
                                    cd.IsPACEReadingProc = true;
                                }
                                if (e.Data.Contains("OK BAC"))
                                {
                                    cd.IsPACEReadingProc = false;
                                }
                                if (e.Data.Contains("OK MRZ"))
                                {
                                    cd.MRZ = File.ReadAllText(ConfigurationSettings.AppSettings["JavaReaderPath"] + "mrz.txt");
                                    callback.MrzFromChipTaken(cd.MRZ);
                                    List<string> lst = File.ReadAllLines(ConfigurationSettings.AppSettings["JavaReaderPath"] + "mrz_parsed.txt").ToList();
                                    foreach (string ss in lst)
                                    {
                                        if (ss.StartsWith("Lastname"))
                                        {
                                            cd.LastName = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("Firstname"))
                                        {
                                            cd.FirstName = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("DateOfBirth"))
                                        {
                                            cd.DOB = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("ExpirationDate"))
                                        {
                                            cd.ExpirationDate = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("IssuingCountry"))
                                        {
                                            cd.IssuingCountry = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("Citizenship"))
                                        {
                                            cd.Citizenship = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("DocumentNumber"))
                                        {
                                            cd.DocumentNumber = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                        if (ss.StartsWith("DocumentType"))
                                        {
                                            cd.DocumentType = ss.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
                                        }
                                    }

                                }
                                if (e.Data.Contains("OK Face image taken"))
                                {
                                    cd.ImagePhoto = File.ReadAllBytes(ConfigurationSettings.AppSettings["JavaReaderPath"] + "twarz.jpeg");
                                    callback.ImageFromChipTaken(cd.ImagePhoto);
                                    if (File.Exists(ConfigurationSettings.AppSettings["JavaReaderPath"] + "twarz.jp2"))
                                    {
                                        cd.IsJPEG2000 = true;
                                    }
                                    else
                                    {
                                        cd.IsJPEG2000 = false;
                                    }
                                }
                                if (e.Data.Contains("OK CA status"))
                                {
                                    cd.CAStatus = true;
                                }
                                if (e.Data.Contains("ERR CA status"))
                                {
                                    cd.CAStatus = false;
                                }
                                if (e.Data.Contains("OK security element"))
                                {
                                    callback.DocChecksPerformed();
                                }
                                if (e.Data.Contains("OK SOD integrity check: POSITIVE"))
                                {
                                    cd.SODIntegrity = true;
                                }
                                if (e.Data.Contains("OK SOD integrity check: NEGATIVE"))
                                {
                                    cd.SODIntegrity = false;
                                }
                                if (e.Data.Contains("ERR Couldn't perform SOD"))
                                {
                                    cd.SODIntegrity = false;
                                }
                                if (e.Data.Contains("OK DS issuer"))
                                {
                                    cd.IsDSTrusted = true;
                                }
                                if (e.Data.Contains("ERR DS issuer"))
                                {
                                    cd.IsDSTrusted = false;

                                }
                                if (e.Data.Contains("ERR Error parsing"))
                                {
                                    cd.IsDSTrusted = false;
                                }
                                if (e.Data.Contains("OK Hash of DG1"))
                                {
                                    cd.DG1Integrity = true;
                                }
                                if (e.Data.Contains("ERR Hash of DG1"))
                                {
                                    cd.DG1Integrity = false;
                                }
                                if (e.Data.Contains("OK Hash of DG2"))
                                {
                                    cd.DG2Integrity = true;
                                }
                                if (e.Data.Contains("ERR Hash of DG2"))
                                {
                                    cd.DG2Integrity = false;
                                }
                                if (e.Data.Contains("OK Hash of DG14"))
                                {
                                    cd.DG14Integrity = true;
                                }
                                if (e.Data.Contains("ERR Hash of DG14"))
                                {
                                    cd.DG14Integrity = false;
                                }

                                if (e.Data.Contains("OK session closed"))
                                {
                                    callback.SessionFinished("Session finished without issues");
                                }
                                if (e.Data.Contains("ERR no suitable readers"))
                                {
                                    callback.SessionFinished(e.Data);
                                }
                                if (e.Data.Contains("ERR error during reading"))
                                {
                                    callback.SessionFinished(e.Data);
                                }

                                output.Add((string)e.Data);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ERROR: " + ex.Message);
                        }
                    }
                });
                process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    if (e.Data != null)
                    {
                        if (!e.Data.StartsWith("WARNING"))
                        {
                            msgBack = msgBack + e.Data;
                        }
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
            finally
            {
                if(output!=null)
                {
                    if(cd!=null)
                    {
                        cd.MessageLOG = new List<string>(output);
                    }
                }
                if (ok && string.IsNullOrEmpty(msgBack))
                {
                    callback.SessionFinished("Reading chip finished successfully");
                    callback.ResultChip(cd);
                }
                else
                {
                    callback.ErrorRaised("Error reading chip: " + msgBack);
                    callback.SessionFinished("Review errors");
                    callback.ResultChip(cd);
                }
            }
        }

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
                catch(Exception ex)
                {
                    msgBack = ex.Message;
                }
            string temp = string.Empty;
            output.ForEach(q => temp = temp+Environment.NewLine+ q);
            msgBack = temp;
            return ok;
            
        }

       

        public bool CheckPythonPrerequisite(ref string msgBack)
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
                process.StartInfo.Arguments = "/c \"" + "mrz.exe --version " + "\"";

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
              

        public bool InvokeMasterlistupdate(ref string message)
        {
            try
            {
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument doc = hw.Load(ConfigurationSettings.AppSettings["CSCAMasterlist"]);
                string relativeUrl = string.Empty;
                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {
                    string href = link.Attributes["href"].Value;
                    if (href.Contains("GermanMasterList.zip"))
                    {
                        relativeUrl = href;
                        break;
                    }
                }
                if(relativeUrl=="")
                {
                    message = "No valid url with CSCA found";
                    return false;
                }
                else
                {
                    string aboluteUrl =  "https://bsi.bund.de"+relativeUrl;
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(aboluteUrl, ConfigurationSettings.AppSettings["JavaReaderPath"] + "CSCAMasterlist.zip");
                        callback.CSCADownloaded("CSCAMasterlist.zip");
                    }

                    System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(new FileStream(ConfigurationSettings.AppSettings["JavaReaderPath"] + "CSCAMasterlist.zip",FileMode.Open), System.IO.Compression.ZipArchiveMode.Read);
                    foreach(System.IO.Compression.ZipArchiveEntry zae in zip.Entries)
                    {
                        System.IO.Compression.DeflateStream ds = (System.IO.Compression.DeflateStream)zae.Open();

                        MemoryStream ms = new MemoryStream();
                        ds.CopyTo(ms);

                        File.WriteAllBytes(ConfigurationSettings.AppSettings["JavaReaderPath"] + "CSCAMasterlist.ml", ms.ToArray());
                        callback.CSCAUnzipped("CSCAMasterlist.ml");
                        break;
                    }

                    bool res = ParseMasterlist();
                    if(res)
                    {
                        callback.CSCAParsed("masterlist-content-current.data");
                        message = "Certificates extracted";
                    }
                    else
                    {
                        message = "Error extracting certificates";
                    }

                    return true;
                }
                

                
            }
            catch(Exception ex)
            {
                message = "Error updating masterlist: " + ex.Message;
                return false;

            }
        }

        private bool ParseMasterlist()
        {
            X509ContentType tst = X509Certificate2.GetCertContentType(File.ReadAllBytes(ConfigurationSettings.AppSettings["JavaReaderPath"] + "CSCAMasterlist.ml"));
            if (tst == X509ContentType.Pkcs7)
            {
                SignedCms signerInfo = new SignedCms();
                signerInfo.Decode(File.ReadAllBytes(ConfigurationSettings.AppSettings["JavaReaderPath"] + "CSCAMasterlist.ml"));

                if (signerInfo.ContentInfo.ContentType.Value == "2.23.136.1.1.2")
                {
                    byte[] cscaMLArr = signerInfo.ContentInfo.Content;
                    File.WriteAllBytes(ConfigurationSettings.AppSettings["JavaReaderPath"] + "masterlist-content-current.data", cscaMLArr);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

    }
}

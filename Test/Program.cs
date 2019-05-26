using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                PassCallback pccallback = new PassCallback();
                InstanceContext ic = new InstanceContext(pccallback);

                PassService.PassClient pc = new PassService.PassClient(ic);
                pc.Open();

                //csca
                //string m = string.Empty;
                //pc.InvokeMasterlistupdate(ref m);
                //Console.WriteLine(m);
                //Console.ReadLine();

                string kom = string.Empty;
                bool res = pc.CheckJavaPrerequisite(ref kom);
                Console.WriteLine(kom);
                Console.WriteLine("JAVA status: "+res.ToString());

                kom = string.Empty;
                res = pc.CheckPythonPrerequisite(ref kom);
                Console.WriteLine(kom);
                Console.WriteLine("PYTHON with PassportEye plugin status: " + res.ToString());

                /*byte[] img = File.ReadAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)+"\\pass_small.jpg");
                
                string msg = string.Empty;
                
                PassService.PersonalDataVIZ pdv = pc.ExtractMrzData(img);
                if (pdv != null)
                {
                    if (string.IsNullOrEmpty(pdv.Errors))
                    {
                        Console.WriteLine("Document: " + pdv.DocumentNumber);

                        string docNumber = string.Empty;
                        string dob = string.Empty;
                        string validityPer = string.Empty;

                        if(pdv.DocNumberChecksum)
                        {
                            docNumber = pdv.DocumentNumber;
                        }
                        else
                        {
                            Console.WriteLine("Doc number seems to be mispelled: " + pdv.DocumentNumber + Environment.NewLine + "Please provide proper value and hit enter: ");
                            docNumber = Console.ReadLine();
                        }

                        if(pdv.DOBChecksum)
                        {
                            dob = pdv.DateOfBirth;
                        }
                        else
                        {
                            Console.WriteLine("Date of birth seems to be mispelled: " + pdv.DateOfBirth + Environment.NewLine + "Please provide proper value and hit enter: ");
                            docNumber = Console.ReadLine();
                        }

                        if(pdv.ExpirationChecksum)
                        {
                            validityPer = pdv.ExpirationDate;
                        }
                        else
                        {
                            Console.WriteLine("Validity period seems to be mispelled: " + pdv.ExpirationDate + Environment.NewLine + "Please provide proper value and hit enter: ");
                            validityPer = Console.ReadLine();
                        }

                        pc.ReadChip(new PassService.KeyData() { DOB = dob, ValidityDate = validityPer, DocNumber = docNumber });
                        Console.WriteLine("Process finished");
                    }
                    else
                    {
                        Console.WriteLine("Error parsing mrz data: " + pdv.Errors);
                    }
                }

                pc.Close();
                */
                
                Console.ReadLine();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}

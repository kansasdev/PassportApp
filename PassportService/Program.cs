using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PassportService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (!Environment.UserInteractive)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new PassService()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {

                try
                {
                    PassService ps = new PassService();
                    ServiceHost serviceHost = new ServiceHost(typeof(Pass));

                    // Open the ServiceHostBase to create listeners and start 
                    // listening for messages.
                    serviceHost.Open();
                    Console.WriteLine("PassportService Started");
                    Console.WriteLine("You may use DocReader now");
                    Console.WriteLine("PassportService is not sending any data outside of your computer");
                    Console.WriteLine("PassportService is using following, great libraries: ");
                    Console.WriteLine("JMRTD,Tesseract-OCR, PassportEye, BouncyCastle, scuba-smartcards,ef-cvca,jai-imageio");
                    Console.WriteLine("Press Enter to stop");
                    Console.ReadLine();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.ReadLine();
                }
            }
        }
    }
}

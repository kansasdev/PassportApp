using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.PassService;

namespace Test
{
    public class PassCallback : PassService.IPassCallback
    {
       
        public void DocChecksPerformed()
        {
            Console.WriteLine("ChecksPErfomed");
        }

        public void ErrorRaised(string err)
        {
            Console.WriteLine("ErrorRaised: "+err);
        }

        public void ImageFromChipTaken(byte[] img)
        {
            Console.WriteLine("ImageTaken");
        }

        public void MrzFromChipTaken(string mrz)
        {
            Console.WriteLine("MRZTaken: "+mrz);
        }
        
        public void ResultChip(ChipData cd)
        {
            Console.WriteLine("END");
            if (cd.MessageLOG != null)
            {
                foreach (string line in cd.MessageLOG)
                {
                    Console.WriteLine(line);
                }
            }
        }

        public void SessionFinished(string messages)
        {
            Console.WriteLine("Session finished: " + messages);
        }

        public void SessionOpened()
        {
            Console.WriteLine("SessionOpened");
        }

        //CSCA steps

        public void CSCADownloaded(string messages)
        {
            Console.WriteLine("CSCA masterlist has been downloaded " + messages);
        }

        public void CSCAUnzipped(string message)
        {
            Console.WriteLine("CSCA unzipped and written successfully " + message);
        }

        public void CSCAParsed(string message)
        {
            Console.WriteLine("CSCA certificates extracted " + message);
        }
    }
}

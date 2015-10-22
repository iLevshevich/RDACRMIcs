using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace RDACRMIcs
{
    class Program
    {
        static void Main(string[] cmdLine)
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new RDACRMIcs(cmdLine)
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                Log.error("Program::Main()", ex);
            }
        }
    }
}

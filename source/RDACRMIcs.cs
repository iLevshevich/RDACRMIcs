using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RDACRMIcs
{
    partial class RDACRMIcs : ServiceBase
    {
        public RDACRMIcs(string[] cmdLine)
        {
            try
            {
                InitializeComponent();
                {
                    Options options = Singleton<Options>.Instance;
                    options.init(cmdLine);
                }
            }
            catch (Exception ex)
            {
                Log.error("RDACRMIcs::RDACRMIcs()", ex);
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                TCPServer server = Singleton<TCPServer>.Instance;
                server.start();
            }
            catch (Exception ex)
            {
                Log.error("RDACRMIcs::OnStart()", ex);
            }
        }

        protected override void OnStop()
        {
            try
            {
                TCPServer server = Singleton<TCPServer>.Instance;
                server.stop();
            }
            catch (Exception ex)
            {
                Log.error("RDACRMIcs::OnStop()", ex);
            }
        }
    }
}

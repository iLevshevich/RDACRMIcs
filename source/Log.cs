using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RDACRMIcs
{
    class Log
    {
        private static String module_name = "RDACRMIcs";

        public static void error(String fn, Exception ex)
        {
            String error_message = String.Format("Error: {0}->{1}", fn, getExceptionMessage(ex));
            EventLog.WriteEntry(module_name, error_message, EventLogEntryType.Error);
        }

        public static void warning(String fn, Exception ex)
        {
            String warning_message = String.Format("Warning: {0}->{1}", fn, getExceptionMessage(ex));
            EventLog.WriteEntry(module_name, warning_message, EventLogEntryType.Warning);
        }

        public static void information(String fn, Exception ex)
        {
            String information_message = String.Format("Information: {0}->{1}", fn, getExceptionMessage(ex));
            EventLog.WriteEntry(module_name, information_message, EventLogEntryType.Information);
        }

        public static String getExceptionMessage(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            do
            {
                sb.AppendLine(ex.Message);

                ex = ex.InnerException;
            } while (ex != null);

            return sb.ToString();
        }
    }
}

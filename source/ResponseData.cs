using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RDACRMIcs
{
    [Serializable]
    public class ResponseData
    {
        public Guid security { get; set; }
        public Guid id { get; set; }
        public String result { get; set; }
        public String console { get; set; }
        public String scope { get; set; }
    }
}

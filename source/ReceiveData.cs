using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RDACRMIcs
{
    [Serializable]
    public class ReceiveData
    {
        public Guid security { get; set; }
        public Guid id { get; set; }
        public String script { get; set; }
    }
}

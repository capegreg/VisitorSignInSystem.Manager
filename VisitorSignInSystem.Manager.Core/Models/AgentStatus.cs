using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class AgentStatus
    {
        public string AuthName { get; set; }
        public string FullName { get; set; }
        public string LastName { get; set; }
        public string StatusName { get; set; }
        public ulong Categories { get; set; }
        public int? VisitorId { get; set; }
        public string Counter { get; set; }

    }
}

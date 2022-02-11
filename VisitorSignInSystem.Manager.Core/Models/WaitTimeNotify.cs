using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class WaitTimeNotify
    {
        public string Mail { get; set; }
        public ulong Category { get; set; }
        public int MaxWaitTimeMinutes { get; set; }
    }
}

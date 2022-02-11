using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class GroupDevices
    {
        public int Id { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Location { get; set; }
        public bool CanReceive { get; set; }
        public bool CanSend { get; set; }
        public bool Enabled { get; set; }
        public DateTime Created { get; set; }
    }
}

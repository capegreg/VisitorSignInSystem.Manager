using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class Counter
    {
        public string Host { get; set; }
        public string CounterNumber { get; set; }
        public string Description { get; set; }
        public string DisplayDescription { get; set; }
        public sbyte? Location { get; set; }
        public string Floor { get; set; }
        public bool IsHandicap { get; set; }
        public bool IsAvailable { get; set; }
        public ulong Category { get; set; }
        public string Icon { get; set; }
        public DateTime Created { get; set; }
    }
}

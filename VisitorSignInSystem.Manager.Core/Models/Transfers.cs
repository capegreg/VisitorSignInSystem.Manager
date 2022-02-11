using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class Transfers
    {
        public ulong Id { get; set; }
        public int Department { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }
        public string Icon { get; set; }
        public sbyte Location { get; set; }
        public DateTime Created { get; set; }
    }
}

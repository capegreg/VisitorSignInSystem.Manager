using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class AdminTypes
    {
        public string Name { get; set; }

        public ICollection<AdminList> Admin { get; set; }
    }
}

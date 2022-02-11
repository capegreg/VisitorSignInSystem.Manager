using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class VsisUser
    {
        public string AuthName { get; set; }
        public string FullName { get; set; }
        public string LastName { get; set; }
        public int Department { get; set; }
        public string DepartmentName { get; set; }
        // category Id used for combo and must match value for categories Id
        //public ulong Id { get; set; }
        public int Categories { get; set; }
        public string Role { get; set; }
        public bool? Active { get; set; }
        public sbyte Location { get; set; }
        public DateTime Created { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class Categories
    {
        public ulong Id { get; set; }
        public ulong VisitCategoryId { get; set; }
        public string Description { get; set; }
        public sbyte DepartmentId { get; set; }
        public bool? Active { get; set; }
        public string Icon { get; set; }
        public sbyte Location { get; set; }
        public DateTime Created { get; set; }
    }
}

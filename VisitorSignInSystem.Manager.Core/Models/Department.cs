using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public partial class Department
    {
        public Department()
        {
        }

        // Changed Id, OrderBy sbyte types to int because edits to sbyte in datagrid is hidden in cell
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string Symbol { get; set; }
        public string SymbolType { get; set; }
        public int OrderBy { get; set; }
        public DateTime Created { get; set; }

    }
}

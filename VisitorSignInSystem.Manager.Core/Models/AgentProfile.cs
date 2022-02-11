using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class AgentProfile : AgentStatus
    {
        public string DepartmentName { get; set; }
        public sbyte Department { get; set; }
        public string CategoriesDescription { get; set; }
        public ulong VisitCategoryId { get; set; }
        public string VisitorName { get; set; }
        public string Role { get; set; }
        public bool? Active { get; set; }
        public sbyte Location { get; set; }
        public int VisitorsToday { get; set; }
        public int VisitorsWtd { get; set; }
        public int VisitorsMtd { get; set; }
        public int VisitorsYtd { get; set; }
        public double CallTimeToday { get; set; }
        public double CallTimeWtd { get; set; }
        public double CallTimeMtd { get; set; }
        public double CallTimeYtd { get; set; }

        public string AgentStatusGlyph { get; set; }
        public string AgentStatusGlyphColor { get; set; }


    }
}

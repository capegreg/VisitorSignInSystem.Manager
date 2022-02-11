using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class CounterDetail
    {
        public string Host { get; set; }
        public string CounterNumber { get; set; }
        public string Description { get; set; }
        public string CounterStatus { get; set; }
        public string AgentFullName { get; set; }
        public int? VisitorId { get; set; }
        public string CounterStatusGlyph { get; set; }
        public string CounterStatusGlyphColor { get; set; }
        //public char CounterStatusSymbol => (char)CounterStatusSymbolCode;
        //public int CounterStatusSymbolCode { get; set; }
        //public string CounterStatusSymbolColor { get; set; }

    }
}
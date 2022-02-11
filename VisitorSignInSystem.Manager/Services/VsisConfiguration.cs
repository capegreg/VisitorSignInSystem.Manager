using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Services
{
    public class VsisConfiguration
    {
        public string Host { get; set; }
        public string AuthName { get; set; }
        public string AuthFullName { get; set; }
        public sbyte Location { get; set; }
        public bool IsAppConfigured { get; set; }
        public bool ShowToastNotification { get; set; }
    }
}

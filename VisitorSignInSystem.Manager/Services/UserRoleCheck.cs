using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Services
{
    public class UserRoleCheck : INotifyPropertyChanged
    {
        bool _isUserInRole = true;

        public bool IsUserInRole
        {
            get { return _isUserInRole; }
            set
            {
                _isUserInRole = value;
                NotifyPropertyChanged("_isUserInRole");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

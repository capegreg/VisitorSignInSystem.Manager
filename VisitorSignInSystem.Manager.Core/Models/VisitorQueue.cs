using System;
using System.ComponentModel;
using System.Threading;
using System.Timers;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class VisitorQueue : INotifyPropertyChanged
    {
        public int _Id { get; set; }
        public string _FirstName { get; set; }
        public string _LastName { get; set; }
        public string _FullName { get; set; }
        public string _Kiosk { get; set; }
        public int _Location { get; set; }
        public string _Mobile { get; set; }
        public bool _IsHandicap { get; set; }
        public bool _IsTransfer { get; set; }
        public ulong _VisitCategoryId { get; set; }
        public string _StatusName { get; set; }
        public string _AssignedCounter { get; set; }
        public string _CounterDescription { get; set; }
        //public bool _AgentIsAvailable { get; set; }
        public DateTime _Created { get; set; }
        public DateTime? _CalledTime { get; set; }
        public int _MaxWaitTime { get; set; }
        public string _CategoryDescription { get; set; }
        public string _WaitDuration { get; set; }
        public string _HandicapIcon { get; set; }
        public string _TransferIcon { get; set; }
        public string _StatusIcon { get; set; }
        public string VisitorStatusGlyph { get; set; }
        public string VisitorStatusGlyphColor { get; set; }
        public string VisitorTransferGlyph { get; set; }
        public string VisitorTransferGlyphColor { get; set; }
        public string VisitorHandicapGlyph { get; set; }
        public string VisitorHandicapGlyphColor { get; set; }
        public char Symbol => (char)SymbolCode;
        public int _SymbolCode { get; set; }

        public int SymbolCode
        {
            get { return _SymbolCode; }
            set
            {
                _SymbolCode = value;
                NotifyPropertyChanged("_SymbolCode");
            }
        }
        public int Id
        {
            get { return _Id; }
            set
            {
                _Id = value;
                NotifyPropertyChanged("_Id");
            }
        }
        public string FirstName
        {
            get { return _FirstName; }
            set
            {
                _FirstName = value;
                NotifyPropertyChanged("_FirstName");
            }
        }
        public string LastName
        {
            get { return _LastName; }
            set
            {
                _LastName = value;
                NotifyPropertyChanged("_LastName");
            }
        }
        public string FullName
        {
            get { return _FullName; }
            set
            {
                _FullName = value;
                NotifyPropertyChanged("_FullName");
            }
        }
        public string Kiosk
        {
            get { return _Kiosk; }
            set
            {
                _Kiosk = value;
                NotifyPropertyChanged("_Kiosk");
            }
        }
        public int Location
        {
            get { return _Location; }
            set
            {
                _Location = value;
                NotifyPropertyChanged("_Location");
            }
        }
        public string Mobile
        {
            get { return _Mobile; }
            set
            {
                _Mobile = value;
                NotifyPropertyChanged("_Mobile");
            }
        }
        public bool IsHandicap
        {
            get { return _IsHandicap; }
            set
            {
                _IsHandicap = value;
                NotifyPropertyChanged("_IsHandicap");
            }
        }
        public bool IsTransfer
        {
            get { return _IsTransfer; }
            set
            {
                _IsTransfer = value;
                NotifyPropertyChanged("_IsTransfer");
            }
        }
        public ulong VisitCategoryId
        {
            get { return _VisitCategoryId; }
            set
            {
                _VisitCategoryId = value;
                NotifyPropertyChanged("_VisitCategoryId");
            }
        }
        public string StatusName
        {
            get { return _StatusName; }
            set
            {
                _StatusName = value;
                NotifyPropertyChanged("_StatusName");
            }
        }
        public string AssignedCounter
        {
            get { return _AssignedCounter; }
            set
            {
                _AssignedCounter = value;
                NotifyPropertyChanged("_AssignedCounter");
            }
        }
        public string CounterDescription
        {
            get { return _CounterDescription; }
            set
            {
                _CounterDescription = value;
                NotifyPropertyChanged("_CounterDescription");
            }
        }
        //public bool AgentIsAvailable
        //{
        //    get { return _AgentIsAvailable; }
        //    set
        //    {
        //        _AgentIsAvailable = value;
        //        NotifyPropertyChanged("_AgentIsAvailable");
        //    }
        //}

        public DateTime Created
        {
            get { return _Created; }
            set
            {
                _Created = value;
                NotifyPropertyChanged("_Created");
            }
        }
        public DateTime? CalledTime
        {
            get { return _CalledTime; }
            set
            {
                _CalledTime = value;
                NotifyPropertyChanged("_CalledTime");
            }
        }

        public int MaxWaitTime
        {
            get { return _MaxWaitTime; }
            set
            {
                _MaxWaitTime = value;
                NotifyPropertyChanged("_MaxWaitTime");
            }
        }
        public string CategoryDescription
        {
            get { return _CategoryDescription; }
            set
            {
                _CategoryDescription = value;
                NotifyPropertyChanged("_CategoryDescription");
            }
        }
        public string HandicapIcon
        {
            get { return _HandicapIcon; }
            set
            {
                _HandicapIcon = value;
                NotifyPropertyChanged("_HandicapIcon");
            }
        }
        public string TransferIcon
        {
            get { return _TransferIcon; }
            set
            {
                _TransferIcon = value;
                NotifyPropertyChanged("_TransferIcon");
            }
        }
        public string StatusIcon
        {
            get { return _StatusIcon; }
            set
            {
                _StatusIcon = value;
                NotifyPropertyChanged("_StatusIcon");
            }
        }
        
        public string WaitDuration
        {
            get { return _WaitDuration; }
            set
            {
                _WaitDuration = value;
                NotifyPropertyChanged("_WaitDuration");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}

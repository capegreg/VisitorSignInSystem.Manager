using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Core.Models
{
    public class NotificationItem
    {
        public string _messageType { get; set; }
        public string _groupDescription { get; set; }
        public string _messageText { get; set; }
        public string _messageTag { get; set; }
        public int _messageCount { get; set; }

        public int MessageCount { get; set; }

        public string MessageType
        {
            get
            {
                return _messageType;
            }
            set
            {
                _messageType = value;
            }
        }
        public string MessageText
        {
            get
            {
                return _messageText;
            }
            set
            {
                _messageText = value;
            }
        }
        public string MessageTag
        {
            get
            {
                return _messageTag;
            }
            set
            {
                _messageTag = value;
            }
        }
        public string GroupDescription
        {
            get
            {
                return _groupDescription;
            }
            set
            {
                _groupDescription = value;
            }
        }

    }
}

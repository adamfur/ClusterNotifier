using System;

namespace Notifier
{
    public class NotifyMessage
    {
        public Guid ApplicationId { get; set; } // application token
        public Guid ApplicationInstanceId { get; set; } // unique application instance id
        public EventType Type { get; set; } // type of event
        public int Started { get; set; } // first always becomes master
    }
}
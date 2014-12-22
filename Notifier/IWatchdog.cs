using System;

namespace Notifier
{
    public interface IWatchdog
    {
        void SetTimeout(TimeSpan argTimeout);
    }
}
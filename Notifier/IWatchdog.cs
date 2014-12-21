using System;

namespace Notifier
{
    public interface IWatchdog
    {
        void Start();
        void SetTimeout(TimeSpan argTimeout);
    }
}
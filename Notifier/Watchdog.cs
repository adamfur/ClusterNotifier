using System;
using System.Threading;
using System.Threading.Tasks;

namespace Notifier
{
    public class Watchdog : IWatchdog
    {
        private readonly IWatchdogEventReceiver _logic;

        public Watchdog(IWatchdogEventReceiver argLogic)
        {
            _logic = argLogic;
        }

        public void SetTimeout(TimeSpan argTimeout)
        {
            Task.Run(() =>
            {
                Thread.Sleep(argTimeout);
                _logic.Interrupt();
            });
        }
    }
}
using System;
using System.Threading;

namespace Notifier
{
    public interface IWatchdogEventReceiver
    {
        void Interrupt();
    }

    public class Watchdog : IWatchdog
    {
        private readonly IWatchdogEventReceiver _logic;
        private readonly Thread _thread;
        private readonly IPriorityQueue<DateTime, DateTime> _interruptTable = new PriorityQueue<DateTime, DateTime>();

        public Watchdog(IWatchdogEventReceiver argLogic)
        {
            _logic = argLogic;
            _thread = new Thread(Run);
        }

        public void Start()
        {
            _thread.Start();
        }

        public void SetTimeout(TimeSpan argTimeout)
        {
            DateTime deadline = SystemTime.Now.Add(argTimeout);

            Log("Set interrupt: " + deadline);
            lock (_interruptTable)
            {
                _interruptTable.Enqueue(deadline, deadline);
                Monitor.Pulse(_interruptTable);
            }
            _thread.Interrupt();
        }

        private void Run()
        {
            while (true)
            {
                try
                {
                    DateTime deadline = SystemTime.Now;

                    lock (_interruptTable)
                    {
                        while (_interruptTable.Empty)
                        {
                            Monitor.Wait(_interruptTable);
                        }

                        while (!_interruptTable.Empty && _interruptTable.Top < SystemTime.Now)
                        {
                            _interruptTable.Dequeue();
                        }

                        if (!_interruptTable.Empty)
                        {
                            deadline = _interruptTable.Top;
                        }
                    }

                    TimeSpan timeSpan = deadline.Subtract(SystemTime.Now);

                    Wait(timeSpan);
                    _logic.Interrupt();
                    Log("[interrupt]");
                }
                catch (ThreadInterruptedException)
                {
                }
            }
        }

        private static void Wait(TimeSpan argTimeSpan)
        {
            if (argTimeSpan > TimeSpan.Zero)
            { 
                Thread.Sleep(argTimeSpan);
            }
        }

        private static void Log(string argText)
        {
            Console.WriteLine("{0}: {1}", SystemTime.Now, argText);
        }
    }
}
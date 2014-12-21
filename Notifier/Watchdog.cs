using System;
using System.Threading;

namespace Notifier
{
    public class Watchdog
    {
        private readonly Thread _logic;
        private readonly Thread _thread;
        private readonly PriorityQueue<DateTime, DateTime> _interruptTable = new PriorityQueue<DateTime, DateTime>();

        public Watchdog(Thread argLogic)
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

            lock (_interruptTable)
            {
                _interruptTable.Enqueue(deadline, deadline);
            }
            _thread.Interrupt();
        }

        private void Run()
        {
            while (true)
            {
                try
                {
                    DateTime dateTime = DateTime.MinValue;

                    lock (_interruptTable)
                    {
                        ConsumeExpiredDeadlines();

                        if (!_interruptTable.Empty)
                        {
                            dateTime = _interruptTable.Top;
                        }
                    }

                    TimeSpan timeSpan = dateTime.Subtract(SystemTime.Now);

                    Wait(timeSpan);
                    _logic.Interrupt();
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
            else
            {
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private void ConsumeExpiredDeadlines()
        {
            while (!_interruptTable.Empty && _interruptTable.Top < SystemTime.Now)
            {
                _interruptTable.Dequeue();
            }
        }
    }
}
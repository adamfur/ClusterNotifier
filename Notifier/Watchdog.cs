using System;
using System.Threading;

namespace Notifier
{
    public interface IWatchdog
    {
        void Start();
        void SetTimeout(TimeSpan argTimeout);
    }

    public class Watchdog : IWatchdog
    {
        private readonly Thread _logic;
        private readonly Thread _thread;
        private readonly IPriorityQueue<DateTime, DateTime> _interruptTable = new PriorityQueue<DateTime, DateTime>();

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

            Console.WriteLine("Set interrupt: " + deadline);
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
                    Console.WriteLine("[interrupt]");
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
    }
}
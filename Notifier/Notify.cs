using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace Notifier
{
    public class Notify
    {
        private readonly Action _action;
        private DateTime _lastHeartbeat = DateTime.Now;
        private DateTime _stateTimestamp;
        private NotifyState _state;
        private readonly int _started = new Random().Next();
        private readonly Guid _applicationId = Guid.Parse("E94A2C93-BE87-4E0D-A295-1E764DFA4D7A");
        private readonly Guid _applicationInstanceId = Guid.NewGuid();
        private readonly Thread _listener;
        private readonly Thread _logic;
        private readonly Thread _watchdog;
        private readonly Queue<NotifyMessage> _queue = new Queue<NotifyMessage>();
        private readonly NotifyClient _client;
        private const int ClaimMasterDelay = 5;
        private const int WaitSecondsIfNoHeartbeats = 10;
        private const int HeartbeatDelay = 3;
        private readonly PriorityQueue<DateTime, DateTime> _interruptTable = new PriorityQueue<DateTime, DateTime>();

        public Notify(Action argAction)
        {
            _action = argAction;
            _listener = new Thread(Listen);
            _logic = new Thread(Run);
            _watchdog = new Thread(Watchdog);
            _client = new NotifyClient(_applicationId, _applicationInstanceId, _started);
        }

        public void Start()
        {
            _listener.Start();
            _logic.Start();
            _watchdog.Start();
        }

        private void Listen()
        {
            var server = new NotifyServer();

            while (true)
            {
                try
                {
                    var message = server.Read();

                    lock (_queue)
                    {
                        _queue.Enqueue(message);
                        Monitor.PulseAll(_queue);
                    }
                }
                catch (JsonSerializationException)
                {
                }
            }
        }

        private void Run()
        {
            Console.WriteLine("Trying to become master, waiting 5 seconds to see if another one claims it.");
            _state = NotifyState.TryPromoteToMaster;
            _stateTimestamp = DateTime.Now;
            _client.PromoteSelf();

            SetTimeout(TimeSpan.FromSeconds(ClaimMasterDelay));
            lock (_queue)
            {
                while (true)
                {
                    try
                    {
                        while (_queue.Count == 0)
                        {
                            Monitor.Wait(_queue);
                        }

                        var message = _queue.Dequeue();

                        if (message.ApplicationInstanceId == _applicationInstanceId || message.ApplicationId != _applicationId)
                        {
                            continue;
                        }

                        if (message.Type == EventType.Heartbeat)
                        {
                            Console.WriteLine("Received heartbeat from master: " + message.ApplicationInstanceId);
                            _lastHeartbeat = DateTime.Now;
                        }

                        if (message.Type == EventType.Notify && _state == NotifyState.Master)
                        {
                            _action();
                        }
                        else if (_state == NotifyState.TryPromoteToMaster && message.Type == EventType.Heartbeat)
                        {
                            _state = NotifyState.Slave;
                            SetTimeout(TimeSpan.FromSeconds(WaitSecondsIfNoHeartbeats));
                        }
                        else if (_state == NotifyState.PreliminaryMaster && message.Type == EventType.Heartbeat && message.Started < _started)
                        {
                            Console.WriteLine("Other server is master: " + message.ApplicationInstanceId);
                            _state = NotifyState.Slave;
                            SetTimeout(TimeSpan.FromSeconds(WaitSecondsIfNoHeartbeats));
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        // No one has claimed the crown
                        if (_state == NotifyState.TryPromoteToMaster && _stateTimestamp.AddSeconds(ClaimMasterDelay) < DateTime.Now)
                        {
                            Console.WriteLine("Is now PreliminaryMaster");
                            _state = NotifyState.PreliminaryMaster;
                            _client.Heartbeat();
                            SetTimeout(TimeSpan.FromSeconds(HeartbeatDelay));
                        }
                        else if (_state == NotifyState.Master)
                        {
                            _client.Heartbeat();
                            SetTimeout(TimeSpan.FromSeconds(HeartbeatDelay));
                        }
                        else if (_state == NotifyState.Slave && _lastHeartbeat.AddSeconds(WaitSecondsIfNoHeartbeats) < DateTime.Now)
                        {
                            _state = NotifyState.TryPromoteToMaster;
                            _client.PromoteSelf();
                            SetTimeout(TimeSpan.FromSeconds(ClaimMasterDelay));
                        }
                        else if (_state == NotifyState.PreliminaryMaster)
                        {
                            Console.WriteLine("Master");
                            _state = NotifyState.Master;
                            _client.Heartbeat();
                            SetTimeout(TimeSpan.FromSeconds(HeartbeatDelay));
                        }
                    }
                }
            }
        }

        private void SetTimeout(TimeSpan timeout)
        {
            DateTime deadline = DateTime.Now.Add(timeout);

            lock (_interruptTable)
            {
                _interruptTable.Enqueue(deadline, deadline);
            }
            _watchdog.Interrupt();
        }

        private void Watchdog()
        {
            while (true)
            {
                try
                {
                    DateTime dateTime = DateTime.MinValue;

                    lock (_interruptTable)
                    {
                        while (!_interruptTable.Empty && _interruptTable.Top < DateTime.Now)
                        {
                            _interruptTable.Dequeue();
                        }

                        if (!_interruptTable.Empty)
                        {
                            dateTime = _interruptTable.Top;
                        }
                    }

                    TimeSpan timeSpan = dateTime.Subtract(DateTime.Now);

                    if (timeSpan > new TimeSpan())
                    {
                        Thread.Sleep(timeSpan);
                    }
                    else
                    {
                        Thread.Sleep(100000);
                    }
                    _logic.Interrupt();
                }
                catch (ThreadInterruptedException)
                {
                }
            }
        }
    }
}
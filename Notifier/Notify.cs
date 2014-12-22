using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace Notifier
{
    public class Notify : IWatchdogEventReceiver
    {
        private readonly int _started = new Random().Next();
        private readonly Guid _applicationId = Guid.Parse("E94A2C93-BE87-4E0D-A295-1E764DFA4D7A");
        private readonly Guid _applicationInstanceId = Guid.NewGuid();
        private readonly Thread _listener;
        private readonly Thread _logic;
        private readonly Queue<NotifyMessage> _queue = new Queue<NotifyMessage>();
        private readonly Watchdog _watchdog;
        private readonly NotifyStateMachine _stateMachine;

        public Notify(Action argAction)
        {
            _listener = new Thread(Listen);
            _logic = new Thread(Run);
            INotifyClient client = new NotifyClient(_applicationId, _applicationInstanceId, _started);
            _watchdog = new Watchdog(this);
            _stateMachine = new NotifyStateMachine(_watchdog, argAction, client, _started);
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
            _stateMachine.Start();
            lock (_queue)
            {
                while (true)
                {
                    while (_queue.Count == 0)
                    {
                        Monitor.Wait(_queue);
                    }

                    var message = _queue.Dequeue();

                    if (message == null)
                    {
                        _stateMachine.Trigger();
                        continue;
                    }

                    if (IsMessageFromSelf(message) || !IsSameApplication(message))
                    {
                        continue;
                    }

                    if (message.Type == EventType.Heartbeat)
                    {
                        _stateMachine.Heartbeat(message);
                    }
                    else if (message.Type == EventType.Notify)
                    {
                        _stateMachine.Notify();
                    }
                }
            }
        }

        private bool IsSameApplication(NotifyMessage argMessage)
        {
            return argMessage.ApplicationId == _applicationId;
        }

        private bool IsMessageFromSelf(NotifyMessage argMessage)
        {
            return argMessage.ApplicationInstanceId == _applicationInstanceId;
        }

        public void Interrupt()
        {
            lock (_queue)
            {
                _queue.Enqueue(null);
                Monitor.Pulse(_queue);
            }
        }
    }
}
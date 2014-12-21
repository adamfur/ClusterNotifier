using System;

namespace Notifier
{
    public class NotifyStateMachine
    {
        public Action Action { get; set; }
        private readonly IWatchdog _watchdog;
        private readonly INotifyClient _client;
        private readonly int _roll;
        private DateTime _promoteToMasterTimer;
        public NotifyState State { get; set; }
        private const int ClaimMasterDelay = 5;
        private const int WaitSecondsIfNoHeartbeats = 10;
        public DateTime StateTimestamp { get; set; }
        private const int HeartbeatDelay = 3;
        public DateTime LastHeartbeat { get; set; }

        public NotifyStateMachine(IWatchdog argWatchdog, Action argAction, INotifyClient argClient, int argRoll)
        {
            Action = argAction;
            _watchdog = argWatchdog;
            _client = argClient;
            _roll = argRoll;
            State = NotifyState.TryPromoteToMaster;
        }

        public void Heartbeat(NotifyMessage argMessage)
        {
            LastHeartbeat = SystemTime.Now;

            if (State == NotifyState.TryPromoteToMaster)
            {
                State = NotifyState.Slave;
                _promoteToMasterTimer = SystemTime.Now;
            }
            else if (State == NotifyState.PreliminaryMaster && argMessage.Started < _roll)
            {
                State = NotifyState.Slave;
            }

            _watchdog.SetTimeout(TimeSpan.FromSeconds(WaitSecondsIfNoHeartbeats));
        }

        public void Notify()
        {
            if (State == NotifyState.Master)
            {
                Action();
            }
        }

        public void Start()
        {
            State = NotifyState.TryPromoteToMaster;
            _client.PromoteSelf();
            _watchdog.SetTimeout(TimeSpan.FromSeconds(ClaimMasterDelay));
            StateTimestamp = SystemTime.Now;
        }

        public void Trigger()
        {
            if (State == NotifyState.TryPromoteToMaster && _promoteToMasterTimer.AddSeconds(WaitSecondsIfNoHeartbeats) < SystemTime.Now)
            {
                State = NotifyState.PreliminaryMaster;
                _client.Heartbeat();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(HeartbeatDelay));
            }
            else if (State == NotifyState.Master && LastHeartbeat.Add(TimeSpan.FromSeconds(HeartbeatDelay)) < SystemTime.Now)
            {
                LastHeartbeat = SystemTime.Now;
                _client.Heartbeat();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(HeartbeatDelay));
            }
            else if (State == NotifyState.Slave && LastHeartbeat.Add(TimeSpan.FromSeconds(HeartbeatDelay)) < SystemTime.Now)
            {
                State = NotifyState.TryPromoteToMaster;
                _client.PromoteSelf();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(ClaimMasterDelay));
            }
            else if (State == NotifyState.PreliminaryMaster && LastHeartbeat.Add(TimeSpan.FromSeconds(HeartbeatDelay)) < SystemTime.Now)
            {
                State = NotifyState.Master;
                _client.Heartbeat();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(HeartbeatDelay));
            }
        }
    }
}

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
        public DateTime StateTimestamp { get; set; }
        public DateTime LastHeartbeat { get; set; }

        private const int SecondToWaitBetweenPreliminaryMasterAndMaster = 5;
        private const int SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat = 10;
        private const int SecondsBetweenHeartbeats = 3;

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

            _watchdog.SetTimeout(TimeSpan.FromSeconds(SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat));
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
            _client.AttemptToBecomeMaster();
            _watchdog.SetTimeout(TimeSpan.FromSeconds(SecondToWaitBetweenPreliminaryMasterAndMaster));
            StateTimestamp = SystemTime.Now;
        }

        public void Trigger()
        {
            if (State == NotifyState.TryPromoteToMaster && _promoteToMasterTimer.AddSeconds(SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat) < SystemTime.Now)
            {
                State = NotifyState.PreliminaryMaster;
                _client.Heartbeat();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(SecondsBetweenHeartbeats));
            }
            else if (State == NotifyState.Master && LastHeartbeat.Add(TimeSpan.FromSeconds(SecondsBetweenHeartbeats)) < SystemTime.Now)
            {
                LastHeartbeat = SystemTime.Now;
                _client.Heartbeat();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(SecondsBetweenHeartbeats));
            }
            else if (State == NotifyState.Slave && LastHeartbeat.Add(TimeSpan.FromSeconds(SecondsBetweenHeartbeats)) < SystemTime.Now)
            {
                State = NotifyState.TryPromoteToMaster;
                _client.AttemptToBecomeMaster();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(SecondToWaitBetweenPreliminaryMasterAndMaster));
            }
            else if (State == NotifyState.PreliminaryMaster && LastHeartbeat.Add(TimeSpan.FromSeconds(SecondsBetweenHeartbeats)) < SystemTime.Now)
            {
                State = NotifyState.Master;
                _client.Heartbeat();
                _watchdog.SetTimeout(TimeSpan.FromSeconds(SecondsBetweenHeartbeats));
            }
        }
    }
}

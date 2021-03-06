﻿using System;
using Notifier;
using NSubstitute;
using NUnit.Framework;

namespace NotifierUnitTests
{
    [TestFixture]
    public class NotifyStateMachineTests
    {
        private NotifyStateMachine _machine;
        private DateTime _now;
        private IWatchdog _watchdog;
        private INotifyClient _client;

        [SetUp]
        public void SetUp()
        {
            _watchdog = Substitute.For<IWatchdog>();
            _client = Substitute.For<INotifyClient>();
            _machine = new NotifyStateMachine(_watchdog, () => { }, _client, 5);
            _now = DateTime.Now;
            SystemTime.Method = () => _now;
        }

        [Test]
        public void Heartbeat_Scenario_ExpectedResult()
        {
            _machine.Heartbeat(new NotifyMessage());

            Assert.That(_machine.LastHeartbeat, Is.EqualTo(_now));

            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat));
        }

        [Test]
        public void Heartbeat_DuringPromotion_ExpectedResult()
        {
            _machine.State = NotifyState.TryPromoteToMaster;

            _machine.Heartbeat(new NotifyMessage());

            Assert.That(_machine.State, Is.EqualTo(NotifyState.Slave));

            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat));
        }

        [Test]
        public void Heartbeat_OtherHasRightForMasterMore_ExpectedResult2()
        {
            _machine.State = NotifyState.PreliminaryMaster;

            _machine.Heartbeat(new NotifyMessage { Started = 3 });

            Assert.That(_machine.State, Is.EqualTo(NotifyState.Slave));
            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat));
        }

        [Test]
        public void Heartbeat_WeDeserveMasterMore_ExpectedResult2()
        {
            _machine.State = NotifyState.PreliminaryMaster;

            _machine.Heartbeat(new NotifyMessage { Started = 7 });

            Assert.That(_machine.State, Is.EqualTo(NotifyState.PreliminaryMaster));
        }

        [TestCase(NotifyState.Master, true)]
        [TestCase(NotifyState.PreliminaryMaster, false)]
        [TestCase(NotifyState.Slave, false)]
        [TestCase(NotifyState.TryPromoteToMaster, false)]
        public void Notify_Scenario_ExpectedResult(NotifyState state, bool expected)
        {
            bool value = false;

            _machine.Action = () => { value = true; };

            _machine.State = state;
            _machine.Notify();

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        public void Start_Scenario_ExpectedResult()
        {
            _machine.Start();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.TryPromoteToMaster));
            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondToWaitBetweenPreliminaryMasterAndMaster));
            _client.Received().AttemptToBecomeMaster();
        }

        [Test]
        public void Trigger_IsTryPromoteToMaster_ExpectedResult()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat));
            _machine.State = NotifyState.TryPromoteToMaster;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.PreliminaryMaster));
            _client.Received().Heartbeat();
            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
        }

        [Test]
        public void Trigger_IsMaster_ExpectedResult()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
            _machine.State = NotifyState.Master;

            _machine.Trigger();

            Assert.That(_machine.LastHeartbeat, Is.EqualTo(_now));
            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
        }

        [Test]
        public void Trigger_IsSlave_ExpectedResult()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat));
            _machine.State = NotifyState.Slave;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.TryPromoteToMaster));
            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondToWaitBetweenPreliminaryMasterAndMaster));
        }

        [Test]
        public void Trigger_IsPreliminaryMaster_ExpectedResult()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
            _machine.State = NotifyState.PreliminaryMaster;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.Master));
            _client.Received().Heartbeat();
            _watchdog.Received().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
        }

        [Test]
        public void Trigger_IsTryPromoteToMaster_ExpectedResult2()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsToWaitBeforeAttemptingBecomeMasterAfterHeartbeat)).AddSeconds(1);
            _machine.State = NotifyState.TryPromoteToMaster;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.TryPromoteToMaster));
            _client.DidNotReceive().Heartbeat();
            _watchdog.DidNotReceive().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
        }

        [Test]
        public void Trigger_IsMaster_ExpectedResult2()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats)).AddSeconds(1);
            _machine.State = NotifyState.Master;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.Master));
            _watchdog.DidNotReceive().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
        }

        [Test]
        public void Trigger_IsSlave_ExpectedResult2()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondToWaitBetweenPreliminaryMasterAndMaster)).AddSeconds(1);
            _machine.State = NotifyState.Slave;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.Slave));
            _watchdog.DidNotReceive().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondToWaitBetweenPreliminaryMasterAndMaster));
        }

        [Test]
        public void Trigger_IsPreliminaryMaster_ExpectedResult2()
        {
            _machine.LastHeartbeat = SystemTime.Now.Add(-TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats)).AddSeconds(1);
            _machine.State = NotifyState.PreliminaryMaster;

            _machine.Trigger();

            Assert.That(_machine.State, Is.EqualTo(NotifyState.PreliminaryMaster));
            _client.DidNotReceive().Heartbeat();
            _watchdog.DidNotReceive().SetTimeout(TimeSpan.FromSeconds(NotifyStateMachine.SecondsBetweenHeartbeats));
        }
    }
}

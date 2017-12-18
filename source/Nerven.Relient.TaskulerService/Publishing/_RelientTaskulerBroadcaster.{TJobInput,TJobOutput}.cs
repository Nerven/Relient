using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nerven.Assertion;
using Nerven.Relient.Publishing;
using Nerven.Taskuler;
using Nerven.Taskuler.Core;

namespace Nerven.Relient.TaskulerService.Publishing
{
    internal sealed class _RelientTaskulerBroadcaster<TJobInput, TJobOutput> : IDisposable
    {
        private const int _ScheduledTasksCount = 4;
        private const int _NumberOfPublishTries = 12;
        private const double _RetryIntervalExponent = 2.5;

        //// ReSharper disable once StaticMemberInGenericType
        private static readonly TimeSpan _ScheduleInterval = TimeSpan.FromSeconds(5);
        //// ReSharper disable once StaticMemberInGenericType
        private static readonly TimeSpan _RetryBaseInterval = TimeSpan.FromMinutes(1);
        
        private readonly IRelientPublisher<TJobInput, TJobOutput>[] _Publishers;
        private readonly ConcurrentQueue<_NotificationRecord> _NotificationsQueue;
        private readonly IDisposable _NotificationsSubscription;
        private readonly ITaskulerTaskHandle[] _Tasks;

        internal _RelientTaskulerBroadcaster(
            IRelientTaskulerService<TJobInput, TJobOutput> service,
            IRelientPublisher<TJobInput, TJobOutput>[] publishers)
        {
            _Publishers = publishers;

            _NotificationsQueue = new ConcurrentQueue<_NotificationRecord>();
            var _observer = new _NotificationsObserver(this);
            _NotificationsSubscription = service.AsRelientService().NotificationsSource.Subscribe(_observer);

            var _schedule = service.AddIntervalSchedule(nameof(RelientTaskulerBroadcaster), _ScheduleInterval);
            _Tasks = Enumerable.Range(0, _ScheduledTasksCount).Select(_i => _schedule.AddTask($"{nameof(RelientTaskulerBroadcaster)}[{_i}]", _Run)).ToArray();
        }

        private async Task<TaskulerTaskResponse> _Run(TaskulerTaskContext taskulerTaskContext, CancellationToken cancellationToken)
        {
            _NotificationRecord _record;
            while ((_record = _GetRecord(taskulerTaskContext)) != null)
            {
                var _response = await _HandleRecordAsync(taskulerTaskContext, _record, cancellationToken).ConfigureAwait(false);
                if (!_response.ContinueScheduling || _response.Error != null)
                {
                    return _response;
                }
            }

            return TaskulerTaskResponse.Continue();
        }

        private _NotificationRecord _GetRecord(TaskulerTaskContext taskulerTaskContext)
        {
            var _loopMax = _NotificationsQueue.Count;
            var _loopIndex = 0;
            do
            {
                if (!_NotificationsQueue.TryDequeue(out var _record))
                {
                    return null;
                }

                if (_record.Tries.Count != 0 &&
                    _record.Tries[_record.Tries.Count - 1] > taskulerTaskContext.Timestamp.Subtract(_GetRetryInterval(_record)))
                {
                    _NotificationsQueue.Enqueue(_record);
                }
                else
                {
                    return _record;
                }

                if (++_loopIndex == _loopMax)
                {
                    return null;
                }
            }
            while (true);
        }

        private TimeSpan _GetRetryInterval(_NotificationRecord record)
        {
            return TimeSpan.FromTicks((long)(_RetryBaseInterval.Ticks * Math.Pow(record.Tries.Count, _RetryIntervalExponent)));
        }

        private async Task<TaskulerTaskResponse> _HandleRecordAsync(TaskulerTaskContext taskulerTaskContext, _NotificationRecord record, CancellationToken cancellationToken)
        {
            RelientPublishResult _publishResult;
            try
            {
                record.NoteTry(taskulerTaskContext.Timestamp);
                _publishResult = await record.Publisher.HandleNotificationAsync(record.RelientNotification, record.Error, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception _error)
            {
                _Escalate(record, _error);
                return TaskulerTaskResponse.Continue(_error);
            }

            switch (_publishResult)
            {
                case RelientPublishResult.Ok:
                    return TaskulerTaskResponse.Continue();
                case RelientPublishResult.Fail when record.Tries.Count < _NumberOfPublishTries:
                    _NotificationsQueue.Enqueue(record);
                    return TaskulerTaskResponse.Continue();
                case RelientPublishResult.Fail:
                case RelientPublishResult.Panic:
                    _Escalate(record, null); // TODO: Exception
                    return TaskulerTaskResponse.Continue();
                default:
                    Must.Assertion.AssertNever();
                    //// ReSharper disable once HeuristicUnreachableCode
                    return default(TaskulerTaskResponse); // TODO
            }
        }

        private void _Escalate(_NotificationRecord record, Exception error)
        {
            if (!record.Escalated)
            {
                var _error = record.Error != null && error != null
                    ? new AggregateException(record.Error, error).Flatten()
                    : record.Error ?? error;

                foreach (var _publisher in _Publishers)
                {
                    if (!ReferenceEquals(_publisher, record.Publisher))
                    {
                        _NotificationsQueue.Enqueue(new _NotificationRecord(_publisher, record.RelientNotification, true, _error));
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var _task in _Tasks)
            {
                _task.Remove();
            }

            _NotificationsSubscription.Dispose();
        }

        private sealed class _NotificationsObserver : IObserver<RelientNotification<TJobInput, TJobOutput>>
        {
            private readonly _RelientTaskulerBroadcaster<TJobInput, TJobOutput> _Broadcaster;

            public _NotificationsObserver(_RelientTaskulerBroadcaster<TJobInput, TJobOutput> broadcaster)
            {
                _Broadcaster = broadcaster;
            }

            public void OnNext(RelientNotification<TJobInput, TJobOutput> value)
            {
                foreach (var _publisher in _Broadcaster._Publishers)
                {
                    _Broadcaster._NotificationsQueue.Enqueue(new _NotificationRecord(_publisher, value, false, null));
                }
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
                foreach (var _publisher in _Broadcaster._Publishers)
                {
                    _Broadcaster._NotificationsQueue.Enqueue(new _NotificationRecord(_publisher, null, false, error));
                }
            }
        }

        private sealed class _NotificationRecord
        {
            private readonly List<DateTimeOffset> _Tries;

            public _NotificationRecord(
                IRelientPublisher<TJobInput, TJobOutput> publisher,
                RelientNotification<TJobInput, TJobOutput> relientNotification, 
                bool escalated, 
                Exception error)
            {
                Publisher = publisher;
                RelientNotification = relientNotification;
                Escalated = escalated;
                Error = error;

                _Tries = new List<DateTimeOffset>();
            }

            public IRelientPublisher<TJobInput, TJobOutput> Publisher { get; }

            public RelientNotification<TJobInput, TJobOutput> RelientNotification { get; }

            public bool Escalated { get; }

            public Exception Error { get; }

            public IReadOnlyList<DateTimeOffset> Tries => _Tries;

            public void NoteTry(DateTimeOffset timestamp)
            {
                _Tries.Add(timestamp);
            }
        }
    }
}

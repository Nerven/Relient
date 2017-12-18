using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Nerven.Taskuler;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;
using Nerven.Taskuler.Core;

namespace Nerven.Relient.TaskulerService
{
    internal sealed class _RelientTaskulerService<TJobInput, TJobOutput> : IRelientTaskulerService<TJobInput, TJobOutput>, IDisposable
    {
        private readonly ITaskulerWorker _Worker;
        private readonly bool _OwnsWorker;
        private readonly object _Lock;
        private readonly Func<RelientJobInfo<TJobInput, TJobOutput>, CancellationToken, Task<TJobInput>> _CreateInputAsync;
        private readonly TimeSpan _DefaultTimeout;
        private readonly TimeSpan _DefaultInterval;
        private readonly Subject<RelientNotification<TJobInput, TJobOutput>> _Notifications;
        private readonly ConcurrentDictionary<TimeSpan, ITaskulerScheduleHandle> _Tracks;
        private readonly Random _Random;
        private readonly object _RandomLock;

        private readonly IReadOnlyDictionary<string, _Job> _Jobs;
        private bool _Disposed;
        private TaskCompletionSource<int> _RunTask;

        internal _RelientTaskulerService(
            ITaskulerWorker worker,
            bool ownsWorker,
            Func<RelientJobInfo<TJobInput, TJobOutput>, CancellationToken, Task<TJobInput>> createInputAsync,
            IReadOnlyCollection<KeyValuePair<string, IRelientJobInstance<TJobInput, TJobOutput>>> jobs)
        {
            _Worker = worker;
            _OwnsWorker = ownsWorker;
            _Lock = new object();
            _CreateInputAsync = createInputAsync;
            _DefaultTimeout = TimeSpan.FromMinutes(5);
            _DefaultInterval = TimeSpan.FromMinutes(20);
            _Notifications = new Subject<RelientNotification<TJobInput, TJobOutput>>();
            NotificationsSource = _Notifications.AsObservable();
            _Tracks = new ConcurrentDictionary<TimeSpan, ITaskulerScheduleHandle>();
            _Random = new Random(Guid.NewGuid().GetHashCode());
            _RandomLock = new object();

            _Jobs = jobs.Select(_jobPair => _SetupJob(_jobPair.Key, _jobPair.Value)).ToDictionary(_job => _job.Name);
        }

        public bool IsRunning
        {
            get
            {
                if (_OwnsWorker)
                {
                    return _Worker.IsRunning;
                }

                var _status = _RunTask?.Task.Status;
                return _status.HasValue && _status.Value < TaskStatus.RanToCompletion;
            }
        }

        public IObservable<RelientNotification<TJobInput, TJobOutput>> NotificationsSource { get; }

        IObservable<TaskulerNotification> ITaskulerWorker.NotificationsSource => _Worker.NotificationsSource;

        public IReadOnlyDictionary<string, RelientJobInfo<TJobInput, TJobOutput>> GetJobInfos() => new ReadOnlyDictionary<string, RelientJobInfo<TJobInput, TJobOutput>>(_Jobs.ToDictionary(_pair => _pair.Key, _pair => _pair.Value.Info));

        IEnumerable<ITaskulerScheduleHandle> ITaskulerWorker.GetSchedules() => _Worker.GetSchedules();

        ITaskulerScheduleHandle ITaskulerWorker.AddSchedule(string scheduleName, ITaskulerSchedule schedule) => _Worker.AddSchedule(scheduleName, schedule);

        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (_OwnsWorker)
            {
                return _Worker.StartAsync(cancellationToken);
            }

            lock (_Lock)
            {
                Must.Assertion
                    .Assert<InvalidOperationException>(!IsRunning);

                cancellationToken.ThrowIfCancellationRequested();
                foreach (var _job in _Jobs)
                {
                    _job.Value.Init();
                }

                _RunTask = new TaskCompletionSource<int>();
            }

            return Task.CompletedTask;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (_OwnsWorker)
            {
                await _Worker.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (cancellationToken.Register(() =>
                    {
                        lock (_Lock)
                        {
                            _RunTask?.SetResult(0);
                        }
                    }))
                {
                    await StartAsync(cancellationToken).ConfigureAwait(false);

                    var _waitTask = _RunTask?.Task;
                    if (_waitTask != null)
                    {
                        await _waitTask.ConfigureAwait(false);
                    }
                }
            }
        }

        public Task StopAsync()
        {
            if (_OwnsWorker)
            {
                return _Worker.StopAsync();
            }

            lock (_Lock)
            {
                Must.Assertion
                    .Assert<InvalidOperationException>(IsRunning);

                foreach (var _job in _Jobs)
                {
                    _job.Value.Destroy();
                }

                _RunTask.SetResult(0);
                _RunTask = null;
            }

            return Task.CompletedTask;
        }

        public Task WaitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (_OwnsWorker)
            {
                return _Worker.WaitAsync(cancellationToken);
            }

            return _RunTask?.Task ?? Task.CompletedTask;
        }

        public void Dispose()
        {
            _Disposed = true;
            if (_OwnsWorker)
            {
                (_Worker as IDisposable)?.Dispose();
            }

            _Notifications.Dispose();

            foreach (var _job in _Jobs)
            {
                if (!_OwnsWorker)
                {
                    _job.Value.Destroy();
                }

                //// ReSharper disable once SuspiciousTypeConversion.Global
                (_job.Value.Instance as IDisposable)?.Dispose();
            }
        }

        private _Job _SetupJob(string name, IRelientJobInstance<TJobInput, TJobOutput> jobInstance)
        {
            var _job = new _Job(this, name, jobInstance);

            if (_OwnsWorker)
            {
                _job.Init();
            }

            return _job;
        }

        private Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> _CreateTaskCreator(_Job job)
        {
            var _maxRandomDelay = (int)Math.Min((long)(job.Interval.Ticks / 30D), TimeSpan.FromMinutes(5).Ticks);

            return async (_taskulerContext, _cancellationToken) =>
                {
                    Must.Assertion
                        .Assert<ObjectDisposedException>(!_Disposed);

                    //// ReSharper disable once AccessToDisposedClosure
                    using (var _cancellationTokenSource = new CancellationTokenSource())
                    using (_cancellationToken.Register(() => _cancellationTokenSource.Cancel()))
                    {
                        if (_maxRandomDelay > 0)
                        {
                            TimeSpan _delay;
                            lock (_RandomLock)
                            {
                                _delay = TimeSpan.FromTicks(_Random.Next(_maxRandomDelay));
                            }

                            if (_delay > TimeSpan.Zero)
                            {
                                await Task.Delay(_delay, _cancellationToken).ConfigureAwait(false);
                            }
                        }

                        var _context = new _JobExecutionContext(job, _Notifications);
                        var _createInputTask = _CreateInputAsync(job.Info, _cancellationTokenSource.Token);
                        var _input = _createInputTask == null ? default(TJobInput) : await _createInputTask.ConfigureAwait(false);
                        var _timer = new Stopwatch();
                        var _output = default(TJobOutput);
                        Exception _runException = null;

                        _cancellationTokenSource.CancelAfter(job.Instance.Timeout ?? _DefaultTimeout);

                        var _startedAt = DateTimeOffset.Now;
                        _timer.Start();
                        try
                        {
                            _output = await job.Instance.RunAsync(_context, _input, _cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (Exception _exception)
                        {
                            _runException = _exception;
                        }

                        _timer.Stop();
                        var _endedAt = DateTimeOffset.Now;

                        var _records = _context.BuildRecords();
                        if (_runException != null)
                        {
                            _records = _records.Add(new RelientStatusRecord(_endedAt, RelientStatus.Fail, null, null, _runException));
                        }

                        var _result = new RelientResult<TJobInput, TJobOutput>(_input, _output, _records, _startedAt, _endedAt, _timer.Elapsed);

                        if (_OwnsWorker || IsRunning)
                        {
                            bool _trackChanged;
                            RelientJobInfo<TJobInput, TJobOutput> _info;
                            lock (job.Lock)
                            {
                                _trackChanged = job.Update(_result);
                                _info = job.Info;
                            }

                            _Notifications.OnNext(RelientNotification<TJobInput, TJobOutput>.JobResultNotification(_info, _endedAt));

                            return _trackChanged
                                ? TaskulerTaskResponse.Stop()
                                : TaskulerTaskResponse.Continue();
                        }

                        return TaskulerTaskResponse.Stop();
                    }
                };
        }

        private enum _Track
        {
            None,
            Normal,
            Intensified,
            Burst
        }

        private sealed class _Job
        {
            private readonly _RelientTaskulerService<TJobInput, TJobOutput> _Service;
            private readonly Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> _RunJob;

            private _Track _Track;
            private ITaskulerTaskHandle _TrackTaskHandle;
            private ImmutableList<RelientResult<TJobInput, TJobOutput>> _RecentResults;

            public _Job(
                _RelientTaskulerService<TJobInput, TJobOutput> service,
                string name,
                IRelientJobInstance<TJobInput, TJobOutput> jobInstance)
            {
                _Service = service;
                Name = name;
                Instance = jobInstance;

                Lock = new object();
                Interval = _SimplifyInterval(jobInstance.Interval) ?? service._DefaultInterval;
                _Track = _Track.None;
                _RecentResults = ImmutableList<RelientResult<TJobInput, TJobOutput>>.Empty;
                Info = new RelientJobInfo<TJobInput, TJobOutput>(
                    name,
                    jobInstance,
                    new RelientJobStatus<TJobInput, TJobOutput>(
                        null,
                        _RecentResults));

                _RunJob = service._CreateTaskCreator(this);
            }

            public string Name { get; }

            public object Lock { get; }

            public TimeSpan Interval { get; }

            public IRelientJobInstance<TJobInput, TJobOutput> Instance { get; }

            public RelientJobInfo<TJobInput, TJobOutput> Info { get; private set; }

            public void Init()
            {
                lock (Lock)
                {
                    _SetTrack(_Track.Intensified);
                }
            }

            public void Destroy()
            {
                lock (Lock)
                {
                    _TrackTaskHandle?.Remove();
                    _TrackTaskHandle = null;
                }
            }

            public bool Update(RelientResult<TJobInput, TJobOutput> result)
            {
                Must.Assertion
                    .AssertArgumentNotNull(result, nameof(result));

                lock (Lock)
                {
                    var _results = _AddResult(_RecentResults, result);
                    _CalculateStateStatus(_results, result);

                    var _newTrack = _CalculateTrack(_results, result);

                    if (_Track != _newTrack)
                    {
                        _SetTrack(_newTrack);

                        return true;
                    }

                    return false;
                }
            }

            private static ImmutableList<RelientResult<TJobInput, TJobOutput>> _AddResult(
                ImmutableList<RelientResult<TJobInput, TJobOutput>> target,
                RelientResult<TJobInput, TJobOutput> result)
            {
                var _keptResults = 100;
                return _GetRecentResults(target.Add(result), _keptResults);
            }

            private void _CalculateStateStatus(
                ImmutableList<RelientResult<TJobInput, TJobOutput>> results,
                RelientResult<TJobInput, TJobOutput> result)
            {
                RelientStatus? _stateStatus;
                if (_GetRecentResults(results, 12).Count(_result => _result.Status == RelientStatus.Fail) > 3 &&
                    _GetRecentResults(results, 3).TrueForAll(_result => _result.Status == RelientStatus.Fail))
                {
                    _stateStatus = RelientStatus.Fail;
                }
                else if (_GetRecentResults(results, 12).Count(_result => _result.Status == RelientStatus.Fail) > 1 ||
                    _GetRecentResults(results, 6).Count(_result => _result.Status != RelientStatus.Ok) > 1)
                {
                    _stateStatus = RelientStatus.Warn;
                }
                else if (_GetRecentResults(results, 6).Exists(_result => _result.Status == RelientStatus.Ok))
                {
                    _stateStatus = RelientStatus.Ok;
                }
                else
                {
                    _stateStatus = null;
                }

                var _status = new RelientJobStatus<TJobInput, TJobOutput>(
                    _stateStatus.HasValue ? RelientState.Create(Info.Status.State, _stateStatus.Value, result.EndedAt) : null,
                    results);

                _RecentResults = results;
                Info = new RelientJobInfo<TJobInput, TJobOutput>(Name, Instance, _status);

                if (_stateStatus.HasValue && (!Info.Status.State.PreviousStateStatus.HasValue || _stateStatus != Info.Status.State.PreviousStateStatus.Value))
                {
                    _Service._Notifications.OnNext(RelientNotification<TJobInput, TJobOutput>.JobStateNotification(Info, result.EndedAt));
                }
            }

            private _Track _CalculateTrack(
                ImmutableList<RelientResult<TJobInput, TJobOutput>> results,
                RelientResult<TJobInput, TJobOutput> result)
            {
                if (result.Status != RelientStatus.Ok)
                {
                    if (_GetRecentResults(results, 4).Exists(_result => _result.Status == RelientStatus.Ok) ||
                        _GetRecentResults(results, 4).Count < 4)
                    {
                        return _Track.Burst;
                    }

                    return _Track.Intensified;
                }

                if (_GetRecentResults(results, 4).Exists(_result => _result.Status != RelientStatus.Ok))
                {
                    return _Track.Intensified;
                }

                return _Track.Normal;
            }

            private static ImmutableList<RelientResult<TJobInput, TJobOutput>> _GetRecentResults(
                ImmutableList<RelientResult<TJobInput, TJobOutput>> results,
                int maxCount)
            {
                return results.Count > maxCount
                    ? results.GetRange(results.Count - maxCount, maxCount)
                    : results;
            }

            private void _SetTrack(_Track newTrack)
            {
                _TrackTaskHandle?.Remove();

                var _newScheduleHandle = _Service._Tracks.GetOrAdd(_GetTrackKey(newTrack), _CreateTrackSchedule);
                var _newTaskHandle = _newScheduleHandle.AddTask(_RunJob);

                _Track = newTrack;
                _TrackTaskHandle = _newTaskHandle;
            }

            private TimeSpan _GetTrackKey(_Track track)
            {
                switch (track)
                {
                    case _Track.None:
                    case _Track.Normal:
                        return Interval;
                    case _Track.Intensified:
                        return _SimplifyInterval(TimeSpan.FromTicks(Interval.Ticks / 4));
                    case _Track.Burst:
                        return _SimplifyInterval(TimeSpan.FromTicks(Interval.Ticks / 10));
                    default:
                        Must.Assertion.AssertNever();
                        //// ReSharper disable once HeuristicUnreachableCode
                        return default(TimeSpan); // TODO
                }
            }

            private static TimeSpan? _SimplifyInterval(TimeSpan? requestedInterval) => requestedInterval.HasValue ? _SimplifyInterval(requestedInterval.Value) : (TimeSpan?)null;

            private static TimeSpan _SimplifyInterval(TimeSpan requestedInterval)
            {
                var _steps = new SortedDictionary<TimeSpan, TimeSpan>
                    {
                        { TimeSpan.Zero, TimeSpan.FromSeconds(10) },
                        { TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30) },
                        { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1) },
                        { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5) },
                        { TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(15) },
                        { TimeSpan.FromHours(2), TimeSpan.FromHours(1) },
                        { TimeSpan.FromHours(12), TimeSpan.FromHours(6) },
                        { TimeSpan.FromDays(1), TimeSpan.FromHours(12) },
                        { TimeSpan.FromDays(3), TimeSpan.FromDays(1) },
                    };

                var _step = _steps
                    .TakeWhile(_s => _s.Key < requestedInterval)
                    .Select(_s => new TimeSpan?(_s.Value))
                    .LastOrDefault() ?? _steps.Values.Last();

                var _factor = (long)Math.Floor((double)requestedInterval.Ticks / _step.Ticks);
                var _interval = TimeSpan.FromTicks(_step.Ticks * Math.Max(1, _factor));

                Must.Assertion
                    .Assert(_factor == 0 || _interval <= requestedInterval)
                    .Assert(_factor == 0 || _interval > requestedInterval - _step)
                    .Assert(_factor != 0 || _interval == _step);

                return _interval;
            }

            private ITaskulerScheduleHandle _CreateTrackSchedule(TimeSpan key)
            {
                return _Service._Worker.AddIntervalSchedule(key);
            }
        }

        private sealed class _JobExecutionContext : IRelientJobContext
        {
            private readonly _Job _Job;
            private readonly ISubject<RelientNotification<TJobInput, TJobOutput>> _Notifications;
            private readonly ImmutableList<RelientStatusRecord>.Builder _RecordsBuilder;

            public _JobExecutionContext(
                _Job job,
                ISubject<RelientNotification<TJobInput, TJobOutput>> notifications)
            {
                _Job = job;
                _Notifications = notifications;
                _RecordsBuilder = ImmutableList.CreateBuilder<RelientStatusRecord>();
            }

            public IRelientJobContext Record(
                RelientStatus? status,
                string key,
                string message,
                Exception exception = null)
            {
                var _now = DateTimeOffset.Now;
                var _record = new RelientStatusRecord(_now, status, key, message, exception);
                _RecordsBuilder.Add(_record);
                _Notifications.OnNext(RelientNotification<TJobInput, TJobOutput>.JobRecordNotification(_Job.Info, _now, _record));

                return this;
            }

            public ImmutableList<RelientStatusRecord> BuildRecords() => _RecordsBuilder.ToImmutable();
        }
    }
}

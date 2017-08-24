using System;
using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientNotification<TJobInput, TJobOutput> : RelientNotification
    {
        private RelientNotification(
            RelientNotificationType notificationType,
            DateTimeOffset timestamp,
            RelientJobInfo<TJobInput, TJobOutput> jobInfo,
            RelientStatusRecord jobStatusRecord)
        {
            NotificationType = notificationType;
            Timestamp = timestamp;
            JobInfo = jobInfo;
            JobStatusRecord = jobStatusRecord;
        }

        public override RelientNotificationType NotificationType { get; }

        public override DateTimeOffset Timestamp { get; }

        public RelientJobInfo<TJobInput, TJobOutput> JobInfo { get; }

        public override RelientState JobState => NotificationType == RelientNotificationType.JobState
            ? JobInfo?.Status.State
            : null;

        public RelientResult<TJobInput, TJobOutput> JobResult => NotificationType == RelientNotificationType.JobResult
            ? JobInfo?.Status.RecentResults[JobInfo.Status.RecentResults.Count - 1]
            : null;

        public override RelientStatusRecord JobStatusRecord { get; }

        public static RelientNotification<TJobInput, TJobOutput> JobStateNotification(
            RelientJobInfo<TJobInput, TJobOutput> jobInfo,
            DateTimeOffset timestamp)
        {
            Must.Assertion
                .AssertArgumentNotNull(jobInfo, nameof(jobInfo))
                .Assert<ArgumentException>(jobInfo.Status.State != null);

            return new RelientNotification<TJobInput, TJobOutput>(
                RelientNotificationType.JobState,
                timestamp,
                jobInfo,
                null);
        }

        public static RelientNotification<TJobInput, TJobOutput> JobResultNotification(
            RelientJobInfo<TJobInput, TJobOutput> jobInfo,
            DateTimeOffset timestamp)
        {
            Must.Assertion
                .AssertArgumentNotNull(jobInfo, nameof(jobInfo))
                .Assert<ArgumentException>(jobInfo.Status.RecentResults.Count != 0);

            return new RelientNotification<TJobInput, TJobOutput>(
                RelientNotificationType.JobResult,
                timestamp,
                jobInfo,
                null);
        }

        public static RelientNotification<TJobInput, TJobOutput> JobRecordNotification(
            RelientJobInfo<TJobInput, TJobOutput> jobInfo,
            DateTimeOffset timestamp,
            RelientStatusRecord statusRecord)
        {
            Must.Assertion
                .AssertArgumentNotNull(jobInfo, nameof(jobInfo))
                .AssertArgumentNotNull(statusRecord, nameof(statusRecord));

            return new RelientNotification<TJobInput, TJobOutput>(
                RelientNotificationType.JobRecord,
                timestamp,
                jobInfo,
                statusRecord);
        }

        public override string ToString()
        {
            var _prefix = $"[{Timestamp}/{JobInfo.Name}/{NotificationType}]";
            string _typeSpecific;
            switch (NotificationType)
            {
                case RelientNotificationType.JobState when JobState.PreviousStateStatus.HasValue:
                    _typeSpecific = $"{JobState.PreviousStateStatus} -> {JobState.Status}";
                    break;
                case RelientNotificationType.JobState when !JobState.PreviousStateStatus.HasValue:
                    _typeSpecific = $"None -> {JobState.Status}";
                    break;
                case RelientNotificationType.JobResult:
                    _typeSpecific = $"{JobResult.Status}/{JobResult.StartedAt}->{JobResult.EndedAt}={JobResult.Duration}";
                    break;
                case RelientNotificationType.JobRecord:
                    _typeSpecific = $"{JobStatusRecord.Status}/{JobStatusRecord.Key}/{JobStatusRecord.Message}/{JobStatusRecord.Exception}";
                    break;
                default:
                    _typeSpecific = string.Empty;
                    break;
            }

            return $"{_prefix} {_typeSpecific}";
        }
    }
}

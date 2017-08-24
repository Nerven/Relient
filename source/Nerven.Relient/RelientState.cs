using System;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientState
    {
        public RelientState(
            RelientStatus status,
            DateTimeOffset timestamp,
            RelientStatus? previousStateStatus,
            DateTimeOffset? previousStateTimestamp)
        {
            Must.Assertion
                .Assert<ArgumentOutOfRangeException>(Enum.IsDefined(typeof(RelientStatus), status))
                .Assert<ArgumentOutOfRangeException>(timestamp > DateTimeOffset.MinValue && timestamp < DateTimeOffset.MaxValue)
                .Assert<ArgumentOutOfRangeException>(!previousStateTimestamp.HasValue || previousStateTimestamp.Value < timestamp)
                .Assert<ArgumentOutOfRangeException>(!previousStateStatus.HasValue || Enum.IsDefined(typeof(RelientStatus), previousStateStatus.Value))
                .Assert<ArgumentException>(previousStateTimestamp.HasValue == previousStateStatus.HasValue);
            
            Status = status;
            Timestamp = timestamp;
            PreviousStateStatus = previousStateStatus;
            PreviousStateTimestamp = previousStateTimestamp;
        }

        public RelientStatus Status { get; }

        public DateTimeOffset Timestamp { get; }

        public RelientStatus? PreviousStateStatus { get; }

        public DateTimeOffset? PreviousStateTimestamp { get; }

        public TimeSpan? PreviousStateDuration => PreviousStateTimestamp.HasValue
            ? Timestamp.Subtract(PreviousStateTimestamp.Value)
            : default(TimeSpan?);

        public static RelientState Create(RelientStatus status, DateTimeOffset timestamp)
        {
            return new RelientState(status, timestamp, null, null);
        }

        public static RelientState Create(RelientState previousState, RelientStatus status, DateTimeOffset timestamp)
        {
            return new RelientState(status, timestamp, previousState?.Status, previousState?.Timestamp);
        }
    }
}

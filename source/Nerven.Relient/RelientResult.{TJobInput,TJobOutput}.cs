using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientResult<TJobInput, TJobOutput>
    {
        public RelientResult(
            TJobInput input,
            TJobOutput output,
            IReadOnlyList<RelientStatusRecord> records,
            DateTimeOffset startedAt,
            DateTimeOffset endedAt,
            TimeSpan duration)
        {
            Must.Assertion
                .Assert<ArgumentOutOfRangeException>(startedAt != DateTimeOffset.MinValue && startedAt != DateTimeOffset.MaxValue)
                .Assert<ArgumentOutOfRangeException>(endedAt != DateTimeOffset.MinValue && endedAt != DateTimeOffset.MaxValue)
                .Assert<ArgumentOutOfRangeException>(startedAt <= endedAt)
                .Assert<ArgumentOutOfRangeException>(duration >= TimeSpan.Zero);
            
            Input = input;
            Output = output;
            Records = records;
            StartedAt = startedAt;
            EndedAt = endedAt;
            Duration = duration;
            
            Status = Records.Any(_record => _record.Status.HasValue)
                ? Records.Where(_record => _record.Status.HasValue).Max(_record => _record.Status.Value)
                : RelientStatus.Warn;
        }

        public TJobInput Input { get; }

        public TJobOutput Output { get; }

        public RelientStatus Status { get; }

        public IReadOnlyList<RelientStatusRecord> Records { get; }

        public DateTimeOffset StartedAt { get; }

        public DateTimeOffset EndedAt { get; }

        public TimeSpan Duration { get; }
    }
}

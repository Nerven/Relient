using System;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Relient
{
    [PublicAPI]
    public sealed class RelientStatusRecord
    {
        public RelientStatusRecord(
            DateTimeOffset timestamp,
            RelientStatus? status,
            string key,
            string message,
            Exception exception)
        {
            Must.Assertion
                .Assert(timestamp > DateTimeOffset.MinValue && timestamp < DateTimeOffset.MaxValue)
                .Assert(!status.HasValue || Enum.IsDefined(typeof(RelientStatus), status.Value));

            Timestamp = timestamp;
            Status = status;
            Key = key;
            Message = message;
            Exception = exception;
        }

        public DateTimeOffset Timestamp { get; }

        public RelientStatus? Status { get; }

        public string Key { get; }

        public string Message { get; }

        public Exception Exception { get; }
    }
}

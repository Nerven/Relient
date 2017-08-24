using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;

namespace Nerven.Relient
{
    [PublicAPI]
    public static class RelientJobInstance
    {
        public static IRelientJobInstance<TJobInput, TJobOutput> Create<TJobInput, TJobOutput>(
            TimeSpan? interval,
            TimeSpan? timeout,
            Func<IRelientJobContext, TJobInput, CancellationToken, Task<TJobOutput>> runAsync)
        {
            Must.Assertion
                .Assert<ArgumentException>(!interval.HasValue || interval.Value > TimeSpan.Zero)
                .Assert<ArgumentException>(!timeout.HasValue || timeout.Value > TimeSpan.Zero)
                .AssertArgumentNotNull(runAsync, nameof(runAsync));

            return new RelientJobInstance<TJobInput, TJobOutput>(interval, timeout, runAsync);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public interface IRelientJobInstance<in TJobInput, TJobOutput>
    {
        TimeSpan? Interval { get; }

        TimeSpan? Timeout { get; }

        Task<TJobOutput> RunAsync(IRelientJobContext context, TJobInput input, CancellationToken cancellationToken = default(CancellationToken));
    }
}

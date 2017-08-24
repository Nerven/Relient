using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public interface IRelientService<TJobInput, TJobOutput>
    {
        bool IsRunning { get; }

        IObservable<RelientNotification<TJobInput, TJobOutput>> NotificationsSource { get; }

        IReadOnlyDictionary<string, RelientJobInfo<TJobInput, TJobOutput>> GetJobInfos();

        Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task RunAsync(CancellationToken cancellationToken);

        Task StopAsync();

        Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}

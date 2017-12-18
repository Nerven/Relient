using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient.Publishing
{
    [PublicAPI]
    public interface IRelientPublisher<TJobInput, TJobOutput>
    {
        Task<RelientPublishResult> HandleNotificationAsync(
            RelientNotification<TJobInput, TJobOutput> notification,
            Exception error,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}

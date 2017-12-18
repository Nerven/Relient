using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Relient.Publishing
{
    [PublicAPI]
    public static class RelientPublisher
    {
        public static IRelientPublisher<TJobInput, TJobOutput> Create<TJobInput, TJobOutput>(
            Func<RelientNotification<TJobInput, TJobOutput>, Exception, CancellationToken, Task<RelientPublishResult>> handleAsync)
        {
            return new _RelientPublisher<TJobInput, TJobOutput>(handleAsync);
        }

        public static IRelientPublisher<TJobInput, TJobOutput> Create<TJobInput, TJobOutput>(
            Func<RelientNotification<TJobInput, TJobOutput>, Exception, CancellationToken, Task> handleAsync)
        {
            return new _RelientPublisher<TJobInput, TJobOutput>(
                (_notification, _error, _cancellationToken) => handleAsync(_notification, _error, _cancellationToken)
                .ContinueWith(_ => RelientPublishResult.Ok, TaskContinuationOptions.OnlyOnRanToCompletion));
        }

        private class _RelientPublisher<TJobInput, TJobOutput> : IRelientPublisher<TJobInput, TJobOutput>
        {
            private readonly Func<RelientNotification<TJobInput, TJobOutput>, Exception, CancellationToken, Task<RelientPublishResult>> _HandleAsync;

            public _RelientPublisher(Func<RelientNotification<TJobInput, TJobOutput>, Exception, CancellationToken, Task<RelientPublishResult>> handleAsync)
            {
                _HandleAsync = handleAsync;
            }

            public Task<RelientPublishResult> HandleNotificationAsync(
                RelientNotification<TJobInput, TJobOutput> notification,
                Exception error,
                CancellationToken cancellationToken = new CancellationToken())
            {
                return _HandleAsync(notification, error, cancellationToken);
            }
        }
    }
}

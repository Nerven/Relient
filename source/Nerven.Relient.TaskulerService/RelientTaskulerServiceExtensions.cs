using System;
using JetBrains.Annotations;
using Nerven.Relient.Publishing;
using Nerven.Relient.TaskulerService.Publishing;
using Nerven.Taskuler;

namespace Nerven.Relient.TaskulerService
{
    [PublicAPI]
    public static class RelientTaskulerServiceExtensions
    {
        public static IRelientService<TJobInput, TJobOutput> AsRelientService<TJobInput, TJobOutput>(
            this IRelientTaskulerService<TJobInput, TJobOutput> service)
        {
            return service;
        }

        public static ITaskulerWorker AsTaskulerWorker<TJobInput, TJobOutput>(
            this IRelientTaskulerService<TJobInput, TJobOutput> service)
        {
            return service;
        }

        public static IDisposable UsingPublishers<TJobInput, TJobOutput>(
            this IRelientTaskulerService<TJobInput, TJobOutput> service,
            params IRelientPublisher<TJobInput, TJobOutput>[] publishers)
        {
            return RelientTaskulerBroadcaster.Create(service, publishers);
        }
    }
}

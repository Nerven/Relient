using System;
using System.Linq;
using JetBrains.Annotations;
using Nerven.Relient.Publishing;

namespace Nerven.Relient.TaskulerService.Publishing
{
    [PublicAPI]
    public static class RelientTaskulerBroadcaster
    {
        public static IDisposable Create<TJobInput, TJobOutput>(
            IRelientTaskulerService<TJobInput, TJobOutput> service,
            params IRelientPublisher<TJobInput, TJobOutput>[] publishers)
        {
            return new _RelientTaskulerBroadcaster<TJobInput, TJobOutput>(service, publishers.ToArray());
        }
    }
}

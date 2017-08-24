using System;
using JetBrains.Annotations;
using Nerven.Taskuler;

namespace Nerven.Relient.TaskulerService
{
    [PublicAPI]
    public interface IRelientTaskulerService<TJobInput, TJobOutput> : IRelientService<TJobInput, TJobOutput>
    {
        IObservable<TaskulerNotification> TaskulerNotificationsSource { get; }
    }
}

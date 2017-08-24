using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nerven.Relient.Runner
{
    internal class _RelientRunnerContext<TService, TJobInput, TJobOutput>
        where TService : IRelientService<TJobInput, TJobOutput>
    {
        public _RelientRunnerContext(TService service, string serviceName, IReadOnlyList<Func<TService, CancellationToken, Task>> serviceHooks)
        {
            Service = service;
            ServiceName = serviceName;
            ServiceHooks = serviceHooks;
        }

        public TService Service { get; }

        public string ServiceName { get; }

        public IReadOnlyList<Func<TService, CancellationToken, Task>> ServiceHooks { get; }
    }
}

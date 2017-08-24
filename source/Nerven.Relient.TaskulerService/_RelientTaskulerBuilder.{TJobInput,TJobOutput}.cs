using System;
using System.Threading;
using System.Threading.Tasks;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;
using Nerven.Taskuler;
using Nerven.Taskuler.Core;

namespace Nerven.Relient.TaskulerService
{
    internal class _RelientTaskulerBuilder<TJobInput, TJobOutput> :
        RelientBuilderBase<IRelientTaskulerBuilder<TJobInput, TJobOutput>, IRelientTaskulerService<TJobInput, TJobOutput>, TJobInput, TJobOutput>, 
        IRelientTaskulerBuilder<TJobInput, TJobOutput>
    {
        private Func<ITaskulerWorker> _CreateOwnedWorker;
        private Func<ITaskulerWorker> _GetBorrowedWorker;
        
        protected override IRelientTaskulerBuilder<TJobInput, TJobOutput> Builder => this;
        
        public IRelientTaskulerBuilder<TJobInput, TJobOutput> WithOwnedWorker(Func<ITaskulerWorker> createWorker)
        {
            Must.Assertion
                .AssertArgumentNotNull(createWorker, nameof(createWorker));
            
            return Mutate(() =>
                {
                    _CreateOwnedWorker = createWorker;
                    _GetBorrowedWorker = null;
                });
        }

        public IRelientTaskulerBuilder<TJobInput, TJobOutput> WithBorrowedWorker(Func<ITaskulerWorker> getWorker)
        {
            Must.Assertion
                .AssertArgumentNotNull(getWorker, nameof(getWorker));

            return Mutate(() =>
                {
                    _CreateOwnedWorker = null;
                    _GetBorrowedWorker = getWorker;
                });
        }

        protected override async Task<IRelientTaskulerService<TJobInput, TJobOutput>> CreateServiceAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var _useBorrowedWorker = _GetBorrowedWorker != null;
            var _worker = _useBorrowedWorker
                ? _GetBorrowedWorker()
                : _CreateOwnedWorker != null
                    ? _CreateOwnedWorker()
                    : TaskulerHosting.TryCreateHostedWorker() ?? TaskulerWorker.Create();

            Must.Assertion
                .Assert(_worker != null);

            var _service = new _RelientTaskulerService<TJobInput, TJobOutput>(
                _worker,
                !_useBorrowedWorker,
                CreateInputAsync ?? ((_job, _cancellationToken) => default(Task<TJobInput>)),
                await CollectJobsAsync(cancellationToken).ConfigureAwait(false));

            return _service;
        }
    }
}

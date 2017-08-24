using JetBrains.Annotations;

namespace Nerven.Relient.TaskulerService
{
    [PublicAPI]
    public static class RelientTaskuler
    {
        public static IRelientTaskulerBuilder<TJobInput, TJobOutput> CreateBuilder<TJobInput, TJobOutput>()
        {
            return new _RelientTaskulerBuilder<TJobInput, TJobOutput>();
        }
    }
}

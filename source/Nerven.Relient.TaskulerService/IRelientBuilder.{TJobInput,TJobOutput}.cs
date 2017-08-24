namespace Nerven.Relient.TaskulerService
{
    public interface IRelientTaskulerBuilder<TJobInput, TJobOutput> : IRelientTaskulerBuilder<IRelientTaskulerBuilder<TJobInput, TJobOutput>, TJobInput, TJobOutput>
    {
    }
}

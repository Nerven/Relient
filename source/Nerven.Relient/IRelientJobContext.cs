using System;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public interface IRelientJobContext
    {
        IRelientJobContext Record(
            RelientStatus? status,
            string key,
            string message,
            Exception exception = null);
    }
}

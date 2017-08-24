using System;
using JetBrains.Annotations;

namespace Nerven.Relient
{
    [PublicAPI]
    public abstract class RelientNotification
    {
        protected internal RelientNotification()
        {
        }

        public abstract RelientNotificationType NotificationType { get; }

        public abstract DateTimeOffset Timestamp { get; }

        public abstract RelientState JobState { get; }

        public abstract RelientStatusRecord JobStatusRecord { get; }
    }
}

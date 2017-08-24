using System;
using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Assertion.Extensions;

namespace Nerven.Relient
{
    [PublicAPI]
    public static class RelientJobContextExtensions
    {
        public static IRelientJobContext Info(
            this IRelientJobContext context,
            string key,
            string message)
        {
            Must.Assertion
                .AssertArgumentNotNull(context, nameof(context));

            return context.Record(null, key, message);
        }

        public static IRelientJobContext Ok(
            this IRelientJobContext context,
            string key,
            string message)
        {
            Must.Assertion
                .AssertArgumentNotNull(context, nameof(context));

            return context.Record(RelientStatus.Ok, key, message);
        }

        public static IRelientJobContext Warn(
            this IRelientJobContext context,
            string key,
            string message,
            Exception exception = null)
        {
            Must.Assertion
                .AssertArgumentNotNull(context, nameof(context));

            return context.Record(RelientStatus.Warn, key, message, exception);
        }

        public static IRelientJobContext Fail(
            this IRelientJobContext context,
            string key,
            string message,
            Exception exception = null)
        {
            Must.Assertion
                .AssertArgumentNotNull(context, nameof(context));

            return context.Record(RelientStatus.Fail, key, message, exception);
        }
    }
}

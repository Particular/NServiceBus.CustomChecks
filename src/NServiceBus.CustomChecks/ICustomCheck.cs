namespace NServiceBus.CustomChecks
{
    using System;

    /// <summary>
    /// Interface to implement a custom check.
    /// </summary>
    public interface ICustomCheck
    {
        /// <summary>
        /// Category for the check.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Check Id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Periodic execution interval.
        /// </summary>
        TimeSpan? Interval { get; }

        /// <summary>
        /// Perfoms the check.
        /// </summary>
        /// <returns>The result of the check.</returns>
        CheckResult PerformCheck();
    }
}

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetActorFramework.Tests
{
    /// <summary>
    /// Provides validation helpers for <see cref="ActorMetricsTests"/>.
    /// </summary>
    public static class ActorMetricsTestsValidation
    {
        /// <summary>
        /// Validates the specified <see cref="ActorMetricsTests"/> instance.
        /// </summary>
        /// <param name="value">The instance to validate.</param>
        /// <returns>A list of human-readable problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this ActorMetricsTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            try
            {
                // Validate RecordMessageReceived_CalledMultipleTimes_IncrementsMessageCount
                var metrics1 = CreateMetrics();
                metrics1.RecordMessageReceived();
                metrics1.RecordMessageReceived();
                metrics1.RecordMessageReceived();

                if (metrics1.MessageCount != 3)
                {
                    problems.Add("RecordMessageReceived_CalledMultipleTimes_IncrementsMessageCount: MessageCount should be 3 but was " + metrics1.MessageCount);
                }

                if (metrics1.LastMessageTime == default)
                {
                    problems.Add("RecordMessageReceived_CalledMultipleTimes_IncrementsMessageCount: LastMessageTime should not be default(DateTime)");
                }

                // Validate GetErrorRate_WithFiftyPercentErrors_ReturnsFiftyPercent
                var metrics2 = CreateMetrics();
                metrics2.RecordMessageReceived();
                metrics2.RecordMessageReceived();
                metrics2.RecordError();

                var errorRate = metrics2.GetErrorRate();
                var expectedErrorRate = 50.0;

                if (Math.Abs(errorRate - expectedErrorRate) > 0.001)
                {
                    problems.Add("GetErrorRate_WithFiftyPercentErrors_ReturnsFiftyPercent: Expected error rate 50.0 but got " + errorRate.ToString(CultureInfo.InvariantCulture));
                }

                var successRate = metrics2.GetSuccessRate();
                var expectedSuccessRate = 50.0;

                if (Math.Abs(successRate - expectedSuccessRate) > 0.001)
                {
                    problems.Add("GetErrorRate_WithFiftyPercentErrors_ReturnsFiftyPercent: Expected success rate 50.0 but got " + successRate.ToString(CultureInfo.InvariantCulture));
                }

                // Validate GetErrorRate_WithNoMessages_ReturnsZeroWithoutDivisionError
                var metrics3 = CreateMetrics();

                try
                {
                    var errorRateNoMsgs = metrics3.GetErrorRate();
                    if (errorRateNoMsgs != 0)
                    {
                        problems.Add("GetErrorRate_WithNoMessages_ReturnsZeroWithoutDivisionError: Expected error rate 0 but got " + errorRateNoMsgs);
                    }
                }
                catch (Exception ex)
                {
                    problems.Add("GetErrorRate_WithNoMessages_ReturnsZeroWithoutDivisionError: Threw exception: " + ex.Message);
                }

                // Validate RecordProcessingTime_WithThreeTimings_AveragesCorrectly
                var metrics4 = CreateMetrics();
                metrics4.RecordProcessingTime(100);
                metrics4.RecordProcessingTime(200);
                metrics4.RecordProcessingTime(300);

                if (metrics4.ProcessedCount != 3)
                {
                    problems.Add("RecordProcessingTime_WithThreeTimings_AveragesCorrectly: ProcessedCount should be 3 but was " + metrics4.ProcessedCount);
                }

                var expectedAvg = 200.0;
                if (Math.Abs(metrics4.AverageProcessingTimeMs - expectedAvg) > 0.001)
                {
                    problems.Add("RecordProcessingTime_WithThreeTimings_AveragesCorrectly: Expected average 200.0 but got " + metrics4.AverageProcessingTimeMs.ToString(CultureInfo.InvariantCulture));
                }

                // Validate IsUnhealthy_WhenErrorRateExceedsThreshold_ReturnsTrue
                var metrics5 = CreateMetrics();
                for (var i = 0; i < 10; i++)
                {
                    metrics5.RecordMessageReceived();
                }
                for (var i = 0; i < 8; i++)
                {
                    metrics5.RecordError();
                }

                if (!metrics5.IsUnhealthy(25.0))
                {
                    problems.Add("IsUnhealthy_WhenErrorRateExceedsThreshold_ReturnsTrue: Expected IsUnhealthy(25.0) to be true but was false");
                }

                if (metrics5.IsUnhealthy(90.0))
                {
                    problems.Add("IsUnhealthy_WhenErrorRateExceedsThreshold_ReturnsTrue: Expected IsUnhealthy(90.0) to be false but was true");
                }

                // Validate GetSummary_ReflectsCurrentMetricState
                var path = new global::DotNetActorFramework.Models.ActorPath("/system/actor");
                var metrics6 = new global::DotNetActorFramework.Models.ActorMetrics(Guid.NewGuid(), path);
                metrics6.RecordMessageReceived();
                metrics6.RecordError();
                metrics6.RecordProcessingTime(150);

                var summary = metrics6.GetSummary();

                if (summary.MessageCount != 1)
                {
                    problems.Add("GetSummary_ReflectsCurrentMetricState: Expected MessageCount 1 but got " + summary.MessageCount);
                }

                if (summary.ErrorCount != 1)
                {
                    problems.Add("GetSummary_ReflectsCurrentMetricState: Expected ErrorCount 1 but got " + summary.ErrorCount);
                }

                if (summary.ProcessedCount != 1)
                {
                    problems.Add("GetSummary_ReflectsCurrentMetricState: Expected ProcessedCount 1 but got " + summary.ProcessedCount);
                }

                var expectedAvgProcTime = 150.0;
                if (Math.Abs(summary.AverageProcessingTimeMs - expectedAvgProcTime) > 0.001)
                {
                    problems.Add("GetSummary_ReflectsCurrentMetricState: Expected AverageProcessingTimeMs 150.0 but got " + summary.AverageProcessingTimeMs.ToString(CultureInfo.InvariantCulture));
                }

                // Validate IsDuplicate_ForUnregisteredMessageId_ReturnsFalse
                var deduplicator1 = new global::DotNetActorFramework.Utilities.MessageDeduplicator();
                var newId = Guid.NewGuid();

                if (deduplicator1.IsDuplicate(newId))
                {
                    problems.Add("IsDuplicate_ForUnregisteredMessageId_ReturnsFalse: Expected IsDuplicate to return false but returned true");
                }

                // Validate IsDuplicate_AfterRegisterMessage_ReturnsTrueForSameId
                var deduplicator2 = new global::DotNetActorFramework.Utilities.MessageDeduplicator();
                var id = Guid.NewGuid();
                deduplicator2.RegisterMessage(id);

                if (!deduplicator2.IsDuplicate(id))
                {
                    problems.Add("IsDuplicate_AfterRegisterMessage_ReturnsTrueForSameId: Expected IsDuplicate to return true for registered ID but returned false");
                }

                var differentId = Guid.NewGuid();
                if (deduplicator2.IsDuplicate(differentId))
                {
                    problems.Add("IsDuplicate_AfterRegisterMessage_ReturnsTrueForSameId: Expected IsDuplicate to return false for different ID but returned true");
                }

                // Validate Clear_AfterRegisteringMultipleIds_RemovesAllRecords
                var deduplicator3 = new global::DotNetActorFramework.Utilities.MessageDeduplicator();
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                deduplicator3.RegisterMessage(id1);
                deduplicator3.RegisterMessage(id2);

                deduplicator3.Clear();

                if (deduplicator3.IsDuplicate(id1))
                {
                    problems.Add("Clear_AfterRegisteringMultipleIds_RemovesAllRecords: Expected IsDuplicate(id1) to return false after Clear but returned true");
                }

                if (deduplicator3.IsDuplicate(id2))
                {
                    problems.Add("Clear_AfterRegisteringMultipleIds_RemovesAllRecords: Expected IsDuplicate(id2) to return false after Clear but returned true");
                }
            }
            catch (Exception ex)
            {
                problems.Add("Validation threw exception: " + ex.Message);
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="ActorMetricsTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The instance to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this ActorMetricsTests value) => Validate(value).Count == 0;

        /// <summary>
        /// Ensures that the specified <see cref="ActorMetricsTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
        public static void EnsureValid(this ActorMetricsTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);
            if (problems.Count == 0)
            {
                return;
            }

            throw new ArgumentException(
                "The ActorMetricsTests instance is not valid:\n" + string.Join("\n", problems)
            );
        }

        private static global::DotNetActorFramework.Models.ActorMetrics CreateMetrics(string pathStr = "/system/actor")
            => new global::DotNetActorFramework.Models.ActorMetrics(Guid.NewGuid(), new global::DotNetActorFramework.Models.ActorPath(pathStr));
    }
}

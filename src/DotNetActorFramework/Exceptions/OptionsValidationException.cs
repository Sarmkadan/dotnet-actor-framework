// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Thrown when an options object contains one or more invalid values.
/// All violations found during a single validation pass are reported together in <see cref="Failures"/>
/// and in the exception message, so a caller can fix every problem before retrying.
/// </summary>
public class OptionsValidationException : ConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsValidationException"/> class.
    /// </summary>
    /// <param name="optionsName">The name of the options type that failed validation.</param>
    /// <param name="failures">The descriptions of every violation found. Must contain at least one entry.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionsName"/> or <paramref name="failures"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="failures"/> is empty.</exception>
    public OptionsValidationException(string optionsName, IReadOnlyList<string> failures)
        : base(BuildMessage(optionsName, failures))
    {
        OptionsName = optionsName;
        Failures = failures;
    }

    /// <summary>Gets the name of the options type that failed validation.</summary>
    public string OptionsName { get; }

    /// <summary>Gets every violation found during validation, in the order they were checked.</summary>
    public IReadOnlyList<string> Failures { get; }

    private static string BuildMessage(string optionsName, IReadOnlyList<string> failures)
    {
        ArgumentNullException.ThrowIfNull(optionsName);
        ArgumentNullException.ThrowIfNull(failures);

        if (failures.Count == 0)
            throw new ArgumentException("At least one validation failure is required.", nameof(failures));

        var lines = failures.Select(failure => $"  - {failure}");
        return $"{optionsName} is invalid ({failures.Count} problem(s)):{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}

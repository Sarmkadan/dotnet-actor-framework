// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Enums;

/// <summary>
/// Defines the available types of persistence backends for actor state and event journals.
/// </summary>
public enum PersistenceBackend
{
    /// <summary>
    /// In-memory persistence, suitable for testing and transient data.
    /// </summary>
    InMemory,

    /// <summary>
    /// File-based persistence, storing data on the local filesystem.
    /// </summary>
    File,

    /// <summary>
    /// Placeholder for a LiteDB persistence backend.
    /// </summary>
    LiteDb,

    /// <summary>
    /// Placeholder for a PostgreSQL persistence backend.
    /// </summary>
    PostgreSql
}
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for ActorPath.
/// Enables JSON serialization of actor paths and their properties.
/// </summary>
public static class ActorPathExtensionsJsonExtensions
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	/// <summary>
	/// Serializes an ActorPath instance to a JSON string.
	/// </summary>
	/// <param name="path">The ActorPath instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the ActorPath.</returns>
	/// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
	public static string ToJson(this ActorPath path, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(path);

		var options = indented
			? new JsonSerializerOptions(JsonOptions)
			{
				WriteIndented = true,
			}
			: JsonOptions;

		return JsonSerializer.Serialize(path, options);
	}

	/// <summary>
	/// Deserializes an ActorPath instance from a JSON string.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized ActorPath instance, or null if the JSON is null or empty.</returns>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static ActorPath? FromJson(string json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<ActorPath>(json, JsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize an ActorPath instance from a JSON string.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized ActorPath instance if successful; null if JSON is null or empty.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	public static bool TryFromJson(string json, [NotNullWhen(true)] out ActorPath? value)
	{
		if (string.IsNullOrEmpty(json))
		{
			value = null;
			return true;
		}

		try
		{
			value = JsonSerializer.Deserialize<ActorPath>(json, JsonOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}
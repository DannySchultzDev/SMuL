using Godot;
using System.Collections.Generic;
using System;
using System.Text;
using System.Globalization;

public static class LobbyString
{
	/// <summary>
	/// Create a lobbyString from provided variables.
	/// </summary>
	/// <param name="lobbyName">The name of the lobby</param>
	/// <param name="startTime">When the player attempted to connect to the lobby</param>
	/// <param name="playerAmt">The number of players allowed in the lobby</param>
	/// <param name="playerId">The players Id</param>
	/// <param name="metadata">Additional data that will not change throught the game</param>
	/// <returns>The lobbyString</returns>
	public static string EncodeString(string lobbyName, DateTime startTime, int playerAmt, int playerId, Dictionary<string, string> metadata = null)
	{
		string encodedString = $"{lobbyName}|{startTime.ToString("yyyy-MM-ddTHH:mm:ss")}|{playerAmt}|{playerId}|{EncodeMeta(metadata)}";
		GD.Print(encodedString);
		return encodedString;
	}

	/// <summary>
	/// Encodes a Dictionary(string, string) into a string to be used in a lobbyString.
	/// </summary>
	/// <param name="meta"></param>
	/// <returns>The metadata encoded as a string</returns>
	private static string EncodeMeta(Dictionary<string, string> meta)
	{
		if (meta == null)
		{
			return null;
		}
		// Convert the dictionary to a single string representation
		StringBuilder encodedMeta = new StringBuilder();
		foreach (var entry in meta)
		{
			encodedMeta.Append($"{entry.Key}:{entry.Value};");
		}
		return encodedMeta.ToString();
	}

	/// <summary>
	/// Decodes a lobbyString into its parts
	/// </summary>
	/// <param name="encodedString"></param>
	/// <returns>The lobbyString's parts as a dictionary(string, object)<br/>
	/// "lobbyName": (string) The lobby's name<br/>
	/// "startTime": (DateTime) The time the player attempted to join the lobby.<br/>
	/// "playerCount": (int) The number of players allowed in the lobby.<br/>
	/// "playerId": (int) The player's Id.<br/>
	/// "metadata": (Dictionary(string, string)) Additional lobby data stored.</returns>
	public static Dictionary<string, object> DecodeString(string encodedString)
	{
		// Split the encoded string into individual parts.
		string[] parts = encodedString.Split('|');

		// Extract values from parts.
		string lobbyName = parts[0];
		DateTime startTime = DateTime.ParseExact(parts[1], "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
		int playerCount = int.Parse(parts[2]);
		int playerId = int.Parse(parts[3]);
		Dictionary<string, string> metadata = DecodeMeta(parts[4]);

		// Create and return the dictionary.
		var decodedData = new Dictionary<string, object>
		{
			{ "lobbyName", lobbyName },
			{ "startTime", startTime },
			{ "playerCount", playerCount },
			{ "playerId", playerId },
			{ "metadata", metadata }
		};

		return decodedData;
	}

	/// <summary>
	/// Convert a string representation of metadata back into a dictionary(string, string).
	/// </summary>
	/// <param name="encodedMeta"></param>
	/// <returns></returns>
	private static Dictionary<string, string> DecodeMeta(string encodedMeta)
	{
		// Convert the encoded meta string back to the dictionary
		var meta = new Dictionary<string, string>();
		string[] entries = encodedMeta.Split(';', StringSplitOptions.RemoveEmptyEntries);

		foreach (var entry in entries)
		{
			string[] keyValue = entry.Split(':');
			if (keyValue.Length == 2)
			{
				meta[keyValue[0]] = keyValue[1];
			}
		}

		return meta;
	}
}

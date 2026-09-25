using System;
using System.Linq;

namespace MPDCtrl.Services;

internal static class MpdProtocol
{
    public static string Quote(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Any(char.IsControl))
        {
            throw new ArgumentException(
                "MPD arguments cannot contain control characters.",
                nameof(value));
        }

        return "\"" +
            value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal) +
            "\"";
    }

    public static int ParseId(string value, string argumentName)
    {
        if (!int.TryParse(value, out var id) || id < 0)
        {
            throw new ArgumentException(
                $"{argumentName} must be a non-negative integer.",
                argumentName);
        }

        return id;
    }

    public static string ValidateCommand(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        foreach (var character in command)
        {
            if (character == '\r' ||
                character == '\0' ||
                char.IsControl(character) && character != '\n')
            {
                throw new InvalidOperationException(
                    "The MPD command contains an invalid control character.");
            }
        }

        return command;
    }
}
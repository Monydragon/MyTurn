using System.Reflection;

namespace MyTurn.Domain;

public static class DisplayNameExtensions
{
    public static string GetDisplayName<T>(this T value) where T : Enum
    {
        var field = typeof(T).GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayNameAttribute>();

        return attribute?.Name ?? value.ToString();
    }
}

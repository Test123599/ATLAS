using System.Text;

namespace MediaLib.Web.Services;

public static class QueryStringHelper
{
    public static string Build(params (string Name, object? Value)[] parameters)
    {
        var parts = new List<string>();
        foreach (var (name, value) in parameters)
        {
            if (value is null) continue;
            var text = value is bool b ? (b ? "true" : "false") : value.ToString();
            if (string.IsNullOrWhiteSpace(text)) continue;
            parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(text)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}

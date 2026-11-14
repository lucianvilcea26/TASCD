using Microsoft.AspNetCore.Html;

namespace Universitate.Shared;

/// <summary>
/// Small inline SVG icons (24x24, stroked) for both apps, so the UI needs no icon font or CDN.
/// Usage in Razor: @Icons.Svg("book")
/// </summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["university"] = "M3 21h18M5 21v-9M9.5 21v-9M14.5 21v-9M19 21v-9M2 9.5 12 3l10 6.5Z",
        ["cap"] = "M22 10 12 5 2 10l10 5 10-5ZM6 12.5V17c3.5 2.5 8.5 2.5 12 0v-4.5",
        ["book"] = "M4 19.5V5a2 2 0 0 1 2-2h14v16H6.5A2.5 2.5 0 0 0 4 19.5Zm0 0A2.5 2.5 0 0 0 6.5 22H20",
        ["books"] = "M4 4h3v16H4zM9.5 4h3v16h-3zM15 5.2l2.9-.8 3.6 14.6-2.9.8z",
        ["users"] = "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75",
        ["user-plus"] = "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM19 8v6M22 11h-6",
        ["upload"] = "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12",
        ["check"] = "M20 6 9 17l-5-5",
        ["x"] = "M18 6 6 18M6 6l12 12",
        ["lock"] = "M5 11h14v10H5zM8 11V7a4 4 0 0 1 8 0v4",
        ["shield"] = "M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z",
        ["logout"] = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9",
        ["login"] = "M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3",
        ["clock"] = "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20ZM12 6v6l4 2",
        ["mail"] = "M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2ZM22 6l-10 7L2 6",
        ["server"] = "M4 3h16v7H4zM4 14h16v7H4zM8 6.5h.01M8 17.5h.01",
        ["code"] = "M16 18l6-6-6-6M8 6l-6 6 6 6",
        ["list"] = "M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01",
        ["arrow-right"] = "M5 12h14M12 5l7 7-7 7",
        ["search"] = "M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14ZM20 20l-3.5-3.5",
        ["file"] = "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6M8 13h8M8 17h5",
        ["key"] = "M15 7a3 3 0 1 1-6 0 3 3 0 0 1 6 0ZM12 10v11M12 15h3M12 18h2",
        ["external"] = "M15 3h6v6M10 14 21 3M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6",
        ["counter"] = "M3 10h18M5 10v10M19 10v10M3 20h18M8 6h8M12 6V3",
        ["ban"] = "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20ZM4.9 4.9l14.2 14.2",
        ["refresh"] = "M21 12a9 9 0 0 1-15.4 6.4L3 16M3 12a9 9 0 0 1 15.4-6.4L21 8M21 3v5h-5M3 21v-5h5",
    };

    public static IHtmlContent Svg(string name, string cssClass = "u-ico") =>
        new HtmlString(
            $"<svg class=\"{cssClass}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" " +
            $"stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"{Paths[name]}\"/></svg>");

    /// <summary>Two-letter initials for an avatar: "Ana Popescu" → "AP", "secretara" → "SE".</summary>
    public static string Initials(string? name)
    {
        var parts = (name ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
            : (parts.Length == 1 ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant() : "?");
    }
}

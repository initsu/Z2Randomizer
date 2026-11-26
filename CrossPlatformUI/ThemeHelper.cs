using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace CrossPlatformUI;

/// Selects Avalonia's theme variant and the app's flag-validation brushes.
public class ThemeHelper
{
    public static bool IsDark(string? theme)
    {
        switch (theme)
        {
            case "Dark":
                return true;
            case "Light":
                return false;
            default:
                var app = App.Current;
                if (app == null)
                {
                    return false;
                }
                // Workaround for `ActualThemeVariant` not being set yet on startup in Linux 
                var platformTheme = app.PlatformSettings?.GetColorValues().ThemeVariant;
                if (platformTheme is not null)
                {
                    return platformTheme == PlatformThemeVariant.Dark;
                }
                return app.ActualThemeVariant == ThemeVariant.Dark;
        }
    }

    public static void SetTheme(string? name)
    {
        var app = App.Current;
        if (app == null)
        {
            return;
        }

        app.RequestedThemeVariant = name switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };

    }

    public static IBrush GetFlagAlertBackgroundBrush(string? theme, bool alert)
    {
        if (alert)
        {
            return GetPaletteBrush("Z2FlagAlertBrush", theme, Brushes.Transparent);
        }
        else
        {
            return Brushes.Transparent;
        }
    }

    public static IBrush GetFlagBorderBrush(string? theme, bool valid)
    {
        var app = App.Current;
        if (app != null && app.TryFindResource(valid ? "Z2TextBoxOutline" : "Z2TextBoxError", out var brush) && brush is IBrush b)
        {
            return b;
        }
        return valid ? Brushes.Gray : GetPaletteBrush("Z2FlagInvalidFallbackBrush", theme, Brushes.Red);
    }

    private static IBrush GetPaletteBrush(string key, string? theme, IBrush fallback)
    {
        var variant = IsDark(theme) ? ThemeVariant.Dark : ThemeVariant.Light;
        return App.Current is { } app && app.TryFindResource(key, variant, out var value) && value is IBrush brush
            ? brush
            : fallback;
    }
}

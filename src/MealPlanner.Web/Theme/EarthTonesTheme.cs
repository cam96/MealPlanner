using MudBlazor;

namespace MealPlanner.Web.Theme;

/// <summary>
/// Central definition of the application's earth-tones visual identity: warm browns, terracotta,
/// olive/sage greens, muted ochre/tan accents, and cream backgrounds. Applied through a single
/// <see cref="MudTheme"/> consumed by the root <c>MudThemeProvider</c>.
/// </summary>
public static class EarthTonesTheme
{
    /// <summary>Gets the shared MudBlazor theme instance for the application.</summary>
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#B5623A",          // terracotta
            Secondary = "#7C8C4E",        // olive / sage green
            Tertiary = "#C89B5B",         // muted ochre / tan
            Info = "#6E8B8B",             // muted teal
            Success = "#6B8E23",          // olive drab
            Warning = "#CB8A2E",          // amber ochre
            Error = "#A63A2A",            // brick red
            Background = "#FBF6EE",       // cream
            BackgroundGray = "#F1E9DC",
            Surface = "#FFFDF8",
            AppbarBackground = "#5C4433", // deep warm brown
            AppbarText = "#F7EFE3",
            DrawerBackground = "#EFE6D6",
            DrawerText = "#3E2F23",
            DrawerIcon = "#7A5C43",
            TextPrimary = "#3E2F23",      // dark cocoa
            TextSecondary = "#6F5B49",
            ActionDefault = "#7A5C43",
            Divider = "#E0D5C3",
            LinesDefault = "#E0D5C3",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Nunito Sans", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif"],
            },
        },
    };
}

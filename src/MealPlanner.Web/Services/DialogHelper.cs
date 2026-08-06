using MudBlazor;

namespace MealPlanner.Web.Services;

/// <summary>
/// Provides consistent dialog options for the app. Form dialogs open full-screen to maximize
/// usability on mobile devices (the primary use case).
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Gets dialog options for form dialogs. Opens full-screen for mobile-first UX.
    /// </summary>
    /// <returns>Dialog options configured for form input.</returns>
    public static DialogOptions FormDialog() => new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        FullScreen = true,
        CloseOnEscapeKey = true,
        BackdropClick = false,
    };
}

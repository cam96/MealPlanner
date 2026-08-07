using MudBlazor;

namespace MealPlanner.Web.Services;

/// <summary>
/// Provides consistent dialog options for the app. Dialogs are constrained to
/// <see cref="MaxWidth.Small"/> on desktop; a CSS media query in <c>app.css</c> promotes
/// them to full-screen on mobile (≤ 599.98 px) for comfortable touch input.
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Gets dialog options for form dialogs. Backdrop clicks are disabled so users
    /// don't accidentally lose input.
    /// </summary>
    /// <returns>Dialog options configured for form input.</returns>
    public static DialogOptions FormDialog() => new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseOnEscapeKey = true,
        BackdropClick = false,
    };

    /// <summary>
    /// Gets dialog options for read-only / informational dialogs. Backdrop clicks
    /// are allowed so the user can dismiss the dialog easily.
    /// </summary>
    /// <returns>Dialog options configured for viewing content.</returns>
    public static DialogOptions ViewDialog() => new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseOnEscapeKey = true,
    };
}

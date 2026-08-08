using System.Net.Http.Headers;

namespace MealPlanner.Web.Services;

/// <summary>
/// A delegating handler that attaches the JWT Bearer token to outbound HTTP requests to the API.
/// The token is obtained from <see cref="JwtTokenService"/> which derives it from the current
/// user's authenticated session.
/// </summary>
public sealed class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly JwtTokenService _tokenService;

    /// <summary>Initializes a new instance of the <see cref="JwtAuthorizationHandler"/> class.</summary>
    /// <param name="tokenService">The service that provides JWT tokens for the current user.</param>
    public JwtAuthorizationHandler(JwtTokenService tokenService)
    {
        ArgumentNullException.ThrowIfNull(tokenService);
        _tokenService = tokenService;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetTokenAsync(cancellationToken);

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

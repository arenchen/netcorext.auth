using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Authorization.Models;
using Netcorext.Auth.Authorization.Services.Token.Commands;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Contracts.AspNetCore;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Controllers;

[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
[Permission("AUTH")]
public class TokenController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public TokenController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    [ProducesResponseType(typeof(TokenResult), 200, "application/json")]
    [ProducesResponseType(typeof(TokenResult), 400, "application/json")]
    [ProducesResponseType(typeof(TokenResult), 401, "application/json")]
    public async Task<IActionResult> PostAsync(CreateToken request, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(request, cancellationToken);
    }

    private async Task<IActionResult> ExecuteAsync(CreateToken request, CancellationToken cancellationToken = default)
    {
        var headerValue = Request.Headers["Authorization"];

        if (!AuthenticationHeaderValue.TryParse(headerValue, out var authHeader) && request.ClientId.IsEmpty() && request.ClientSecret.IsEmpty())
            return new TokenResult
                   {
                       Error = Constants.OAuth.INVALID_REQUEST,
                       ErrorDescription = Constants.OAuth.INVALID_REQUEST_TOKEN
                   }.ToActionResult(400);

        var clientId = request.ClientId;
        var clientSecret = request.ClientSecret;

        if (authHeader != null && authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            var token = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter)).Split(":", StringSplitOptions.RemoveEmptyEntries);

            if (token.Length == 2)
            {
                clientId = token[0];
                clientSecret = token[1];
            }
        }

        request.ClientId ??= clientId;
        request.ClientSecret ??= clientSecret;

        var result = await _dispatcher.SendAsync(request, cancellationToken);

        return result.ToActionResult("Content");
    }
}
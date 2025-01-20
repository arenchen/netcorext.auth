using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Netcorext.Auth.Authorization.Models;
using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authorization.Services.Token.Commands;

public class CreateToken : IRequest<Result<TokenResult>>
{
    [FromForm(Name = "grant_type")]
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = null!;

    [FromForm(Name = "client_id")]
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [FromForm(Name = "username")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [FromForm(Name = "password")]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [FromForm(Name = "unique_id")]
    [JsonPropertyName("unique_id")]
    public string? UniqueId { get; set; }

    [FromForm(Name = "scope")]
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "refresh_token")]
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [FromForm(Name = "include_roles_info")]
    [JsonPropertyName("include_roles_info")]
    public bool IncludeRolesInfo { get; set; }
}
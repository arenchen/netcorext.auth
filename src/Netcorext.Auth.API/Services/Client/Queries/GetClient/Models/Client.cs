namespace Netcorext.Auth.API.Services.Client.Models;

public class Client
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? CallbackUrl { get; set; }
    public int? TokenExpireSeconds { get; set; }
    public int? RefreshTokenExpireSeconds { get; set; }
    public int? CodeExpireSeconds { get; set; }
    public bool Disabled { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public long CreatorId { get; set; }
    public DateTimeOffset ModificationDate { get; set; }
    public long ModifierId { get; set; }
    public IEnumerable<ClientRole>? Roles { get; set; }
    public IEnumerable<ClientExtendData>? ExtendData { get; set; }
}
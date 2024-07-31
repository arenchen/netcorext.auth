namespace Netcorext.Auth.Authentication.Services.Client.Queries.Models;

public class Client
{
    public long Id { get; set; }
    public string Secret { get; set; } = null!;
    public Dictionary<long, DateTimeOffset?> Roles { get; set; } = new Dictionary<long, DateTimeOffset?>();
    public DateTimeOffset CreationDate { get; set; }
}

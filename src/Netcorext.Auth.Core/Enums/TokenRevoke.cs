namespace Netcorext.Auth.Enums;

[Flags]
public enum TokenRevoke
{
    None = 0,
    AccessToken = 1,
    RefreshToken = 2,
    Both = AccessToken | RefreshToken
}

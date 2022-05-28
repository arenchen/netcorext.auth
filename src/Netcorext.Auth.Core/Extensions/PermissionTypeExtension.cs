using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Extensions;

public static class PermissionTypeExtension
{
    public static Protobufs.Enums.PermissionType ToProtobufPermissionType(this PermissionType permissionType)
    {
        return permissionType switch
               {
                   PermissionType.Read => Protobufs.Enums.PermissionType.Read,
                   PermissionType.Write => Protobufs.Enums.PermissionType.Write,
                   PermissionType.ReadWrite => Protobufs.Enums.PermissionType.ReadWrite,
                   PermissionType.Delete => Protobufs.Enums.PermissionType.Delete,
                   PermissionType.ReadDelete => Protobufs.Enums.PermissionType.ReadDelete,
                   PermissionType.WriteDelete => Protobufs.Enums.PermissionType.WriteDelete,
                   PermissionType.All => Protobufs.Enums.PermissionType.All,
                   _ => Protobufs.Enums.PermissionType.None
               };
    }

    public static PermissionType ToPermissionType(this Protobufs.Enums.PermissionType permissionType)
    {
        return permissionType switch
               {
                   Protobufs.Enums.PermissionType.Read => PermissionType.Read,
                   Protobufs.Enums.PermissionType.Write => PermissionType.Write,
                   Protobufs.Enums.PermissionType.ReadWrite => PermissionType.ReadWrite,
                   Protobufs.Enums.PermissionType.Delete => PermissionType.Delete,
                   Protobufs.Enums.PermissionType.ReadDelete => PermissionType.ReadDelete,
                   Protobufs.Enums.PermissionType.WriteDelete => PermissionType.WriteDelete,
                   Protobufs.Enums.PermissionType.All => PermissionType.All,
                   _ => PermissionType.None
               };
    }

    public static PermissionType ToPermissionType(this string? httpMethod)
    {
        return httpMethod?.ToUpper() switch
               {
                   "DELETE" => PermissionType.Delete,
                   "GET" => PermissionType.Read,
                   "HEAD" => PermissionType.Read,
                   "OPTIONS" => PermissionType.Read,
                   "PATCH" => PermissionType.Write,
                   "POST" => PermissionType.Write,
                   "PUT" => PermissionType.Write,
                   _ => PermissionType.None
               };
    }
}
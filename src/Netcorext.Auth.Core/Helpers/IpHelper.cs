using System.Net;
using System.Numerics;

namespace Netcorext.Auth.Helpers;

public static class IpHelper
{
    public static BigInteger ConvertToNumber(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address)) return default;

        return ConvertToNumber(address);
    }

    public static BigInteger ConvertToNumber(IPAddress ip)
    {
        var result = default(BigInteger);

        var addrBytes = ip.GetAddressBytes();

        if (BitConverter.IsLittleEndian)
        {
            var byteList = new List<byte>(addrBytes);

            byteList.Reverse();

            addrBytes = byteList.ToArray();
        }

        if (addrBytes.Length > 8)
        {
            //IPv6
            result = BitConverter.ToUInt64(addrBytes, 8);
            result <<= 64;
            result += BitConverter.ToUInt64(addrBytes, 0);
        }
        else
        {
            //IPv4
            result = BitConverter.ToUInt32(addrBytes, 0);
        }

        return result;
    }
}
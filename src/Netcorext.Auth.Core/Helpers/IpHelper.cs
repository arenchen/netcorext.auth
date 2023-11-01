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
        BigInteger result;

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

    public static (long BeginRange, long EndRange, long Mask) ParseCidrToRange(string cidr)
    {
        var inCidr = cidr;

        if (!inCidr.Contains('/')) inCidr += "/32";

        var cidrParts = inCidr.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (cidrParts.Length != 2) return default;

        if (!uint.TryParse(cidrParts[1], out var prefixLength)) return default;

        if (prefixLength > 32) return default;

        var cidrIp = cidrParts[0];

        var cidrIpParts = cidrIp.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (cidrIpParts.Length != 4) return default;

        var cidrIpBytes = cidrIpParts.Reverse().Select(byte.Parse).ToArray();

        var cidrIpLong = BitConverter.ToInt32(cidrIpBytes, 0);

        var subnetMask = (uint.MaxValue << (int)(32 - prefixLength)) & uint.MaxValue;

        var cidrIpLongBegin = cidrIpLong & subnetMask;

        var cidrIpLongEnd = (uint)cidrIpLong | ~subnetMask;

        return (cidrIpLongBegin, cidrIpLongEnd, subnetMask);
    }

    public static (IPAddress BeginIp, IPAddress EndIp, IPAddress Mask) ParseCidrToIpRange(string cidr)
    {
        var (beginRange, endRange, mask) = ParseCidrToRange(cidr);

        var beginBytes = BitConverter.GetBytes(beginRange);
        var endBytes = BitConverter.GetBytes(endRange);
        var maskBytes = BitConverter.GetBytes(mask);

        if (BitConverter.IsLittleEndian)
        {
            var byteList = new List<byte>(beginBytes);

            byteList.Reverse();

            beginBytes = byteList.ToArray();

            byteList = new List<byte>(endBytes);

            byteList.Reverse();

            endBytes = byteList.ToArray();

            byteList = new List<byte>(maskBytes);

            byteList.Reverse();

            maskBytes = byteList.ToArray();
        }

        var beginIp = new IPAddress(beginBytes);
        var endIp = new IPAddress(endBytes);
        var maskIp = new IPAddress(maskBytes);

        return (beginIp, endIp, maskIp);
    }
}

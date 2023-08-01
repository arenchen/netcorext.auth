namespace Netcorext.Auth.Helpers;

public static class MathHelper
{
    public static int? DifferentBetween(int? a, int? b, bool isAbs = true)
    {
        if (a == null || b == null)
        {
            return null;
        }

        return isAbs ? Math.Abs(a.Value - b.Value) : a.Value - b.Value;
    }
}
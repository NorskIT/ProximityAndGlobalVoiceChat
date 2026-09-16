namespace Concentus.Enums;

internal static class OpusBandwidthHelpers
{
	internal static int GetOrdinal(OpusBandwidth bw)
	{
		return (int)(bw - 1101);
	}

	internal static OpusBandwidth MIN(OpusBandwidth a, OpusBandwidth b)
	{
		if (a < b)
		{
			return a;
		}
		return b;
	}

	internal static OpusBandwidth MAX(OpusBandwidth a, OpusBandwidth b)
	{
		if (a > b)
		{
			return a;
		}
		return b;
	}
}

using Concentus.Common.CPlusPlus;

namespace Concentus.Silk.Structs;

internal class TOCStruct
{
	internal int VADFlag;

	internal readonly int[] VADFlags = new int[3];

	internal int inbandFECFlag;

	internal void Reset()
	{
		VADFlag = 0;
		Arrays.MemSetInt(VADFlags, 0, 3);
		inbandFECFlag = 0;
	}
}

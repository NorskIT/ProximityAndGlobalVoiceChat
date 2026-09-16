using Concentus.Common.CPlusPlus;

namespace Concentus.Silk.Structs;

internal class SilkVADState
{
	internal readonly int[] AnaState = new int[2];

	internal readonly int[] AnaState1 = new int[2];

	internal readonly int[] AnaState2 = new int[2];

	internal readonly int[] XnrgSubfr = new int[4];

	internal readonly int[] NrgRatioSmth_Q8 = new int[4];

	internal short HPstate;

	internal readonly int[] NL = new int[4];

	internal readonly int[] inv_NL = new int[4];

	internal readonly int[] NoiseLevelBias = new int[4];

	internal int counter;

	internal void Reset()
	{
		Arrays.MemSetInt(AnaState, 0, 2);
		Arrays.MemSetInt(AnaState1, 0, 2);
		Arrays.MemSetInt(AnaState2, 0, 2);
		Arrays.MemSetInt(XnrgSubfr, 0, 4);
		Arrays.MemSetInt(NrgRatioSmth_Q8, 0, 4);
		HPstate = 0;
		Arrays.MemSetInt(NL, 0, 4);
		Arrays.MemSetInt(inv_NL, 0, 4);
		Arrays.MemSetInt(NoiseLevelBias, 0, 4);
		counter = 0;
	}
}

using System;
using Concentus.Common.CPlusPlus;

namespace Concentus.Silk.Structs;

internal class SideInfoIndices
{
	internal readonly sbyte[] GainsIndices = new sbyte[4];

	internal readonly sbyte[] LTPIndex = new sbyte[4];

	internal readonly sbyte[] NLSFIndices = new sbyte[17];

	internal short lagIndex;

	internal sbyte contourIndex;

	internal sbyte signalType;

	internal sbyte quantOffsetType;

	internal sbyte NLSFInterpCoef_Q2;

	internal sbyte PERIndex;

	internal sbyte LTP_scaleIndex;

	internal sbyte Seed;

	internal void Reset()
	{
		Arrays.MemSetSbyte(GainsIndices, 0, 4);
		Arrays.MemSetSbyte(LTPIndex, 0, 4);
		Arrays.MemSetSbyte(NLSFIndices, 0, 17);
		lagIndex = 0;
		contourIndex = 0;
		signalType = 0;
		quantOffsetType = 0;
		NLSFInterpCoef_Q2 = 0;
		PERIndex = 0;
		LTP_scaleIndex = 0;
		Seed = 0;
	}

	internal void Assign(SideInfoIndices other)
	{
		Array.Copy(other.GainsIndices, GainsIndices, 4);
		Array.Copy(other.LTPIndex, LTPIndex, 4);
		Array.Copy(other.NLSFIndices, NLSFIndices, 17);
		lagIndex = other.lagIndex;
		contourIndex = other.contourIndex;
		signalType = other.signalType;
		quantOffsetType = other.quantOffsetType;
		NLSFInterpCoef_Q2 = other.NLSFInterpCoef_Q2;
		PERIndex = other.PERIndex;
		LTP_scaleIndex = other.LTP_scaleIndex;
		Seed = other.Seed;
	}
}

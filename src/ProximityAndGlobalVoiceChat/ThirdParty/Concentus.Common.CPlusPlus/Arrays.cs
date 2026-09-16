using System;

namespace Concentus.Common.CPlusPlus;

internal static class Arrays
{
	internal static T[][] InitTwoDimensionalArray<T>(int x, int y)
	{
		T[][] array = new T[x][];
		for (int i = 0; i < x; i++)
		{
			array[i] = new T[y];
		}
		return array;
	}

	internal static Pointer<Pointer<T>> InitTwoDimensionalArrayPointer<T>(int x, int y)
	{
		Pointer<Pointer<T>> pointer = Pointer.Malloc<Pointer<T>>(x);
		for (int i = 0; i < x; i++)
		{
			pointer[i] = Pointer.Malloc<T>(y);
		}
		return pointer;
	}

	internal static T[][][] InitThreeDimensionalArray<T>(int x, int y, int z)
	{
		T[][][] array = new T[x][][];
		for (int i = 0; i < x; i++)
		{
			array[i] = new T[y][];
			for (int j = 0; j < y; j++)
			{
				array[i][j] = new T[z];
			}
		}
		return array;
	}

	internal static void MemSetByte(byte[] array, byte value)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = value;
		}
	}

	internal static void MemSetInt(int[] array, int value, int length)
	{
		for (int i = 0; i < length; i++)
		{
			array[i] = value;
		}
	}

	internal static void MemSetShort(short[] array, short value, int length)
	{
		for (int i = 0; i < length; i++)
		{
			array[i] = value;
		}
	}

	internal static void MemSetFloat(float[] array, float value, int length)
	{
		for (int i = 0; i < length; i++)
		{
			array[i] = value;
		}
	}

	internal static void MemSetSbyte(sbyte[] array, sbyte value, int length)
	{
		for (int i = 0; i < length; i++)
		{
			array[i] = value;
		}
	}

	internal static void MemSetWithOffset<T>(T[] array, T value, int offset, int length)
	{
		for (int i = offset; i < offset + length; i++)
		{
			array[i] = value;
		}
	}

	internal static void MemMove<T>(T[] array, int src_idx, int dst_idx, int length)
	{
		if (src_idx == dst_idx || length == 0)
		{
			return;
		}
		if (src_idx + length > dst_idx || dst_idx + length > src_idx)
		{
			if (dst_idx < src_idx)
			{
				for (int i = 0; i < length; i++)
				{
					array[i + dst_idx] = array[i + src_idx];
				}
				return;
			}
			for (int num = length - 1; num >= 0; num--)
			{
				array[num + dst_idx] = array[num + src_idx];
			}
		}
		else
		{
			Array.Copy(array, src_idx, array, dst_idx, length);
		}
	}

	internal static void MemMoveInt(int[] array, int src_idx, int dst_idx, int length)
	{
		if (src_idx == dst_idx || length == 0)
		{
			return;
		}
		if (src_idx + length > dst_idx || dst_idx + length > src_idx)
		{
			if (dst_idx < src_idx)
			{
				for (int i = 0; i < length; i++)
				{
					array[i + dst_idx] = array[i + src_idx];
				}
				return;
			}
			for (int num = length - 1; num >= 0; num--)
			{
				array[num + dst_idx] = array[num + src_idx];
			}
		}
		else
		{
			Array.Copy(array, src_idx, array, dst_idx, length);
		}
	}

	internal static void MemMoveShort(short[] array, int src_idx, int dst_idx, int length)
	{
		if (src_idx == dst_idx || length == 0)
		{
			return;
		}
		if (src_idx + length > dst_idx || dst_idx + length > src_idx)
		{
			if (dst_idx < src_idx)
			{
				for (int i = 0; i < length; i++)
				{
					array[i + dst_idx] = array[i + src_idx];
				}
				return;
			}
			for (int num = length - 1; num >= 0; num--)
			{
				array[num + dst_idx] = array[num + src_idx];
			}
		}
		else
		{
			Array.Copy(array, src_idx, array, dst_idx, length);
		}
	}
}

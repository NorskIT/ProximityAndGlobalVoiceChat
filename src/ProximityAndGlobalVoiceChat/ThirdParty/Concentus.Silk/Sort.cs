namespace Concentus.Silk;

internal static class Sort
{
	internal static void silk_insertion_sort_increasing(int[] a, int[] idx, int L, int K)
	{
		for (int i = 0; i < K; i++)
		{
			idx[i] = i;
		}
		for (int i = 1; i < K; i++)
		{
			int num = a[i];
			int num2 = i - 1;
			while (num2 >= 0 && num < a[num2])
			{
				a[num2 + 1] = a[num2];
				idx[num2 + 1] = idx[num2];
				num2--;
			}
			a[num2 + 1] = num;
			idx[num2 + 1] = i;
		}
		for (int i = K; i < L; i++)
		{
			int num = a[i];
			if (num < a[K - 1])
			{
				int num2 = K - 2;
				while (num2 >= 0 && num < a[num2])
				{
					a[num2 + 1] = a[num2];
					idx[num2 + 1] = idx[num2];
					num2--;
				}
				a[num2 + 1] = num;
				idx[num2 + 1] = i;
			}
		}
	}

	internal static void silk_insertion_sort_increasing_all_values_int16(short[] a, int L)
	{
		for (int i = 1; i < L; i++)
		{
			short num = a[i];
			int num2 = i - 1;
			while (num2 >= 0 && num < a[num2])
			{
				a[num2 + 1] = a[num2];
				num2--;
			}
			a[num2 + 1] = num;
		}
	}

	internal static void silk_insertion_sort_decreasing_int16(short[] a, int[] idx, int L, int K)
	{
		for (int i = 0; i < K; i++)
		{
			idx[i] = i;
		}
		for (int i = 1; i < K; i++)
		{
			short num = a[i];
			int num2 = i - 1;
			while (num2 >= 0 && num > a[num2])
			{
				a[num2 + 1] = a[num2];
				idx[num2 + 1] = idx[num2];
				num2--;
			}
			a[num2 + 1] = num;
			idx[num2 + 1] = i;
		}
		for (int i = K; i < L; i++)
		{
			short num = a[i];
			if (num > a[K - 1])
			{
				int num2 = K - 2;
				while (num2 >= 0 && num > a[num2])
				{
					a[num2 + 1] = a[num2];
					idx[num2 + 1] = idx[num2];
					num2--;
				}
				a[num2 + 1] = num;
				idx[num2 + 1] = i;
			}
		}
	}
}

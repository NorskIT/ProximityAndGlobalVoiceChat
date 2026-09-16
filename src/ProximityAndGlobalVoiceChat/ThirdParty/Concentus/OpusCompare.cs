using System;
using Concentus.Common.CPlusPlus;

namespace Concentus;

internal static class OpusCompare
{
	private const int NBANDS = 21;

	private const int NFREQS = 240;

	private const int TEST_WIN_SIZE = 480;

	private const int TEST_WIN_STEP = 120;

	private static readonly int[] BANDS = new int[22]
	{
		0, 2, 4, 6, 8, 10, 12, 14, 16, 20,
		24, 28, 32, 40, 48, 56, 68, 80, 96, 120,
		156, 200
	};

	private static void band_energy(Pointer<float> _out, Pointer<float> _ps, Pointer<int> _bands, int _nbands, Pointer<float> _in, int _nchannels, int _nframes, int _window_sz, int _step, int _downsample)
	{
		Pointer<float> pointer = Pointer.Malloc<float>((3 + _nchannels) * _window_sz);
		Pointer<float> pointer2 = pointer.Point(_window_sz);
		Pointer<float> pointer3 = pointer2.Point(_window_sz);
		Pointer<float> pointer4 = pointer3.Point(_window_sz);
		int num = _window_sz / 2;
		for (int i = 0; i < _window_sz; i++)
		{
			pointer[i] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * 2.0 / (double)(_window_sz - 1) * (double)i));
		}
		for (int i = 0; i < _window_sz; i++)
		{
			pointer2[i] = (float)Math.Cos(Math.PI * 2.0 / (double)_window_sz * (double)i);
		}
		for (int i = 0; i < _window_sz; i++)
		{
			pointer3[i] = (float)Math.Sin(Math.PI * 2.0 / (double)_window_sz * (double)i);
		}
		for (int j = 0; j < _nframes; j++)
		{
			for (int k = 0; k < _nchannels; k++)
			{
				for (int l = 0; l < _window_sz; l++)
				{
					pointer4[k * _window_sz + l] = pointer[l] * _in[(j * _step + l) * _nchannels + k];
				}
			}
			int i;
			for (int m = (i = 0); m < _nbands; m++)
			{
				float[] array = new float[2];
				for (; i < _bands[m + 1]; i++)
				{
					for (int k = 0; k < _nchannels; k++)
					{
						int num2 = 0;
						float num4;
						float num3 = (num4 = 0f);
						for (int l = 0; l < _window_sz; l++)
						{
							num3 += pointer2[num2] * pointer4[k * _window_sz + l];
							num4 -= pointer3[num2] * pointer4[k * _window_sz + l];
							num2 += i;
							if (num2 >= _window_sz)
							{
								num2 -= _window_sz;
							}
						}
						num3 *= (float)_downsample;
						num4 *= (float)_downsample;
						_ps[(j * num + i) * _nchannels + k] = num3 * num3 + num4 * num4 + 100000f;
						array[k] += _ps[(j * num + i) * _nchannels + k];
					}
				}
				if (_out != null)
				{
					_out[(j * _nbands + m) * _nchannels] = array[0] / (float)(_bands[m + 1] - _bands[m]);
					if (_nchannels == 2)
					{
						_out[(j * _nbands + m) * _nchannels + 1] = array[1] / (float)(_bands[m + 1] - _bands[m]);
					}
				}
			}
		}
	}

	internal static float compare(float[] x, float[] y, int nchannels, int rate = 48000)
	{
		int num = x.Length;
		int num2 = y.Length;
		int num3 = 21;
		int num4 = 240;
		if (rate != 8000 && rate != 12000 && rate != 16000 && rate != 24000 && rate != 48000)
		{
			throw new ArgumentException("Sampling rate must be 8000, 12000, 16000, 24000, or 48000\n");
		}
		int num5;
		if (rate != 48000)
		{
			num5 = 48000 / rate;
			switch (rate)
			{
			case 8000:
				num3 = 13;
				break;
			case 12000:
				num3 = 15;
				break;
			case 16000:
				num3 = 17;
				break;
			case 24000:
				num3 = 19;
				break;
			}
			num4 = 240 / num5;
		}
		else
		{
			num5 = 1;
		}
		if (num != num2 * num5)
		{
			throw new ArgumentException("Sample counts do not match");
		}
		if (num < 480)
		{
			throw new ArgumentException("Insufficient sample data");
		}
		int num6 = (num - 480 + 120) / 120;
		Pointer<float> pointer = Pointer.Malloc<float>(num6 * 21 * nchannels);
		Pointer<float> pointer2 = Pointer.Malloc<float>(num6 * 240 * nchannels);
		Pointer<float> pointer3 = Pointer.Malloc<float>(num6 * num4 * nchannels);
		band_energy(pointer, pointer2, BANDS.GetPointer(), 21, x.GetPointer(), nchannels, num6, 480, 120, 1);
		band_energy(null, pointer3, BANDS.GetPointer(), num3, y.GetPointer(), nchannels, num6, 480 / num5, 120 / num5, num5);
		for (int i = 0; i < num6; i++)
		{
			int j;
			for (j = 1; j < 21; j++)
			{
				for (int k = 0; k < nchannels; k++)
				{
					pointer[(i * 21 + j) * nchannels + k] += 0.1f * pointer[(i * 21 + j - 1) * nchannels + k];
				}
			}
			j = 20;
			while (j-- > 0)
			{
				for (int k = 0; k < nchannels; k++)
				{
					pointer[(i * 21 + j) * nchannels + k] += 0.03f * pointer[(i * 21 + j + 1) * nchannels + k];
				}
			}
			if (i > 0)
			{
				for (j = 0; j < 21; j++)
				{
					for (int k = 0; k < nchannels; k++)
					{
						pointer[(i * 21 + j) * nchannels + k] += 0.5f * pointer[((i - 1) * 21 + j) * nchannels + k];
					}
				}
			}
			if (nchannels == 2)
			{
				for (j = 0; j < 21; j++)
				{
					float num7 = pointer[(i * 21 + j) * nchannels];
					float num8 = pointer[(i * 21 + j) * nchannels + 1];
					pointer[(i * 21 + j) * nchannels] += 0.01f * num8;
					pointer[(i * 21 + j) * nchannels + 1] += 0.01f * num7;
				}
			}
			for (j = 0; j < num3; j++)
			{
				for (int l = BANDS[j]; l < BANDS[j + 1]; l++)
				{
					for (int k = 0; k < nchannels; k++)
					{
						pointer2[(i * 240 + l) * nchannels + k] += 0.1f * pointer[(i * 21 + j) * nchannels + k];
						pointer3[(i * num4 + l) * nchannels + k] += 0.1f * pointer[(i * 21 + j) * nchannels + k];
					}
				}
			}
		}
		for (int j = 0; j < num3; j++)
		{
			for (int l = BANDS[j]; l < BANDS[j + 1]; l++)
			{
				for (int k = 0; k < nchannels; k++)
				{
					float num9 = pointer2[l * nchannels + k];
					float num10 = pointer3[l * nchannels + k];
					for (int i = 1; i < num6; i++)
					{
						float num11 = pointer2[(i * 240 + l) * nchannels + k];
						float num12 = pointer3[(i * num4 + l) * nchannels + k];
						pointer2[(i * 240 + l) * nchannels + k] += num9;
						pointer3[(i * num4 + l) * nchannels + k] += num10;
						num9 = num11;
						num10 = num12;
					}
				}
			}
		}
		int num13 = rate switch
		{
			48000 => BANDS[21], 
			12000 => BANDS[num3], 
			_ => BANDS[num3] - 3, 
		};
		double num14 = 0.0;
		for (int i = 0; i < num6; i++)
		{
			double num15 = 0.0;
			for (int j = 0; j < num3; j++)
			{
				double num16 = 0.0;
				for (int l = BANDS[j]; l < BANDS[j + 1] && l < num13; l++)
				{
					for (int k = 0; k < nchannels; k++)
					{
						float num17 = pointer3[(i * num4 + l) * nchannels + k] / pointer2[(i * 240 + l) * nchannels + k];
						float num18 = num17 - (float)Math.Log(num17) - 1f;
						if (l >= 79 && l <= 81)
						{
							num18 *= 0.1f;
						}
						if (l == 80)
						{
							num18 *= 0.1f;
						}
						num16 += (double)num18;
					}
				}
				num16 /= (double)((BANDS[j + 1] - BANDS[j]) * nchannels);
				num15 += num16 * num16;
			}
			num15 /= 21.0;
			num15 *= num15;
			num14 += num15 * num15;
		}
		num14 = Math.Pow(num14 / (double)num6, 0.0625);
		float result = (float)(100.0 * (1.0 - 0.5 * Math.Log(1.0 + num14) / Math.Log(1.13)));
		_ = 0f;
		return result;
	}
}

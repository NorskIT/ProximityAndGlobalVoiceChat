using System;

namespace Concentus.Common;

internal class SpeexResampler
{
	private class FuncDef
	{
		public double[] table;

		public int oversample;

		public static readonly double[] kaiser12_table = new double[68]
		{
			0.99859849, 1.0, 0.99859849, 0.99440475, 0.98745105, 0.97779076, 0.9654977, 0.95066529, 0.93340547, 0.91384741,
			0.89213598, 0.86843014, 0.84290116, 0.81573067, 0.78710866, 0.75723148, 0.7262997, 0.69451601, 0.66208321, 0.62920216,
			0.59606986, 0.56287762, 0.52980938, 0.49704014, 0.46473455, 0.43304576, 0.40211431, 0.37206735, 0.343018, 0.3150649,
			0.28829195, 0.26276832, 0.23854851, 0.21567274, 0.19416736, 0.17404546, 0.15530766, 0.13794294, 0.12192957, 0.10723616,
			0.09382272, 0.08164178, 0.0706395, 0.06075685, 0.05193064, 0.04409466, 0.03718069, 0.03111947, 0.02584161, 0.02127838,
			0.0173625, 0.01402878, 0.01121463, 0.00886058, 0.00691064, 0.00531256, 0.00401805, 0.00298291, 0.00216702, 0.00153438,
			0.00105297, 0.00069463, 0.00043489, 0.00025272, 0.00013031, 5.27734E-05, 1E-05, 0.0
		};

		public static readonly double[] kaiser10_table = new double[36]
		{
			0.99537781, 1.0, 0.99537781, 0.98162644, 0.95908712, 0.92831446, 0.89005583, 0.84522401, 0.79486424, 0.74011713,
			0.68217934, 0.62226347, 0.56155915, 0.5011968, 0.44221549, 0.38553619, 0.33194107, 0.28205962, 0.23636152, 0.19515633,
			0.15859932, 0.1267028, 0.09935205, 0.07632451, 0.05731132, 0.0419398, 0.02979584, 0.0204451, 0.01345224, 0.00839739,
			0.00488951, 0.00257636, 0.00115101, 0.00035515, 0.0, 0.0
		};

		public static readonly double[] kaiser8_table = new double[36]
		{
			0.99635258, 1.0, 0.99635258, 0.98548012, 0.96759014, 0.943022, 0.91223751, 0.87580811, 0.83439927, 0.78875245,
			0.73966538, 0.68797126, 0.6345175, 0.58014482, 0.52566725, 0.47185369, 0.4194115, 0.36897272, 0.32108304, 0.27619388,
			0.23465776, 0.1967267, 0.1625538, 0.13219758, 0.10562887, 0.08273982, 0.06335451, 0.04724088, 0.03412321, 0.0236949,
			0.01563093, 0.00959968, 0.00527363, 0.00233883, 0.0005, 0.0
		};

		public static readonly double[] kaiser6_table = new double[36]
		{
			0.99733006, 1.0, 0.99733006, 0.98935595, 0.97618418, 0.95799003, 0.93501423, 0.90755855, 0.87598009, 0.84068475,
			0.80211977, 0.76076565, 0.71712752, 0.67172623, 0.62508937, 0.57774224, 0.53019925, 0.48295561, 0.43647969, 0.39120616,
			0.34752997, 0.30580127, 0.26632152, 0.22934058, 0.19505503, 0.16360756, 0.13508755, 0.10953262, 0.0869312, 0.067226,
			0.0503182, 0.03607231, 0.02432151, 0.01487334, 0.00752, 0.0
		};

		public FuncDef(double[] t, int os)
		{
			table = t;
			oversample = os;
		}
	}

	private class QualityMapping
	{
		public int base_length;

		public int oversample;

		public float downsample_bandwidth;

		public float upsample_bandwidth;

		public FuncDef window_func;

		public static readonly QualityMapping[] quality_map = new QualityMapping[11]
		{
			new QualityMapping(8, 4, 0.83f, 0.86f, new FuncDef(FuncDef.kaiser6_table, 32)),
			new QualityMapping(16, 4, 0.85f, 0.88f, new FuncDef(FuncDef.kaiser6_table, 32)),
			new QualityMapping(32, 4, 0.882f, 0.91f, new FuncDef(FuncDef.kaiser6_table, 32)),
			new QualityMapping(48, 8, 0.895f, 0.917f, new FuncDef(FuncDef.kaiser8_table, 32)),
			new QualityMapping(64, 8, 0.921f, 0.94f, new FuncDef(FuncDef.kaiser8_table, 32)),
			new QualityMapping(80, 16, 0.922f, 0.94f, new FuncDef(FuncDef.kaiser10_table, 32)),
			new QualityMapping(96, 16, 0.94f, 0.945f, new FuncDef(FuncDef.kaiser10_table, 32)),
			new QualityMapping(128, 16, 0.95f, 0.95f, new FuncDef(FuncDef.kaiser10_table, 32)),
			new QualityMapping(160, 16, 0.96f, 0.96f, new FuncDef(FuncDef.kaiser10_table, 32)),
			new QualityMapping(192, 32, 0.968f, 0.968f, new FuncDef(FuncDef.kaiser12_table, 64)),
			new QualityMapping(256, 32, 0.975f, 0.975f, new FuncDef(FuncDef.kaiser12_table, 64))
		};

		private QualityMapping(int bl, int os, float dsb, float usb, FuncDef wf)
		{
			base_length = bl;
			oversample = os;
			downsample_bandwidth = dsb;
			upsample_bandwidth = usb;
			window_func = wf;
		}
	}

	private delegate int resampler_basic_func(int channel_index, short[] input, int input_ptr, ref int in_len, short[] output, int output_ptr, ref int out_len);

	private const int FIXED_STACK_ALLOC = 8192;

	private int in_rate;

	private int out_rate;

	private int num_rate;

	private int den_rate;

	private int quality;

	private int nb_channels;

	private int filt_len;

	private int mem_alloc_size;

	private int buffer_size;

	private int int_advance;

	private int frac_advance;

	private float cutoff;

	private int oversample;

	private int initialised;

	private int started;

	private int[] last_sample;

	private int[] samp_frac_num;

	private int[] magic_samples;

	private short[] mem;

	private short[] sinc_table;

	private int sinc_table_length;

	private resampler_basic_func resampler_ptr;

	private int in_stride;

	private int out_stride;

	public int Quality
	{
		get
		{
			return quality;
		}
		set
		{
			if (value > 10 || value < 0)
			{
				throw new ArgumentException("Quality must be between 0 and 10");
			}
			if (quality != value)
			{
				quality = value;
				if (initialised != 0)
				{
					update_filter();
				}
			}
		}
	}

	public int InputStride
	{
		get
		{
			return in_stride;
		}
		set
		{
			in_stride = value;
		}
	}

	public int OutputStride
	{
		get
		{
			return out_stride;
		}
		set
		{
			out_stride = value;
		}
	}

	public int InputLatency => filt_len / 2;

	public int OutputLatency => (filt_len / 2 * den_rate + (num_rate >> 1)) / num_rate;

	private static short WORD2INT(float x)
	{
		if (!(x < -32768f))
		{
			if (!(x > 32767f))
			{
				return (short)x;
			}
			return short.MaxValue;
		}
		return short.MinValue;
	}

	private static double compute_func(float x, FuncDef func)
	{
		float num = x * (float)func.oversample;
		int num2 = (int)Math.Floor(num);
		float num3 = num - (float)num2;
		double num4 = -0.1666666667 * (double)num3 + 0.1666666667 * (double)(num3 * num3 * num3);
		double num5 = (double)num3 + 0.5 * (double)(num3 * num3) - 0.5 * (double)(num3 * num3 * num3);
		double num6 = -0.3333333333 * (double)num3 + 0.5 * (double)(num3 * num3) - 0.1666666667 * (double)(num3 * num3 * num3);
		double num7 = 1.0 - num4 - num5 - num6;
		return num6 * func.table[num2] + num7 * func.table[num2 + 1] + num5 * func.table[num2 + 2] + num4 * func.table[num2 + 3];
	}

	private static short sinc(float cutoff, float x, int N, FuncDef window_func)
	{
		float num = x * cutoff;
		if (Math.Abs(x) < 1E-06f)
		{
			return WORD2INT(32768f * cutoff);
		}
		if (Math.Abs(x) > 0.5f * (float)N)
		{
			return 0;
		}
		return WORD2INT(32768f * cutoff * (float)Math.Sin(((float)Math.PI) * num) / (((float)Math.PI) * num) * (float)compute_func(Math.Abs(2f * x / (float)N), window_func));
	}

	private static void cubic_coef(short x, out short interp0, out short interp1, out short interp2, out short interp3)
	{
		short num = Inlines.MULT16_16_P15(x, x);
		short num2 = Inlines.MULT16_16_P15(x, num);
		interp0 = (short)Inlines.PSHR32(Inlines.MULT16_16(Inlines.QCONST16(-0.16667f, 15), x) + Inlines.MULT16_16(Inlines.QCONST16(0.16667f, 15), num2), 15);
		interp1 = Inlines.EXTRACT16(Inlines.EXTEND32(x) + Inlines.SHR32(Inlines.SUB32(Inlines.EXTEND32(num), Inlines.EXTEND32(num2)), 1));
		interp3 = (short)Inlines.PSHR32(Inlines.MULT16_16(Inlines.QCONST16(-0.33333f, 15), x) + Inlines.MULT16_16(Inlines.QCONST16(0.5f, 15), num) - Inlines.MULT16_16(Inlines.QCONST16(0.16667f, 15), num2), 15);
		interp2 = (short)(32767 - interp0 - interp1 - interp3);
		if (interp2 < short.MaxValue)
		{
			interp2++;
		}
	}

	private int resampler_basic_direct_single(int channel_index, short[] input, int input_ptr, ref int in_len, short[] output, int output_ptr, ref int out_len)
	{
		int num = filt_len;
		int num2 = 0;
		int num3 = last_sample[channel_index];
		int num4 = samp_frac_num[channel_index];
		while (num3 < in_len && num2 < out_len)
		{
			int num5 = num4 * num;
			int num6 = input_ptr + num3;
			int num7 = 0;
			for (int i = 0; i < num; i++)
			{
				num7 += Inlines.MULT16_16(sinc_table[num5 + i], input[num6 + i]);
			}
			output[output_ptr + out_stride * num2++] = Inlines.SATURATE16(Inlines.PSHR32(num7, 15));
			num3 += int_advance;
			num4 += frac_advance;
			if (num4 >= den_rate)
			{
				num4 -= den_rate;
				num3++;
			}
		}
		last_sample[channel_index] = num3;
		samp_frac_num[channel_index] = num4;
		return num2;
	}

	private int resampler_basic_interpolate_single(int channel_index, short[] input, int input_ptr, ref int in_len, short[] output, int output_ptr, ref int out_len)
	{
		int num = filt_len;
		int num2 = 0;
		int num3 = last_sample[channel_index];
		int num4 = samp_frac_num[channel_index];
		while (num3 < in_len && num2 < out_len)
		{
			int num5 = input_ptr + num3;
			int num6 = num4 * oversample / den_rate;
			short x = (short)Inlines.PDIV32(Inlines.SHL32(num4 * oversample % den_rate, 15), den_rate);
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			int num10 = 0;
			for (int i = 0; i < num; i++)
			{
				short a = input[num5 + i];
				num7 += Inlines.MULT16_16(a, sinc_table[4 + (i + 1) * oversample - num6 - 2]);
				num8 += Inlines.MULT16_16(a, sinc_table[4 + (i + 1) * oversample - num6 - 1]);
				num9 += Inlines.MULT16_16(a, sinc_table[4 + (i + 1) * oversample - num6]);
				num10 += Inlines.MULT16_16(a, sinc_table[4 + (i + 1) * oversample - num6 + 1]);
			}
			cubic_coef(x, out var interp, out var interp2, out var interp3, out var interp4);
			int a2 = Inlines.MULT16_32_Q15(interp, Inlines.SHR32(num7, 1)) + Inlines.MULT16_32_Q15(interp2, Inlines.SHR32(num8, 1)) + Inlines.MULT16_32_Q15(interp3, Inlines.SHR32(num9, 1)) + Inlines.MULT16_32_Q15(interp4, Inlines.SHR32(num10, 1));
			output[output_ptr + out_stride * num2++] = Inlines.SATURATE16(Inlines.PSHR32(a2, 14));
			num3 += int_advance;
			num4 += frac_advance;
			if (num4 >= den_rate)
			{
				num4 -= den_rate;
				num3++;
			}
		}
		last_sample[channel_index] = num3;
		samp_frac_num[channel_index] = num4;
		return num2;
	}

	private void update_filter()
	{
		int num = filt_len;
		oversample = QualityMapping.quality_map[quality].oversample;
		filt_len = QualityMapping.quality_map[quality].base_length;
		if (num_rate > den_rate)
		{
			cutoff = QualityMapping.quality_map[quality].downsample_bandwidth * (float)den_rate / (float)num_rate;
			filt_len = filt_len * num_rate / den_rate;
			filt_len = ((filt_len - 1) & -8) + 8;
			if (2 * den_rate < num_rate)
			{
				oversample >>= 1;
			}
			if (4 * den_rate < num_rate)
			{
				oversample >>= 1;
			}
			if (8 * den_rate < num_rate)
			{
				oversample >>= 1;
			}
			if (16 * den_rate < num_rate)
			{
				oversample >>= 1;
			}
			if (oversample < 1)
			{
				oversample = 1;
			}
		}
		else
		{
			cutoff = QualityMapping.quality_map[quality].upsample_bandwidth;
		}
		if (den_rate <= 16 * (oversample + 8))
		{
			if (sinc_table == null)
			{
				sinc_table = new short[filt_len * den_rate];
			}
			else if (sinc_table_length < filt_len * den_rate)
			{
				sinc_table = new short[filt_len * den_rate];
				sinc_table_length = filt_len * den_rate;
			}
			for (int i = 0; i < den_rate; i++)
			{
				for (int j = 0; j < filt_len; j++)
				{
					sinc_table[i * filt_len + j] = sinc(cutoff, (float)(j - filt_len / 2 + 1) - (float)i / (float)den_rate, filt_len, QualityMapping.quality_map[quality].window_func);
				}
			}
			resampler_ptr = resampler_basic_direct_single;
		}
		else
		{
			if (sinc_table == null)
			{
				sinc_table = new short[filt_len * oversample + 8];
			}
			else if (sinc_table_length < filt_len * oversample + 8)
			{
				sinc_table = new short[filt_len * oversample + 8];
				sinc_table_length = filt_len * oversample + 8;
			}
			for (int k = -4; k < oversample * filt_len + 4; k++)
			{
				sinc_table[k + 4] = sinc(cutoff, (float)k / (float)oversample - (float)(filt_len / 2), filt_len, QualityMapping.quality_map[quality].window_func);
			}
			resampler_ptr = resampler_basic_interpolate_single;
		}
		int_advance = num_rate / den_rate;
		frac_advance = num_rate % den_rate;
		if (mem == null)
		{
			mem_alloc_size = filt_len - 1 + buffer_size;
			mem = new short[nb_channels * mem_alloc_size];
			for (int l = 0; l < nb_channels * mem_alloc_size; l++)
			{
				mem[l] = 0;
			}
		}
		else if (started == 0)
		{
			mem_alloc_size = filt_len - 1 + buffer_size;
			mem = new short[nb_channels * mem_alloc_size];
			for (int m = 0; m < nb_channels * mem_alloc_size; m++)
			{
				mem[m] = 0;
			}
		}
		else if (filt_len > num)
		{
			int num2 = mem_alloc_size;
			if (filt_len - 1 + buffer_size > mem_alloc_size)
			{
				mem_alloc_size = filt_len - 1 + buffer_size;
				mem = new short[nb_channels * mem_alloc_size];
			}
			for (int num3 = nb_channels - 1; num3 >= 0; num3--)
			{
				int num4 = num;
				num4 = num + 2 * magic_samples[num3];
				for (int num5 = num - 2 + magic_samples[num3]; num5 >= 0; num5--)
				{
					mem[num3 * mem_alloc_size + num5 + magic_samples[num3]] = mem[num3 * num2 + num5];
				}
				for (int num5 = 0; num5 < magic_samples[num3]; num5++)
				{
					mem[num3 * mem_alloc_size + num5] = 0;
				}
				magic_samples[num3] = 0;
				if (filt_len > num4)
				{
					int num5;
					for (num5 = 0; num5 < num4 - 1; num5++)
					{
						mem[num3 * mem_alloc_size + (filt_len - 2 - num5)] = mem[num3 * mem_alloc_size + (num4 - 2 - num5)];
					}
					for (; num5 < filt_len - 1; num5++)
					{
						mem[num3 * mem_alloc_size + (filt_len - 2 - num5)] = 0;
					}
					last_sample[num3] += (filt_len - num4) / 2;
				}
				else
				{
					magic_samples[num3] = (num4 - filt_len) / 2;
					for (int num5 = 0; num5 < filt_len - 1 + magic_samples[num3]; num5++)
					{
						mem[num3 * mem_alloc_size + num5] = mem[num3 * mem_alloc_size + num5 + magic_samples[num3]];
					}
				}
			}
		}
		else
		{
			if (filt_len >= num)
			{
				return;
			}
			for (int n = 0; n < nb_channels; n++)
			{
				int num6 = magic_samples[n];
				magic_samples[n] = (num - filt_len) / 2;
				for (int num7 = 0; num7 < filt_len - 1 + magic_samples[n] + num6; num7++)
				{
					mem[n * mem_alloc_size + num7] = mem[n * mem_alloc_size + num7 + magic_samples[n]];
				}
				magic_samples[n] += num6;
			}
		}
	}

	private void speex_resampler_process_native(int channel_index, ref int in_len, short[] output, int output_ptr, ref int out_len)
	{
		int num = 0;
		int num2 = filt_len;
		int num3 = 0;
		int num4 = channel_index * mem_alloc_size;
		started = 1;
		num3 = resampler_ptr(channel_index, mem, num4, ref in_len, output, output_ptr, ref out_len);
		if (last_sample[channel_index] < in_len)
		{
			in_len = last_sample[channel_index];
		}
		out_len = num3;
		last_sample[channel_index] -= in_len;
		int num5 = in_len;
		for (num = num4; num < num2 - 1 + num4; num++)
		{
			mem[num] = mem[num + num5];
		}
	}

	private int speex_resampler_magic(int channel_index, short[] output, ref int output_ptr, int out_len)
	{
		int in_len = magic_samples[channel_index];
		int num = channel_index * mem_alloc_size;
		int num2 = filt_len;
		speex_resampler_process_native(channel_index, ref in_len, output, output_ptr, ref out_len);
		magic_samples[channel_index] -= in_len;
		if (magic_samples[channel_index] != 0)
		{
			for (int i = num; i < magic_samples[channel_index] + num; i++)
			{
				mem[num2 - 1 + i] = mem[num2 - 1 + i + in_len];
			}
		}
		output_ptr += out_len * out_stride;
		return out_len;
	}

	public SpeexResampler(int nb_channels, int in_rate, int out_rate, int quality)
		: this(nb_channels, in_rate, out_rate, in_rate, out_rate, quality)
	{
	}

	public SpeexResampler(int nb_channels, int ratio_num, int ratio_den, int in_rate, int out_rate, int quality)
	{
		if (quality > 10 || quality < 0)
		{
			throw new ArgumentException("Quality must be between 0 and 10");
		}
		initialised = 0;
		started = 0;
		this.in_rate = 0;
		this.out_rate = 0;
		num_rate = 0;
		den_rate = 0;
		this.quality = -1;
		sinc_table_length = 0;
		mem_alloc_size = 0;
		filt_len = 0;
		mem = null;
		resampler_ptr = null;
		cutoff = 1f;
		this.nb_channels = nb_channels;
		in_stride = 1;
		out_stride = 1;
		buffer_size = 160;
		last_sample = new int[nb_channels];
		magic_samples = new int[nb_channels];
		samp_frac_num = new int[nb_channels];
		for (int i = 0; i < nb_channels; i++)
		{
			last_sample[i] = 0;
			magic_samples[i] = 0;
			samp_frac_num[i] = 0;
		}
		Quality = quality;
		SetRateFraction(ratio_num, ratio_den, in_rate, out_rate);
		update_filter();
		initialised = 1;
	}

	public static SpeexResampler Create(int nb_channels, int in_rate, int out_rate, int quality)
	{
		return new SpeexResampler(nb_channels, in_rate, out_rate, in_rate, out_rate, quality);
	}

	public static SpeexResampler Create(int nb_channels, int ratio_num, int ratio_den, int in_rate, int out_rate, int quality)
	{
		return new SpeexResampler(nb_channels, ratio_num, ratio_den, in_rate, out_rate, quality);
	}

	public void Process(int channel_index, short[] input, int input_ptr, ref int in_len, short[] output, int output_ptr, ref int out_len)
	{
		int num = in_len;
		int num2 = out_len;
		int num3 = channel_index * mem_alloc_size;
		int num4 = filt_len - 1;
		int num5 = mem_alloc_size - num4;
		int num6 = in_stride;
		if (magic_samples[channel_index] != 0)
		{
			num2 -= speex_resampler_magic(channel_index, output, ref output_ptr, num2);
		}
		if (magic_samples[channel_index] == 0)
		{
			while (num != 0 && num2 != 0)
			{
				int in_len2 = ((num > num5) ? num5 : num);
				int out_len2 = num2;
				if (input != null)
				{
					for (int i = 0; i < in_len2; i++)
					{
						mem[num3 + i + num4] = input[input_ptr + i * num6];
					}
				}
				else
				{
					for (int i = 0; i < in_len2; i++)
					{
						mem[num3 + i + num4] = 0;
					}
				}
				speex_resampler_process_native(channel_index, ref in_len2, output, output_ptr, ref out_len2);
				num -= in_len2;
				num2 -= out_len2;
				output_ptr += out_len2 * out_stride;
				if (input != null)
				{
					input_ptr += in_len2 * num6;
				}
			}
		}
		in_len -= num;
		out_len -= num2;
	}

	public void Process(int channel_index, float[] input, int input_ptr, ref int in_len, float[] output, int output_ptr, ref int out_len)
	{
		int num = in_stride;
		int num2 = out_stride;
		int num3 = in_len;
		int num4 = out_len;
		int num5 = channel_index * mem_alloc_size;
		int num6 = mem_alloc_size - (filt_len - 1);
		int num7 = ((num4 < 8192) ? num4 : 8192);
		short[] array = new short[num7];
		out_stride = 1;
		while (num3 != 0 && num4 != 0)
		{
			int output_ptr2 = 0;
			int in_len2 = ((num3 > num6) ? num6 : num3);
			int out_len2 = ((num4 > num7) ? num7 : num4);
			int num8 = 0;
			if (magic_samples[channel_index] != 0)
			{
				num8 = speex_resampler_magic(channel_index, array, ref output_ptr2, out_len2);
				out_len2 -= num8;
				num4 -= num8;
			}
			if (magic_samples[channel_index] == 0)
			{
				if (input != null)
				{
					for (int i = 0; i < in_len2; i++)
					{
						mem[num5 + i + filt_len - 1] = WORD2INT(input[input_ptr + i * num]);
					}
				}
				else
				{
					for (int i = 0; i < in_len2; i++)
					{
						mem[num5 + i + filt_len - 1] = 0;
					}
				}
				speex_resampler_process_native(channel_index, ref in_len2, array, output_ptr2, ref out_len2);
			}
			else
			{
				in_len2 = 0;
				out_len2 = 0;
			}
			for (int i = 0; i < out_len2 + num8; i++)
			{
				output[output_ptr + i * num2] = array[i];
			}
			num3 -= in_len2;
			num4 -= out_len2;
			output_ptr += (out_len2 + num8) * num2;
			if (input != null)
			{
				input_ptr += in_len2 * num;
			}
		}
		out_stride = num2;
		in_len -= num3;
		out_len -= num4;
	}

	public void ProcessInterleaved(float[] input, int input_ptr, ref int in_len, float[] output, int output_ptr, ref int out_len)
	{
		int num = out_len;
		int num2 = in_len;
		int num3 = in_stride;
		int num4 = out_stride;
		in_stride = (out_stride = nb_channels);
		for (int i = 0; i < nb_channels; i++)
		{
			out_len = num;
			in_len = num2;
			if (input != null)
			{
				Process(i, input, input_ptr + i, ref in_len, output, output_ptr + i, ref out_len);
			}
			else
			{
				Process(i, null, 0, ref in_len, output, output_ptr + i, ref out_len);
			}
		}
		in_stride = num3;
		out_stride = num4;
	}

	public void ProcessInterleaved(short[] input, int input_ptr, ref int in_len, short[] output, int output_ptr, ref int out_len)
	{
		int num = out_len;
		int num2 = in_len;
		int num3 = in_stride;
		int num4 = out_stride;
		in_stride = (out_stride = nb_channels);
		for (int i = 0; i < nb_channels; i++)
		{
			out_len = num;
			in_len = num2;
			if (input != null)
			{
				Process(i, input, input_ptr + i, ref in_len, output, output_ptr + i, ref out_len);
			}
			else
			{
				Process(i, null, 0, ref in_len, output, output_ptr + i, ref out_len);
			}
		}
		in_stride = num3;
		out_stride = num4;
	}

	public void SkipZeroes()
	{
		for (int i = 0; i < nb_channels; i++)
		{
			last_sample[i] = filt_len / 2;
		}
	}

	public void ResetMem()
	{
		for (int i = 0; i < nb_channels; i++)
		{
			last_sample[i] = 0;
			magic_samples[i] = 0;
			samp_frac_num[i] = 0;
		}
		for (int i = 0; i < nb_channels * (filt_len - 1); i++)
		{
			mem[i] = 0;
		}
	}

	public void SetRates(int in_rate, int out_rate)
	{
		SetRateFraction(in_rate, out_rate, in_rate, out_rate);
	}

	public void GetRates(out int in_rate, out int out_rate)
	{
		in_rate = this.in_rate;
		out_rate = this.out_rate;
	}

	public void SetRateFraction(int ratio_num, int ratio_den, int in_rate, int out_rate)
	{
		if (this.in_rate == in_rate && this.out_rate == out_rate && num_rate == ratio_num && den_rate == ratio_den)
		{
			return;
		}
		int num = den_rate;
		this.in_rate = in_rate;
		this.out_rate = out_rate;
		num_rate = ratio_num;
		den_rate = ratio_den;
		for (int i = 2; i <= Inlines.IMIN(num_rate, den_rate); i++)
		{
			while (num_rate % i == 0 && den_rate % i == 0)
			{
				num_rate /= i;
				den_rate /= i;
			}
		}
		if (num > 0)
		{
			for (int j = 0; j < nb_channels; j++)
			{
				samp_frac_num[j] = samp_frac_num[j] * den_rate / num;
				if (samp_frac_num[j] >= den_rate)
				{
					samp_frac_num[j] = den_rate - 1;
				}
			}
		}
		if (initialised != 0)
		{
			update_filter();
		}
	}

	public void GetRateFraction(out int ratio_num, out int ratio_den)
	{
		ratio_num = num_rate;
		ratio_den = den_rate;
	}
}

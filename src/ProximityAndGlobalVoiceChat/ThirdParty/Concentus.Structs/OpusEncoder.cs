using System;
using Concentus.Celt.Structs;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Enums;
using Concentus.Silk;
using Concentus.Silk.Structs;

namespace Concentus.Structs;

internal class OpusEncoder
{
	internal readonly EncControlState silk_mode = new EncControlState();

	internal OpusApplication application;

	internal int channels;

	internal int delay_compensation;

	internal int force_channels;

	internal OpusSignal signal_type;

	internal OpusBandwidth user_bandwidth;

	internal OpusBandwidth max_bandwidth;

	internal OpusMode user_forced_mode;

	internal int voice_ratio;

	internal int Fs;

	internal int use_vbr;

	internal int vbr_constraint;

	internal OpusFramesize variable_duration;

	internal int bitrate_bps;

	internal int user_bitrate_bps;

	internal int lsb_depth;

	internal int encoder_buffer;

	internal int lfe;

	internal readonly TonalityAnalysisState analysis = new TonalityAnalysisState();

	internal int stream_channels;

	internal short hybrid_stereo_width_Q14;

	internal int variable_HP_smth2_Q15;

	internal int prev_HB_gain;

	internal readonly int[] hp_mem = new int[4];

	internal OpusMode mode;

	internal OpusMode prev_mode;

	internal int prev_channels;

	internal int prev_framesize;

	internal OpusBandwidth bandwidth;

	internal int silk_bw_switch;

	internal int first;

	internal int[] energy_masking;

	internal readonly StereoWidthState width_mem = new StereoWidthState();

	internal readonly short[] delay_buffer = new short[960];

	internal OpusBandwidth detected_bandwidth;

	internal uint rangeFinal;

	private int? _vqLevel;

	internal readonly SilkEncoder SilkEncoder = new SilkEncoder();

	internal readonly CeltEncoder Celt_Encoder = new CeltEncoder();

	private static readonly int[][] vqTable = new int[11][]
	{
		new int[2] { 7000, 16000 },
		new int[2] { 10000, 24000 },
		new int[2] { 13000, 32000 },
		new int[2] { 17000, 48000 },
		new int[2] { 20000, 64000 },
		new int[2] { 24000, 80000 },
		new int[2] { 28000, 96000 },
		new int[2] { 32000, 112000 },
		new int[2] { 38000, 128000 },
		new int[2] { 48000, 192000 },
		new int[2] { 64000, 256000 }
	};

	public OpusApplication Application
	{
		get
		{
			return application;
		}
		set
		{
			if (first == 0 && application != value)
			{
				throw new ArgumentException("Application cannot be changed after encoding has started");
			}
			application = value;
		}
	}

	public int Bitrate
	{
		get
		{
			return user_bitrate_to_bitrate(user_bitrate_bps, prev_framesize, 1276);
		}
		set
		{
			if (ConstantQuality.HasValue)
			{
				throw new ArgumentException("Bitrate is read-only while the ConstantQuality parameter is set");
			}
			if (value != -1000 && value != -1)
			{
				if (value <= 0)
				{
					throw new ArgumentException("Bitrate must be positive");
				}
				if (value <= 500)
				{
					value = 500;
				}
				else if (value > 300000 * channels)
				{
					value = 300000 * channels;
				}
			}
			user_bitrate_bps = value;
		}
	}

	public int ForceChannels
	{
		get
		{
			return force_channels;
		}
		set
		{
			if ((value < 1 || value > channels) && value != -1000)
			{
				throw new ArgumentException("Force channels must be <= num. of channels");
			}
			force_channels = value;
		}
	}

	public OpusBandwidth MaxBandwidth
	{
		get
		{
			return max_bandwidth;
		}
		set
		{
			max_bandwidth = value;
			if (max_bandwidth == OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND)
			{
				silk_mode.maxInternalSampleRate = 8000;
			}
			else if (max_bandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
			{
				silk_mode.maxInternalSampleRate = 12000;
			}
			else
			{
				silk_mode.maxInternalSampleRate = 16000;
			}
		}
	}

	public OpusBandwidth Bandwidth
	{
		get
		{
			return bandwidth;
		}
		set
		{
			user_bandwidth = value;
			if (user_bandwidth == OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND)
			{
				silk_mode.maxInternalSampleRate = 8000;
			}
			else if (user_bandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
			{
				silk_mode.maxInternalSampleRate = 12000;
			}
			else
			{
				silk_mode.maxInternalSampleRate = 16000;
			}
		}
	}

	public bool UseDTX
	{
		get
		{
			return silk_mode.useDTX != 0;
		}
		set
		{
			silk_mode.useDTX = (value ? 1 : 0);
		}
	}

	public int Complexity
	{
		get
		{
			return silk_mode.complexity;
		}
		set
		{
			if (value < 0 || value > 10)
			{
				throw new ArgumentException("Complexity must be between 0 and 10");
			}
			silk_mode.complexity = value;
			Celt_Encoder.SetComplexity(value);
		}
	}

	public bool UseInbandFEC
	{
		get
		{
			return silk_mode.useInBandFEC != 0;
		}
		set
		{
			silk_mode.useInBandFEC = (value ? 1 : 0);
		}
	}

	public int PacketLossPercent
	{
		get
		{
			return silk_mode.packetLossPercentage;
		}
		set
		{
			if (value < 0 || value > 100)
			{
				throw new ArgumentException("Packet loss must be between 0 and 100");
			}
			silk_mode.packetLossPercentage = value;
			Celt_Encoder.SetPacketLossPercent(value);
		}
	}

	public bool UseVBR
	{
		get
		{
			return use_vbr != 0;
		}
		set
		{
			use_vbr = (value ? 1 : 0);
			silk_mode.useCBR = ((!value) ? 1 : 0);
		}
	}

	public bool UseConstrainedVBR
	{
		get
		{
			return vbr_constraint != 0;
		}
		set
		{
			vbr_constraint = (value ? 1 : 0);
		}
	}

	public OpusSignal SignalType
	{
		get
		{
			return signal_type;
		}
		set
		{
			signal_type = value;
		}
	}

	public int Lookahead
	{
		get
		{
			int num = Fs / 400;
			if (application != OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY)
			{
				num += delay_compensation;
			}
			return num;
		}
	}

	public int SampleRate => Fs;

	public int NumChannels => channels;

	public uint FinalRange => rangeFinal;

	public int LSBDepth
	{
		get
		{
			return lsb_depth;
		}
		set
		{
			if (value < 8 || value > 24)
			{
				throw new ArgumentException("LSB depth must be between 8 and 24");
			}
			lsb_depth = value;
		}
	}

	public OpusFramesize ExpertFrameDuration
	{
		get
		{
			return variable_duration;
		}
		set
		{
			variable_duration = value;
			Celt_Encoder.SetExpertFrameDuration(value);
		}
	}

	public OpusMode ForceMode
	{
		get
		{
			return user_forced_mode;
		}
		set
		{
			user_forced_mode = value;
		}
	}

	public bool IsLFE
	{
		get
		{
			return lfe != 0;
		}
		set
		{
			lfe = (value ? 1 : 0);
			Celt_Encoder.SetLFE(value ? 1 : 0);
		}
	}

	public bool PredictionDisabled
	{
		get
		{
			return silk_mode.reducedDependency != 0;
		}
		set
		{
			silk_mode.reducedDependency = (value ? 1 : 0);
		}
	}

	public bool EnableAnalysis
	{
		get
		{
			return analysis.enabled;
		}
		set
		{
			if (!value && _vqLevel.HasValue)
			{
				throw new ArgumentException("You cannot disable analysis while also specifying a ConstantQuality parameter");
			}
			if (value && Fs != 48000)
			{
				throw new ArgumentException("EnableAnalysis only works if the encoder is in 48000Khz mode");
			}
			analysis.enabled = value;
		}
	}

	public int? ConstantQuality
	{
		get
		{
			return _vqLevel;
		}
		set
		{
			if (value.HasValue && (value.Value < 0 || value.Value > 10))
			{
				throw new ArgumentException("Constant quality VBR level must be either null (disabled) or between 0 and 10, inclusive.");
			}
			if (value.HasValue && Fs != 48000)
			{
				throw new ArgumentException("ConstantQuality only works if the encoder is in 48000Khz mode");
			}
			EnableAnalysis = true;
			_vqLevel = value;
		}
	}

	public float MusicProbability => analysis.music_prob;

	internal OpusEncoder()
	{
	}

	internal void Reset()
	{
		silk_mode.Reset();
		application = OpusApplication.OPUS_APPLICATION_UNIMPLEMENTED;
		channels = 0;
		delay_compensation = 0;
		force_channels = 0;
		signal_type = (OpusSignal)0;
		user_bandwidth = (OpusBandwidth)0;
		max_bandwidth = (OpusBandwidth)0;
		user_forced_mode = (OpusMode)0;
		voice_ratio = 0;
		Fs = 0;
		use_vbr = 0;
		vbr_constraint = 0;
		variable_duration = (OpusFramesize)0;
		bitrate_bps = 0;
		user_bitrate_bps = 0;
		lsb_depth = 0;
		encoder_buffer = 0;
		lfe = 0;
		analysis.Reset();
		PartialReset();
	}

	internal void PartialReset()
	{
		stream_channels = 0;
		hybrid_stereo_width_Q14 = 0;
		variable_HP_smth2_Q15 = 0;
		prev_HB_gain = 0;
		Arrays.MemSetInt(hp_mem, 0, 4);
		mode = (OpusMode)0;
		prev_mode = (OpusMode)0;
		prev_channels = 0;
		prev_framesize = 0;
		bandwidth = (OpusBandwidth)0;
		silk_bw_switch = 0;
		first = 0;
		energy_masking = null;
		width_mem.Reset();
		Arrays.MemSetShort(delay_buffer, 0, 960);
		detected_bandwidth = (OpusBandwidth)0;
		rangeFinal = 0u;
	}

	public void ResetState()
	{
		EncControlState encStatus = new EncControlState();
		analysis.Reset();
		PartialReset();
		Celt_Encoder.ResetState();
		EncodeAPI.silk_InitEncoder(SilkEncoder, encStatus);
		stream_channels = channels;
		hybrid_stereo_width_Q14 = 16384;
		prev_HB_gain = 32767;
		first = 1;
		mode = OpusMode.MODE_HYBRID;
		bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
		variable_HP_smth2_Q15 = Inlines.silk_LSHIFT(Inlines.silk_lin2log(60), 8);
	}

	public static OpusEncoder Create(int Fs, int channels, OpusApplication application)
	{
		return new OpusEncoder(Fs, channels, application);
	}

	public OpusEncoder(int Fs, int channels, OpusApplication application)
	{
		if (Fs != 48000 && Fs != 24000 && Fs != 16000 && Fs != 12000 && Fs != 8000)
		{
			throw new ArgumentException("Sample rate is invalid (must be 8/12/16/24/48 Khz)");
		}
		if (channels != 1 && channels != 2)
		{
			throw new ArgumentException("Number of channels must be 1 or 2");
		}
		int num = opus_init_encoder(Fs, channels, application);
		switch (num)
		{
		case -1:
			throw new ArgumentException("OPUS_BAD_ARG when creating encoder");
		default:
			throw new OpusException("Error while initializing encoder", num);
		case 0:
			break;
		}
	}

	internal int opus_init_encoder(int Fs, int channels, OpusApplication application)
	{
		if ((Fs != 48000 && Fs != 24000 && Fs != 16000 && Fs != 12000 && Fs != 8000) || (channels != 1 && channels != 2) || application == OpusApplication.OPUS_APPLICATION_UNIMPLEMENTED)
		{
			return -1;
		}
		Reset();
		SilkEncoder silkEncoder = SilkEncoder;
		CeltEncoder celt_Encoder = Celt_Encoder;
		stream_channels = (this.channels = channels);
		this.Fs = Fs;
		if (EncodeAPI.silk_InitEncoder(silkEncoder, silk_mode) != 0)
		{
			return -3;
		}
		silk_mode.nChannelsAPI = channels;
		silk_mode.nChannelsInternal = channels;
		silk_mode.API_sampleRate = this.Fs;
		silk_mode.maxInternalSampleRate = 16000;
		silk_mode.minInternalSampleRate = 8000;
		silk_mode.desiredInternalSampleRate = 16000;
		silk_mode.payloadSize_ms = 20;
		silk_mode.bitRate = 25000;
		silk_mode.packetLossPercentage = 0;
		silk_mode.complexity = 9;
		silk_mode.useInBandFEC = 0;
		silk_mode.useDTX = 0;
		silk_mode.useCBR = 0;
		silk_mode.reducedDependency = 0;
		if (celt_Encoder.celt_encoder_init(Fs, channels) != 0)
		{
			return -3;
		}
		celt_Encoder.SetSignalling(0);
		celt_Encoder.SetComplexity(silk_mode.complexity);
		use_vbr = 1;
		vbr_constraint = 1;
		user_bitrate_bps = -1000;
		bitrate_bps = 3000 + Fs * channels;
		this.application = application;
		signal_type = OpusSignal.OPUS_SIGNAL_AUTO;
		user_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_AUTO;
		max_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
		force_channels = -1000;
		user_forced_mode = OpusMode.MODE_AUTO;
		voice_ratio = -1;
		encoder_buffer = this.Fs / 100;
		lsb_depth = 24;
		variable_duration = OpusFramesize.OPUS_FRAMESIZE_ARG;
		delay_compensation = this.Fs / 250;
		hybrid_stereo_width_Q14 = 16384;
		prev_HB_gain = 32767;
		variable_HP_smth2_Q15 = Inlines.silk_LSHIFT(Inlines.silk_lin2log(60), 8);
		first = 1;
		mode = OpusMode.MODE_HYBRID;
		bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
		Analysis.tonality_analysis_init(analysis);
		return 0;
	}

	internal int user_bitrate_to_bitrate(int user_bitrate, int frame_size, int max_data_bytes)
	{
		if (frame_size == 0)
		{
			frame_size = Fs / 400;
		}
		return user_bitrate switch
		{
			-1000 => 60 * Fs / frame_size + Fs * channels, 
			-1 => max_data_bytes * 8 * Fs / frame_size, 
			_ => user_bitrate, 
		};
	}

	internal int opus_encode_native<T>(short[] pcm, int pcm_ptr, int frame_size, byte[] data, int data_ptr, int out_data_bytes, int lsb_depth, T[] analysis_pcm, int analysis_pcm_ptr, int analysis_size, int c1, int c2, int analysis_channels, Downmix.downmix_func<T> downmix, int float_api)
	{
		int num = 0;
		EntropyCoder entropyCoder = new EntropyCoder();
		int num2 = 0;
		int startBand = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		uint num7 = 0u;
		AnalysisInfo analysisInfo = new AnalysisInfo();
		int num8 = -1;
		int read_subframe = -1;
		int num9 = Inlines.IMIN(1276, out_data_bytes);
		rangeFinal = 0u;
		if ((variable_duration == (OpusFramesize)0 && 400 * frame_size != Fs && 200 * frame_size != Fs && 100 * frame_size != Fs && 50 * frame_size != Fs && 25 * frame_size != Fs && 50 * frame_size != 3 * Fs) || 400 * frame_size < Fs || num9 <= 0)
		{
			return -1;
		}
		SilkEncoder silkEncoder = SilkEncoder;
		CeltEncoder celt_Encoder = Celt_Encoder;
		int num10 = ((application != OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY) ? delay_compensation : 0);
		lsb_depth = Inlines.IMIN(lsb_depth, this.lsb_depth);
		CeltMode celtMode = celt_Encoder.GetMode();
		voice_ratio = -1;
		if (analysis.enabled)
		{
			analysisInfo.valid = 0;
			if ((_vqLevel.HasValue || silk_mode.complexity >= 7) && Fs == 48000)
			{
				num8 = analysis.read_pos;
				read_subframe = analysis.read_subframe;
				Analysis.run_analysis(analysis, celtMode, (analysis_pcm != null) ? analysis_pcm : null, analysis_pcm_ptr, analysis_size, frame_size, c1, c2, analysis_channels, Fs, lsb_depth, downmix, analysisInfo);
			}
			detected_bandwidth = (OpusBandwidth)0;
			if (analysisInfo.valid != 0)
			{
				if (signal_type == OpusSignal.OPUS_SIGNAL_AUTO)
				{
					voice_ratio = (int)Math.Floor(0.5f + 100f * (1f - analysisInfo.music_prob));
				}
				int num11 = analysisInfo.bandwidth;
				if (num11 <= 12)
				{
					detected_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
				}
				else if (num11 <= 14)
				{
					detected_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND;
				}
				else if (num11 <= 16)
				{
					detected_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
				}
				else if (num11 <= 18)
				{
					detected_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND;
				}
				else
				{
					detected_bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
				}
				if (_vqLevel.HasValue)
				{
					user_bitrate_bps = GetVariableQualityBitrate(_vqLevel.Value, analysis.music_prob);
				}
			}
		}
		int num12 = ((channels == 2 && force_channels != 1) ? CodecHelpers.compute_stereo_width(pcm, pcm_ptr, frame_size, Fs, width_mem) : 0);
		int num13 = num10;
		bitrate_bps = user_bitrate_to_bitrate(user_bitrate_bps, frame_size, num9);
		int num14 = Fs / frame_size;
		if (use_vbr == 0)
		{
			int num15 = 3 * Fs / frame_size;
			int num16 = Inlines.IMIN((3 * bitrate_bps / 8 + num15 / 2) / num15, num9);
			bitrate_bps = num16 * num15 * 8 / 3;
			num9 = num16;
		}
		if (num9 < 3 || bitrate_bps < 3 * num14 * 8 || (num14 < 50 && (num9 * num14 < 300 || bitrate_bps < 2400)))
		{
			OpusMode opusMode = mode;
			OpusBandwidth opusBandwidth = ((bandwidth == (OpusBandwidth)0) ? OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND : bandwidth);
			if (opusMode == (OpusMode)0)
			{
				opusMode = OpusMode.MODE_SILK_ONLY;
			}
			if (num14 > 100)
			{
				opusMode = OpusMode.MODE_CELT_ONLY;
			}
			if (num14 < 50)
			{
				opusMode = OpusMode.MODE_SILK_ONLY;
			}
			if (opusMode == OpusMode.MODE_SILK_ONLY && opusBandwidth > OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND)
			{
				opusBandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
			}
			else if (opusMode == OpusMode.MODE_CELT_ONLY && opusBandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
			{
				opusBandwidth = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
			}
			else if (opusMode == OpusMode.MODE_HYBRID && opusBandwidth <= OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND)
			{
				opusBandwidth = OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND;
			}
			data[data_ptr] = CodecHelpers.gen_toc(opusMode, num14, opusBandwidth, stream_channels);
			num = 1;
			if (use_vbr == 0)
			{
				num = OpusRepacketizer.PadPacket(data, data_ptr, num, num9);
				if (num == 0)
				{
					num = num9;
				}
			}
			return num;
		}
		int num17 = num14 * num9 * 8;
		int num18 = bitrate_bps - (40 * channels + 20) * (Fs / frame_size - 50);
		int num19;
		if (signal_type == OpusSignal.OPUS_SIGNAL_VOICE)
		{
			num19 = 127;
		}
		else if (signal_type == OpusSignal.OPUS_SIGNAL_MUSIC)
		{
			num19 = 0;
		}
		else if (voice_ratio < 0)
		{
			num19 = ((application != OpusApplication.OPUS_APPLICATION_VOIP) ? 48 : 115);
		}
		else
		{
			num19 = voice_ratio * 327 >> 8;
			if (application == OpusApplication.OPUS_APPLICATION_AUDIO)
			{
				num19 = Inlines.IMIN(num19, 115);
			}
		}
		if (force_channels != -1000 && channels == 2)
		{
			stream_channels = force_channels;
		}
		else if (channels == 2)
		{
			int num20 = 30000 + (0 >> 14);
			num20 = ((stream_channels != 2) ? (num20 + 1000) : (num20 - 1000));
			stream_channels = ((num18 <= num20) ? 1 : 2);
		}
		else
		{
			stream_channels = channels;
		}
		num18 = bitrate_bps - (40 * stream_channels + 20) * (Fs / frame_size - 50);
		if (application == OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY)
		{
			mode = OpusMode.MODE_CELT_ONLY;
		}
		else if (user_forced_mode == OpusMode.MODE_AUTO)
		{
			int num21 = Inlines.MULT16_32_Q15(32767 - num12, Tables.mode_thresholds[0][0]) + Inlines.MULT16_32_Q15(num12, Tables.mode_thresholds[1][0]);
			int num22 = Inlines.MULT16_32_Q15(32767 - num12, Tables.mode_thresholds[1][1]) + Inlines.MULT16_32_Q15(num12, Tables.mode_thresholds[1][1]);
			int num23 = num22 + (num19 * num19 * (num21 - num22) >> 14);
			if (application == OpusApplication.OPUS_APPLICATION_VOIP)
			{
				num23 += 8000;
			}
			if (prev_mode == OpusMode.MODE_CELT_ONLY)
			{
				num23 -= 4000;
			}
			else if (prev_mode > (OpusMode)0)
			{
				num23 += 4000;
			}
			mode = ((num18 >= num23) ? OpusMode.MODE_CELT_ONLY : OpusMode.MODE_SILK_ONLY);
			if (silk_mode.useInBandFEC != 0 && silk_mode.packetLossPercentage > 128 - num19 >> 4)
			{
				mode = OpusMode.MODE_SILK_ONLY;
			}
			if (silk_mode.useDTX != 0 && num19 > 100)
			{
				mode = OpusMode.MODE_SILK_ONLY;
			}
		}
		else
		{
			mode = user_forced_mode;
		}
		if (mode != OpusMode.MODE_CELT_ONLY && frame_size < Fs / 100)
		{
			mode = OpusMode.MODE_CELT_ONLY;
		}
		if (lfe != 0)
		{
			mode = OpusMode.MODE_CELT_ONLY;
		}
		if (num9 < ((num14 > 50) ? 12000 : 8000) * frame_size / (Fs * 8))
		{
			mode = OpusMode.MODE_CELT_ONLY;
		}
		if (stream_channels == 1 && prev_channels == 2 && silk_mode.toMono == 0 && mode != OpusMode.MODE_CELT_ONLY && prev_mode != OpusMode.MODE_CELT_ONLY)
		{
			silk_mode.toMono = 1;
			stream_channels = 2;
		}
		else
		{
			silk_mode.toMono = 0;
		}
		if (prev_mode > (OpusMode)0 && ((mode != OpusMode.MODE_CELT_ONLY && prev_mode == OpusMode.MODE_CELT_ONLY) || (mode == OpusMode.MODE_CELT_ONLY && prev_mode != OpusMode.MODE_CELT_ONLY)))
		{
			num3 = 1;
			num5 = ((mode != OpusMode.MODE_CELT_ONLY) ? 1 : 0);
			if (num5 == 0)
			{
				if (frame_size >= Fs / 100)
				{
					mode = prev_mode;
					num6 = 1;
				}
				else
				{
					num3 = 0;
				}
			}
		}
		if (silk_bw_switch != 0)
		{
			num3 = 1;
			num5 = 1;
			silk_bw_switch = 0;
			num2 = 1;
		}
		if (num3 != 0)
		{
			num4 = Inlines.IMIN(257, num9 * (Fs / 200) / (frame_size + Fs / 200));
			if (use_vbr != 0)
			{
				num4 = Inlines.IMIN(num4, bitrate_bps / 1600);
			}
		}
		if (mode != OpusMode.MODE_CELT_ONLY && prev_mode == OpusMode.MODE_CELT_ONLY)
		{
			EncControlState encStatus = new EncControlState();
			EncodeAPI.silk_InitEncoder(silkEncoder, encStatus);
			num2 = 1;
		}
		if (mode == OpusMode.MODE_CELT_ONLY || first != 0 || silk_mode.allowBandwidthSwitch != 0)
		{
			int[] array = new int[8];
			OpusBandwidth opusBandwidth2 = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
			int num24 = num18;
			if (mode != OpusMode.MODE_CELT_ONLY)
			{
				num24 = num24 * (45 + silk_mode.complexity) / 50;
				if (use_vbr == 0)
				{
					num24 -= 1000;
				}
			}
			int[] array2;
			int[] array3;
			if (channels == 2 && force_channels != 1)
			{
				array2 = Tables.stereo_voice_bandwidth_thresholds;
				array3 = Tables.stereo_music_bandwidth_thresholds;
			}
			else
			{
				array2 = Tables.mono_voice_bandwidth_thresholds;
				array3 = Tables.mono_music_bandwidth_thresholds;
			}
			for (int i = 0; i < 8; i++)
			{
				array[i] = array3[i] + (num19 * num19 * (array2[i] - array3[i]) >> 14);
			}
			int num25;
			do
			{
				num25 = array[2 * (int)(opusBandwidth2 - 1102)];
				int num26 = array[2 * (int)(opusBandwidth2 - 1102) + 1];
				if (first == 0)
				{
					num25 = ((bandwidth < opusBandwidth2) ? (num25 + num26) : (num25 - num26));
				}
			}
			while (num24 < num25 && --opusBandwidth2 > OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND);
			bandwidth = opusBandwidth2;
			if (first == 0 && mode != OpusMode.MODE_CELT_ONLY && silk_mode.inWBmodeWithoutVariableLP == 0 && bandwidth > OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND)
			{
				bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
			}
		}
		if (bandwidth > max_bandwidth)
		{
			bandwidth = max_bandwidth;
		}
		if (user_bandwidth != OpusBandwidth.OPUS_BANDWIDTH_AUTO)
		{
			bandwidth = user_bandwidth;
		}
		if (mode != OpusMode.MODE_CELT_ONLY && num17 < 15000)
		{
			bandwidth = OpusBandwidthHelpers.MIN(bandwidth, OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND);
		}
		if (Fs <= 24000 && bandwidth > OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND)
		{
			bandwidth = OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND;
		}
		if (Fs <= 16000 && bandwidth > OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND)
		{
			bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
		}
		if (Fs <= 12000 && bandwidth > OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
		{
			bandwidth = OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND;
		}
		if (Fs <= 8000 && bandwidth > OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND)
		{
			bandwidth = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
		}
		if (detected_bandwidth != 0 && user_bandwidth == OpusBandwidth.OPUS_BANDWIDTH_AUTO)
		{
			detected_bandwidth = OpusBandwidthHelpers.MAX(b: (num18 <= 18000 * stream_channels && mode == OpusMode.MODE_CELT_ONLY) ? OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND : ((num18 <= 24000 * stream_channels && mode == OpusMode.MODE_CELT_ONLY) ? OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND : ((num18 <= 30000 * stream_channels) ? OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND : ((num18 > 44000 * stream_channels) ? OpusBandwidth.OPUS_BANDWIDTH_FULLBAND : OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND))), a: detected_bandwidth);
			bandwidth = OpusBandwidthHelpers.MIN(bandwidth, detected_bandwidth);
		}
		celt_Encoder.SetLSBDepth(lsb_depth);
		if (mode == OpusMode.MODE_CELT_ONLY && bandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
		{
			bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
		}
		if (lfe != 0)
		{
			bandwidth = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
		}
		if (frame_size > Fs / 50 && (mode == OpusMode.MODE_CELT_ONLY || bandwidth > OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND))
		{
			if (analysis.enabled && num8 != -1)
			{
				analysis.read_pos = num8;
				analysis.read_subframe = read_subframe;
			}
			int num27 = ((frame_size > Fs / 25) ? 3 : 2);
			int num28 = Inlines.IMIN(1276, (out_data_bytes - 3) / num27);
			byte[] data2 = new byte[num27 * num28];
			OpusRepacketizer opusRepacketizer = new OpusRepacketizer();
			OpusMode opusMode2 = user_forced_mode;
			OpusBandwidth opusBandwidth3 = user_bandwidth;
			int num29 = force_channels;
			user_forced_mode = mode;
			user_bandwidth = bandwidth;
			force_channels = stream_channels;
			int toMono = silk_mode.toMono;
			if (toMono != 0)
			{
				force_channels = 1;
			}
			else
			{
				prev_channels = stream_channels;
			}
			for (int i = 0; i < num27; i++)
			{
				silk_mode.toMono = 0;
				if (num6 != 0 && i == num27 - 1)
				{
					user_forced_mode = OpusMode.MODE_CELT_ONLY;
				}
				int num30 = opus_encode_native(pcm, pcm_ptr + i * (channels * Fs / 50), Fs / 50, data2, i * num28, num28, lsb_depth, null, 0, 0, c1, c2, analysis_channels, downmix, float_api);
				if (num30 < 0)
				{
					return -3;
				}
				num = opusRepacketizer.AddPacket(data2, i * num28, num30);
				if (num < 0)
				{
					return -3;
				}
			}
			int maxlen = ((use_vbr == 0) ? Inlines.IMIN(3 * bitrate_bps / (1200 / num27), out_data_bytes) : out_data_bytes);
			num = opusRepacketizer.opus_repacketizer_out_range_impl(0, num27, data, data_ptr, maxlen, 0, (use_vbr == 0) ? 1 : 0);
			if (num < 0)
			{
				return -3;
			}
			user_forced_mode = opusMode2;
			user_bandwidth = opusBandwidth3;
			force_channels = num29;
			silk_mode.toMono = toMono;
			return num;
		}
		OpusBandwidth opusBandwidth4 = bandwidth;
		if (mode == OpusMode.MODE_SILK_ONLY && opusBandwidth4 > OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND)
		{
			mode = OpusMode.MODE_HYBRID;
		}
		if (mode == OpusMode.MODE_HYBRID && opusBandwidth4 <= OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND)
		{
			mode = OpusMode.MODE_SILK_ONLY;
		}
		int num31 = Inlines.IMIN(num9 - num4, bitrate_bps * frame_size / (Fs * 8)) - 1;
		data_ptr++;
		entropyCoder.enc_init(data, data_ptr, (uint)(num9 - 1));
		short[] array4 = new short[(num13 + frame_size) * channels];
		Array.Copy(delay_buffer, (encoder_buffer - num13) * channels, array4, 0, num13 * channels);
		variable_HP_smth2_Q15 = Inlines.silk_SMLAWB(b32: ((mode != OpusMode.MODE_CELT_ONLY) ? silkEncoder.state_Fxx[0].variable_HP_smth1_Q15 : Inlines.silk_LSHIFT(Inlines.silk_lin2log(60), 8)) - variable_HP_smth2_Q15, a32: variable_HP_smth2_Q15, c32: 983);
		int cutoff_Hz = Inlines.silk_log2lin(Inlines.silk_RSHIFT(variable_HP_smth2_Q15, 8));
		if (application == OpusApplication.OPUS_APPLICATION_VOIP)
		{
			CodecHelpers.hp_cutoff(pcm, pcm_ptr, cutoff_Hz, array4, num13 * channels, hp_mem, frame_size, channels, Fs);
		}
		else
		{
			CodecHelpers.dc_reject(pcm, pcm_ptr, 3, array4, num13 * channels, hp_mem, frame_size, channels, Fs);
		}
		int num32 = 32767;
		if (mode != OpusMode.MODE_CELT_ONLY)
		{
			short[] array5 = new short[channels * frame_size];
			int num33 = 8 * num31 * num14;
			if (mode == OpusMode.MODE_HYBRID)
			{
				silk_mode.bitRate = stream_channels * (5000 + ((Fs == 100 * frame_size) ? 1000 : 0));
				if (opusBandwidth4 == OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND)
				{
					silk_mode.bitRate += (num33 - silk_mode.bitRate) * 2 / 3;
				}
				else
				{
					silk_mode.bitRate += (num33 - silk_mode.bitRate) * 3 / 5;
				}
				if (silk_mode.bitRate > num33 * 4 / 5)
				{
					silk_mode.bitRate = num33 * 4 / 5;
				}
				if (energy_masking == null)
				{
					int num34 = num33 - silk_mode.bitRate;
					int num35 = ((opusBandwidth4 == OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND) ? 3000 : 3600);
					num32 = Inlines.SHL32(num34, 9) / Inlines.SHR32(num34 + stream_channels * num35, 6);
					num32 = ((num32 < 28086) ? (num32 + 4681) : 32767);
				}
			}
			else
			{
				silk_mode.bitRate = num33;
			}
			if (energy_masking != null && use_vbr != 0 && lfe == 0)
			{
				int num36 = 0;
				int num37 = 17;
				short a = 16000;
				if (bandwidth == OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND)
				{
					num37 = 13;
					a = 8000;
				}
				else if (bandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
				{
					num37 = 15;
					a = 12000;
				}
				for (int j = 0; j < channels; j++)
				{
					for (int i = 0; i < num37; i++)
					{
						int num38 = Inlines.MAX16(Inlines.MIN16(energy_masking[21 * j + i], 512), -2048);
						if (num38 > 0)
						{
							num38 = Inlines.HALF16(num38);
						}
						num36 += num38;
					}
				}
				int num39 = num36 / num37 * channels;
				num39 += 205;
				int a2 = Inlines.PSHR32(Inlines.MULT16_16(a, num39), 10);
				a2 = Inlines.MAX32(a2, -2 * silk_mode.bitRate / 3);
				if (bandwidth == OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND || bandwidth == OpusBandwidth.OPUS_BANDWIDTH_FULLBAND)
				{
					silk_mode.bitRate += 3 * a2 / 5;
				}
				else
				{
					silk_mode.bitRate += a2;
				}
				num31 += a2 * frame_size / (8 * Fs);
			}
			silk_mode.payloadSize_ms = 1000 * frame_size / Fs;
			silk_mode.nChannelsAPI = channels;
			silk_mode.nChannelsInternal = stream_channels;
			switch (opusBandwidth4)
			{
			case OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND:
				silk_mode.desiredInternalSampleRate = 8000;
				break;
			case OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND:
				silk_mode.desiredInternalSampleRate = 12000;
				break;
			default:
				silk_mode.desiredInternalSampleRate = 16000;
				break;
			}
			if (mode == OpusMode.MODE_HYBRID)
			{
				silk_mode.minInternalSampleRate = 16000;
			}
			else
			{
				silk_mode.minInternalSampleRate = 8000;
			}
			if (mode == OpusMode.MODE_SILK_ONLY)
			{
				int num40 = num17;
				silk_mode.maxInternalSampleRate = 16000;
				if (num14 > 50)
				{
					num40 = num40 * 2 / 3;
				}
				if (num40 < 13000)
				{
					silk_mode.maxInternalSampleRate = 12000;
					silk_mode.desiredInternalSampleRate = Inlines.IMIN(12000, silk_mode.desiredInternalSampleRate);
				}
				if (num40 < 9600)
				{
					silk_mode.maxInternalSampleRate = 8000;
					silk_mode.desiredInternalSampleRate = Inlines.IMIN(8000, silk_mode.desiredInternalSampleRate);
				}
			}
			else
			{
				silk_mode.maxInternalSampleRate = 16000;
			}
			silk_mode.useCBR = ((use_vbr == 0) ? 1 : 0);
			int num41 = Inlines.IMIN(1275, num9 - 1 - num4);
			silk_mode.maxBits = num41 * 8;
			if (mode == OpusMode.MODE_HYBRID)
			{
				silk_mode.maxBits = silk_mode.maxBits * 9 / 10;
			}
			if (silk_mode.useCBR != 0)
			{
				silk_mode.maxBits = silk_mode.bitRate * frame_size / (Fs * 8) * 8;
				silk_mode.bitRate = Inlines.IMAX(1, silk_mode.bitRate - 2000);
			}
			if (num2 != 0)
			{
				BoxedValueInt nBytesOut = new BoxedValueInt();
				int num42 = channels * (encoder_buffer - delay_compensation - Fs / 400);
				CodecHelpers.gain_fade(delay_buffer, num42, 0, 32767, celtMode.overlap, Fs / 400, channels, celtMode.window, Fs);
				Arrays.MemSetShort(delay_buffer, 0, num42);
				Array.Copy(delay_buffer, 0, array5, 0, encoder_buffer * channels);
				EncodeAPI.silk_Encode(silkEncoder, silk_mode, array5, encoder_buffer, null, nBytesOut, 1);
			}
			Array.Copy(array4, num13 * channels, array5, 0, frame_size * channels);
			BoxedValueInt boxedValueInt = new BoxedValueInt(num41);
			num = EncodeAPI.silk_Encode(silkEncoder, silk_mode, array5, frame_size, entropyCoder, boxedValueInt, 0);
			num41 = boxedValueInt.Val;
			if (num != 0)
			{
				return -3;
			}
			if (num41 == 0)
			{
				rangeFinal = 0u;
				data[data_ptr - 1] = CodecHelpers.gen_toc(mode, Fs / frame_size, opusBandwidth4, stream_channels);
				return 1;
			}
			if (mode == OpusMode.MODE_SILK_ONLY)
			{
				if (silk_mode.internalSampleRate == 8000)
				{
					opusBandwidth4 = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
				}
				else if (silk_mode.internalSampleRate == 12000)
				{
					opusBandwidth4 = OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND;
				}
				else if (silk_mode.internalSampleRate == 16000)
				{
					opusBandwidth4 = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
				}
			}
			silk_mode.opusCanSwitch = silk_mode.switchReady;
			if (silk_mode.opusCanSwitch != 0)
			{
				num3 = 1;
				num5 = 0;
				silk_bw_switch = 1;
			}
		}
		int endBand = 21;
		switch (opusBandwidth4)
		{
		case OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND:
			endBand = 13;
			break;
		case OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND:
		case OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND:
			endBand = 17;
			break;
		case OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND:
			endBand = 19;
			break;
		case OpusBandwidth.OPUS_BANDWIDTH_FULLBAND:
			endBand = 21;
			break;
		}
		celt_Encoder.SetEndBand(endBand);
		celt_Encoder.SetChannels(stream_channels);
		celt_Encoder.SetBitrate(-1);
		int num44;
		if (mode != OpusMode.MODE_SILK_ONLY)
		{
			int prediction = 2;
			celt_Encoder.SetVBR(value: false);
			if (silk_mode.reducedDependency != 0)
			{
				prediction = 0;
			}
			celt_Encoder.SetPrediction(prediction);
			if (mode == OpusMode.MODE_HYBRID)
			{
				int num43 = entropyCoder.tell() + 7 >> 3;
				if (num3 != 0)
				{
					num43 += ((mode != OpusMode.MODE_HYBRID) ? 1 : 3);
				}
				num44 = ((use_vbr == 0) ? ((num43 > num31) ? num43 : num31) : (num43 + num31 - silk_mode.bitRate * frame_size / (8 * Fs)));
			}
			else if (use_vbr != 0)
			{
				int num45 = 0;
				if (analysis.enabled && variable_duration == OpusFramesize.OPUS_FRAMESIZE_VARIABLE && frame_size != Fs / 50)
				{
					num45 = (60 * stream_channels + 40) * (Fs / frame_size - 50);
					if (analysisInfo.valid != 0)
					{
						num45 = (int)((float)num45 * (1f + 0.5f * analysisInfo.tonality));
					}
				}
				celt_Encoder.SetVBR(value: true);
				celt_Encoder.SetVBRConstraint(vbr_constraint != 0);
				celt_Encoder.SetBitrate(bitrate_bps + num45);
				num44 = num9 - 1 - num4;
			}
			else
			{
				num44 = num31;
			}
		}
		else
		{
			num44 = 0;
		}
		short[] array6 = new short[channels * Fs / 400];
		if (mode != OpusMode.MODE_SILK_ONLY && mode != prev_mode && prev_mode > (OpusMode)0)
		{
			Array.Copy(delay_buffer, (encoder_buffer - num13 - Fs / 400) * channels, array6, 0, channels * Fs / 400);
		}
		if (channels * (encoder_buffer - (frame_size + num13)) > 0)
		{
			Arrays.MemMoveShort(delay_buffer, channels * frame_size, 0, channels * (encoder_buffer - frame_size - num13));
			Array.Copy(array4, 0, delay_buffer, channels * (encoder_buffer - frame_size - num13), (frame_size + num13) * channels);
		}
		else
		{
			Array.Copy(array4, (frame_size + num13 - encoder_buffer) * channels, delay_buffer, 0, encoder_buffer * channels);
		}
		if (prev_HB_gain < 32767 || num32 < 32767)
		{
			CodecHelpers.gain_fade(array4, 0, prev_HB_gain, num32, celtMode.overlap, frame_size, channels, celtMode.window, Fs);
		}
		prev_HB_gain = num32;
		if (mode != OpusMode.MODE_HYBRID || stream_channels == 1)
		{
			silk_mode.stereoWidth_Q14 = Inlines.IMIN(16384, 2 * Inlines.IMAX(0, num18 - 30000));
		}
		if (energy_masking == null && channels == 2 && (hybrid_stereo_width_Q14 < 16384 || silk_mode.stereoWidth_Q14 < 16384))
		{
			int num46 = hybrid_stereo_width_Q14;
			int stereoWidth_Q = silk_mode.stereoWidth_Q14;
			num46 = ((num46 == 16384) ? 32767 : Inlines.SHL16(num46, 1));
			stereoWidth_Q = ((stereoWidth_Q == 16384) ? 32767 : Inlines.SHL16(stereoWidth_Q, 1));
			CodecHelpers.stereo_fade(array4, num46, stereoWidth_Q, celtMode.overlap, frame_size, channels, celtMode.window, Fs);
			hybrid_stereo_width_Q14 = (short)silk_mode.stereoWidth_Q14;
		}
		if (mode != OpusMode.MODE_CELT_ONLY && entropyCoder.tell() + 17 + 20 * ((mode == OpusMode.MODE_HYBRID) ? 1 : 0) <= 8 * (num9 - 1))
		{
			if (mode == OpusMode.MODE_HYBRID && (num3 != 0 || entropyCoder.tell() + 37 <= 8 * num44))
			{
				entropyCoder.enc_bit_logp(num3, 12u);
			}
			if (num3 != 0)
			{
				entropyCoder.enc_bit_logp(num5, 1u);
				int a3 = ((mode != OpusMode.MODE_HYBRID) ? (num9 - 1 - (entropyCoder.tell() + 7 >> 3)) : (num9 - 1 - num44));
				num4 = Inlines.IMIN(a3, bitrate_bps / 1600);
				num4 = Inlines.IMIN(257, Inlines.IMAX(2, num4));
				if (mode == OpusMode.MODE_HYBRID)
				{
					entropyCoder.enc_uint((uint)(num4 - 2), 256u);
				}
			}
		}
		else
		{
			num3 = 0;
		}
		if (num3 == 0)
		{
			silk_bw_switch = 0;
			num4 = 0;
		}
		if (mode != OpusMode.MODE_CELT_ONLY)
		{
			startBand = 17;
		}
		if (mode == OpusMode.MODE_SILK_ONLY)
		{
			num = entropyCoder.tell() + 7 >> 3;
			entropyCoder.enc_done();
			num44 = num;
		}
		else
		{
			num44 = Inlines.IMIN(num9 - 1 - num4, num44);
			entropyCoder.enc_shrink((uint)num44);
		}
		if ((analysis.enabled && num3 != 0) || mode != OpusMode.MODE_SILK_ONLY)
		{
			celt_Encoder.SetAnalysis(analysisInfo);
		}
		if (num3 != 0 && num5 != 0)
		{
			celt_Encoder.SetStartBand(0);
			celt_Encoder.SetVBR(value: false);
			if (celt_Encoder.celt_encode_with_ec(array4, 0, Fs / 200, data, data_ptr + num44, num4, null) < 0)
			{
				return -3;
			}
			num7 = celt_Encoder.GetFinalRange();
			celt_Encoder.ResetState();
		}
		celt_Encoder.SetStartBand(startBand);
		if (mode != OpusMode.MODE_SILK_ONLY)
		{
			if (mode != prev_mode && prev_mode > (OpusMode)0)
			{
				byte[] compressed = new byte[2];
				celt_Encoder.ResetState();
				celt_Encoder.celt_encode_with_ec(array6, 0, Fs / 400, compressed, 0, 2, null);
				celt_Encoder.SetPrediction(0);
			}
			if (entropyCoder.tell() <= 8 * num44)
			{
				num = celt_Encoder.celt_encode_with_ec(array4, 0, frame_size, null, 0, num44, entropyCoder);
				if (num < 0)
				{
					return -3;
				}
			}
		}
		if (num3 != 0 && num5 == 0)
		{
			byte[] compressed2 = new byte[2];
			int num47 = Fs / 200;
			int num48 = Fs / 400;
			celt_Encoder.ResetState();
			celt_Encoder.SetStartBand(0);
			celt_Encoder.SetPrediction(0);
			celt_Encoder.celt_encode_with_ec(array4, channels * (frame_size - num47 - num48), num48, compressed2, 0, 2, null);
			if (celt_Encoder.celt_encode_with_ec(array4, channels * (frame_size - num47), num47, data, data_ptr + num44, num4, null) < 0)
			{
				return -3;
			}
			num7 = celt_Encoder.GetFinalRange();
		}
		data_ptr--;
		data[data_ptr] = CodecHelpers.gen_toc(mode, Fs / frame_size, opusBandwidth4, stream_channels);
		rangeFinal = entropyCoder.rng ^ num7;
		if (num6 != 0)
		{
			prev_mode = OpusMode.MODE_CELT_ONLY;
		}
		else
		{
			prev_mode = mode;
		}
		prev_channels = stream_channels;
		prev_framesize = frame_size;
		first = 0;
		if (entropyCoder.tell() > (num9 - 1) * 8)
		{
			if (num9 < 2)
			{
				return -2;
			}
			data[data_ptr + 1] = 0;
			num = 1;
			rangeFinal = 0u;
		}
		else if (mode == OpusMode.MODE_SILK_ONLY && num3 == 0)
		{
			while (num > 2 && data[data_ptr + num] == 0)
			{
				num--;
			}
		}
		num += 1 + num4;
		if (use_vbr == 0)
		{
			if (OpusRepacketizer.PadPacket(data, data_ptr, num, num9) != 0)
			{
				return -3;
			}
			num = num9;
		}
		return num;
	}

	private static int GetVariableQualityBitrate(int vqLevel, float music_prob)
	{
		float num = vqTable[vqLevel][0];
		float num2 = vqTable[vqLevel][1];
		float num3 = (float)Math.Sqrt(music_prob);
		return (int)(num2 * num3 + num * (1f - num3));
	}

	public int Encode(short[] in_pcm, int pcm_offset, int frame_size, byte[] out_data, int out_data_offset, int max_data_bytes)
	{
		if (out_data_offset + max_data_bytes > out_data.Length)
		{
			throw new ArgumentException(string.Format("Output buffer is too small: Stated size is {0} bytes, actual size is {1} bytes", new object[2]
			{
				max_data_bytes,
				out_data.Length - out_data_offset
			}));
		}
		int num = CodecHelpers.compute_frame_size(delay_compensation: (application != OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY) ? delay_compensation : 0, analysis_pcm: in_pcm, analysis_pcm_ptr: pcm_offset, frame_size: frame_size, variable_duration: variable_duration, C: channels, Fs: Fs, bitrate_bps: bitrate_bps, downmix: Downmix.downmix_int, subframe_mem: analysis.subframe_mem, analysis_enabled: analysis.enabled);
		if (pcm_offset + num > in_pcm.Length)
		{
			throw new ArgumentException(string.Format("Not enough samples provided in input signal: Expected {0} samples, found {1}", new object[2]
			{
				num,
				in_pcm.Length - pcm_offset
			}));
		}
		try
		{
			int num2 = opus_encode_native(in_pcm, pcm_offset, num, out_data, out_data_offset, max_data_bytes, 16, in_pcm, pcm_offset, frame_size, 0, -2, channels, Downmix.downmix_int, 0);
			if (num2 < 0)
			{
				if (num2 == -1)
				{
					throw new ArgumentException("OPUS_BAD_ARG while encoding");
				}
				throw new OpusException("An error occurred during encoding", num2);
			}
			return num2;
		}
		catch (OutOfMemoryException ex)
		{
			throw new OpusException("Internal error during encoding: " + ex.Message);
		}
	}

	public int Encode(float[] in_pcm, int pcm_offset, int frame_size, byte[] out_data, int out_data_offset, int max_data_bytes)
	{
		if (out_data_offset + max_data_bytes > out_data.Length)
		{
			throw new ArgumentException(string.Format("Output buffer is too small: Stated size is {0} bytes, actual size is {1} bytes", new object[2]
			{
				max_data_bytes,
				out_data.Length - out_data_offset
			}));
		}
		int num = CodecHelpers.compute_frame_size(delay_compensation: (application != OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY) ? delay_compensation : 0, analysis_pcm: in_pcm, analysis_pcm_ptr: pcm_offset, frame_size: frame_size, variable_duration: variable_duration, C: channels, Fs: Fs, bitrate_bps: bitrate_bps, downmix: Downmix.downmix_float, subframe_mem: analysis.subframe_mem, analysis_enabled: analysis.enabled);
		if (pcm_offset + num > in_pcm.Length)
		{
			throw new ArgumentException(string.Format("Not enough samples provided in input signal: Expected {0} samples, found {1}", new object[2]
			{
				num,
				in_pcm.Length - pcm_offset
			}));
		}
		short[] array = new short[num * channels];
		for (int i = 0; i < num * channels; i++)
		{
			array[i] = Inlines.FLOAT2INT16(in_pcm[pcm_offset + i]);
		}
		try
		{
			int num2 = opus_encode_native(array, 0, num, out_data, out_data_offset, max_data_bytes, 16, in_pcm, pcm_offset, frame_size, 0, -2, channels, Downmix.downmix_float, 1);
			if (num2 < 0)
			{
				if (num2 == -1)
				{
					throw new ArgumentException("OPUS_BAD_ARG while decoding");
				}
				throw new OpusException("An error occurred during encoding", num2);
			}
			return num2;
		}
		catch (OutOfMemoryException ex)
		{
			throw new OpusException("Internal error during encoding: " + ex.Message);
		}
	}

	internal void SetEnergyMask(int[] value)
	{
		energy_masking = value;
		Celt_Encoder.SetEnergyMask(value);
	}

	internal CeltMode GetCeltMode()
	{
		return Celt_Encoder.GetMode();
	}
}

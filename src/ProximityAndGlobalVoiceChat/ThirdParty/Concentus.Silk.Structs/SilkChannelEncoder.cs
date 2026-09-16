using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Enums;

namespace Concentus.Silk.Structs;

internal class SilkChannelEncoder
{
	internal readonly int[] In_HP_State = new int[2];

	internal int variable_HP_smth1_Q15;

	internal int variable_HP_smth2_Q15;

	internal readonly SilkLPState sLP = new SilkLPState();

	internal readonly SilkVADState sVAD = new SilkVADState();

	internal readonly SilkNSQState sNSQ = new SilkNSQState();

	internal readonly short[] prev_NLSFq_Q15 = new short[16];

	internal int speech_activity_Q8;

	internal int allow_bandwidth_switch;

	internal sbyte LBRRprevLastGainIndex;

	internal sbyte prevSignalType;

	internal int prevLag;

	internal int pitch_LPC_win_length;

	internal int max_pitch_lag;

	internal int API_fs_Hz;

	internal int prev_API_fs_Hz;

	internal int maxInternal_fs_Hz;

	internal int minInternal_fs_Hz;

	internal int desiredInternal_fs_Hz;

	internal int fs_kHz;

	internal int nb_subfr;

	internal int frame_length;

	internal int subfr_length;

	internal int ltp_mem_length;

	internal int la_pitch;

	internal int la_shape;

	internal int shapeWinLength;

	internal int TargetRate_bps;

	internal int PacketSize_ms;

	internal int PacketLoss_perc;

	internal int frameCounter;

	internal int Complexity;

	internal int nStatesDelayedDecision;

	internal int useInterpolatedNLSFs;

	internal int shapingLPCOrder;

	internal int predictLPCOrder;

	internal int pitchEstimationComplexity;

	internal int pitchEstimationLPCOrder;

	internal int pitchEstimationThreshold_Q16;

	internal int LTPQuantLowComplexity;

	internal int mu_LTP_Q9;

	internal int sum_log_gain_Q7;

	internal int NLSF_MSVQ_Survivors;

	internal int first_frame_after_reset;

	internal int controlled_since_last_payload;

	internal int warping_Q16;

	internal int useCBR;

	internal int prefillFlag;

	internal byte[] pitch_lag_low_bits_iCDF;

	internal byte[] pitch_contour_iCDF;

	internal NLSFCodebook psNLSF_CB;

	internal readonly int[] input_quality_bands_Q15 = new int[4];

	internal int input_tilt_Q15;

	internal int SNR_dB_Q7;

	internal readonly sbyte[] VAD_flags = new sbyte[3];

	internal sbyte LBRR_flag;

	internal readonly int[] LBRR_flags = new int[3];

	internal readonly SideInfoIndices indices = new SideInfoIndices();

	internal readonly sbyte[] pulses = new sbyte[320];

	internal readonly short[] inputBuf = new short[322];

	internal int inputBufIx;

	internal int nFramesPerPacket;

	internal int nFramesEncoded;

	internal int nChannelsAPI;

	internal int nChannelsInternal;

	internal int channelNb;

	internal int frames_since_onset;

	internal int ec_prevSignalType;

	internal short ec_prevLagIndex;

	internal readonly SilkResamplerState resampler_state = new SilkResamplerState();

	internal int useDTX;

	internal int inDTX;

	internal int noSpeechCounter;

	internal int useInBandFEC;

	internal int LBRR_enabled;

	internal int LBRR_GainIncreases;

	internal readonly SideInfoIndices[] indices_LBRR = new SideInfoIndices[3];

	internal readonly sbyte[][] pulses_LBRR = Arrays.InitTwoDimensionalArray<sbyte>(3, 320);

	internal readonly SilkShapeState sShape = new SilkShapeState();

	internal readonly SilkPrefilterState sPrefilt = new SilkPrefilterState();

	internal readonly short[] x_buf = new short[720];

	internal int LTPCorr_Q15;

	internal SilkChannelEncoder()
	{
		for (int i = 0; i < 3; i++)
		{
			indices_LBRR[i] = new SideInfoIndices();
		}
	}

	internal void Reset()
	{
		Arrays.MemSetInt(In_HP_State, 0, 2);
		variable_HP_smth1_Q15 = 0;
		variable_HP_smth2_Q15 = 0;
		sLP.Reset();
		sVAD.Reset();
		sNSQ.Reset();
		Arrays.MemSetShort(prev_NLSFq_Q15, 0, 16);
		speech_activity_Q8 = 0;
		allow_bandwidth_switch = 0;
		LBRRprevLastGainIndex = 0;
		prevSignalType = 0;
		prevLag = 0;
		pitch_LPC_win_length = 0;
		max_pitch_lag = 0;
		API_fs_Hz = 0;
		prev_API_fs_Hz = 0;
		maxInternal_fs_Hz = 0;
		minInternal_fs_Hz = 0;
		desiredInternal_fs_Hz = 0;
		fs_kHz = 0;
		nb_subfr = 0;
		frame_length = 0;
		subfr_length = 0;
		ltp_mem_length = 0;
		la_pitch = 0;
		la_shape = 0;
		shapeWinLength = 0;
		TargetRate_bps = 0;
		PacketSize_ms = 0;
		PacketLoss_perc = 0;
		frameCounter = 0;
		Complexity = 0;
		nStatesDelayedDecision = 0;
		useInterpolatedNLSFs = 0;
		shapingLPCOrder = 0;
		predictLPCOrder = 0;
		pitchEstimationComplexity = 0;
		pitchEstimationLPCOrder = 0;
		pitchEstimationThreshold_Q16 = 0;
		LTPQuantLowComplexity = 0;
		mu_LTP_Q9 = 0;
		sum_log_gain_Q7 = 0;
		NLSF_MSVQ_Survivors = 0;
		first_frame_after_reset = 0;
		controlled_since_last_payload = 0;
		warping_Q16 = 0;
		useCBR = 0;
		prefillFlag = 0;
		pitch_lag_low_bits_iCDF = null;
		pitch_contour_iCDF = null;
		psNLSF_CB = null;
		Arrays.MemSetInt(input_quality_bands_Q15, 0, 4);
		input_tilt_Q15 = 0;
		SNR_dB_Q7 = 0;
		Arrays.MemSetSbyte(VAD_flags, 0, 3);
		LBRR_flag = 0;
		Arrays.MemSetInt(LBRR_flags, 0, 3);
		indices.Reset();
		Arrays.MemSetSbyte(pulses, 0, 320);
		Arrays.MemSetShort(inputBuf, 0, 322);
		inputBufIx = 0;
		nFramesPerPacket = 0;
		nFramesEncoded = 0;
		nChannelsAPI = 0;
		nChannelsInternal = 0;
		channelNb = 0;
		frames_since_onset = 0;
		ec_prevSignalType = 0;
		ec_prevLagIndex = 0;
		resampler_state.Reset();
		useDTX = 0;
		inDTX = 0;
		noSpeechCounter = 0;
		useInBandFEC = 0;
		LBRR_enabled = 0;
		LBRR_GainIncreases = 0;
		for (int i = 0; i < 3; i++)
		{
			indices_LBRR[i].Reset();
			Arrays.MemSetSbyte(pulses_LBRR[i], 0, 320);
		}
		sShape.Reset();
		sPrefilt.Reset();
		Arrays.MemSetShort(x_buf, 0, 720);
		LTPCorr_Q15 = 0;
	}

	internal int silk_control_encoder(EncControlState encControl, int TargetRate_bps, int allow_bw_switch, int channelNb, int force_fs_kHz)
	{
		int result = SilkError.SILK_NO_ERROR;
		useDTX = encControl.useDTX;
		useCBR = encControl.useCBR;
		API_fs_Hz = encControl.API_sampleRate;
		maxInternal_fs_Hz = encControl.maxInternalSampleRate;
		minInternal_fs_Hz = encControl.minInternalSampleRate;
		desiredInternal_fs_Hz = encControl.desiredInternalSampleRate;
		useInBandFEC = encControl.useInBandFEC;
		nChannelsAPI = encControl.nChannelsAPI;
		nChannelsInternal = encControl.nChannelsInternal;
		allow_bandwidth_switch = allow_bw_switch;
		this.channelNb = channelNb;
		if (controlled_since_last_payload != 0 && prefillFlag == 0)
		{
			if (API_fs_Hz != prev_API_fs_Hz && fs_kHz > 0)
			{
				result = silk_setup_resamplers(fs_kHz);
			}
			return result;
		}
		int num = silk_control_audio_bandwidth(encControl);
		if (force_fs_kHz != 0)
		{
			num = force_fs_kHz;
		}
		result = silk_setup_resamplers(num);
		result = silk_setup_fs(num, encControl.payloadSize_ms);
		result = silk_setup_complexity(encControl.complexity);
		PacketLoss_perc = encControl.packetLossPercentage;
		result = silk_setup_LBRR(TargetRate_bps);
		controlled_since_last_payload = 1;
		return result;
	}

	private int silk_setup_resamplers(int fs_kHz)
	{
		int num = 0;
		if (this.fs_kHz != fs_kHz || prev_API_fs_Hz != API_fs_Hz)
		{
			if (this.fs_kHz == 0)
			{
				num += Resampler.silk_resampler_init(resampler_state, API_fs_Hz, fs_kHz * 1000, 1);
			}
			else
			{
				SilkResamplerState silkResamplerState = null;
				int num2 = Inlines.silk_LSHIFT(nb_subfr * 5, 1) + 5;
				int inLen = num2 * this.fs_kHz;
				silkResamplerState = new SilkResamplerState();
				num += Resampler.silk_resampler_init(silkResamplerState, Inlines.silk_SMULBB(this.fs_kHz, 1000), API_fs_Hz, 0);
				int num3 = num2 * Inlines.silk_DIV32_16(API_fs_Hz, 1000);
				short[] array = new short[num3];
				num += Resampler.silk_resampler(silkResamplerState, array, 0, x_buf, 0, inLen);
				num += Resampler.silk_resampler_init(resampler_state, API_fs_Hz, Inlines.silk_SMULBB(fs_kHz, 1000), 1);
				num += Resampler.silk_resampler(resampler_state, x_buf, 0, array, 0, num3);
			}
		}
		prev_API_fs_Hz = API_fs_Hz;
		return num;
	}

	private int silk_setup_fs(int fs_kHz, int PacketSize_ms)
	{
		int result = SilkError.SILK_NO_ERROR;
		if (PacketSize_ms != this.PacketSize_ms)
		{
			if (PacketSize_ms != 10 && PacketSize_ms != 20 && PacketSize_ms != 40 && PacketSize_ms != 60)
			{
				result = SilkError.SILK_ENC_PACKET_SIZE_NOT_SUPPORTED;
			}
			if (PacketSize_ms <= 10)
			{
				nFramesPerPacket = 1;
				nb_subfr = ((PacketSize_ms != 10) ? 1 : 2);
				frame_length = Inlines.silk_SMULBB(PacketSize_ms, fs_kHz);
				pitch_LPC_win_length = Inlines.silk_SMULBB(14, fs_kHz);
				if (this.fs_kHz == 8)
				{
					pitch_contour_iCDF = Tables.silk_pitch_contour_10_ms_NB_iCDF;
				}
				else
				{
					pitch_contour_iCDF = Tables.silk_pitch_contour_10_ms_iCDF;
				}
			}
			else
			{
				nFramesPerPacket = Inlines.silk_DIV32_16(PacketSize_ms, 20);
				nb_subfr = 4;
				frame_length = Inlines.silk_SMULBB(20, fs_kHz);
				pitch_LPC_win_length = Inlines.silk_SMULBB(24, fs_kHz);
				if (this.fs_kHz == 8)
				{
					pitch_contour_iCDF = Tables.silk_pitch_contour_NB_iCDF;
				}
				else
				{
					pitch_contour_iCDF = Tables.silk_pitch_contour_iCDF;
				}
			}
			this.PacketSize_ms = PacketSize_ms;
			TargetRate_bps = 0;
		}
		if (this.fs_kHz != fs_kHz)
		{
			sShape.Reset();
			sPrefilt.Reset();
			sNSQ.Reset();
			Arrays.MemSetShort(prev_NLSFq_Q15, 0, 16);
			Arrays.MemSetInt(sLP.In_LP_State, 0, 2);
			inputBufIx = 0;
			nFramesEncoded = 0;
			TargetRate_bps = 0;
			prevLag = 100;
			first_frame_after_reset = 1;
			sPrefilt.lagPrev = 100;
			sShape.LastGainIndex = 10;
			sNSQ.lagPrev = 100;
			sNSQ.prev_gain_Q16 = 65536;
			prevSignalType = 0;
			this.fs_kHz = fs_kHz;
			if (this.fs_kHz == 8)
			{
				if (nb_subfr == 4)
				{
					pitch_contour_iCDF = Tables.silk_pitch_contour_NB_iCDF;
				}
				else
				{
					pitch_contour_iCDF = Tables.silk_pitch_contour_10_ms_NB_iCDF;
				}
			}
			else if (nb_subfr == 4)
			{
				pitch_contour_iCDF = Tables.silk_pitch_contour_iCDF;
			}
			else
			{
				pitch_contour_iCDF = Tables.silk_pitch_contour_10_ms_iCDF;
			}
			if (this.fs_kHz == 8 || this.fs_kHz == 12)
			{
				predictLPCOrder = 10;
				psNLSF_CB = Tables.silk_NLSF_CB_NB_MB;
			}
			else
			{
				predictLPCOrder = 16;
				psNLSF_CB = Tables.silk_NLSF_CB_WB;
			}
			subfr_length = 5 * fs_kHz;
			frame_length = Inlines.silk_SMULBB(subfr_length, nb_subfr);
			ltp_mem_length = Inlines.silk_SMULBB(20, fs_kHz);
			la_pitch = Inlines.silk_SMULBB(2, fs_kHz);
			max_pitch_lag = Inlines.silk_SMULBB(18, fs_kHz);
			if (nb_subfr == 4)
			{
				pitch_LPC_win_length = Inlines.silk_SMULBB(24, fs_kHz);
			}
			else
			{
				pitch_LPC_win_length = Inlines.silk_SMULBB(14, fs_kHz);
			}
			if (this.fs_kHz == 16)
			{
				mu_LTP_Q9 = 10;
				pitch_lag_low_bits_iCDF = Tables.silk_uniform8_iCDF;
			}
			else if (this.fs_kHz == 12)
			{
				mu_LTP_Q9 = 13;
				pitch_lag_low_bits_iCDF = Tables.silk_uniform6_iCDF;
			}
			else
			{
				mu_LTP_Q9 = 15;
				pitch_lag_low_bits_iCDF = Tables.silk_uniform4_iCDF;
			}
		}
		return result;
	}

	private int silk_setup_complexity(int Complexity)
	{
		if (Complexity < 2)
		{
			pitchEstimationComplexity = 0;
			pitchEstimationThreshold_Q16 = 52429;
			pitchEstimationLPCOrder = 6;
			shapingLPCOrder = 8;
			la_shape = 3 * fs_kHz;
			nStatesDelayedDecision = 1;
			useInterpolatedNLSFs = 0;
			LTPQuantLowComplexity = 1;
			NLSF_MSVQ_Survivors = 2;
			warping_Q16 = 0;
		}
		else if (Complexity < 4)
		{
			pitchEstimationComplexity = 1;
			pitchEstimationThreshold_Q16 = 49807;
			pitchEstimationLPCOrder = 8;
			shapingLPCOrder = 10;
			la_shape = 5 * fs_kHz;
			nStatesDelayedDecision = 1;
			useInterpolatedNLSFs = 0;
			LTPQuantLowComplexity = 0;
			NLSF_MSVQ_Survivors = 4;
			warping_Q16 = 0;
		}
		else if (Complexity < 6)
		{
			pitchEstimationComplexity = 1;
			pitchEstimationThreshold_Q16 = 48497;
			pitchEstimationLPCOrder = 10;
			shapingLPCOrder = 12;
			la_shape = 5 * fs_kHz;
			nStatesDelayedDecision = 2;
			useInterpolatedNLSFs = 1;
			LTPQuantLowComplexity = 0;
			NLSF_MSVQ_Survivors = 8;
			warping_Q16 = fs_kHz * 983;
		}
		else if (Complexity < 8)
		{
			pitchEstimationComplexity = 1;
			pitchEstimationThreshold_Q16 = 47186;
			pitchEstimationLPCOrder = 12;
			shapingLPCOrder = 14;
			la_shape = 5 * fs_kHz;
			nStatesDelayedDecision = 3;
			useInterpolatedNLSFs = 1;
			LTPQuantLowComplexity = 0;
			NLSF_MSVQ_Survivors = 16;
			warping_Q16 = fs_kHz * 983;
		}
		else
		{
			pitchEstimationComplexity = 2;
			pitchEstimationThreshold_Q16 = 45875;
			pitchEstimationLPCOrder = 16;
			shapingLPCOrder = 16;
			la_shape = 5 * fs_kHz;
			nStatesDelayedDecision = 4;
			useInterpolatedNLSFs = 1;
			LTPQuantLowComplexity = 0;
			NLSF_MSVQ_Survivors = 32;
			warping_Q16 = fs_kHz * 983;
		}
		pitchEstimationLPCOrder = Inlines.silk_min_int(pitchEstimationLPCOrder, predictLPCOrder);
		shapeWinLength = 5 * fs_kHz + 2 * la_shape;
		this.Complexity = Complexity;
		return 0;
	}

	private int silk_setup_LBRR(int TargetRate_bps)
	{
		int sILK_NO_ERROR = SilkError.SILK_NO_ERROR;
		int lBRR_enabled = LBRR_enabled;
		LBRR_enabled = 0;
		if (useInBandFEC != 0 && PacketLoss_perc > 0)
		{
			int a = ((fs_kHz == 8) ? 12000 : ((fs_kHz != 12) ? 16000 : 14000));
			a = Inlines.silk_SMULWB(Inlines.silk_MUL(a, 125 - Inlines.silk_min(PacketLoss_perc, 25)), 655);
			if (TargetRate_bps > a)
			{
				if (lBRR_enabled == 0)
				{
					LBRR_GainIncreases = 7;
				}
				else
				{
					LBRR_GainIncreases = Inlines.silk_max_int(7 - Inlines.silk_SMULWB(PacketLoss_perc, 26214), 2);
				}
				LBRR_enabled = 1;
			}
		}
		return sILK_NO_ERROR;
	}

	internal int silk_control_audio_bandwidth(EncControlState encControl)
	{
		int num = fs_kHz;
		int num2 = Inlines.silk_SMULBB(num, 1000);
		if (num2 == 0)
		{
			num2 = Inlines.silk_min(desiredInternal_fs_Hz, API_fs_Hz);
			num = Inlines.silk_DIV32_16(num2, 1000);
		}
		else if (num2 > API_fs_Hz || num2 > maxInternal_fs_Hz || num2 < minInternal_fs_Hz)
		{
			num2 = API_fs_Hz;
			num2 = Inlines.silk_min(num2, maxInternal_fs_Hz);
			num2 = Inlines.silk_max(num2, minInternal_fs_Hz);
			num = Inlines.silk_DIV32_16(num2, 1000);
		}
		else
		{
			if (sLP.transition_frame_no >= 256)
			{
				sLP.mode = 0;
			}
			if (allow_bandwidth_switch != 0 || encControl.opusCanSwitch != 0)
			{
				if (Inlines.silk_SMULBB(fs_kHz, 1000) > desiredInternal_fs_Hz)
				{
					if (sLP.mode == 0)
					{
						sLP.transition_frame_no = 256;
						Arrays.MemSetInt(sLP.In_LP_State, 0, 2);
					}
					if (encControl.opusCanSwitch != 0)
					{
						sLP.mode = 0;
						num = ((fs_kHz == 16) ? 12 : 8);
					}
					else if (sLP.transition_frame_no <= 0)
					{
						encControl.switchReady = 1;
						encControl.maxBits -= encControl.maxBits * 5 / (encControl.payloadSize_ms + 5);
					}
					else
					{
						sLP.mode = -2;
					}
				}
				else if (Inlines.silk_SMULBB(fs_kHz, 1000) < desiredInternal_fs_Hz)
				{
					if (encControl.opusCanSwitch != 0)
					{
						num = ((fs_kHz == 8) ? 12 : 16);
						sLP.transition_frame_no = 0;
						Arrays.MemSetInt(sLP.In_LP_State, 0, 2);
						sLP.mode = 1;
					}
					else if (sLP.mode == 0)
					{
						encControl.switchReady = 1;
						encControl.maxBits -= encControl.maxBits * 5 / (encControl.payloadSize_ms + 5);
					}
					else
					{
						sLP.mode = 1;
					}
				}
				else if (sLP.mode < 0)
				{
					sLP.mode = 1;
				}
			}
		}
		return num;
	}

	internal int silk_control_SNR(int TargetRate_bps)
	{
		int sILK_NO_ERROR = SilkError.SILK_NO_ERROR;
		TargetRate_bps = Inlines.silk_LIMIT(TargetRate_bps, 5000, 80000);
		if (TargetRate_bps != this.TargetRate_bps)
		{
			this.TargetRate_bps = TargetRate_bps;
			int[] array = ((fs_kHz == 8) ? Tables.silk_TargetRate_table_NB : ((fs_kHz != 12) ? Tables.silk_TargetRate_table_WB : Tables.silk_TargetRate_table_MB));
			if (nb_subfr == 2)
			{
				TargetRate_bps -= 2200;
			}
			for (int i = 1; i < 8; i++)
			{
				if (TargetRate_bps <= array[i])
				{
					int a = Inlines.silk_DIV32(Inlines.silk_LSHIFT(TargetRate_bps - array[i - 1], 6), array[i] - array[i - 1]);
					SNR_dB_Q7 = Inlines.silk_LSHIFT(Tables.silk_SNR_table_Q1[i - 1], 6) + Inlines.silk_MUL(a, Tables.silk_SNR_table_Q1[i] - Tables.silk_SNR_table_Q1[i - 1]);
					break;
				}
			}
		}
		return sILK_NO_ERROR;
	}

	internal void silk_encode_do_VAD()
	{
		VoiceActivityDetection.silk_VAD_GetSA_Q8(this, inputBuf, 1);
		if (speech_activity_Q8 < 13)
		{
			indices.signalType = 0;
			noSpeechCounter++;
			if (noSpeechCounter < 10)
			{
				inDTX = 0;
			}
			else if (noSpeechCounter > 30)
			{
				noSpeechCounter = 10;
				inDTX = 0;
			}
			VAD_flags[nFramesEncoded] = 0;
		}
		else
		{
			noSpeechCounter = 0;
			inDTX = 0;
			indices.signalType = 1;
			VAD_flags[nFramesEncoded] = 1;
		}
	}

	internal int silk_encode_frame(BoxedValueInt pnBytesOut, EntropyCoder psRangeEnc, int condCoding, int maxBits, int useCBR)
	{
		SilkEncoderControl silkEncoderControl = new SilkEncoderControl();
		int result = 0;
		EntropyCoder entropyCoder = new EntropyCoder();
		EntropyCoder entropyCoder2 = new EntropyCoder();
		SilkNSQState silkNSQState = new SilkNSQState();
		SilkNSQState silkNSQState2 = new SilkNSQState();
		sbyte lastGainIndex = 0;
		int num2;
		int num3;
		int num4;
		int num = (num2 = (num3 = (num4 = 0)));
		indices.Seed = (sbyte)(frameCounter++ & 3);
		int num5 = ltp_mem_length;
		sLP.silk_LP_variable_cutoff(inputBuf, 1, frame_length);
		Array.Copy(inputBuf, 1, x_buf, num5 + 5 * fs_kHz, frame_length);
		if (prefillFlag == 0)
		{
			short[] array = new short[la_pitch + frame_length + ltp_mem_length];
			int pitch_res_ptr = ltp_mem_length;
			FindPitchLags.silk_find_pitch_lags(this, silkEncoderControl, array, x_buf, num5);
			NoiseShapeAnalysis.silk_noise_shape_analysis(this, silkEncoderControl, array, pitch_res_ptr, x_buf, num5);
			FindPredCoefs.silk_find_pred_coefs(this, silkEncoderControl, array, x_buf, num5, condCoding);
			ProcessGains.silk_process_gains(this, silkEncoderControl, condCoding);
			int[] array2 = new int[frame_length];
			Filters.silk_prefilter(this, silkEncoderControl, array2, x_buf, num5);
			silk_LBRR_encode(silkEncoderControl, array2, condCoding);
			int num6 = 6;
			short num7 = 256;
			int num8 = 0;
			int num9 = 0;
			int num10 = GainQuantization.silk_gains_ID(indices.GainsIndices, nb_subfr);
			int num11 = -1;
			int num12 = -1;
			entropyCoder.Assign(psRangeEnc);
			silkNSQState.Assign(sNSQ);
			sbyte seed = indices.Seed;
			short num13 = ec_prevLagIndex;
			int num14 = ec_prevSignalType;
			byte[] array3 = new byte[1275];
			int num15 = 0;
			while (true)
			{
				int num16;
				if (num10 == num11)
				{
					num16 = num;
				}
				else if (num10 == num12)
				{
					num16 = num2;
				}
				else
				{
					if (num15 > 0)
					{
						psRangeEnc.Assign(entropyCoder);
						sNSQ.Assign(silkNSQState);
						indices.Seed = seed;
						ec_prevLagIndex = num13;
						ec_prevSignalType = num14;
					}
					if (nStatesDelayedDecision > 1 || warping_Q16 > 0)
					{
						sNSQ.silk_NSQ_del_dec(this, indices, array2, pulses, silkEncoderControl.PredCoef_Q12, silkEncoderControl.LTPCoef_Q14, silkEncoderControl.AR2_Q13, silkEncoderControl.HarmShapeGain_Q14, silkEncoderControl.Tilt_Q14, silkEncoderControl.LF_shp_Q14, silkEncoderControl.Gains_Q16, silkEncoderControl.pitchL, silkEncoderControl.Lambda_Q10, silkEncoderControl.LTP_scale_Q14);
					}
					else
					{
						sNSQ.silk_NSQ(this, indices, array2, pulses, silkEncoderControl.PredCoef_Q12, silkEncoderControl.LTPCoef_Q14, silkEncoderControl.AR2_Q13, silkEncoderControl.HarmShapeGain_Q14, silkEncoderControl.Tilt_Q14, silkEncoderControl.LF_shp_Q14, silkEncoderControl.Gains_Q16, silkEncoderControl.pitchL, silkEncoderControl.Lambda_Q10, silkEncoderControl.LTP_scale_Q14);
					}
					EncodeIndices.silk_encode_indices(this, psRangeEnc, nFramesEncoded, 0, condCoding);
					EncodePulses.silk_encode_pulses(psRangeEnc, indices.signalType, indices.quantOffsetType, pulses, frame_length);
					num16 = psRangeEnc.tell();
					if (useCBR == 0 && num15 == 0 && num16 <= maxBits)
					{
						break;
					}
				}
				if (num15 == num6)
				{
					if (num8 != 0 && (num10 == num11 || num16 > maxBits))
					{
						psRangeEnc.Assign(entropyCoder2);
						Array.Copy(array3, 0, psRangeEnc.buf, psRangeEnc.buf_ptr, (int)entropyCoder2.offs);
						sNSQ.Assign(silkNSQState2);
						sShape.LastGainIndex = lastGainIndex;
					}
					break;
				}
				if (num16 > maxBits)
				{
					if (num8 == 0 && num15 >= 2)
					{
						silkEncoderControl.Lambda_Q10 = Inlines.silk_ADD_RSHIFT32(silkEncoderControl.Lambda_Q10, silkEncoderControl.Lambda_Q10, 1);
						num9 = 0;
						num12 = -1;
					}
					else
					{
						num9 = 1;
						num2 = num16;
						num4 = num7;
						num12 = num10;
					}
				}
				else
				{
					if (num16 >= maxBits - 5)
					{
						break;
					}
					num8 = 1;
					num = num16;
					num3 = num7;
					if (num10 != num11)
					{
						num11 = num10;
						entropyCoder2.Assign(psRangeEnc);
						Array.Copy(psRangeEnc.buf, psRangeEnc.buf_ptr, array3, 0, (int)psRangeEnc.offs);
						silkNSQState2.Assign(sNSQ);
						lastGainIndex = sShape.LastGainIndex;
					}
				}
				if ((num8 & num9) == 0)
				{
					int a = Inlines.silk_log2lin(Inlines.silk_LSHIFT(num16 - maxBits, 7) / frame_length + 2048);
					a = Inlines.silk_min_32(a, 131072);
					if (num16 > maxBits)
					{
						a = Inlines.silk_max_32(a, 85197);
					}
					num7 = (short)Inlines.silk_SMULWB(a, num7);
				}
				else
				{
					num7 = (short)(num3 + Inlines.silk_DIV32_16(Inlines.silk_MUL(num4 - num3, maxBits - num), num2 - num));
					if (num7 > Inlines.silk_ADD_RSHIFT32(num3, num4 - num3, 2))
					{
						num7 = (short)Inlines.silk_ADD_RSHIFT32(num3, num4 - num3, 2);
					}
					else if (num7 < Inlines.silk_SUB_RSHIFT32(num4, num4 - num3, 2))
					{
						num7 = (short)Inlines.silk_SUB_RSHIFT32(num4, num4 - num3, 2);
					}
				}
				for (int i = 0; i < nb_subfr; i++)
				{
					silkEncoderControl.Gains_Q16[i] = Inlines.silk_LSHIFT_SAT32(Inlines.silk_SMULWB(silkEncoderControl.GainsUnq_Q16[i], num7), 8);
				}
				sShape.LastGainIndex = silkEncoderControl.lastGainIndexPrev;
				BoxedValueSbyte boxedValueSbyte = new BoxedValueSbyte(sShape.LastGainIndex);
				GainQuantization.silk_gains_quant(indices.GainsIndices, silkEncoderControl.Gains_Q16, boxedValueSbyte, (condCoding == 2) ? 1 : 0, nb_subfr);
				sShape.LastGainIndex = boxedValueSbyte.Val;
				num10 = GainQuantization.silk_gains_ID(indices.GainsIndices, nb_subfr);
				num15++;
			}
		}
		Arrays.MemMove(x_buf, frame_length, 0, ltp_mem_length + 5 * fs_kHz);
		if (prefillFlag != 0)
		{
			pnBytesOut.Val = 0;
			return result;
		}
		prevLag = silkEncoderControl.pitchL[nb_subfr - 1];
		prevSignalType = indices.signalType;
		first_frame_after_reset = 0;
		pnBytesOut.Val = Inlines.silk_RSHIFT(psRangeEnc.tell() + 7, 3);
		return result;
	}

	internal void silk_LBRR_encode(SilkEncoderControl thisCtrl, int[] xfw_Q3, int condCoding)
	{
		int[] array = new int[nb_subfr];
		SideInfoIndices sideInfoIndices = indices_LBRR[nFramesEncoded];
		SilkNSQState silkNSQState = new SilkNSQState();
		if (LBRR_enabled != 0 && speech_activity_Q8 > 77)
		{
			LBRR_flags[nFramesEncoded] = 1;
			silkNSQState.Assign(sNSQ);
			sideInfoIndices.Assign(indices);
			Array.Copy(thisCtrl.Gains_Q16, array, nb_subfr);
			if (nFramesEncoded == 0 || LBRR_flags[nFramesEncoded - 1] == 0)
			{
				LBRRprevLastGainIndex = sShape.LastGainIndex;
				sideInfoIndices.GainsIndices[0] = (sbyte)(sideInfoIndices.GainsIndices[0] + LBRR_GainIncreases);
				sideInfoIndices.GainsIndices[0] = (sbyte)Inlines.silk_min_int(sideInfoIndices.GainsIndices[0], 63);
			}
			BoxedValueSbyte boxedValueSbyte = new BoxedValueSbyte(LBRRprevLastGainIndex);
			GainQuantization.silk_gains_dequant(thisCtrl.Gains_Q16, sideInfoIndices.GainsIndices, boxedValueSbyte, (condCoding == 2) ? 1 : 0, nb_subfr);
			LBRRprevLastGainIndex = boxedValueSbyte.Val;
			if (nStatesDelayedDecision > 1 || warping_Q16 > 0)
			{
				silkNSQState.silk_NSQ_del_dec(this, sideInfoIndices, xfw_Q3, pulses_LBRR[nFramesEncoded], thisCtrl.PredCoef_Q12, thisCtrl.LTPCoef_Q14, thisCtrl.AR2_Q13, thisCtrl.HarmShapeGain_Q14, thisCtrl.Tilt_Q14, thisCtrl.LF_shp_Q14, thisCtrl.Gains_Q16, thisCtrl.pitchL, thisCtrl.Lambda_Q10, thisCtrl.LTP_scale_Q14);
			}
			else
			{
				silkNSQState.silk_NSQ(this, sideInfoIndices, xfw_Q3, pulses_LBRR[nFramesEncoded], thisCtrl.PredCoef_Q12, thisCtrl.LTPCoef_Q14, thisCtrl.AR2_Q13, thisCtrl.HarmShapeGain_Q14, thisCtrl.Tilt_Q14, thisCtrl.LF_shp_Q14, thisCtrl.Gains_Q16, thisCtrl.pitchL, thisCtrl.Lambda_Q10, thisCtrl.LTP_scale_Q14);
			}
			Array.Copy(array, thisCtrl.Gains_Q16, nb_subfr);
		}
	}
}

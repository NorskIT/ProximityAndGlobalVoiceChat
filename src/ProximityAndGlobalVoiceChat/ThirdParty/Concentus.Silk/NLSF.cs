using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class NLSF
{
	private const int MAX_STABILIZE_LOOPS = 20;

	private const int QA = 16;

	private const int BIN_DIV_STEPS_A2NLSF = 3;

	private const int MAX_ITERATIONS_A2NLSF = 30;

	private static readonly byte[] ordering16 = new byte[16]
	{
		0, 15, 8, 7, 4, 11, 12, 3, 2, 13,
		10, 5, 6, 9, 14, 1
	};

	private static readonly byte[] ordering10 = new byte[10] { 0, 9, 6, 3, 4, 5, 8, 1, 2, 7 };

	internal static void silk_NLSF_VQ(int[] err_Q26, short[] in_Q15, byte[] pCB_Q8, int K, int LPC_order)
	{
		int num = 0;
		for (int i = 0; i < K; i++)
		{
			int num2 = 0;
			for (int j = 0; j < LPC_order; j += 2)
			{
				int num3 = Inlines.silk_SUB_LSHIFT32(in_Q15[j], pCB_Q8[num++], 7);
				int a = Inlines.silk_SMULBB(num3, num3);
				num3 = Inlines.silk_SUB_LSHIFT32(in_Q15[j + 1], pCB_Q8[num++], 7);
				a = Inlines.silk_SMLABB(a, num3, num3);
				num2 = Inlines.silk_ADD_RSHIFT32(num2, a, 4);
			}
			err_Q26[i] = num2;
		}
	}

	internal static void silk_NLSF_VQ_weights_laroia(short[] pNLSFW_Q_OUT, short[] pNLSF_Q15, int D)
	{
		int b = Inlines.silk_max_int(pNLSF_Q15[0], 1);
		b = Inlines.silk_DIV32(131072, b);
		int b2 = Inlines.silk_max_int(pNLSF_Q15[1] - pNLSF_Q15[0], 1);
		b2 = Inlines.silk_DIV32(131072, b2);
		pNLSFW_Q_OUT[0] = (short)Inlines.silk_min_int(b + b2, 32767);
		for (int i = 1; i < D - 1; i += 2)
		{
			b = Inlines.silk_max_int(pNLSF_Q15[i + 1] - pNLSF_Q15[i], 1);
			b = Inlines.silk_DIV32(131072, b);
			pNLSFW_Q_OUT[i] = (short)Inlines.silk_min_int(b + b2, 32767);
			b2 = Inlines.silk_max_int(pNLSF_Q15[i + 2] - pNLSF_Q15[i + 1], 1);
			b2 = Inlines.silk_DIV32(131072, b2);
			pNLSFW_Q_OUT[i + 1] = (short)Inlines.silk_min_int(b + b2, 32767);
		}
		b = Inlines.silk_max_int(32768 - pNLSF_Q15[D - 1], 1);
		b = Inlines.silk_DIV32(131072, b);
		pNLSFW_Q_OUT[D - 1] = (short)Inlines.silk_min_int(b + b2, 32767);
	}

	internal static void silk_NLSF_residual_dequant(short[] x_Q10, sbyte[] indices, int indices_ptr, byte[] pred_coef_Q8, int quant_step_size_Q16, short order)
	{
		short a = 0;
		for (int num = order - 1; num >= 0; num--)
		{
			int a2 = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(a, pred_coef_Q8[num]), 8);
			a = Inlines.silk_LSHIFT16(indices[indices_ptr + num], 10);
			if (a > 0)
			{
				a = Inlines.silk_SUB16(a, 102);
			}
			else if (a < 0)
			{
				a = Inlines.silk_ADD16(a, 102);
			}
			a = (x_Q10[num] = (short)Inlines.silk_SMLAWB(a2, a, quant_step_size_Q16));
		}
	}

	internal static void silk_NLSF_unpack(short[] ec_ix, byte[] pred_Q8, NLSFCodebook psNLSF_CB, int CB1_index)
	{
		byte[] ec_sel = psNLSF_CB.ec_sel;
		int num = CB1_index * psNLSF_CB.order / 2;
		for (int i = 0; i < psNLSF_CB.order; i += 2)
		{
			byte b = ec_sel[num];
			num++;
			ec_ix[i] = (short)Inlines.silk_SMULBB(Inlines.silk_RSHIFT(b, 1) & 7, 9);
			pred_Q8[i] = psNLSF_CB.pred_Q8[i + (b & 1) * (psNLSF_CB.order - 1)];
			ec_ix[i + 1] = (short)Inlines.silk_SMULBB(Inlines.silk_RSHIFT(b, 5) & 7, 9);
			pred_Q8[i + 1] = psNLSF_CB.pred_Q8[i + (Inlines.silk_RSHIFT(b, 4) & 1) * (psNLSF_CB.order - 1) + 1];
		}
	}

	internal static void silk_NLSF_stabilize(short[] NLSF_Q15, short[] NDeltaMin_Q15, int L)
	{
		int num = 0;
		int i;
		for (i = 0; i < 20; i++)
		{
			int num2 = NLSF_Q15[0] - NDeltaMin_Q15[0];
			num = 0;
			int num3;
			for (int j = 1; j <= L - 1; j++)
			{
				num3 = NLSF_Q15[j] - (NLSF_Q15[j - 1] + NDeltaMin_Q15[j]);
				if (num3 < num2)
				{
					num2 = num3;
					num = j;
				}
			}
			num3 = 32768 - (NLSF_Q15[L - 1] + NDeltaMin_Q15[L]);
			if (num3 < num2)
			{
				num2 = num3;
				num = L;
			}
			if (num2 >= 0)
			{
				return;
			}
			if (num == 0)
			{
				NLSF_Q15[0] = NDeltaMin_Q15[0];
				continue;
			}
			if (num == L)
			{
				NLSF_Q15[L - 1] = (short)(32768 - NDeltaMin_Q15[L]);
				continue;
			}
			int num4 = 0;
			for (int k = 0; k < num; k++)
			{
				num4 += NDeltaMin_Q15[k];
			}
			num4 += Inlines.silk_RSHIFT(NDeltaMin_Q15[num], 1);
			int num5 = 32768;
			for (int k = L; k > num; k--)
			{
				num5 -= NDeltaMin_Q15[k];
			}
			num5 -= Inlines.silk_RSHIFT(NDeltaMin_Q15[num], 1);
			short num6 = (short)Inlines.silk_LIMIT_32(Inlines.silk_RSHIFT_ROUND(NLSF_Q15[num - 1] + NLSF_Q15[num], 1), num4, num5);
			NLSF_Q15[num - 1] = (short)(num6 - Inlines.silk_RSHIFT(NDeltaMin_Q15[num], 1));
			NLSF_Q15[num] = (short)(NLSF_Q15[num - 1] + NDeltaMin_Q15[num]);
		}
		if (i == 20)
		{
			Sort.silk_insertion_sort_increasing_all_values_int16(NLSF_Q15, L);
			NLSF_Q15[0] = (short)Inlines.silk_max_int(NLSF_Q15[0], NDeltaMin_Q15[0]);
			for (int j = 1; j < L; j++)
			{
				NLSF_Q15[j] = (short)Inlines.silk_max_int(NLSF_Q15[j], NLSF_Q15[j - 1] + NDeltaMin_Q15[j]);
			}
			NLSF_Q15[L - 1] = (short)Inlines.silk_min_int(NLSF_Q15[L - 1], 32768 - NDeltaMin_Q15[L]);
			for (int j = L - 2; j >= 0; j--)
			{
				NLSF_Q15[j] = (short)Inlines.silk_min_int(NLSF_Q15[j], NLSF_Q15[j + 1] - NDeltaMin_Q15[j + 1]);
			}
		}
	}

	internal static void silk_NLSF_decode(short[] pNLSF_Q15, sbyte[] NLSFIndices, NLSFCodebook psNLSF_CB)
	{
		byte[] array = new byte[psNLSF_CB.order];
		short[] ec_ix = new short[psNLSF_CB.order];
		short[] array2 = new short[psNLSF_CB.order];
		short[] array3 = new short[psNLSF_CB.order];
		byte[] cB1_NLSF_Q = psNLSF_CB.CB1_NLSF_Q8;
		int num = NLSFIndices[0] * psNLSF_CB.order;
		for (int i = 0; i < psNLSF_CB.order; i++)
		{
			pNLSF_Q15[i] = Inlines.silk_LSHIFT16(cB1_NLSF_Q[num + i], 7);
		}
		silk_NLSF_unpack(ec_ix, array, psNLSF_CB, NLSFIndices[0]);
		silk_NLSF_residual_dequant(array2, NLSFIndices, 1, array, psNLSF_CB.quantStepSize_Q16, psNLSF_CB.order);
		silk_NLSF_VQ_weights_laroia(array3, pNLSF_Q15, psNLSF_CB.order);
		for (int i = 0; i < psNLSF_CB.order; i++)
		{
			int num2 = Inlines.silk_SQRT_APPROX(Inlines.silk_LSHIFT(array3[i], 16));
			int a = Inlines.silk_ADD32(pNLSF_Q15[i], Inlines.silk_DIV32_16(Inlines.silk_LSHIFT(array2[i], 14), (short)num2));
			pNLSF_Q15[i] = (short)Inlines.silk_LIMIT(a, 0, 32767);
		}
		silk_NLSF_stabilize(pNLSF_Q15, psNLSF_CB.deltaMin_Q15, psNLSF_CB.order);
	}

	internal static int silk_NLSF_del_dec_quant(sbyte[] indices, short[] x_Q10, short[] w_Q5, byte[] pred_coef_Q8, short[] ec_ix, byte[] ec_rates_Q5, int quant_step_size_Q16, short inv_quant_step_size_Q6, int mu_Q20, short order)
	{
		int[] array = new int[4];
		sbyte[][] array2 = new sbyte[4][];
		int i;
		for (i = 0; i < 4; i++)
		{
			array2[i] = new sbyte[16];
		}
		short[] array3 = new short[8];
		int[] array4 = new int[8];
		int[] array5 = new int[4];
		int[] array6 = new int[4];
		int[] array7 = new int[20];
		int[] array8 = new int[20];
		for (i = -10; i <= 9; i++)
		{
			int num = Inlines.silk_LSHIFT(i, 10);
			int num2 = Inlines.silk_ADD16((short)num, 1024);
			if (i > 0)
			{
				num = Inlines.silk_SUB16((short)num, 102);
				num2 = Inlines.silk_SUB16((short)num2, 102);
			}
			else
			{
				switch (i)
				{
				case 0:
					num2 = Inlines.silk_SUB16((short)num2, 102);
					break;
				case -1:
					num = Inlines.silk_ADD16((short)num, 102);
					break;
				default:
					num = Inlines.silk_ADD16((short)num, 102);
					num2 = Inlines.silk_ADD16((short)num2, 102);
					break;
				}
			}
			array7[i + 10] = Inlines.silk_SMULWB(num, quant_step_size_Q16);
			array8[i + 10] = Inlines.silk_SMULWB(num2, quant_step_size_Q16);
		}
		int num3 = 1;
		array4[0] = 0;
		array3[0] = 0;
		i = order - 1;
		int a2;
		while (true)
		{
			int a = Inlines.silk_LSHIFT(pred_coef_Q8[i], 8);
			int num4 = x_Q10[i];
			for (int j = 0; j < num3; j++)
			{
				int num5 = Inlines.silk_SMULWB(a, array3[j]);
				int b = Inlines.silk_SUB16((short)num4, (short)num5);
				a2 = Inlines.silk_SMULWB(inv_quant_step_size_Q6, b);
				a2 = Inlines.silk_LIMIT(a2, -10, 9);
				array2[j][i] = (sbyte)a2;
				int num6 = ec_ix[i] + a2;
				int num = array7[a2 + 10];
				int num2 = array8[a2 + 10];
				num = Inlines.silk_ADD16((short)num, (short)num5);
				num2 = Inlines.silk_ADD16((short)num2, (short)num5);
				array3[j] = (short)num;
				array3[j + num3] = (short)num2;
				int num7;
				int c;
				if (a2 + 1 >= 4)
				{
					if (a2 + 1 == 4)
					{
						num7 = ec_rates_Q5[num6 + 4];
						c = 280;
					}
					else
					{
						num7 = Inlines.silk_SMLABB(108, 43, a2);
						c = Inlines.silk_ADD16((short)num7, 43);
					}
				}
				else if (a2 <= -4)
				{
					if (a2 == -4)
					{
						num7 = 280;
						c = ec_rates_Q5[num6 + 1 + 4];
					}
					else
					{
						num7 = Inlines.silk_SMLABB(108, -43, a2);
						c = Inlines.silk_SUB16((short)num7, 43);
					}
				}
				else
				{
					num7 = ec_rates_Q5[num6 + 4];
					c = ec_rates_Q5[num6 + 1 + 4];
				}
				int a3 = array4[j];
				int num8 = Inlines.silk_SUB16((short)num4, (short)num);
				array4[j] = Inlines.silk_SMLABB(Inlines.silk_MLA(a3, Inlines.silk_SMULBB(num8, num8), w_Q5[i]), mu_Q20, num7);
				num8 = Inlines.silk_SUB16((short)num4, (short)num2);
				array4[j + num3] = Inlines.silk_SMLABB(Inlines.silk_MLA(a3, Inlines.silk_SMULBB(num8, num8), w_Q5[i]), mu_Q20, c);
			}
			if (num3 <= 2)
			{
				for (int j = 0; j < num3; j++)
				{
					array2[j + num3][i] = (sbyte)(array2[j][i] + 1);
				}
				num3 = Inlines.silk_LSHIFT(num3, 1);
				for (int j = num3; j < 4; j++)
				{
					array2[j][i] = array2[j - num3][i];
				}
			}
			else
			{
				if (i <= 0)
				{
					break;
				}
				for (int j = 0; j < 4; j++)
				{
					if (array4[j] > array4[j + 4])
					{
						array6[j] = array4[j];
						array5[j] = array4[j + 4];
						array4[j] = array5[j];
						array4[j + 4] = array6[j];
						int num = array3[j];
						array3[j] = array3[j + 4];
						array3[j + 4] = (short)num;
						array[j] = j + 4;
					}
					else
					{
						array5[j] = array4[j];
						array6[j] = array4[j + 4];
						array[j] = j;
					}
				}
				while (true)
				{
					int num9 = int.MaxValue;
					int num10 = 0;
					int num11 = 0;
					int num12 = 0;
					for (int j = 0; j < 4; j++)
					{
						if (num9 > array6[j])
						{
							num9 = array6[j];
							num11 = j;
						}
						if (num10 < array5[j])
						{
							num10 = array5[j];
							num12 = j;
						}
					}
					if (num9 >= num10)
					{
						break;
					}
					array[num12] = array[num11] ^ 4;
					array4[num12] = array4[num11 + 4];
					array3[num12] = array3[num11 + 4];
					array5[num12] = 0;
					array6[num11] = int.MaxValue;
					Buffer.BlockCopy(array2[num11], 0, array2[num12], 0, order);
				}
				for (int j = 0; j < 4; j++)
				{
					sbyte b2 = (sbyte)Inlines.silk_RSHIFT(array[j], 2);
					array2[j][i] += b2;
				}
			}
			i--;
		}
		a2 = 0;
		int num13 = int.MaxValue;
		for (int j = 0; j < 8; j++)
		{
			if (num13 > array4[j])
			{
				num13 = array4[j];
				a2 = j;
			}
		}
		for (int j = 0; j < order; j++)
		{
			indices[j] = array2[a2 & 3][j];
		}
		indices[0] = (sbyte)(indices[0] + Inlines.silk_RSHIFT(a2, 2));
		return num13;
	}

	internal static int silk_NLSF_encode(sbyte[] NLSFIndices, short[] pNLSF_Q15, NLSFCodebook psNLSF_CB, short[] pW_QW, int NLSF_mu_Q20, int nSurvivors, int signalType)
	{
		short[] array = new short[psNLSF_CB.order];
		short[] array2 = new short[psNLSF_CB.order];
		short[] array3 = new short[psNLSF_CB.order];
		short[] array4 = new short[psNLSF_CB.order];
		short[] array5 = new short[psNLSF_CB.order];
		byte[] array6 = new byte[psNLSF_CB.order];
		short[] ec_ix = new short[psNLSF_CB.order];
		byte[] cB1_NLSF_Q = psNLSF_CB.CB1_NLSF_Q8;
		silk_NLSF_stabilize(pNLSF_Q15, psNLSF_CB.deltaMin_Q15, psNLSF_CB.order);
		int[] array7 = new int[psNLSF_CB.nVectors];
		silk_NLSF_VQ(array7, pNLSF_Q15, psNLSF_CB.CB1_NLSF_Q8, psNLSF_CB.nVectors, psNLSF_CB.order);
		int[] array8 = new int[nSurvivors];
		Sort.silk_insertion_sort_increasing(array7, array8, psNLSF_CB.nVectors, nSurvivors);
		int[] array9 = new int[nSurvivors];
		sbyte[][] array10 = Arrays.InitTwoDimensionalArray<sbyte>(nSurvivors, 16);
		for (int i = 0; i < nSurvivors; i++)
		{
			int num = array8[i];
			int num2 = num * psNLSF_CB.order;
			for (int j = 0; j < psNLSF_CB.order; j++)
			{
				array3[j] = Inlines.silk_LSHIFT16(cB1_NLSF_Q[num2 + j], 7);
				array[j] = (short)(pNLSF_Q15[j] - array3[j]);
			}
			silk_NLSF_VQ_weights_laroia(array4, array3, psNLSF_CB.order);
			for (int j = 0; j < psNLSF_CB.order; j++)
			{
				int b = Inlines.silk_SQRT_APPROX(Inlines.silk_LSHIFT(array4[j], 16));
				array2[j] = (short)Inlines.silk_RSHIFT(Inlines.silk_SMULBB(array[j], b), 14);
			}
			for (int j = 0; j < psNLSF_CB.order; j++)
			{
				array5[j] = (short)Inlines.silk_DIV32_16(Inlines.silk_LSHIFT(pW_QW[j], 5), array4[j]);
			}
			silk_NLSF_unpack(ec_ix, array6, psNLSF_CB, num);
			array9[i] = silk_NLSF_del_dec_quant(array10[i], array2, array5, array6, ec_ix, psNLSF_CB.ec_Rates_Q5, psNLSF_CB.quantStepSize_Q16, psNLSF_CB.invQuantStepSize_Q6, NLSF_mu_Q20, psNLSF_CB.order);
			int num3 = (signalType >> 1) * psNLSF_CB.nVectors;
			int inLin = ((num != 0) ? (psNLSF_CB.CB1_iCDF[num3 + num - 1] - psNLSF_CB.CB1_iCDF[num3 + num]) : (256 - psNLSF_CB.CB1_iCDF[num3 + num]));
			int b2 = 1024 - Inlines.silk_lin2log(inLin);
			array9[i] = Inlines.silk_SMLABB(array9[i], b2, Inlines.silk_RSHIFT(NLSF_mu_Q20, 2));
		}
		int[] array11 = new int[1];
		Sort.silk_insertion_sort_increasing(array9, array11, nSurvivors, 1);
		NLSFIndices[0] = (sbyte)array8[array11[0]];
		Array.Copy(array10[array11[0]], 0, NLSFIndices, 1, psNLSF_CB.order);
		silk_NLSF_decode(pNLSF_Q15, NLSFIndices, psNLSF_CB);
		return array9[0];
	}

	internal static void silk_NLSF2A_find_poly(int[] o, int[] cLSF, int cLSF_ptr, int dd)
	{
		o[0] = Inlines.silk_LSHIFT(1, 16);
		o[1] = -cLSF[cLSF_ptr];
		for (int i = 1; i < dd; i++)
		{
			int num = cLSF[cLSF_ptr + 2 * i];
			o[i + 1] = Inlines.silk_LSHIFT(o[i - 1], 1) - (int)Inlines.silk_RSHIFT_ROUND64(Inlines.silk_SMULL(num, o[i]), 16);
			for (int num2 = i; num2 > 1; num2--)
			{
				o[num2] += o[num2 - 2] - (int)Inlines.silk_RSHIFT_ROUND64(Inlines.silk_SMULL(num, o[num2 - 1]), 16);
			}
			o[1] -= num;
		}
	}

	internal static void silk_NLSF2A(short[] a_Q12, short[] NLSF, int d)
	{
		int[] array = new int[d];
		int[] array2 = new int[d / 2 + 1];
		int[] array3 = new int[d / 2 + 1];
		int[] array4 = new int[d];
		int num = 0;
		byte[] array5 = ((d == 16) ? ordering16 : ordering10);
		for (int i = 0; i < d; i++)
		{
			int num2 = Inlines.silk_RSHIFT(NLSF[i], 8);
			int b = NLSF[i] - Inlines.silk_LSHIFT(num2, 8);
			int num3 = Tables.silk_LSFCosTab_Q12[num2];
			int a = Tables.silk_LSFCosTab_Q12[num2 + 1] - num3;
			array[array5[i]] = Inlines.silk_RSHIFT_ROUND(Inlines.silk_LSHIFT(num3, 8) + Inlines.silk_MUL(a, b), 4);
		}
		int num4 = Inlines.silk_RSHIFT(d, 1);
		silk_NLSF2A_find_poly(array2, array, 0, num4);
		silk_NLSF2A_find_poly(array3, array, 1, num4);
		for (int i = 0; i < num4; i++)
		{
			int num5 = array2[i + 1] + array2[i];
			int num6 = array3[i + 1] - array3[i];
			array4[i] = -num6 - num5;
			array4[d - i - 1] = num6 - num5;
		}
		int j;
		for (j = 0; j < 10; j++)
		{
			int num7 = 0;
			for (int i = 0; i < d; i++)
			{
				int num8 = Inlines.silk_abs(array4[i]);
				if (num8 > num7)
				{
					num7 = num8;
					num = i;
				}
			}
			num7 = Inlines.silk_RSHIFT_ROUND(num7, 5);
			if (num7 <= 32767)
			{
				break;
			}
			num7 = Inlines.silk_min(num7, 163838);
			int chirp_Q = 65470 - Inlines.silk_DIV32(Inlines.silk_LSHIFT(num7 - 32767, 14), Inlines.silk_RSHIFT32(Inlines.silk_MUL(num7, num + 1), 2));
			Filters.silk_bwexpander_32(array4, d, chirp_Q);
		}
		if (j == 10)
		{
			for (int i = 0; i < d; i++)
			{
				a_Q12[i] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(array4[i], 5));
				array4[i] = Inlines.silk_LSHIFT(a_Q12[i], 5);
			}
		}
		else
		{
			for (int i = 0; i < d; i++)
			{
				a_Q12[i] = (short)Inlines.silk_RSHIFT_ROUND(array4[i], 5);
			}
		}
		for (j = 0; j < 16; j++)
		{
			if (Filters.silk_LPC_inverse_pred_gain(a_Q12, d) >= 107374)
			{
				break;
			}
			Filters.silk_bwexpander_32(array4, d, 65536 - Inlines.silk_LSHIFT(2, j));
			for (int i = 0; i < d; i++)
			{
				a_Q12[i] = (short)Inlines.silk_RSHIFT_ROUND(array4[i], 5);
			}
		}
	}

	internal static void silk_A2NLSF_trans_poly(int[] p, int dd)
	{
		for (int i = 2; i <= dd; i++)
		{
			for (int num = dd; num > i; num--)
			{
				p[num - 2] -= p[num];
			}
			p[i - 2] -= Inlines.silk_LSHIFT(p[i], 1);
		}
	}

	internal static int silk_A2NLSF_eval_poly(int[] p, int x, int dd)
	{
		int num = p[dd];
		int c = Inlines.silk_LSHIFT(x, 4);
		if (8 == dd)
		{
			num = Inlines.silk_SMLAWW(p[7], num, c);
			num = Inlines.silk_SMLAWW(p[6], num, c);
			num = Inlines.silk_SMLAWW(p[5], num, c);
			num = Inlines.silk_SMLAWW(p[4], num, c);
			num = Inlines.silk_SMLAWW(p[3], num, c);
			num = Inlines.silk_SMLAWW(p[2], num, c);
			num = Inlines.silk_SMLAWW(p[1], num, c);
			num = Inlines.silk_SMLAWW(p[0], num, c);
		}
		else
		{
			for (int num2 = dd - 1; num2 >= 0; num2--)
			{
				num = Inlines.silk_SMLAWW(p[num2], num, c);
			}
		}
		return num;
	}

	internal static void silk_A2NLSF_init(int[] a_Q16, int[] P, int[] Q, int dd)
	{
		P[dd] = Inlines.silk_LSHIFT(1, 16);
		Q[dd] = Inlines.silk_LSHIFT(1, 16);
		for (int i = 0; i < dd; i++)
		{
			P[i] = -a_Q16[dd - i - 1] - a_Q16[dd + i];
			Q[i] = -a_Q16[dd - i - 1] + a_Q16[dd + i];
		}
		for (int i = dd; i > 0; i--)
		{
			P[i - 1] -= P[i];
			Q[i - 1] += Q[i];
		}
		silk_A2NLSF_trans_poly(P, dd);
		silk_A2NLSF_trans_poly(Q, dd);
	}

	internal static void silk_A2NLSF(short[] NLSF, int[] a_Q16, int d)
	{
		int[] array = new int[9];
		int[] array2 = new int[9];
		int[][] array3 = new int[2][] { array, array2 };
		int dd = Inlines.silk_RSHIFT(d, 1);
		silk_A2NLSF_init(a_Q16, array, array2, dd);
		int[] p = array;
		int num = Tables.silk_LSFCosTab_Q12[0];
		int num2 = silk_A2NLSF_eval_poly(p, num, dd);
		int num3;
		if (num2 < 0)
		{
			NLSF[0] = 0;
			p = array2;
			num2 = silk_A2NLSF_eval_poly(p, num, dd);
			num3 = 1;
		}
		else
		{
			num3 = 0;
		}
		int num4 = 1;
		int num5 = 0;
		int num6 = 0;
		while (true)
		{
			int num7 = Tables.silk_LSFCosTab_Q12[num4];
			int num8 = silk_A2NLSF_eval_poly(p, num7, dd);
			if ((num2 <= 0 && num8 >= num6) || (num2 >= 0 && num8 <= -num6))
			{
				num6 = ((num8 == 0) ? 1 : 0);
				int num9 = -256;
				for (int i = 0; i < 3; i++)
				{
					int num10 = Inlines.silk_RSHIFT_ROUND(num + num7, 1);
					int num11 = silk_A2NLSF_eval_poly(p, num10, dd);
					if ((num2 <= 0 && num11 >= 0) || (num2 >= 0 && num11 <= 0))
					{
						num7 = num10;
						num8 = num11;
					}
					else
					{
						num = num10;
						num2 = num11;
						num9 = Inlines.silk_ADD_RSHIFT(num9, 128, i);
					}
				}
				if (Inlines.silk_abs(num2) < 65536)
				{
					int num12 = num2 - num8;
					int a = Inlines.silk_LSHIFT(num2, 5) + Inlines.silk_RSHIFT(num12, 1);
					if (num12 != 0)
					{
						num9 += Inlines.silk_DIV32(a, num12);
					}
				}
				else
				{
					num9 += Inlines.silk_DIV32(num2, Inlines.silk_RSHIFT(num2 - num8, 5));
				}
				NLSF[num3] = (short)Inlines.silk_min_32(Inlines.silk_LSHIFT(num4, 8) + num9, 32767);
				num3++;
				if (num3 < d)
				{
					p = array3[num3 & 1];
					num = Tables.silk_LSFCosTab_Q12[num4 - 1];
					num2 = Inlines.silk_LSHIFT(1 - (num3 & 2), 12);
					continue;
				}
				break;
			}
			num4++;
			num = num7;
			num2 = num8;
			num6 = 0;
			if (num4 <= 128)
			{
				continue;
			}
			num5++;
			if (num5 > 30)
			{
				NLSF[0] = (short)Inlines.silk_DIV32_16(32768, (short)(d + 1));
				for (num4 = 1; num4 < d; num4++)
				{
					NLSF[num4] = (short)Inlines.silk_SMULBB(num4 + 1, NLSF[0]);
				}
				break;
			}
			Filters.silk_bwexpander_32(a_Q16, d, 65536 - Inlines.silk_SMULBB(10 + num5, num5));
			silk_A2NLSF_init(a_Q16, array, array2, dd);
			p = array;
			num = Tables.silk_LSFCosTab_Q12[0];
			num2 = silk_A2NLSF_eval_poly(p, num, dd);
			if (num2 < 0)
			{
				NLSF[0] = 0;
				p = array2;
				num2 = silk_A2NLSF_eval_poly(p, num, dd);
				num3 = 1;
			}
			else
			{
				num3 = 0;
			}
			num4 = 1;
		}
	}

	internal static void silk_process_NLSFs(SilkChannelEncoder psEncC, short[][] PredCoef_Q12, short[] pNLSF_Q15, short[] prev_NLSFq_Q15)
	{
		short[] array = new short[16];
		short[] array2 = new short[16];
		short[] array3 = new short[16];
		int num = Inlines.silk_SMLAWB(3146, -268434, psEncC.speech_activity_Q8);
		if (psEncC.nb_subfr == 2)
		{
			num = Inlines.silk_ADD_RSHIFT(num, num, 1);
		}
		silk_NLSF_VQ_weights_laroia(array2, pNLSF_Q15, psEncC.predictLPCOrder);
		bool flag = psEncC.useInterpolatedNLSFs == 1 && psEncC.indices.NLSFInterpCoef_Q2 < 4;
		if (flag)
		{
			Inlines.silk_interpolate(array, prev_NLSFq_Q15, pNLSF_Q15, psEncC.indices.NLSFInterpCoef_Q2, psEncC.predictLPCOrder);
			silk_NLSF_VQ_weights_laroia(array3, array, psEncC.predictLPCOrder);
			int c = Inlines.silk_LSHIFT(Inlines.silk_SMULBB(psEncC.indices.NLSFInterpCoef_Q2, psEncC.indices.NLSFInterpCoef_Q2), 11);
			for (int i = 0; i < psEncC.predictLPCOrder; i++)
			{
				array2[i] = (short)Inlines.silk_SMLAWB(Inlines.silk_RSHIFT(array2[i], 1), array3[i], c);
			}
		}
		silk_NLSF_encode(psEncC.indices.NLSFIndices, pNLSF_Q15, psEncC.psNLSF_CB, array2, num, psEncC.NLSF_MSVQ_Survivors, psEncC.indices.signalType);
		silk_NLSF2A(PredCoef_Q12[1], pNLSF_Q15, psEncC.predictLPCOrder);
		if (flag)
		{
			Inlines.silk_interpolate(array, prev_NLSFq_Q15, pNLSF_Q15, psEncC.indices.NLSFInterpCoef_Q2, psEncC.predictLPCOrder);
			silk_NLSF2A(PredCoef_Q12[0], array, psEncC.predictLPCOrder);
		}
		else
		{
			Array.Copy(PredCoef_Q12[1], 0, PredCoef_Q12[0], 0, psEncC.predictLPCOrder);
		}
	}
}

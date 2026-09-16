namespace Concentus.Celt;

internal static class CeltConstants
{
	public const int Q15ONE = 32767;

	public const float CELT_SIG_SCALE = 32768f;

	public const int SIG_SHIFT = 12;

	public const int NORM_SCALING = 16384;

	public const int DB_SHIFT = 10;

	public const int EPSILON = 1;

	public const int VERY_SMALL = 0;

	public const short VERY_LARGE16 = short.MaxValue;

	public const short Q15_ONE = short.MaxValue;

	public const int COMBFILTER_MAXPERIOD = 1024;

	public const int COMBFILTER_MINPERIOD = 15;

	public const int DECODE_BUFFER_SIZE = 2048;

	public const int BITALLOC_SIZE = 11;

	public const int MAX_PERIOD = 1024;

	public const int TOTAL_MODES = 1;

	public const int MAX_PSEUDO = 40;

	public const int LOG_MAX_PSEUDO = 6;

	public const int CELT_MAX_PULSES = 128;

	public const int MAX_FINE_BITS = 8;

	public const int FINE_OFFSET = 21;

	public const int QTHETA_OFFSET = 4;

	public const int QTHETA_OFFSET_TWOPHASE = 16;

	public const int PLC_PITCH_LAG_MAX = 720;

	public const int PLC_PITCH_LAG_MIN = 100;

	public const int LPC_ORDER = 24;
}

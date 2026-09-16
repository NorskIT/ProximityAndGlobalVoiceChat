using System;

namespace ProximityVoiceChat.Voice.Audio;

internal sealed class JitterEstimator
{
	private const int OffsetWindow = 100;

	private const float TimeWindow = 0.25f;

	private const int Capacity = 64;

	private readonly double[] _offsets = new double[100];

	private int _offsetCount;

	private int _offsetHead;

	private double _offsetSum;

	private readonly double[] _deviation = new double[64];

	private readonly double[] _arrived = new double[64];

	private int _count;

	private int _head;

	internal float JitterSeconds { get; private set; }

	internal void Reset()
	{
		_offsetCount = 0;
		_offsetHead = 0;
		_offsetSum = 0.0;
		_count = 0;
		_head = 0;
		JitterSeconds = 0f;
	}

	internal void Sample(double remoteSeconds, double localSeconds)
	{
		double num = localSeconds - remoteSeconds;
		if (_offsetCount == 100)
		{
			_offsetSum -= _offsets[_offsetHead];
			_offsets[_offsetHead] = num;
			_offsetHead = (_offsetHead + 1) % 100;
		}
		else
		{
			_offsets[(_offsetHead + _offsetCount) % 100] = num;
			_offsetCount++;
		}
		_offsetSum += num;
		double num2 = _offsetSum / (double)_offsetCount;
		int num3 = (_head + _count) % 64;
		if (_count == 64)
		{
			_head = (_head + 1) % 64;
			num3 = (_head + _count - 1) % 64;
		}
		else
		{
			_count++;
		}
		_deviation[num3] = num - num2;
		_arrived[num3] = localSeconds;
		double num4 = 0.0;
		int num5 = 0;
		for (int i = 0; i < _count; i++)
		{
			int num6 = (_head + i) % 64;
			if (!(localSeconds - _arrived[num6] > 0.25))
			{
				num4 += _deviation[num6] * _deviation[num6];
				num5++;
			}
		}
		JitterSeconds = ((num5 > 0) ? ((float)Math.Sqrt(num4 / (double)num5)) : 0f);
	}
}

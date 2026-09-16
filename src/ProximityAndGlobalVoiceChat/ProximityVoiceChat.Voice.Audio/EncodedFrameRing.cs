using System.Threading;

namespace ProximityVoiceChat.Voice.Audio;

internal sealed class EncodedFrameRing
{
	private readonly EncodedFrame[] _slots;

	private readonly int _capacity;

	private int _write;

	private int _read;

	internal int Overruns;

	internal EncodedFrameRing(int capacity)
	{
		_capacity = capacity;
		_slots = new EncodedFrame[capacity];
		for (int i = 0; i < capacity; i++)
		{
			_slots[i] = new EncodedFrame();
		}
	}

	internal EncodedFrame? BeginWrite()
	{
		int write = _write;
		if ((write + 1) % _capacity == Volatile.Read(ref _read))
		{
			Overruns++;
			return null;
		}
		return _slots[write];
	}

	internal void CommitWrite()
	{
		Volatile.Write(ref _write, (_write + 1) % _capacity);
	}

	internal EncodedFrame? BeginRead()
	{
		int read = _read;
		if (read == Volatile.Read(ref _write))
		{
			return null;
		}
		return _slots[read];
	}

	internal void CommitRead()
	{
		Volatile.Write(ref _read, (_read + 1) % _capacity);
	}

	internal void Clear()
	{
		Volatile.Write(ref _read, 0);
		Volatile.Write(ref _write, 0);
	}
}

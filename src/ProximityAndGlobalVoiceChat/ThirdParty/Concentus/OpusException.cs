using System;

namespace Concentus;

internal class OpusException : Exception
{
	public OpusException()
	{
	}

	public OpusException(string message)
		: base(message)
	{
	}

	public OpusException(string message, int opus_error_code)
		: base(message + ": " + CodecHelpers.opus_strerror(opus_error_code))
	{
	}
}

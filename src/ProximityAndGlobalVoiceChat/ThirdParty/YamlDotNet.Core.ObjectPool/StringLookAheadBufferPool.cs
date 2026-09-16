using System;

namespace YamlDotNet.Core.ObjectPool;

internal static class StringLookAheadBufferPool
{
	internal readonly struct BufferWrapper(StringLookAheadBuffer buffer, ObjectPool<StringLookAheadBuffer> pool) : IDisposable
	{
		public readonly StringLookAheadBuffer Buffer = buffer;

		private readonly ObjectPool<StringLookAheadBuffer> pool = pool;

		public override string ToString()
		{
			return Buffer.ToString();
		}

		public void Dispose()
		{
			pool.Return(Buffer);
		}
	}

	private static readonly ObjectPool<StringLookAheadBuffer> Pool = ObjectPool.Create(new DefaultPooledObjectPolicy<StringLookAheadBuffer>());

	public static BufferWrapper Rent(string value)
	{
		StringLookAheadBuffer stringLookAheadBuffer = Pool.Get();
		stringLookAheadBuffer.Value = value;
		return new BufferWrapper(stringLookAheadBuffer, Pool);
	}
}

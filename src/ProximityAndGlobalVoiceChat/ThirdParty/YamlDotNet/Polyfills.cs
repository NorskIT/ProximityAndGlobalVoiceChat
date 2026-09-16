using System.Runtime.CompilerServices;

namespace YamlDotNet;

internal static class Polyfills
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool Contains(this string source, char c)
	{
		return source.IndexOf(c) != -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool EndsWith(this string source, char c)
	{
		if (source.Length > 0)
		{
			return source[source.Length - 1] == c;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool StartsWith(this string source, char c)
	{
		if (source.Length > 0)
		{
			return source[0] == c;
		}
		return false;
	}
}

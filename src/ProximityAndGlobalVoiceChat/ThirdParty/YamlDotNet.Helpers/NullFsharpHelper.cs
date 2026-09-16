using System;
using YamlDotNet.Serialization;

namespace YamlDotNet.Helpers;

internal class NullFsharpHelper : IFsharpHelper
{
	public object? CreateFsharpListFromArray(Type t, Type itemsType, Array arr)
	{
		return null;
	}

	public Type? GetOptionUnderlyingType(Type t)
	{
		return null;
	}

	public object? GetValue(IObjectDescriptor objectDescriptor)
	{
		return null;
	}

	public bool IsFsharpListType(Type t)
	{
		return false;
	}

	public bool IsOptionType(Type t)
	{
		return false;
	}
}

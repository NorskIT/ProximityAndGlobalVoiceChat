using System;
using YamlDotNet.Serialization;

namespace YamlDotNet.Helpers;

internal class DefaultFsharpHelper : IFsharpHelper
{
	private static bool IsFsharpCore(Type t)
	{
		return t.Namespace == "Microsoft.FSharp.Core";
	}

	public bool IsOptionType(Type t)
	{
		if (IsFsharpCore(t))
		{
			return t.Name == "FSharpOption`1";
		}
		return false;
	}

	public Type? GetOptionUnderlyingType(Type t)
	{
		if (!t.IsGenericType || !IsOptionType(t))
		{
			return null;
		}
		return t.GenericTypeArguments[0];
	}

	public object? GetValue(IObjectDescriptor objectDescriptor)
	{
		if (!IsOptionType(objectDescriptor.Type))
		{
			throw new InvalidOperationException("Should not be called on non-Option<> type");
		}
		if (objectDescriptor.Value == null)
		{
			return null;
		}
		return objectDescriptor.Type.GetProperty("Value").GetValue(objectDescriptor.Value);
	}

	public bool IsFsharpListType(Type t)
	{
		if (t.Namespace == "Microsoft.FSharp.Collections")
		{
			return t.Name == "FSharpList`1";
		}
		return false;
	}

	public object? CreateFsharpListFromArray(Type t, Type itemsType, Array arr)
	{
		if (!IsFsharpListType(t))
		{
			return null;
		}
		return t.Assembly.GetType("Microsoft.FSharp.Collections.ListModule").GetMethod("OfArray").MakeGenericMethod(itemsType)
			.Invoke(null, new object[1] { arr });
	}
}

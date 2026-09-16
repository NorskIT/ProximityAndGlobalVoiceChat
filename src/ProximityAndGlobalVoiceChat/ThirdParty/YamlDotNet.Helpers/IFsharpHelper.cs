using System;
using YamlDotNet.Serialization;

namespace YamlDotNet.Helpers;

internal interface IFsharpHelper
{
	bool IsOptionType(Type t);

	Type? GetOptionUnderlyingType(Type t);

	object? GetValue(IObjectDescriptor objectDescriptor);

	bool IsFsharpListType(Type t);

	object? CreateFsharpListFromArray(Type t, Type itemsType, Array arr);
}

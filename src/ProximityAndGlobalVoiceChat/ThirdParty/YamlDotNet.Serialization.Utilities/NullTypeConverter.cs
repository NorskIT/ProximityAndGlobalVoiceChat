using System;

namespace YamlDotNet.Serialization.Utilities;

internal class NullTypeConverter : ITypeConverter
{
	public object? ChangeType(object? value, Type expectedType, INamingConvention enumNamingConvention, ITypeInspector typeInspector)
	{
		return value;
	}
}

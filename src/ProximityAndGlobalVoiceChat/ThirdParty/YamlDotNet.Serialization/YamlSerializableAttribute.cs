using System;

namespace YamlDotNet.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = true)]
internal sealed class YamlSerializableAttribute : Attribute
{
	public YamlSerializableAttribute()
	{
	}

	public YamlSerializableAttribute(Type serializableType)
	{
	}
}

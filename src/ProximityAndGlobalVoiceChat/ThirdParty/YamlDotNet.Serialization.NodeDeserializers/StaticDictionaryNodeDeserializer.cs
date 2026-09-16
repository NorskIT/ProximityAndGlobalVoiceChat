using System;
using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Serialization.ObjectFactories;

namespace YamlDotNet.Serialization.NodeDeserializers;

internal class StaticDictionaryNodeDeserializer : DictionaryDeserializer, INodeDeserializer
{
	private readonly StaticObjectFactory objectFactory;

	public StaticDictionaryNodeDeserializer(StaticObjectFactory objectFactory, bool duplicateKeyChecking)
		: base(duplicateKeyChecking)
	{
		this.objectFactory = objectFactory ?? throw new ArgumentNullException("objectFactory");
	}

	public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
	{
		if (objectFactory.IsDictionary(expectedType))
		{
			if (!(objectFactory.Create(expectedType) is IDictionary dictionary))
			{
				value = null;
				return false;
			}
			Type keyType = objectFactory.GetKeyType(expectedType);
			Type valueType = objectFactory.GetValueType(expectedType);
			value = dictionary;
			base.Deserialize(keyType, valueType, reader, nestedObjectDeserializer, dictionary, rootDeserializer);
			return true;
		}
		value = null;
		return false;
	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using YamlDotNet.Helpers;

namespace YamlDotNet.Serialization.Utilities;

internal sealed class TypeConverterCache
{
	private readonly IYamlTypeConverter[] typeConverters;

	private readonly ConcurrentDictionary<Type, (bool HasMatch, IYamlTypeConverter? TypeConverter)> cache = new ConcurrentDictionary<Type, (bool, IYamlTypeConverter)>();

	public TypeConverterCache(IEnumerable<IYamlTypeConverter>? typeConverters)
		: this(typeConverters?.ToArray() ?? Array.Empty<IYamlTypeConverter>())
	{
	}

	public TypeConverterCache(IYamlTypeConverter[] typeConverters)
	{
		this.typeConverters = typeConverters;
	}

	public bool TryGetConverterForType(Type type, [NotNullWhen(true)] out IYamlTypeConverter? typeConverter)
	{
		(bool, IYamlTypeConverter) orAdd = DictionaryExtensions.GetOrAdd(cache, type, (Type t, IYamlTypeConverter[] tc) => LookupTypeConverter(t, tc), typeConverters);
		typeConverter = orAdd.Item2;
		return orAdd.Item1;
	}

	public IYamlTypeConverter GetConverterByType(Type converter)
	{
		IYamlTypeConverter[] array = typeConverters;
		foreach (IYamlTypeConverter yamlTypeConverter in array)
		{
			if (yamlTypeConverter.GetType() == converter)
			{
				return yamlTypeConverter;
			}
		}
		throw new ArgumentException("IYamlTypeConverter of type " + converter.FullName + " not found", "converter");
	}

	private static (bool HasMatch, IYamlTypeConverter? TypeConverter) LookupTypeConverter(Type type, IYamlTypeConverter[] typeConverters)
	{
		foreach (IYamlTypeConverter yamlTypeConverter in typeConverters)
		{
			if (yamlTypeConverter.Accepts(type))
			{
				return (HasMatch: true, TypeConverter: yamlTypeConverter);
			}
		}
		return (HasMatch: false, TypeConverter: null);
	}
}

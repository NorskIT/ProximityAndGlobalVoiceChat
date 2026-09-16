using System;
using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.NodeDeserializers;

internal abstract class CollectionDeserializer
{
	protected static void DeserializeHelper(Type tItem, IParser parser, Func<IParser, Type, object?> nestedObjectDeserializer, IList result, bool canUpdate, IObjectFactory objectFactory)
	{
		parser.Consume<SequenceStart>();
		SequenceEnd @event;
		while (!parser.TryConsume<SequenceEnd>(out @event))
		{
			ParsingEvent current = parser.Current;
			object obj = nestedObjectDeserializer(parser, tItem);
			if (obj is IValuePromise valuePromise)
			{
				if (!canUpdate)
				{
					throw new ForwardAnchorNotSupportedException(current?.Start ?? Mark.Empty, current?.End ?? Mark.Empty, "Forward alias references are not allowed because this type does not implement IList<>");
				}
				int index = result.Add(objectFactory.CreatePrimitive(tItem));
				valuePromise.ValueAvailable += delegate(object? v)
				{
					result[index] = v;
				};
			}
			else
			{
				result.Add(obj);
			}
		}
	}
}

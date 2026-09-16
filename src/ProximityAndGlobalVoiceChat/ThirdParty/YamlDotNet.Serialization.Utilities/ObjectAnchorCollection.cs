using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;

namespace YamlDotNet.Serialization.Utilities;

internal sealed class ObjectAnchorCollection
{
	private readonly Dictionary<string, object> objectsByAnchor = new Dictionary<string, object>();

	private readonly Dictionary<object, string> anchorsByObject = new Dictionary<object, string>();

	public object this[string anchor]
	{
		get
		{
			if (objectsByAnchor.TryGetValue(anchor, out object value))
			{
				return value;
			}
			throw new AnchorNotFoundException("The anchor '" + anchor + "' does not exists");
		}
	}

	public void Add(string anchor, object @object)
	{
		objectsByAnchor.Add(anchor, @object);
		if (@object != null)
		{
			anchorsByObject.Add(@object, anchor);
		}
	}

	public bool TryGetAnchor(object @object, [MaybeNullWhen(false)] out string? anchor)
	{
		return anchorsByObject.TryGetValue(@object, out anchor);
	}
}

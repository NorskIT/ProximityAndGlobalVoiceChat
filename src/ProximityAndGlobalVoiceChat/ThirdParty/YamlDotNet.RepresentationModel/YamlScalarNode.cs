using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Schemas;

namespace YamlDotNet.RepresentationModel;

[DebuggerDisplay("{Value}")]
internal sealed class YamlScalarNode : YamlNode, IYamlConvertible
{
	private bool forceImplicitPlain;

	private string? value;

	public string? Value
	{
		get
		{
			return value;
		}
		set
		{
			if (value == null)
			{
				forceImplicitPlain = true;
			}
			else
			{
				forceImplicitPlain = false;
			}
			this.value = value;
		}
	}

	public ScalarStyle Style { get; set; }

	public override YamlNodeType NodeType => YamlNodeType.Scalar;

	internal YamlScalarNode(IParser parser, DocumentLoadingState state)
	{
		Load(parser, state);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Load(IParser parser, DocumentLoadingState state)
	{
		Scalar scalar = parser.Consume<Scalar>();
		Load(scalar, state);
		string text = scalar.Value;
		if (scalar.Style == ScalarStyle.Plain && base.Tag.IsEmpty)
		{
			forceImplicitPlain = text.Length switch
			{
				0 => true, 
				1 => text == "~", 
				4 => text == "null" || text == "Null" || text == "NULL", 
				_ => false, 
			};
		}
		value = text;
		Style = scalar.Style;
	}

	public YamlScalarNode()
	{
	}

	public YamlScalarNode(string? value)
	{
		Value = value;
	}

	internal override void ResolveAliases(DocumentLoadingState state)
	{
		throw new NotSupportedException("Resolving an alias on a scalar node does not make sense");
	}

	internal override void Emit(IEmitter emitter, EmitterState state)
	{
		TagName tag = base.Tag;
		bool isPlainImplicit = tag.IsEmpty;
		if (forceImplicitPlain && Style == ScalarStyle.Plain && (Value == null || Value == ""))
		{
			tag = JsonSchema.Tags.Null;
			isPlainImplicit = true;
		}
		else if (tag.IsEmpty && Value == null && (Style == ScalarStyle.Plain || Style == ScalarStyle.Any))
		{
			tag = JsonSchema.Tags.Null;
			isPlainImplicit = true;
		}
		emitter.Emit(new Scalar(base.Anchor, tag, Value ?? string.Empty, Style, isPlainImplicit, isQuotedImplicit: false));
	}

	public override void Accept(IYamlVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override bool Equals(object? obj)
	{
		if (obj is YamlScalarNode yamlScalarNode && object.Equals(base.Tag, yamlScalarNode.Tag))
		{
			return object.Equals(Value, yamlScalarNode.Value);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return YamlDotNet.Core.HashCode.CombineHashCodes(base.Tag.GetHashCode(), Value);
	}

	public static explicit operator string?(YamlScalarNode value)
	{
		return value.Value;
	}

	internal override string ToString(RecursionLevel level)
	{
		return Value ?? string.Empty;
	}

	internal override IEnumerable<YamlNode> SafeAllNodes(RecursionLevel level)
	{
		yield return this;
	}

	void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
	{
		Load(parser, new DocumentLoadingState());
	}

	void IYamlConvertible.Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
	{
		Emit(emitter, new EmitterState());
	}
}

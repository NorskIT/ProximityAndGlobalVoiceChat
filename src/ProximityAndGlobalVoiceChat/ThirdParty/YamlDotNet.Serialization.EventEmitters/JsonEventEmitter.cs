using System;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.EventEmitters;

internal sealed class JsonEventEmitter : ChainedEventEmitter
{
	private readonly YamlFormatter formatter;

	private readonly INamingConvention enumNamingConvention;

	private readonly ITypeInspector typeInspector;

	public JsonEventEmitter(IEventEmitter nextEmitter, YamlFormatter formatter, INamingConvention enumNamingConvention, ITypeInspector typeInspector)
		: base(nextEmitter)
	{
		this.formatter = formatter;
		this.enumNamingConvention = enumNamingConvention;
		this.typeInspector = typeInspector;
	}

	public override void Emit(AliasEventInfo eventInfo, IEmitter emitter)
	{
		eventInfo.NeedsExpansion = true;
	}

	public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
	{
		eventInfo.IsPlainImplicit = true;
		eventInfo.Style = ScalarStyle.Plain;
		object value = eventInfo.Source.Value;
		if (value == null)
		{
			eventInfo.RenderedValue = "null";
		}
		else
		{
			TypeCode typeCode = eventInfo.Source.Type.GetTypeCode();
			switch (typeCode)
			{
			case TypeCode.Boolean:
				eventInfo.RenderedValue = formatter.FormatBoolean(value);
				break;
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
				if (eventInfo.Source.Type.IsEnum())
				{
					eventInfo.RenderedValue = formatter.FormatEnum(value, typeInspector, enumNamingConvention);
					eventInfo.Style = ((!formatter.PotentiallyQuoteEnums(value)) ? ScalarStyle.Plain : ScalarStyle.DoubleQuoted);
				}
				else
				{
					eventInfo.RenderedValue = formatter.FormatNumber(value);
				}
				break;
			case TypeCode.Single:
			{
				float f = (float)value;
				eventInfo.RenderedValue = f.ToString("G", CultureInfo.InvariantCulture);
				if (float.IsNaN(f) || float.IsInfinity(f))
				{
					eventInfo.Style = ScalarStyle.DoubleQuoted;
				}
				break;
			}
			case TypeCode.Double:
			{
				double d = (double)value;
				eventInfo.RenderedValue = d.ToString("G", CultureInfo.InvariantCulture);
				if (double.IsNaN(d) || double.IsInfinity(d))
				{
					eventInfo.Style = ScalarStyle.DoubleQuoted;
				}
				break;
			}
			case TypeCode.Decimal:
				eventInfo.RenderedValue = ((decimal)value).ToString(CultureInfo.InvariantCulture);
				break;
			case TypeCode.Char:
			case TypeCode.String:
				eventInfo.RenderedValue = value.ToString();
				eventInfo.Style = ScalarStyle.DoubleQuoted;
				break;
			case TypeCode.DateTime:
				eventInfo.RenderedValue = formatter.FormatDateTime(value);
				break;
			case TypeCode.Empty:
				eventInfo.RenderedValue = "null";
				break;
			default:
				if (eventInfo.Source.Type == typeof(TimeSpan))
				{
					eventInfo.RenderedValue = formatter.FormatTimeSpan(value);
					break;
				}
				throw new NotSupportedException($"TypeCode.{typeCode} is not supported.");
			}
		}
		base.Emit(eventInfo, emitter);
	}

	public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
	{
		eventInfo.Style = MappingStyle.Flow;
		base.Emit(eventInfo, emitter);
	}

	public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
	{
		eventInfo.Style = SequenceStyle.Flow;
		base.Emit(eventInfo, emitter);
	}
}

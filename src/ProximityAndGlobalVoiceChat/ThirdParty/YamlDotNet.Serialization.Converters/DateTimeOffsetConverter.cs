using System;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.Converters;

internal class DateTimeOffsetConverter : IYamlTypeConverter
{
	private readonly IFormatProvider provider;

	private readonly ScalarStyle style;

	private readonly DateTimeStyles dateStyle;

	private readonly string[] formats;

	public DateTimeOffsetConverter(IFormatProvider? provider = null, ScalarStyle style = ScalarStyle.Any, DateTimeStyles dateStyle = DateTimeStyles.None, params string[] formats)
	{
		this.provider = provider ?? CultureInfo.InvariantCulture;
		this.style = style;
		this.dateStyle = dateStyle;
		this.formats = formats.DefaultIfEmpty<string>("O").ToArray();
	}

	public bool Accepts(Type type)
	{
		return type == typeof(DateTimeOffset);
	}

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		string value = parser.Consume<Scalar>().Value;
		DateTimeOffset dateTimeOffset = DateTimeOffset.ParseExact(value, formats, provider, dateStyle);
		return dateTimeOffset;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		string value2 = ((DateTimeOffset)value).ToString(formats.First(), provider);
		emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, value2, style, isPlainImplicit: true, isQuotedImplicit: false));
	}
}

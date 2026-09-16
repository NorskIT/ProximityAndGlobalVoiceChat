using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Core.ObjectPool;
using YamlDotNet.Helpers;
using YamlDotNet.Serialization.Utilities;

namespace YamlDotNet.Serialization.NodeDeserializers;

internal sealed class ScalarNodeDeserializer : INodeDeserializer
{
	private const string BooleanTruePattern = "^(true|y|yes|on)$";

	private const string BooleanFalsePattern = "^(false|n|no|off)$";

	private readonly bool attemptUnknownTypeDeserialization;

	private readonly ITypeConverter typeConverter;

	private readonly ITypeInspector typeInspector;

	private readonly YamlFormatter formatter;

	private readonly INamingConvention enumNamingConvention;

	public ScalarNodeDeserializer(bool attemptUnknownTypeDeserialization, ITypeConverter typeConverter, ITypeInspector typeInspector, YamlFormatter formatter, INamingConvention enumNamingConvention)
	{
		this.attemptUnknownTypeDeserialization = attemptUnknownTypeDeserialization;
		this.typeConverter = typeConverter ?? throw new ArgumentNullException("typeConverter");
		this.typeInspector = typeInspector;
		this.formatter = formatter;
		this.enumNamingConvention = enumNamingConvention;
	}

	public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<Scalar>(out var @event))
		{
			value = null;
			return false;
		}
		Type type = Nullable.GetUnderlyingType(expectedType) ?? FsharpHelper.GetOptionUnderlyingType(expectedType) ?? expectedType;
		if (type.IsEnum())
		{
			string name = enumNamingConvention.Reverse(@event.Value);
			name = typeInspector.GetEnumName(type, name);
			value = Enum.Parse(type, name, ignoreCase: true);
			return true;
		}
		TypeCode typeCode = type.GetTypeCode();
		switch (typeCode)
		{
		case TypeCode.Boolean:
			value = DeserializeBooleanHelper(@event.Value);
			break;
		case TypeCode.SByte:
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.UInt16:
		case TypeCode.Int32:
		case TypeCode.UInt32:
		case TypeCode.Int64:
		case TypeCode.UInt64:
			value = DeserializeIntegerHelper(typeCode, @event.Value);
			break;
		case TypeCode.Single:
			value = float.Parse(@event.Value, formatter.NumberFormat);
			break;
		case TypeCode.Double:
			value = double.Parse(@event.Value, formatter.NumberFormat);
			break;
		case TypeCode.Decimal:
			value = decimal.Parse(@event.Value, formatter.NumberFormat);
			break;
		case TypeCode.String:
			value = @event.Value;
			break;
		case TypeCode.Char:
			value = @event.Value[0];
			break;
		case TypeCode.DateTime:
			value = DateTime.Parse(@event.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			break;
		default:
			if (expectedType == typeof(object))
			{
				if (!@event.IsKey && attemptUnknownTypeDeserialization)
				{
					value = AttemptUnknownTypeDeserialization(@event);
				}
				else
				{
					value = @event.Value;
				}
			}
			else
			{
				value = typeConverter.ChangeType(@event.Value, expectedType, enumNamingConvention, typeInspector);
			}
			break;
		}
		return true;
	}

	private static bool DeserializeBooleanHelper(string value)
	{
		if (Regex.IsMatch(value, "^(true|y|yes|on)$", RegexOptions.IgnoreCase))
		{
			return true;
		}
		if (Regex.IsMatch(value, "^(false|n|no|off)$", RegexOptions.IgnoreCase))
		{
			return false;
		}
		throw new FormatException("The value \"" + value + "\" is not a valid YAML Boolean");
	}

	private object DeserializeIntegerHelper(TypeCode typeCode, string value)
	{
		StringBuilderPool.BuilderWrapper builderWrapper = StringBuilderPool.Rent();
		try
		{
			StringBuilder builder = builderWrapper.Builder;
			int i = 0;
			bool flag = false;
			ulong num = 0uL;
			if (value[0] == '-')
			{
				i++;
				flag = true;
			}
			else if (value[0] == '+')
			{
				i++;
			}
			if (value[i] == '0')
			{
				int num2;
				if (i == value.Length - 1)
				{
					num2 = 10;
					num = 0uL;
				}
				else
				{
					i++;
					if (value[i] == 'b')
					{
						num2 = 2;
						i++;
					}
					else if (value[i] == 'x')
					{
						num2 = 16;
						i++;
					}
					else
					{
						num2 = 8;
					}
				}
				for (; i < value.Length; i++)
				{
					if (value[i] != '_')
					{
						builder.Append(value[i]);
					}
				}
				switch (num2)
				{
				case 2:
				case 8:
					num = Convert.ToUInt64(builder.ToString(), num2);
					break;
				case 16:
					num = ulong.Parse(builder.ToString(), NumberStyles.HexNumber, formatter.NumberFormat);
					break;
				}
			}
			else
			{
				string[] array = value.Substring(i).Split(new char[1] { ':' });
				num = 0uL;
				for (int j = 0; j < array.Length; j++)
				{
					num *= 60;
					num += ulong.Parse(array[j].Replace("_", ""), CultureInfo.InvariantCulture);
				}
			}
			if (!flag)
			{
				return CastInteger(num, typeCode);
			}
			long number = ((num != 9223372036854775808uL) ? checked(-(long)num) : long.MinValue);
			return CastInteger(number, typeCode);
		}
		finally
		{
			((IDisposable)builderWrapper/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private static object CastInteger(long number, TypeCode typeCode)
	{
		return checked(typeCode switch
		{
			TypeCode.Byte => (byte)number, 
			TypeCode.Int16 => (short)number, 
			TypeCode.Int32 => (int)number, 
			TypeCode.Int64 => number, 
			TypeCode.SByte => (sbyte)number, 
			TypeCode.UInt16 => (ushort)number, 
			TypeCode.UInt32 => (uint)number, 
			TypeCode.UInt64 => (ulong)number, 
			_ => number, 
		});
	}

	private static object CastInteger(ulong number, TypeCode typeCode)
	{
		return checked(typeCode switch
		{
			TypeCode.Byte => (byte)number, 
			TypeCode.Int16 => (short)number, 
			TypeCode.Int32 => (int)number, 
			TypeCode.Int64 => (long)number, 
			TypeCode.SByte => (sbyte)number, 
			TypeCode.UInt16 => (ushort)number, 
			TypeCode.UInt32 => (uint)number, 
			TypeCode.UInt64 => number, 
			_ => number, 
		});
	}

	private object? AttemptUnknownTypeDeserialization(Scalar value)
	{
		if (value.Style == ScalarStyle.SingleQuoted || value.Style == ScalarStyle.DoubleQuoted || value.Style == ScalarStyle.Folded)
		{
			return value.Value;
		}
		string v = value.Value;
		switch (v)
		{
		case "null":
		case "Null":
		case "NULL":
		case "~":
		case "":
			return null;
		case "true":
		case "True":
		case "TRUE":
			return true;
		case "False":
		case "FALSE":
		case "false":
			return false;
		default:
			if (Regex.IsMatch(v, "^0x[0-9a-fA-F]+$"))
			{
				v = v.Substring(2);
				if (byte.TryParse(v, NumberStyles.AllowHexSpecifier, formatter.NumberFormat, out var result))
				{
					return result;
				}
				if (short.TryParse(v, NumberStyles.AllowHexSpecifier, formatter.NumberFormat, out var result2))
				{
					return result2;
				}
				if (int.TryParse(v, NumberStyles.AllowHexSpecifier, formatter.NumberFormat, out var result3))
				{
					return result3;
				}
				if (long.TryParse(v, NumberStyles.AllowHexSpecifier, formatter.NumberFormat, out var result4))
				{
					return result4;
				}
				if (ulong.TryParse(v, NumberStyles.AllowHexSpecifier, formatter.NumberFormat, out var result5))
				{
					return result5;
				}
				return v;
			}
			if (Regex.IsMatch(v, "^0o[0-9a-fA-F]+$"))
			{
				if (!TryAndSwallow(() => Convert.ToByte(v, 8), out object value2) && !TryAndSwallow(() => Convert.ToInt16(v, 8), out value2) && !TryAndSwallow(() => Convert.ToInt32(v, 8), out value2) && !TryAndSwallow(() => Convert.ToInt64(v, 8), out value2) && !TryAndSwallow(() => Convert.ToUInt64(v, 8), out value2))
				{
					return v;
				}
				return value2;
			}
			if (Regex.IsMatch(v, "^[-+]?(\\.[0-9]+|[0-9]+(\\.[0-9]*)?)([eE][-+]?[0-9]+)?$"))
			{
				if (byte.TryParse(v, NumberStyles.Integer, formatter.NumberFormat, out var result6))
				{
					return result6;
				}
				if (short.TryParse(v, NumberStyles.Integer, formatter.NumberFormat, out var result7))
				{
					return result7;
				}
				if (int.TryParse(v, NumberStyles.Integer, formatter.NumberFormat, out var result8))
				{
					return result8;
				}
				if (long.TryParse(v, NumberStyles.Integer, formatter.NumberFormat, out var result9))
				{
					return result9;
				}
				if (ulong.TryParse(v, NumberStyles.Integer, formatter.NumberFormat, out var result10))
				{
					return result10;
				}
				if (float.TryParse(v, NumberStyles.Float, formatter.NumberFormat, out var result11))
				{
					return result11;
				}
				if (double.TryParse(v, NumberStyles.Float, formatter.NumberFormat, out var result12))
				{
					return result12;
				}
				return v;
			}
			if (Regex.IsMatch(v, "^[-+]?(\\.inf|\\.Inf|\\.INF)$"))
			{
				if (Polyfills.StartsWith(v, '-'))
				{
					return float.NegativeInfinity;
				}
				return float.PositiveInfinity;
			}
			if (Regex.IsMatch(v, "^(\\.nan|\\.NaN|\\.NAN)$"))
			{
				return float.NaN;
			}
			return v;
		}
	}

	private static bool TryAndSwallow(Func<object> attempt, out object? value)
	{
		try
		{
			value = attempt();
			return true;
		}
		catch
		{
			value = null;
			return false;
		}
	}
}

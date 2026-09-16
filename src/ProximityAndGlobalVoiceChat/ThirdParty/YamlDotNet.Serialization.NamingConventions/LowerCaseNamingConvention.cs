using System.Globalization;
using YamlDotNet.Serialization.Utilities;

namespace YamlDotNet.Serialization.NamingConventions;

internal sealed class LowerCaseNamingConvention : INamingConvention
{
	public static readonly INamingConvention Instance = new LowerCaseNamingConvention();

	private LowerCaseNamingConvention()
	{
	}

	public string Apply(string value)
	{
		return value.ToCamelCase().ToLower(CultureInfo.InvariantCulture);
	}

	public string Reverse(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		return char.ToUpperInvariant(value[0]) + value.Substring(1);
	}
}

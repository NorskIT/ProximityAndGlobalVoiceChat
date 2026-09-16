using System.Collections.Generic;

namespace UIManager;

internal class StyleOptions
{
	public bool RebindAssets = true;

	public bool FixBrokenShaders = true;

	public bool ApplyGameMaterial;

	public bool MatchMenuPixelDensity = true;

	public bool MatchGameScrolling = true;

	public float FallbackScrollSensitivity = 40f;

	public bool FallbackToGameFonts = true;

	public bool StyleByComponent = true;

	public bool AddButtonSfx = true;

	public bool AddTextOutline = true;

	public int DefaultFontSize;

	public readonly List<StyleRule> Rules = new List<StyleRule>();

	public static StyleOptions Default => new StyleOptions();

	public static StyleOptions RebindOnly => new StyleOptions
	{
		StyleByComponent = false,
		AddButtonSfx = false,
		AddTextOutline = false
	};

	public StyleOptions Rule(StyleRule rule)
	{
		if (rule != null)
		{
			Rules.Add(rule);
		}
		return this;
	}

	public StyleOptions Named(string pattern, ElementType element, bool recursive = false)
	{
		return Rule(StyleRule.Named(pattern, element, recursive));
	}

	public StyleOptions Path(string path, ElementType element, bool recursive = false)
	{
		return Rule(StyleRule.Path(path, element, recursive));
	}
}

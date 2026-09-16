using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal static class GameColors
{
	public static Color Orange = new Color(1f, 0.631f, 0.235f, 1f);

	public static Color Yellow = new Color(1f, 0.889f, 0f, 1f);

	public static Color Beige = new Color(0.8529f, 0.725f, 0.5331f, 1f);

	public static Color Muted = new Color(0.639f, 0.596f, 0.518f, 1f);

	public static Color Disabled = new Color(0.42f, 0.4f, 0.36f, 1f);

	public static Color Danger = new Color(0.85f, 0.27f, 0.22f, 1f);

	public static ColorBlock ButtonColors = new ColorBlock
	{
		normalColor = new Color(0.824f, 0.824f, 0.824f, 1f),
		highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f),
		pressedColor = new Color(0.537f, 0.556f, 0.556f, 1f),
		selectedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
		disabledColor = new Color(0.566f, 0.566f, 0.566f, 0.502f),
		colorMultiplier = 1f,
		fadeDuration = 0.1f
	};
}

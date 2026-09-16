using System.Collections.Generic;

namespace UIManager;

internal class StyleReport
{
	public int SpritesRebound;

	public int FontsRebound;

	public int WidgetsStyled;

	public int ShadersFixed;

	public int MaterialsApplied;

	public readonly List<string> UnresolvedSprites = new List<string>();

	public readonly List<string> UnresolvedFonts = new List<string>();

	public override string ToString()
	{
		return $"rebound {SpritesRebound} sprites / {FontsRebound} fonts, styled {WidgetsStyled} widgets, " + $"lit {MaterialsApplied} graphics, fixed {ShadersFixed} materials" + ((UnresolvedSprites.Count > 0) ? $", {UnresolvedSprites.Count} sprites had no game match" : string.Empty);
	}
}

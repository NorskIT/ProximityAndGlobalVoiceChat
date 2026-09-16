using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal static class Styler
{
	public static StyleReport Apply(GameObject root, StyleOptions options = null)
	{
		StyleReport styleReport = new StyleReport();
		if (root == null)
		{
			return styleReport;
		}
		options = options ?? StyleOptions.Default;
		VanillaUI.Harvest();
		GameAssets.Index();
		if (options.RebindAssets || options.FixBrokenShaders || options.ApplyGameMaterial)
		{
			Rebind(root, options, styleReport);
		}
		if (options.RebindAssets)
		{
			Scrollbar[] componentsInChildren = root.GetComponentsInChildren<Scrollbar>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Skin.Scrollbar(componentsInChildren[i]);
			}
		}
		if (options.MatchGameScrolling)
		{
			ScrollRect[] componentsInChildren2 = root.GetComponentsInChildren<ScrollRect>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				Skin.ScrollRect(componentsInChildren2[i], options.FallbackScrollSensitivity);
			}
		}
		if (options.StyleByComponent)
		{
			StyleByComponent(root, options, styleReport);
		}
		ApplyRules(root, options, styleReport);
		UILog.Debug($"styled '{root.name}': {styleReport}");
		if (UILog.Verbose && styleReport.UnresolvedSprites.Count > 0)
		{
			UILog.Debug("no game sprite named: " + string.Join(", ", styleReport.UnresolvedSprites.ToArray()));
		}
		return styleReport;
	}

	public static int Lit(GameObject root, params string[] paths)
	{
		return Set(root, paths, lit: true);
	}

	public static int Unlit(GameObject root, params string[] paths)
	{
		return Set(root, paths, lit: false);
	}

	private static int Set(GameObject root, string[] paths, bool lit)
	{
		if (root == null)
		{
			return 0;
		}
		int num = 0;
		foreach (GameObject item in Targets(root, paths))
		{
			Graphic[] components = item.GetComponents<Graphic>();
			foreach (Graphic graphic in components)
			{
				if (lit ? Skin.GameMaterial(graphic) : Skin.Unlit(graphic))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static IEnumerable<GameObject> Targets(GameObject root, string[] paths)
	{
		if (paths == null || paths.Length == 0)
		{
			yield return root;
			yield break;
		}
		foreach (string text in paths)
		{
			GameObject gameObject = UIExtensions.Find(root, text) ?? root.FindDeep(text);
			if (gameObject == null)
			{
				UILog.Warning("could not find '" + text + "' under '" + root.name + "' to light");
			}
			else
			{
				yield return gameObject;
			}
		}
	}

	public static void ApplyTo(GameObject root, string path, ElementType element, StyleOptions options = null)
	{
		GameObject gameObject = UIExtensions.Find(root, path);
		if (gameObject == null)
		{
			UILog.Warning("could not find '" + path + "' under '" + ((root != null) ? root.name : "null") + "'");
		}
		else
		{
			ApplyElement(gameObject, element, options ?? StyleOptions.Default, new StyleReport());
		}
	}

	private static void Rebind(GameObject root, StyleOptions options, StyleReport report)
	{
		Graphic[] componentsInChildren = root.GetComponentsInChildren<Graphic>(includeInactive: true);
		foreach (Graphic graphic in componentsInChildren)
		{
			if (options.FixBrokenShaders)
			{
				FixMaterial(graphic, report);
			}
			if (options.ApplyGameMaterial && Skin.GameMaterial(graphic))
			{
				report.MaterialsApplied++;
			}
			if (options.RebindAssets)
			{
				if (graphic is Image image)
				{
					RebindSprite(image, options, report);
				}
				else if (graphic is Text text)
				{
					RebindFont(text, options, report);
				}
				else if (graphic is TMP_Text text2)
				{
					RebindTmpFont(text2, options, report);
				}
			}
		}
		if (!options.RebindAssets)
		{
			return;
		}
		Selectable[] componentsInChildren2 = root.GetComponentsInChildren<Selectable>(includeInactive: true);
		foreach (Selectable selectable in componentsInChildren2)
		{
			SpriteState spriteState = selectable.spriteState;
			bool flag = false;
			Sprite sprite = spriteState.highlightedSprite;
			if (RebindSprite(ref sprite, report))
			{
				spriteState.highlightedSprite = sprite;
				flag = true;
			}
			Sprite sprite2 = spriteState.pressedSprite;
			if (RebindSprite(ref sprite2, report))
			{
				spriteState.pressedSprite = sprite2;
				flag = true;
			}
			Sprite sprite3 = spriteState.selectedSprite;
			if (RebindSprite(ref sprite3, report))
			{
				spriteState.selectedSprite = sprite3;
				flag = true;
			}
			Sprite sprite4 = spriteState.disabledSprite;
			if (RebindSprite(ref sprite4, report))
			{
				spriteState.disabledSprite = sprite4;
				flag = true;
			}
			if (flag)
			{
				selectable.spriteState = spriteState;
			}
		}
	}

	private static void RebindSprite(Image image, StyleOptions options, StyleReport report)
	{
		Sprite sprite = image.sprite;
		if (sprite == null)
		{
			return;
		}
		if (!GameAssets.TryGetSprite(sprite.name, out var sprite2))
		{
			if (!report.UnresolvedSprites.Contains(sprite.name))
			{
				report.UnresolvedSprites.Add(sprite.name);
			}
			return;
		}
		if (options.MatchMenuPixelDensity && image.type == Image.Type.Sliced)
		{
			image.pixelsPerUnitMultiplier = Skin.PixelsPerUnit;
		}
		if (!(sprite2 == sprite))
		{
			image.sprite = sprite2;
			report.SpritesRebound++;
		}
	}

	private static bool RebindSprite(ref Sprite sprite, StyleReport report)
	{
		if (sprite == null)
		{
			return false;
		}
		if (!GameAssets.TryGetSprite(sprite.name, out var sprite2) || sprite2 == sprite)
		{
			return false;
		}
		sprite = sprite2;
		report.SpritesRebound++;
		return true;
	}

	private static void RebindFont(Text text, StyleOptions options, StyleReport report)
	{
		if (text.font == null)
		{
			return;
		}
		Font font = VanillaUI.Font(text.font.name);
		if (font == null)
		{
			if (!report.UnresolvedFonts.Contains(text.font.name))
			{
				report.UnresolvedFonts.Add(text.font.name);
			}
			if (options.FallbackToGameFonts && Skin.Font(text, IsTitleFont(text.font.name) ? TextRole.Title : TextRole.Body))
			{
				report.FontsRebound++;
			}
		}
		else if (!(font == text.font))
		{
			text.font = font;
			report.FontsRebound++;
		}
	}

	private static void RebindTmpFont(TMP_Text text, StyleOptions options, StyleReport report)
	{
		if (text.font == null)
		{
			return;
		}
		TMP_FontAsset tMP_FontAsset = VanillaUI.TmpFont(text.font.name);
		if (tMP_FontAsset == null)
		{
			if (!report.UnresolvedFonts.Contains(text.font.name))
			{
				report.UnresolvedFonts.Add(text.font.name);
			}
			if (options.FallbackToGameFonts && Skin.Font(text, IsTitleFont(text.font.name) ? TextRole.Title : TextRole.Body))
			{
				report.FontsRebound++;
			}
		}
		else if (!(tMP_FontAsset == text.font))
		{
			text.font = tMP_FontAsset;
			if (tMP_FontAsset.material != null)
			{
				text.fontSharedMaterial = tMP_FontAsset.material;
			}
			text.fontStyle &= ~(FontStyles.Underline | FontStyles.Strikethrough);
			report.FontsRebound++;
		}
	}

	private static bool IsTitleFont(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			return name.IndexOf("norse", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private static void FixMaterial(Graphic graphic, StyleReport report)
	{
		Material material = graphic.material;
		if (!(material == null) && !(material == Graphic.defaultGraphicMaterial) && (!(material.shader != null) || !(material.shader.name != "Hidden/InternalErrorShader")))
		{
			graphic.material = null;
			report.ShadersFixed++;
		}
	}

	private static void StyleByComponent(GameObject root, StyleOptions options, StyleReport report)
	{
		Button[] componentsInChildren = root.GetComponentsInChildren<Button>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Skin.Button(componentsInChildren[i], options.DefaultFontSize, options.AddButtonSfx);
			report.WidgetsStyled++;
		}
		InputField[] componentsInChildren2 = root.GetComponentsInChildren<InputField>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			Skin.InputField(componentsInChildren2[i], options.DefaultFontSize);
			report.WidgetsStyled++;
		}
		Toggle[] componentsInChildren3 = root.GetComponentsInChildren<Toggle>(includeInactive: true);
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			Skin.Toggle(componentsInChildren3[i]);
			report.WidgetsStyled++;
		}
		Scrollbar[] componentsInChildren4 = root.GetComponentsInChildren<Scrollbar>(includeInactive: true);
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			Skin.Scrollbar(componentsInChildren4[i]);
			report.WidgetsStyled++;
		}
		Slider[] componentsInChildren5 = root.GetComponentsInChildren<Slider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren5.Length; i++)
		{
			Skin.Slider(componentsInChildren5[i]);
			report.WidgetsStyled++;
		}
		Text[] componentsInChildren6 = root.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren6)
		{
			if (!(text.GetComponentInParent<Button>() != null))
			{
				Skin.Text(text, TextRole.Body, options.DefaultFontSize, options.AddTextOutline);
				report.WidgetsStyled++;
			}
		}
		TMP_Text[] componentsInChildren7 = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren7)
		{
			if (!(tMP_Text.GetComponentInParent<Button>() != null))
			{
				Skin.Text(tMP_Text, TextRole.Body, options.DefaultFontSize);
				report.WidgetsStyled++;
			}
		}
	}

	private static void ApplyRules(GameObject root, StyleOptions options, StyleReport report)
	{
		if (options.Rules.Count == 0)
		{
			return;
		}
		List<Transform> list = new List<Transform>();
		UIExtensions.Collect(root.transform, list);
		foreach (StyleRule rule in options.Rules)
		{
			if (rule == null || rule.Match == null)
			{
				continue;
			}
			foreach (Transform item in list)
			{
				if (!rule.Match(item.gameObject))
				{
					continue;
				}
				if (rule.Recursive)
				{
					List<Transform> list2 = new List<Transform>();
					UIExtensions.Collect(item, list2);
					foreach (Transform item2 in list2)
					{
						ApplyElement(item2.gameObject, rule.Element, options, report);
					}
				}
				else
				{
					ApplyElement(item.gameObject, rule.Element, options, report);
				}
			}
		}
	}

	private static void ApplyElement(GameObject target, ElementType element, StyleOptions options, StyleReport report)
	{
		if (!(target == null))
		{
			report.WidgetsStyled++;
			switch (element)
			{
			case ElementType.Window:
				Skin.Window(target.GetComponent<Image>());
				break;
			case ElementType.Panel:
				Skin.Panel(target.GetComponent<Image>());
				break;
			case ElementType.Slot:
				Skin.Slot(target.GetComponent<Image>());
				break;
			case ElementType.Button:
				Skin.Button(target.GetComponent<Button>(), options.DefaultFontSize, options.AddButtonSfx);
				break;
			case ElementType.InputField:
				Skin.InputField(target.GetComponent<InputField>(), options.DefaultFontSize);
				break;
			case ElementType.Toggle:
				Skin.Toggle(target.GetComponent<Toggle>());
				break;
			case ElementType.Scrollbar:
				Skin.Scrollbar(target.GetComponent<Scrollbar>());
				break;
			case ElementType.Slider:
				Skin.Slider(target.GetComponent<Slider>());
				break;
			default:
				ApplyTextRole(target, element, options);
				break;
			case ElementType.Ignore:
				break;
			}
		}
	}

	private static void ApplyTextRole(GameObject target, ElementType element, StyleOptions options)
	{
		TextRole role = element switch
		{
			ElementType.Title => TextRole.Title, 
			ElementType.Header => TextRole.Header, 
			ElementType.Label => TextRole.Label, 
			_ => TextRole.Body, 
		};
		Text component = target.GetComponent<Text>();
		if (component != null)
		{
			Skin.Text(component, role, options.DefaultFontSize, options.AddTextOutline);
		}
		TMP_Text component2 = target.GetComponent<TMP_Text>();
		if (component2 != null)
		{
			Skin.Text(component2, role, options.DefaultFontSize);
		}
	}
}

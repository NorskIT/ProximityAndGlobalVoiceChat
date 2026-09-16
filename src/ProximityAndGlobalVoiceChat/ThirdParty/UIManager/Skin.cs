using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UIManager;

internal static class Skin
{
	private static readonly Color CheckmarkColour = new Color(1f, 0.678f, 0.103f, 1f);

	private static readonly ColorBlock CheckboxColours = new ColorBlock
	{
		normalColor = new Color(52f / 85f, 52f / 85f, 52f / 85f, 1f),
		highlightedColor = new Color(49f / 51f, 49f / 51f, 49f / 51f, 1f),
		pressedColor = new Color(40f / 51f, 40f / 51f, 40f / 51f, 1f),
		selectedColor = Color.white,
		disabledColor = new Color(40f / 51f, 40f / 51f, 40f / 51f, 0.5019608f),
		colorMultiplier = 1f,
		fadeDuration = 0.1f
	};

	private static bool _loggedScrollbar;

	private static readonly Color TrackColour = new Color(0.192f, 0.126f, 0.078f, 1f);

	private static readonly ColorBlock HandleColours = new ColorBlock
	{
		normalColor = new Color(63f / 68f, 0.64584446f, 0.34061417f, 1f),
		highlightedColor = new Color(1f, 114f / 145f, 0.08823532f, 1f),
		pressedColor = new Color(0.83823526f, 0.6471056f, 0.030817453f, 1f),
		selectedColor = new Color(1f, 114f / 145f, 0.08823532f, 1f),
		disabledColor = new Color(40f / 51f, 40f / 51f, 40f / 51f, 0.5019608f),
		colorMultiplier = 1f,
		fadeDuration = 0.1f
	};

	private static bool _loggedScrollRect;

	public const float VanillaScrollSensitivity = 3000f;

	private static readonly Color SliderFillColour = new Color(0.647f, 0.647f, 0.647f, 1f);

	private static readonly Color SliderHandleColour = new Color(1f, 0.641f, 0f, 1f);

	private static readonly ColorBlock SliderColours = new ColorBlock
	{
		normalColor = new Color(0.271f, 0.271f, 0.271f, 1f),
		highlightedColor = new Color(0.957f, 0.957f, 0.957f, 1f),
		pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f),
		selectedColor = new Color(0.957f, 0.957f, 0.957f, 1f),
		disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f),
		colorMultiplier = 1f,
		fadeDuration = 0.1f
	};

	public static float PixelsPerUnit
	{
		get
		{
			if (!(SceneManager.GetActiveScene().name == "start"))
			{
				return 1f;
			}
			return 2f;
		}
	}

	public static void Highlight(Image image, Color? color = null)
	{
		if (!(image == null))
		{
			image.color = color ?? GameColors.Yellow;
			image.material = null;
		}
	}

	public static void Window(Image image)
	{
		if (!(image == null))
		{
			Sprite sprite = GameAssets.FirstSprite(SpriteNames.Window);
			if (!(sprite == null))
			{
				image.sprite = sprite;
				image.type = Image.Type.Sliced;
				image.color = Color.white;
				image.material = null;
			}
		}
	}

	public static void Panel(Image image)
	{
		if (!(image == null))
		{
			Sprite sprite = GameAssets.FirstSprite(SpriteNames.Panel);
			if (!(sprite == null))
			{
				image.sprite = sprite;
				image.type = Image.Type.Sliced;
				image.color = Color.white;
			}
		}
	}

	public static void Slot(Image image)
	{
		if (!(image == null))
		{
			Sprite sprite = GameAssets.FirstSprite(SpriteNames.Slot);
			if (!(sprite == null))
			{
				image.sprite = sprite;
				image.type = Image.Type.Sliced;
				image.color = Color.white;
			}
		}
	}

	public static void Button(Button button, int fontSize = 0, bool withSfx = true)
	{
		if (button == null)
		{
			return;
		}
		Image image = (button.targetGraphic as Image) ?? button.GetComponent<Image>();
		Sprite sprite = GameAssets.FirstSprite(SpriteNames.Button);
		if (image != null && sprite != null)
		{
			image.sprite = sprite;
			image.type = Image.Type.Sliced;
			image.pixelsPerUnitMultiplier = PixelsPerUnit;
			image.color = Color.white;
			button.targetGraphic = image;
		}
		button.colors = GameColors.ButtonColors;
		Sprite sprite2 = GameAssets.FirstSprite(SpriteNames.ButtonHighlight);
		if (sprite2 != null)
		{
			button.transition = Selectable.Transition.SpriteSwap;
			SpriteState spriteState = button.spriteState;
			spriteState.highlightedSprite = sprite2;
			spriteState.pressedSprite = sprite2;
			spriteState.selectedSprite = sprite2;
			Sprite sprite3 = GameAssets.FirstSprite(SpriteNames.ButtonDisabled);
			if (sprite3 != null)
			{
				spriteState.disabledSprite = sprite3;
			}
			button.spriteState = spriteState;
		}
		Text[] componentsInChildren = button.GetComponentsInChildren<Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Text(componentsInChildren[i], TextRole.Label, fontSize);
		}
		TMP_Text[] componentsInChildren2 = button.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			Text(componentsInChildren2[i], TextRole.Label, fontSize);
		}
		if (withSfx)
		{
			Sfx(button);
		}
	}

	public static void Sfx(Button button)
	{
		if (!(button == null) && !(button.GetComponent<ButtonSfx>() != null))
		{
			ButtonSfx buttonSfx = UIRoot.FindSfxTemplate();
			if (!(buttonSfx == null))
			{
				ButtonSfx buttonSfx2 = button.gameObject.AddComponent<ButtonSfx>();
				buttonSfx2.m_sfxPrefab = buttonSfx.m_sfxPrefab;
				buttonSfx2.m_selectSfxPrefab = buttonSfx.m_selectSfxPrefab;
			}
		}
	}

	public static void Text(Text text, TextRole role = TextRole.Body, int fontSize = 0, bool outline = true)
	{
		if (!(text == null))
		{
			Font font = ((role == TextRole.Title || role == TextRole.Header) ? (GameAssets.FirstFont(FontNames.NorseBold) ?? GameAssets.FirstFont(FontNames.Bold)) : (GameAssets.FirstFont(FontNames.Body) ?? GameAssets.FirstFont(FontNames.Bold)));
			if (font != null)
			{
				text.font = font;
			}
			text.color = ColorFor(role);
			if (fontSize > 0)
			{
				text.fontSize = fontSize;
			}
			if (outline)
			{
				Outline(text.gameObject);
			}
		}
	}

	public static bool Font(TMP_Text text, TextRole role = TextRole.Body)
	{
		if (text == null)
		{
			return false;
		}
		TMP_FontAsset tMP_FontAsset = ((role == TextRole.Title || role == TextRole.Header) ? VanillaUI.TitleTmpFont : VanillaUI.PrimaryTmpFont);
		tMP_FontAsset = tMP_FontAsset ?? GameAssets.FirstTmpFont((role == TextRole.Title) ? FontNames.TmpTitle : FontNames.TmpBody);
		if (tMP_FontAsset == null)
		{
			return false;
		}
		text.fontStyle &= ~(FontStyles.Underline | FontStyles.Strikethrough);
		if (tMP_FontAsset == text.font)
		{
			return false;
		}
		text.font = tMP_FontAsset;
		if (tMP_FontAsset.material != null)
		{
			text.fontSharedMaterial = tMP_FontAsset.material;
		}
		return true;
	}

	public static bool Font(Text text, TextRole role = TextRole.Body)
	{
		if (text == null)
		{
			return false;
		}
		Font font = VanillaUI.PrimaryFont ?? ((role == TextRole.Title || role == TextRole.Header) ? GameAssets.FirstFont(FontNames.NorseBold) : GameAssets.FirstFont(FontNames.Body));
		if (font == null || font == text.font)
		{
			return false;
		}
		text.font = font;
		return true;
	}

	public static void Text(TMP_Text text, TextRole role = TextRole.Body, int fontSize = 0)
	{
		if (!(text == null))
		{
			Font(text, role);
			text.color = ColorFor(role);
			if (fontSize > 0)
			{
				text.fontSize = fontSize;
			}
		}
	}

	public static Color ColorFor(TextRole role)
	{
		return role switch
		{
			TextRole.Title => GameColors.Orange, 
			TextRole.Header => GameColors.Yellow, 
			TextRole.Label => GameColors.Beige, 
			TextRole.Disabled => GameColors.Disabled, 
			_ => GameColors.Beige, 
		};
	}

	public static void Outline(GameObject target, Color? color = null)
	{
		if (!(target == null))
		{
			Outline outline = target.GetComponent<Outline>();
			if (outline == null)
			{
				outline = target.AddComponent<Outline>();
			}
			outline.effectColor = color ?? Color.black;
			outline.effectDistance = new Vector2(1f, -1f);
		}
	}

	public static void InputField(InputField field, int fontSize = 0)
	{
		if (!(field == null))
		{
			Image component = field.GetComponent<Image>();
			Sprite sprite = GameAssets.FirstSprite(SpriteNames.Field);
			if (component != null && sprite != null)
			{
				component.sprite = sprite;
				component.type = Image.Type.Sliced;
				component.color = Color.white;
			}
			if (field.textComponent != null)
			{
				Text(field.textComponent, TextRole.Body, fontSize);
			}
			if (field.placeholder is Text text)
			{
				Text(text, TextRole.Disabled, fontSize);
			}
		}
	}

	public static void Toggle(Toggle toggle)
	{
		if (toggle == null)
		{
			return;
		}
		Image image = toggle.targetGraphic as Image;
		Sprite sprite = GameAssets.FirstSprite(SpriteNames.Checkbox);
		if (image != null && sprite != null)
		{
			image.sprite = sprite;
			image.type = Image.Type.Simple;
			image.color = Color.white;
		}
		if (toggle.graphic is Image image2)
		{
			Sprite sprite2 = GameAssets.FirstSprite(SpriteNames.CheckboxMarker);
			if (sprite2 != null)
			{
				image2.sprite = sprite2;
				image2.type = Image.Type.Simple;
			}
			image2.color = CheckmarkColour;
		}
		toggle.transition = Selectable.Transition.ColorTint;
		toggle.colors = CheckboxColours;
		Text[] componentsInChildren = toggle.GetComponentsInChildren<Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Text(componentsInChildren[i], TextRole.Label);
		}
	}

	public static void Scrollbar(Scrollbar scrollbar)
	{
		if (scrollbar == null)
		{
			return;
		}
		Scrollbar scrollbarTemplate = VanillaUI.ScrollbarTemplate;
		if (scrollbarTemplate == null)
		{
			ScrollbarByName(scrollbar);
			ApplyVanillaScrollbarColours(scrollbar);
			return;
		}
		Image image = HandleImage(scrollbarTemplate);
		Image image2 = TrackImage(scrollbarTemplate, image);
		Image image3 = HandleImage(scrollbar);
		Image image4 = TrackImage(scrollbar, image3);
		if (!_loggedScrollbar)
		{
			_loggedScrollbar = true;
			UILog.Info("scrollbar copied from " + UIExtensions.PathOf(scrollbarTemplate.transform) + " (track '" + ((image2 != null) ? SpriteName(image2) : "none") + "', handle '" + ((image != null) ? SpriteName(image) : "none") + "')");
		}
		scrollbar.transition = scrollbarTemplate.transition;
		scrollbar.colors = scrollbarTemplate.colors;
		if (image4 != null)
		{
			if (image2 != null)
			{
				CopyImage(image2, image4);
				image4.enabled = true;
			}
			else
			{
				image4.enabled = false;
			}
		}
		if (image3 != null && image != null)
		{
			CopyImage(image, image3);
		}
		scrollbar.colors = scrollbarTemplate.colors;
		scrollbar.transition = scrollbarTemplate.transition;
		scrollbar.spriteState = scrollbarTemplate.spriteState;
		scrollbar.navigation = new Navigation
		{
			mode = Navigation.Mode.None
		};
		if (scrollbar.targetGraphic != null)
		{
			scrollbar.targetGraphic.canvasRenderer.SetColor((scrollbar.transition == Selectable.Transition.ColorTint) ? scrollbar.colors.normalColor : Color.white);
		}
		scrollbar.enabled = false;
		scrollbar.enabled = true;
	}

	public static void ApplyVanillaScrollbarColours(Scrollbar scrollbar)
	{
		if (!(scrollbar == null))
		{
			Image image = HandleImage(scrollbar);
			Image image2 = TrackImage(scrollbar, image);
			if (image2 != null)
			{
				image2.color = TrackColour;
				image2.enabled = true;
			}
			if (image != null)
			{
				image.color = Color.white;
			}
			scrollbar.transition = Selectable.Transition.ColorTint;
			scrollbar.colors = HandleColours;
			scrollbar.navigation = new Navigation
			{
				mode = Navigation.Mode.None
			};
			if (scrollbar.targetGraphic != null)
			{
				scrollbar.targetGraphic.canvasRenderer.SetColor(HandleColours.normalColor);
			}
			scrollbar.enabled = false;
			scrollbar.enabled = true;
		}
	}

	private static string SpriteName(Image image)
	{
		if (!(image.sprite != null))
		{
			return "no sprite";
		}
		return image.sprite.name;
	}

	private static Image HandleImage(Scrollbar scrollbar)
	{
		if (scrollbar.handleRect != null)
		{
			Image component = scrollbar.handleRect.GetComponent<Image>();
			if (component != null)
			{
				return component;
			}
		}
		return scrollbar.targetGraphic as Image;
	}

	private static Image TrackImage(Scrollbar scrollbar, Image handle)
	{
		Image component = scrollbar.GetComponent<Image>();
		if (component != null && component != handle)
		{
			return component;
		}
		Image[] componentsInChildren = scrollbar.GetComponentsInChildren<Image>(includeInactive: true);
		foreach (Image image in componentsInChildren)
		{
			if (!(image == null) && !(image == handle) && (!(handle != null) || !image.transform.IsChildOf(handle.transform)) && (!(scrollbar.handleRect != null) || !image.transform.IsChildOf(scrollbar.handleRect)))
			{
				return image;
			}
		}
		return null;
	}

	private static void ScrollbarByName(Scrollbar scrollbar)
	{
		Image image = HandleImage(scrollbar);
		Image image2 = TrackImage(scrollbar, image);
		if (image2 != null && VanillaUI.TryGetSprite(SpriteNames.ScrollTrack[0], out var sprite))
		{
			image2.sprite = sprite;
			image2.type = Image.Type.Sliced;
			image2.color = new Color(0f, 0f, 0f, 0.5f);
		}
		if (image != null && VanillaUI.TryGetSprite(SpriteNames.ScrollHandle[0], out var sprite2))
		{
			image.sprite = sprite2;
			image.type = Image.Type.Sliced;
			image.color = GameColors.Muted;
		}
		if (!_loggedScrollbar)
		{
			_loggedScrollbar = true;
			UILog.Warning("no vanilla scrollbar found to copy; left the prefab's own art in place");
		}
	}

	public static void ResetLogs()
	{
		_loggedScrollRect = false;
		_loggedScrollbar = false;
	}

	public static void ScrollRect(ScrollRect scroll, float fallbackSensitivity = 3000f)
	{
		if (scroll == null)
		{
			return;
		}
		ScrollRect scrollRectTemplate = VanillaUI.ScrollRectTemplate;
		if (scrollRectTemplate == null)
		{
			scroll.scrollSensitivity = fallbackSensitivity;
			Report(scroll, "no template");
			return;
		}
		scroll.scrollSensitivity = scrollRectTemplate.scrollSensitivity;
		scroll.inertia = scrollRectTemplate.inertia;
		scroll.decelerationRate = scrollRectTemplate.decelerationRate;
		scroll.elasticity = scrollRectTemplate.elasticity;
		if (scroll.scrollSensitivity < fallbackSensitivity)
		{
			scroll.scrollSensitivity = fallbackSensitivity;
		}
		Report(scroll, UIExtensions.PathOf(scrollRectTemplate.transform));
	}

	private static void Report(ScrollRect scroll, string source)
	{
		if (!_loggedScrollRect)
		{
			_loggedScrollRect = true;
			UILog.Info($"scrollrect sensitivity {scroll.scrollSensitivity} (from {source}), " + $"inertia {scroll.inertia}, deceleration {scroll.decelerationRate}");
		}
	}

	private static void CopyImage(Image from, Image to)
	{
		if (!(from == null) && !(to == null))
		{
			to.sprite = from.sprite;
			to.type = from.type;
			to.color = from.color;
			to.material = from.material;
			to.pixelsPerUnitMultiplier = from.pixelsPerUnitMultiplier;
			to.fillCenter = from.fillCenter;
			to.preserveAspect = from.preserveAspect;
		}
	}

	public static bool GameMaterial(Graphic graphic)
	{
		if (graphic == null || graphic is TMP_Text)
		{
			return false;
		}
		Material uiMaterial = VanillaUI.UiMaterial;
		if (uiMaterial == null || graphic.material == uiMaterial)
		{
			return false;
		}
		graphic.material = uiMaterial;
		return true;
	}

	public static bool Unlit(Graphic graphic)
	{
		if (graphic == null || graphic is TMP_Text)
		{
			return false;
		}
		if (graphic.material == null || graphic.material == Graphic.defaultGraphicMaterial)
		{
			return false;
		}
		graphic.material = null;
		return true;
	}

	public static void Slider(Slider slider)
	{
		if (slider == null)
		{
			return;
		}
		if (slider.targetGraphic is Image image)
		{
			image.sprite = null;
			image.color = Color.white;
		}
		if (slider.fillRect != null)
		{
			Image component = slider.fillRect.GetComponent<Image>();
			if ((object)component != null)
			{
				component.sprite = null;
				component.color = SliderFillColour;
			}
		}
		if (slider.handleRect != null)
		{
			Image component2 = slider.handleRect.GetComponent<Image>();
			if ((object)component2 != null)
			{
				Sprite sprite = GameAssets.FirstSprite(SpriteNames.CheckboxMarker);
				if (sprite != null)
				{
					component2.sprite = sprite;
					component2.type = Image.Type.Simple;
				}
				component2.color = SliderHandleColour;
			}
		}
		slider.transition = Selectable.Transition.ColorTint;
		slider.colors = SliderColours;
		if (slider.targetGraphic != null)
		{
			slider.targetGraphic.canvasRenderer.SetColor(SliderColours.normalColor);
		}
	}
}

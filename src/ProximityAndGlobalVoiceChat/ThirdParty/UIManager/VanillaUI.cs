using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace UIManager;

internal static class VanillaUI
{
	private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, TMP_FontAsset> TmpFonts = new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);

	private static Material _uiMaterial;

	private static TMP_FontAsset _primaryTmpFont;

	private static TMP_FontAsset _titleTmpFont;

	private static Font _primaryFont;

	private static Scrollbar _scrollbar;

	private static ScrollRect _scrollRect;

	private static int _scrollbarScore;

	private static int _harvestedFrame = -1;

	private static int _retries;

	private static Scrollbar _reportedScrollbar;

	private const int RetryFrames = 30;

	private static bool _hooked;

	private static readonly Dictionary<TMP_FontAsset, int> _tmpFontUse = new Dictionary<TMP_FontAsset, int>();

	private static readonly Dictionary<Font, int> _fontUse = new Dictionary<Font, int>();

	private static float _largestText;

	private static readonly Dictionary<string, Sprite> AtlasSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

	private static SpriteAtlas[] _atlases;

	private static bool Incomplete
	{
		get
		{
			if (!(_scrollbar == null) && !(_scrollRect == null) && !(_uiMaterial == null))
			{
				return _primaryTmpFont == null;
			}
			return true;
		}
	}

	public static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

	public static bool IsHarvested => _harvestedFrame >= 0;

	public static int SpriteCount => Sprites.Count;

	public static Material UiMaterial
	{
		get
		{
			Harvest();
			return _uiMaterial;
		}
	}

	public static TMP_FontAsset PrimaryTmpFont
	{
		get
		{
			Harvest();
			return _primaryTmpFont;
		}
	}

	public static TMP_FontAsset TitleTmpFont
	{
		get
		{
			Harvest();
			if (!(_titleTmpFont != null))
			{
				return _primaryTmpFont;
			}
			return _titleTmpFont;
		}
	}

	public static Font PrimaryFont
	{
		get
		{
			Harvest();
			return _primaryFont;
		}
	}

	public static Scrollbar ScrollbarTemplate
	{
		get
		{
			Harvest();
			if (!(_scrollbar != null))
			{
				return null;
			}
			return _scrollbar;
		}
	}

	public static ScrollRect ScrollRectTemplate
	{
		get
		{
			Harvest();
			if (!(_scrollRect != null))
			{
				return null;
			}
			return _scrollRect;
		}
	}

	public static IEnumerable<string> SpriteNames
	{
		get
		{
			Harvest();
			return Sprites.Keys;
		}
	}

	public static void Invalidate()
	{
		_harvestedFrame = -1;
		_retries = 0;
		_reportedScrollbar = null;
		AtlasSprites.Clear();
		_atlases = null;
		Skin.ResetLogs();
	}

	private static void HookSceneChanges()
	{
		if (!_hooked)
		{
			_hooked = true;
			SceneManager.sceneLoaded += delegate
			{
				Invalidate();
			};
		}
	}

	public static void Harvest(bool force = false)
	{
		if (IsHeadless)
		{
			return;
		}
		HookSceneChanges();
		if (!force)
		{
			if (_harvestedFrame == Time.frameCount)
			{
				return;
			}
			if (_harvestedFrame >= 0)
			{
				if (!Incomplete || Time.frameCount - _harvestedFrame < 30)
				{
					return;
				}
				_retries++;
			}
		}
		Sprites.Clear();
		Fonts.Clear();
		TmpFonts.Clear();
		_uiMaterial = null;
		_scrollbar = null;
		_scrollRect = null;
		_scrollbarScore = 0;
		_primaryTmpFont = null;
		_titleTmpFont = null;
		_primaryFont = null;
		Dictionary<Material, int> dictionary = new Dictionary<Material, int>();
		_tmpFontUse.Clear();
		_fontUse.Clear();
		_largestText = 0f;
		bool flag = false;
		Scrollbar scrollbar = NamedScrollbar();
		if (scrollbar != null)
		{
			_scrollbar = scrollbar;
			_scrollbarScore = int.MaxValue;
			ScrollRect componentInParent = scrollbar.GetComponentInParent<ScrollRect>();
			if (componentInParent != null)
			{
				_scrollRect = componentInParent;
			}
		}
		foreach (GameObject item in Roots())
		{
			if (!(item == null))
			{
				flag = true;
				Collect(item, dictionary);
			}
		}
		if (!flag)
		{
			return;
		}
		foreach (KeyValuePair<Material, int> item2 in dictionary)
		{
			if (!(item2.Key == null) && item2.Key.name.StartsWith("litpanel", StringComparison.OrdinalIgnoreCase))
			{
				_uiMaterial = item2.Key;
				break;
			}
		}
		int num = 0;
		if (_uiMaterial == null)
		{
			foreach (KeyValuePair<Material, int> item3 in dictionary)
			{
				if (!(item3.Key == null) && item3.Value > num)
				{
					num = item3.Value;
					_uiMaterial = item3.Key;
				}
			}
		}
		_primaryTmpFont = MostUsed(_tmpFontUse);
		_primaryFont = MostUsed(_fontUse);
		_harvestedFrame = Time.frameCount;
		if (!Incomplete)
		{
			_retries = 0;
		}
		if (_scrollbar != null && _scrollbar != _reportedScrollbar)
		{
			_reportedScrollbar = _scrollbar;
			Image component = _scrollbar.GetComponent<Image>();
			UILog.Info("scrollbar template '" + UIExtensions.PathOf(_scrollbar.transform) + "' " + $"score {ScoreScrollbar(_scrollbar)}, " + "track " + ((component != null) ? component.color.ToString() : "none") + ", " + $"transition {_scrollbar.transition}, " + "scrollrect '" + ((_scrollRect != null) ? UIExtensions.PathOf(_scrollRect.transform) : "none") + "' " + $"sensitivity {((_scrollRect != null) ? _scrollRect.scrollSensitivity : 0f)}");
		}
		UILog.Debug($"harvested {Sprites.Count} sprites, {Fonts.Count} fonts, {TmpFonts.Count} TMP fonts from the game UI; " + "material '" + ((_uiMaterial != null) ? _uiMaterial.name : "none") + "', scrollbar '" + ((_scrollbar != null) ? UIExtensions.PathOf(_scrollbar.transform) : "none") + "' " + $"(score {_scrollbarScore}), " + "body font '" + ((_primaryTmpFont != null) ? _primaryTmpFont.name : "none") + "', title font '" + ((_titleTmpFont != null) ? _titleTmpFont.name : "none") + "'");
	}

	private static T MostUsed<T>(Dictionary<T, int> counts) where T : UnityEngine.Object
	{
		T result = null;
		int num = 0;
		foreach (KeyValuePair<T, int> count in counts)
		{
			if (!(count.Key == null) && count.Value > num)
			{
				num = count.Value;
				result = count.Key;
			}
		}
		return result;
	}

	public static IEnumerable<GameObject> RootObjects()
	{
		return Roots();
	}

	private static IEnumerable<GameObject> Roots()
	{
		yield return Instance(FejdStartup.instance);
		if (FejdStartup.instance != null)
		{
			yield return FejdStartup.instance.m_settingsPrefab;
		}
		if (Menu.instance != null)
		{
			yield return Menu.instance.m_settingsPrefab;
		}
		yield return Instance(InventoryGui.instance);
		yield return Instance(Menu.instance);
		yield return Instance(Hud.instance);
		yield return Instance(StoreGui.instance);
		yield return Instance(TextViewer.instance);
		yield return Instance(Minimap.instance);
	}

	private static GameObject Instance(Component component)
	{
		if (!(component != null))
		{
			return null;
		}
		return component.gameObject;
	}

	private static void Collect(GameObject root, IDictionary<Material, int> materialUse)
	{
		Image[] componentsInChildren = root.GetComponentsInChildren<Image>(includeInactive: true);
		foreach (Image image in componentsInChildren)
		{
			if (!(image == null))
			{
				if (image.sprite != null)
				{
					Put(Sprites, image.sprite.name, image.sprite);
				}
				Material material = image.material;
				if (material != null && material != Graphic.defaultGraphicMaterial)
				{
					materialUse[material] = ((!materialUse.TryGetValue(material, out var value)) ? 1 : (value + 1));
				}
			}
		}
		Text[] componentsInChildren2 = root.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren2)
		{
			if (!(text == null) && !(text.font == null))
			{
				Put(Fonts, text.font.name, text.font);
				_fontUse[text.font] = ((!_fontUse.TryGetValue(text.font, out var value2)) ? 1 : (value2 + 1));
			}
		}
		TMP_Text[] componentsInChildren3 = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren3)
		{
			if (!(tMP_Text == null) && !(tMP_Text.font == null))
			{
				Put(TmpFonts, tMP_Text.font.name, tMP_Text.font);
				_tmpFontUse[tMP_Text.font] = ((!_tmpFontUse.TryGetValue(tMP_Text.font, out var value3)) ? 1 : (value3 + 1));
				if (tMP_Text.fontSize > _largestText)
				{
					_largestText = tMP_Text.fontSize;
					_titleTmpFont = tMP_Text.font;
				}
			}
		}
		if (_scrollRect == null)
		{
			ScrollRect[] componentsInChildren4 = root.GetComponentsInChildren<ScrollRect>(includeInactive: true);
			foreach (ScrollRect scrollRect in componentsInChildren4)
			{
				if (!(scrollRect == null))
				{
					_scrollRect = scrollRect;
					break;
				}
			}
		}
		Scrollbar[] componentsInChildren5 = root.GetComponentsInChildren<Scrollbar>(includeInactive: true);
		foreach (Scrollbar scrollbar in componentsInChildren5)
		{
			int num = ScoreScrollbar(scrollbar);
			if (num > _scrollbarScore)
			{
				_scrollbarScore = num;
				_scrollbar = scrollbar;
			}
		}
	}

	private static Scrollbar NamedScrollbar()
	{
		if (FejdStartup.instance != null)
		{
			FejdStartup instance = FejdStartup.instance;
			if (Usable(InPanel(instance.m_worldListPanel), out var result))
			{
				return result;
			}
			if (Usable(InPanel(instance.m_serverListPanel), out var result2))
			{
				return result2;
			}
			if (Usable(InPanel(instance.m_characterSelectScreen), out var result3))
			{
				return result3;
			}
			if (Usable(FromSettings(instance.m_settingsPrefab), out var result4))
			{
				return result4;
			}
		}
		if (InventoryGui.instance != null)
		{
			if (Usable(InventoryGui.instance.m_recipeListScroll, out var result5))
			{
				return result5;
			}
			if (Usable(InventoryGui.instance.m_trophyListScroll, out var result6))
			{
				return result6;
			}
		}
		if (Menu.instance != null && Usable(FromSettings(Menu.instance.m_settingsPrefab), out var result7))
		{
			return result7;
		}
		if (StoreGui.instance != null && Usable(StoreGui.instance.m_listScroll, out var result8))
		{
			return result8;
		}
		return null;
	}

	private static bool IsUnskinned(Color color)
	{
		if (color.r > 0.95f && color.g > 0.95f)
		{
			return color.b > 0.95f;
		}
		return false;
	}

	private static Scrollbar InPanel(GameObject panel)
	{
		if (!(panel != null))
		{
			return null;
		}
		return panel.GetComponentInChildren<Scrollbar>(includeInactive: true);
	}

	private static Scrollbar FromSettings(GameObject settingsPrefab)
	{
		if (settingsPrefab == null)
		{
			return null;
		}
		Valheim.SettingsGui.GraphicsSettings componentInChildren = settingsPrefab.GetComponentInChildren<Valheim.SettingsGui.GraphicsSettings>(includeInactive: true);
		if (!(componentInChildren != null))
		{
			return null;
		}
		return componentInChildren.m_resolutionListScroll;
	}

	private static bool Usable(Scrollbar candidate, out Scrollbar result)
	{
		result = ((ScoreScrollbar(candidate) > 0) ? candidate : null);
		return result != null;
	}

	private static int ScoreScrollbar(Scrollbar scrollbar)
	{
		if (scrollbar == null || scrollbar.handleRect == null)
		{
			return 0;
		}
		Image image = scrollbar.handleRect.GetComponent<Image>() ?? (scrollbar.targetGraphic as Image);
		if (image == null)
		{
			return 0;
		}
		Image component = scrollbar.GetComponent<Image>();
		if (component == null || IsUnskinned(component.color))
		{
			return 0;
		}
		int num = 3;
		if (scrollbar.transition == Selectable.Transition.ColorTint)
		{
			num += 2;
		}
		if (image.material != null && image.material != Graphic.defaultGraphicMaterial)
		{
			num += 2;
		}
		if (scrollbar.GetComponentInParent<ScrollRect>() != null)
		{
			num++;
		}
		return num;
	}

	private static void Put<T>(IDictionary<string, T> index, string name, T value) where T : UnityEngine.Object
	{
		if (!(value == null) && !string.IsNullOrEmpty(name) && !index.ContainsKey(name))
		{
			index[name] = value;
		}
	}

	public static bool TryGetSprite(string name, out Sprite sprite)
	{
		sprite = null;
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		Harvest();
		if (Sprites.TryGetValue(name, out sprite) && sprite != null)
		{
			return true;
		}
		sprite = FromAtlas(name);
		return sprite != null;
	}

	private static Sprite FromAtlas(string name)
	{
		if (IsHeadless)
		{
			return null;
		}
		if (AtlasSprites.TryGetValue(name, out var value))
		{
			return value;
		}
		SpriteAtlas[] array;
		if (_atlases == null)
		{
			List<SpriteAtlas> list = new List<SpriteAtlas>();
			array = Resources.FindObjectsOfTypeAll<SpriteAtlas>();
			foreach (SpriteAtlas spriteAtlas in array)
			{
				if (!(spriteAtlas == null))
				{
					if (spriteAtlas.name == "UIAtlas" || spriteAtlas.name == "IconAtlas")
					{
						list.Insert(0, spriteAtlas);
					}
					else
					{
						list.Add(spriteAtlas);
					}
				}
			}
			_atlases = list.ToArray();
		}
		array = _atlases;
		foreach (SpriteAtlas spriteAtlas2 in array)
		{
			if (!(spriteAtlas2 == null))
			{
				Sprite sprite = spriteAtlas2.GetSprite(name);
				if (!(sprite == null))
				{
					sprite.name = name;
					AtlasSprites[name] = sprite;
					return sprite;
				}
			}
		}
		AtlasSprites[name] = null;
		return null;
	}

	public static Font Font(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Harvest();
		if (!Fonts.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public static TMP_FontAsset TmpFont(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Harvest();
		if (!TmpFonts.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}
}

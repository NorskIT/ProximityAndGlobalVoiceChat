using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

namespace UIManager;

internal static class GameAssets
{
	private static readonly Dictionary<string, Sprite> SpriteIndex = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, List<Sprite>> SpriteDuplicates = new Dictionary<string, List<Sprite>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Font> FontIndex = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, TMP_FontAsset> TmpFontIndex = new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Material> MaterialIndex = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, AudioClip> ClipIndex = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

	private static int _indexedFrame = -1;

	public static bool TrustedOnly;

	public static bool IsIndexed => _indexedFrame >= 0;

	public static int SpriteCount => SpriteIndex.Count;

	public static IEnumerable<string> SpriteNames => SpriteIndex.Keys;

	public static IEnumerable<string> FontNames => FontIndex.Keys;

	public static IEnumerable<string> TmpFontNames => TmpFontIndex.Keys;

	public static IEnumerable<Sprite> AllSprites => SpriteIndex.Values;

	public static void Index(bool force = false)
	{
		if (force || _indexedFrame != Time.frameCount)
		{
			SpriteIndex.Clear();
			SpriteDuplicates.Clear();
			FontIndex.Clear();
			TmpFontIndex.Clear();
			MaterialIndex.Clear();
			ClipIndex.Clear();
			Sprite[] array = Resources.FindObjectsOfTypeAll<Sprite>();
			for (int i = 0; i < array.Length; i++)
			{
				AddSprite(array[i]);
			}
			IndexAtlases();
			Font[] array2 = Resources.FindObjectsOfTypeAll<Font>();
			foreach (Font font in array2)
			{
				Put(FontIndex, (font != null) ? font.name : null, font);
			}
			TMP_FontAsset[] array3 = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			foreach (TMP_FontAsset tMP_FontAsset in array3)
			{
				Put(TmpFontIndex, (tMP_FontAsset != null) ? tMP_FontAsset.name : null, tMP_FontAsset);
			}
			Material[] array4 = Resources.FindObjectsOfTypeAll<Material>();
			foreach (Material material in array4)
			{
				Put(MaterialIndex, (material != null) ? material.name : null, material);
			}
			AudioClip[] array5 = Resources.FindObjectsOfTypeAll<AudioClip>();
			foreach (AudioClip audioClip in array5)
			{
				Put(ClipIndex, (audioClip != null) ? audioClip.name : null, audioClip);
			}
			_indexedFrame = Time.frameCount;
			UILog.Debug($"indexed {SpriteIndex.Count} sprites, {FontIndex.Count} fonts, {TmpFontIndex.Count} TMP fonts, {MaterialIndex.Count} materials, {ClipIndex.Count} clips");
		}
	}

	private static void IndexAtlases()
	{
		SpriteAtlas[] array = Resources.FindObjectsOfTypeAll<SpriteAtlas>();
		foreach (SpriteAtlas spriteAtlas in array)
		{
			if (spriteAtlas == null || spriteAtlas.spriteCount <= 0)
			{
				continue;
			}
			try
			{
				Sprite[] array2 = new Sprite[spriteAtlas.spriteCount];
				spriteAtlas.GetSprites(array2);
				Sprite[] array3 = array2;
				for (int j = 0; j < array3.Length; j++)
				{
					AddSprite(array3[j]);
				}
			}
			catch (Exception ex)
			{
				UILog.Debug("could not read atlas '" + spriteAtlas.name + "': " + ex.Message);
			}
		}
	}

	private static void AddSprite(Sprite sprite)
	{
		if (sprite == null)
		{
			return;
		}
		string text = sprite.name;
		if (text.EndsWith("(Clone)", StringComparison.Ordinal))
		{
			text = text.Substring(0, text.Length - 7);
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (!SpriteIndex.ContainsKey(text))
		{
			SpriteIndex[text] = sprite;
		}
		else if (!(SpriteIndex[text] == sprite))
		{
			if (!SpriteDuplicates.TryGetValue(text, out var value))
			{
				value = (SpriteDuplicates[text] = new List<Sprite>());
			}
			if (!value.Contains(sprite))
			{
				value.Add(sprite);
			}
		}
	}

	private static void Put<T>(IDictionary<string, T> index, string name, T value) where T : UnityEngine.Object
	{
		if (!(value == null) && !string.IsNullOrEmpty(name) && !index.ContainsKey(name))
		{
			index[name] = value;
		}
	}

	public static Sprite Sprite(string name)
	{
		TryGetSprite(name, out var sprite);
		return sprite;
	}

	public static bool TryGetSprite(string name, out Sprite sprite)
	{
		sprite = null;
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (VanillaUI.TryGetSprite(name, out sprite))
		{
			return true;
		}
		if (TrustedOnly)
		{
			return false;
		}
		if (!IsIndexed)
		{
			Index();
		}
		if (SpriteIndex.TryGetValue(name, out sprite) && sprite != null)
		{
			return true;
		}
		Index(force: true);
		if (SpriteIndex.TryGetValue(name, out sprite))
		{
			return sprite != null;
		}
		return false;
	}

	public static IList<Sprite> SpritesNamed(string name)
	{
		List<Sprite> list = new List<Sprite>();
		if (TryGetSprite(name, out var sprite))
		{
			list.Add(sprite);
		}
		if (SpriteDuplicates.TryGetValue(name, out var value))
		{
			list.AddRange(value);
		}
		return list;
	}

	public static Sprite FirstSprite(params string[] candidates)
	{
		for (int i = 0; i < candidates.Length; i++)
		{
			if (TryGetSprite(candidates[i], out var sprite))
			{
				return sprite;
			}
		}
		return null;
	}

	public static Font Font(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Font font = VanillaUI.Font(name);
		if (font != null)
		{
			return font;
		}
		if (TrustedOnly)
		{
			return null;
		}
		if (!IsIndexed)
		{
			Index();
		}
		if (FontIndex.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		Index(force: true);
		if (!FontIndex.TryGetValue(name, out value))
		{
			return null;
		}
		return value;
	}

	public static Font FirstFont(params string[] candidates)
	{
		for (int i = 0; i < candidates.Length; i++)
		{
			Font font = Font(candidates[i]);
			if (font != null)
			{
				return font;
			}
		}
		return null;
	}

	public static TMP_FontAsset TmpFont(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		TMP_FontAsset tMP_FontAsset = VanillaUI.TmpFont(name);
		if (tMP_FontAsset != null)
		{
			return tMP_FontAsset;
		}
		if (TrustedOnly)
		{
			return null;
		}
		if (!IsIndexed)
		{
			Index();
		}
		if (TmpFontIndex.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		Index(force: true);
		if (!TmpFontIndex.TryGetValue(name, out value))
		{
			return null;
		}
		return value;
	}

	public static TMP_FontAsset FirstTmpFont(params string[] candidates)
	{
		for (int i = 0; i < candidates.Length; i++)
		{
			TMP_FontAsset tMP_FontAsset = TmpFont(candidates[i]);
			if (tMP_FontAsset != null)
			{
				return tMP_FontAsset;
			}
		}
		return null;
	}

	public static TMP_FontAsset AnyTmpFont()
	{
		if (!IsIndexed)
		{
			Index();
		}
		foreach (TMP_FontAsset value in TmpFontIndex.Values)
		{
			if (value != null)
			{
				return value;
			}
		}
		return null;
	}

	public static Material Material(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (!IsIndexed)
		{
			Index();
		}
		if (!MaterialIndex.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public static AudioClip Clip(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (!IsIndexed)
		{
			Index();
		}
		if (!ClipIndex.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public static AudioClip FirstClip(params string[] candidates)
	{
		for (int i = 0; i < candidates.Length; i++)
		{
			AudioClip audioClip = Clip(candidates[i]);
			if (audioClip != null)
			{
				return audioClip;
			}
		}
		return null;
	}

	public static IEnumerable<string> Search(string term, int limit = 60)
	{
		if (!IsIndexed)
		{
			Index();
		}
		if (string.IsNullOrEmpty(term))
		{
			return SpriteIndex.Keys.OrderBy((string k) => k).Take(limit);
		}
		return (from k in SpriteIndex.Keys
			where k.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
			orderby k
			select k).Take(limit);
	}
}

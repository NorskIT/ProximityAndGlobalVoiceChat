using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace UIManager;

internal static class AssetDump
{
	private static bool _registered;

	public static string DefaultDirectory
	{
		get
		{
			try
			{
				return Path.Combine(Paths.PluginPath, "UIManager_StyleKit");
			}
			catch
			{
				return Path.Combine(Application.dataPath, "../UIManager_StyleKit");
			}
		}
	}

	public static string Dump(string filter = null, string directory = null)
	{
		directory = directory ?? DefaultDirectory;
		string text = Path.Combine(directory, "Sprites");
		Directory.CreateDirectory(text);
		GameAssets.Index(force: true);
		DumpManifest dumpManifest = new DumpManifest
		{
			generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
			unityVersion = Application.unityVersion
		};
		int num = 0;
		int num2 = 0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Sprite item in GameAssets.AllSprites.ToList())
		{
			if (item == null)
			{
				continue;
			}
			string name = item.name;
			if (!string.IsNullOrEmpty(filter) && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			string text2 = Sanitize(name);
			if (!hashSet.Add(text2))
			{
				continue;
			}
			try
			{
				Texture2D texture2D = Extract(item);
				if (texture2D == null)
				{
					num2++;
					continue;
				}
				string text3 = text2 + ".png";
				File.WriteAllBytes(Path.Combine(text, text3), texture2D.EncodeToPNG());
				UnityEngine.Object.DestroyImmediate(texture2D);
				dumpManifest.sprites.Add(new SpriteRecord
				{
					name = name,
					file = text3,
					texture = ((item.texture != null) ? item.texture.name : string.Empty),
					width = Mathf.RoundToInt(item.rect.width),
					height = Mathf.RoundToInt(item.rect.height),
					pixelsPerUnit = item.pixelsPerUnit,
					border = new float[4]
					{
						item.border.x,
						item.border.y,
						item.border.z,
						item.border.w
					},
					pivot = new float[2]
					{
						(item.rect.width > 0f) ? (item.pivot.x / item.rect.width) : 0.5f,
						(item.rect.height > 0f) ? (item.pivot.y / item.rect.height) : 0.5f
					}
				});
				num++;
			}
			catch (Exception ex)
			{
				num2++;
				UILog.Debug("could not dump sprite '" + name + "': " + ex.Message);
			}
		}
		dumpManifest.fonts.AddRange(GameAssets.FontNames.OrderBy((string n) => n));
		dumpManifest.tmpFonts.AddRange(GameAssets.TmpFontNames.OrderBy((string n) => n));
		File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonUtility.ToJson(dumpManifest, prettyPrint: true));
		File.WriteAllText(Path.Combine(directory, "README.txt"), Readme(dumpManifest));
		string text4 = $"dumped {num} sprites to {directory}" + ((num2 > 0) ? $" ({num2} skipped)" : string.Empty);
		UILog.Info(text4);
		return text4;
	}

	private static Texture2D Extract(Sprite sprite)
	{
		Texture2D texture = sprite.texture;
		if (texture == null)
		{
			return null;
		}
		Rect rect = sprite.textureRect;
		if (rect.width < 1f || rect.height < 1f)
		{
			rect = sprite.rect;
		}
		if (rect.width < 1f || rect.height < 1f)
		{
			return null;
		}
		RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		RenderTexture active = RenderTexture.active;
		try
		{
			Graphics.Blit(texture, temporary);
			RenderTexture.active = temporary;
			Texture2D texture2D = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, mipChain: false);
			texture2D.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
			texture2D.Apply();
			return texture2D;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
		}
	}

	private static string Sanitize(string name)
	{
		StringBuilder stringBuilder = new StringBuilder(name.Length);
		foreach (char c in name)
		{
			stringBuilder.Append((Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0) ? '_' : c);
		}
		return stringBuilder.ToString();
	}

	private static string Readme(DumpManifest manifest)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("UIManager style kit");
		stringBuilder.AppendLine("===================");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Generated " + manifest.generated + " from Valheim running Unity " + manifest.unityVersion + ".");
		stringBuilder.AppendLine($"{manifest.sprites.Count} sprites.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("How to use:");
		stringBuilder.AppendLine(" 1. Copy this whole folder into your Unity project.");
		stringBuilder.AppendLine(" 2. The UIManagerStyleKit editor script reads manifest.json and applies the");
		stringBuilder.AppendLine("    original 9-slice borders, pivots and pixels-per-unit to every PNG.");
		stringBuilder.AppendLine(" 3. Build your UI using these sprites. Keep the sprite ASSET names unchanged -");
		stringBuilder.AppendLine("    that name is what Styler matches on at runtime. GameObject names are yours");
		stringBuilder.AppendLine("    to prefix however you like.");
		stringBuilder.AppendLine(" 4. At runtime Styler.Apply swaps each one for the live game asset, so your UI");
		stringBuilder.AppendLine("    picks up the real atlas, material and any art changes from game patches.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Fonts available in game (use the same names on your Text components):");
		foreach (string font in manifest.fonts)
		{
			stringBuilder.AppendLine("  " + font);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("TextMeshPro fonts:");
		foreach (string tmpFont in manifest.tmpFonts)
		{
			stringBuilder.AppendLine("  " + tmpFont);
		}
		return stringBuilder.ToString();
	}

	private static void Command(string name, string description, Terminal.ConsoleEvent action)
	{
		if (Terminal.commands != null && Terminal.commands.ContainsKey(name))
		{
			UILog.Debug("console command '" + name + "' is already registered, leaving it alone");
		}
		else
		{
			new Terminal.ConsoleCommand(name, description, action);
		}
	}

	public static void RegisterConsoleCommands()
	{
		if (_registered)
		{
			return;
		}
		_registered = true;
		Command("uim_dump", "[filter] - dump Valheim UI sprites to a style kit folder", delegate(Terminal.ConsoleEventArgs args)
		{
			string filter = ((args.Length > 1) ? args[1] : null);
			args.Context.AddString(Dump(filter));
		});
		Command("uim_find", "<term> - list game sprites whose name contains term", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				args.Context.AddString("usage: uim_find <term>");
			}
			else
			{
				List<string> list = GameAssets.Search(args[1]).ToList();
				args.Context.AddString((list.Count == 0) ? ("no sprite matches " + args[1]) : string.Join("\n", list.ToArray()));
			}
		});
		Command("uim_overlap", "what your windows are covering, and how far to move", delegate(Terminal.ConsoleEventArgs args)
		{
			if (UIRoot.Front == null)
			{
				args.Context.AddString("the GUI is not up yet");
			}
			else
			{
				bool flag = false;
				foreach (Transform item in UIRoot.Front)
				{
					if (item.gameObject.activeInHierarchy)
					{
						flag = true;
						string text = UILayout.Report(item.gameObject);
						args.Context.AddString(text);
						UILog.Info(Environment.NewLine + text);
					}
				}
				if (!flag)
				{
					args.Context.AddString("no visible windows on the UIManager canvas");
				}
			}
		});
		Command("uim_fonts", "list the fonts the game has loaded", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Font: " + string.Join(", ", GameAssets.FontNames.OrderBy((string n) => n).ToArray()));
			args.Context.AddString("TMP: " + string.Join(", ", GameAssets.TmpFontNames.OrderBy((string n) => n).ToArray()));
		});
	}
}

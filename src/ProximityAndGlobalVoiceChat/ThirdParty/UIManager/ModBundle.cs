using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIManager;

internal class ModBundle : IDisposable
{
	private static readonly Dictionary<string, ModBundle> Cache = new Dictionary<string, ModBundle>(StringComparer.OrdinalIgnoreCase);

	public AssetBundle Bundle { get; private set; }

	public string Name { get; private set; }

	public IEnumerable<string> AssetNames
	{
		get
		{
			if (!(Bundle != null))
			{
				return Enumerable.Empty<string>();
			}
			return Bundle.GetAllAssetNames();
		}
	}

	private ModBundle(string name, AssetBundle bundle)
	{
		Name = name;
		Bundle = bundle;
	}

	public static ModBundle FromEmbeddedResource(string fileName, Assembly assembly = null)
	{
		if (Cache.TryGetValue(fileName, out var value) && value.Bundle != null)
		{
			return value;
		}
		assembly = assembly ?? Assembly.GetCallingAssembly();
		string[] manifestResourceNames = assembly.GetManifestResourceNames();
		string text = manifestResourceNames.FirstOrDefault((string n) => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
		if (text == null)
		{
			UILog.Error("no embedded resource ending in '" + fileName + "' in " + assembly.GetName().Name + ". Found: " + ((manifestResourceNames.Length == 0) ? "none" : string.Join(", ", manifestResourceNames)));
			return null;
		}
		byte[] binary;
		using (Stream stream = assembly.GetManifestResourceStream(text))
		{
			if (stream == null)
			{
				UILog.Error("could not open embedded resource '" + text + "'");
				return null;
			}
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			binary = memoryStream.ToArray();
		}
		AssetBundle assetBundle = AssetBundle.LoadFromMemory(binary);
		if (assetBundle == null)
		{
			UILog.Error("'" + text + "' is not a valid AssetBundle for this Unity version");
			return null;
		}
		ModBundle modBundle = new ModBundle(fileName, assetBundle);
		Cache[fileName] = modBundle;
		UILog.Debug($"loaded bundle '{fileName}' with {assetBundle.GetAllAssetNames().Length} assets");
		return modBundle;
	}

	public static ModBundle FromFile(string path)
	{
		if (!File.Exists(path))
		{
			UILog.Error("no bundle at '" + path + "'");
			return null;
		}
		string fileName = Path.GetFileName(path);
		if (Cache.TryGetValue(fileName, out var value) && value.Bundle != null)
		{
			return value;
		}
		AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
		if (assetBundle == null)
		{
			UILog.Error("'" + path + "' is not a valid AssetBundle");
			return null;
		}
		ModBundle modBundle = new ModBundle(fileName, assetBundle);
		Cache[fileName] = modBundle;
		return modBundle;
	}

	public T Load<T>(string assetName) where T : UnityEngine.Object
	{
		if (Bundle == null)
		{
			return null;
		}
		T val = Bundle.LoadAsset<T>(assetName);
		if (val == null)
		{
			UILog.Error("bundle '" + Name + "' has no " + typeof(T).Name + " called '" + assetName + "'");
		}
		return val;
	}

	public GameObject Prefab(string prefabName)
	{
		return Load<GameObject>(prefabName);
	}

	public GameObject Instantiate(string prefabName, Transform parent = null, bool style = true, StyleOptions options = null)
	{
		GameObject gameObject = Prefab(prefabName);
		if (!(gameObject == null))
		{
			return UIRoot.Instantiate(gameObject, parent, style, options);
		}
		return null;
	}

	public void Dispose()
	{
		if (!(Bundle == null))
		{
			Cache.Remove(Name);
			Bundle.Unload(unloadAllLoadedObjects: false);
			Bundle = null;
		}
	}
}

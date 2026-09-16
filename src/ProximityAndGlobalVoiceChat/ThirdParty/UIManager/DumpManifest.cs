using System;
using System.Collections.Generic;

namespace UIManager;

[Serializable]
internal class DumpManifest
{
	public string generated;

	public string unityVersion;

	public List<SpriteRecord> sprites = new List<SpriteRecord>();

	public List<string> fonts = new List<string>();

	public List<string> tmpFonts = new List<string>();

	public List<string> materials = new List<string>();
}

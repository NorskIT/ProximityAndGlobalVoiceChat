using System;

namespace UIManager;

[Serializable]
internal class SpriteRecord
{
	public string name;

	public string file;

	public string texture;

	public int width;

	public int height;

	public float pixelsPerUnit;

	public float[] border = new float[4];

	public float[] pivot = new float[2];
}

using UnityEngine;

namespace ProximityVoiceChat.UI;

internal static class VoiceSprites
{
	private const int Size = 128;

	private const int Samples = 3;

	private const int MaxAlpha = 255;

	private const float PixelsPerUnit = 100f;

	private const int QuietWaves = 1;

	private const int TalkingWaves = 3;

	private const float Middle = 0.5f;

	private const float ConeMouth = 0.5f;

	private const float BodyLeft = 0.16f;

	private const float BodyRight = 0.3f;

	private const float BodyHalfHeight = 0.11f;

	private const float ConeFlare = 0.2f;

	private const float SlashHalfWidth = 0.035f;

	private const float SlashGap = 0.075f;

	private const float WaveArcDegrees = 52f;

	private const float WaveInner = 0.1f;

	private const float WaveSpacing = 0.1f;

	private const float WaveThickness = 0.045f;

	private static readonly Vector2 SlashStart = new Vector2(0.17f, 0.2f);

	private static readonly Vector2 SlashEnd = new Vector2(0.83f, 0.8f);

	private static bool _built;

	internal static Sprite? Speaker { get; private set; }

	internal static Sprite? SpeakerTalking { get; private set; }

	internal static Sprite? Muted { get; private set; }

	internal static void Resolve()
	{
		if (!_built)
		{
			_built = true;
			Speaker = Build(1, slash: false);
			SpeakerTalking = Build(3, slash: false);
			Muted = Build(0, slash: true);
		}
	}

	private static Sprite Build(int waves, bool slash)
	{
		Texture2D texture2D = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false)
		{
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp,
			hideFlags = HideFlags.HideAndDontSave
		};
		Color32[] array = new Color32[16384];
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num = 0f;
				for (int k = 0; k < 3; k++)
				{
					for (int l = 0; l < 3; l++)
					{
						float u = ((float)j + ((float)l + 0.5f) / 3f) / 128f;
						float v = ((float)i + ((float)k + 0.5f) / 3f) / 128f;
						if (Inside(u, v, waves, slash))
						{
							num++;
						}
					}
				}
				num /= 9f;
				byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(num * 255f), 0, 255);
				array[i * 128 + j] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, a);
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
		Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f), 100f);
		sprite.hideFlags = HideFlags.HideAndDontSave;
		return sprite;
	}

	private static bool Inside(float u, float v, int waves, bool slash)
	{
		if (slash)
		{
			float num = DistanceToSlash(u, v);
			if (num <= 0.035f)
			{
				return true;
			}
			if (num <= 0.075f)
			{
				return false;
			}
		}
		float num2 = Mathf.Abs(v - 0.5f);
		if (u >= 0.16f && u <= 0.3f && num2 <= 0.11f)
		{
			return true;
		}
		if (u > 0.3f && u <= 0.5f && num2 <= 0.11f + (u - 0.3f) / 0.19999999f * 0.2f)
		{
			return true;
		}
		if (waves <= 0)
		{
			return false;
		}
		float num3 = u - 0.5f;
		float num4 = v - 0.5f;
		float num5 = Mathf.Sqrt(num3 * num3 + num4 * num4);
		if (num3 <= 0f)
		{
			return false;
		}
		if (Mathf.Abs(Mathf.Atan2(num4, num3)) * 57.29578f > 52f)
		{
			return false;
		}
		for (int i = 0; i < waves; i++)
		{
			float num6 = 0.1f + (float)i * 0.1f;
			if (num5 >= num6 && num5 <= num6 + 0.045f)
			{
				return true;
			}
		}
		return false;
	}

	private static float DistanceToSlash(float u, float v)
	{
		Vector2 vector = new Vector2(u, v);
		Vector2 vector2 = SlashEnd - SlashStart;
		float num = Mathf.Clamp01(Vector2.Dot(vector - SlashStart, vector2) / Vector2.Dot(vector2, vector2));
		return Vector2.Distance(vector, SlashStart + vector2 * num);
	}
}

using UnityEngine.UI;

namespace ProximityVoiceChat.UI;

internal sealed class SliderRow
{
	internal Slider Slider;

	internal Text Value;

	internal string Format = "0.00";

	internal void Set(float value)
	{
		Slider.SetValueWithoutNotify(value);
		Value.text = value.ToString(Format);
	}
}

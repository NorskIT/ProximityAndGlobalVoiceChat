using UnityEngine;

namespace UIManager;

internal class UIRootHost : MonoBehaviour
{
	private void Update()
	{
		InputBlocker.Tick();
		UIPanels.Tick();
	}
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal class UIWindow : MonoBehaviour
{
	public bool BlocksInput = true;

	public bool ClosesOnEscape = true;

	public bool FitToScreen = true;

	private bool _blocking;

	private Vector2 _fittedFor;

	public bool IsOpen => base.gameObject.activeSelf;

	public event Action Opened;

	public event Action Closed;

	public static UIWindow Attach(GameObject root, string dragHandlePath = null, string closeButtonPath = null)
	{
		if (root == null)
		{
			return null;
		}
		UIWindow uIWindow = root.GetComponent<UIWindow>() ?? root.AddComponent<UIWindow>();
		GameObject gameObject = (string.IsNullOrEmpty(dragHandlePath) ? root : UIExtensions.Find(root, dragHandlePath));
		if (gameObject != null)
		{
			DragHandle.Attach(gameObject, root.transform as RectTransform);
		}
		if (!string.IsNullOrEmpty(closeButtonPath))
		{
			Button button = root.Get<Button>(closeButtonPath);
			if (button != null)
			{
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(uIWindow.Hide);
			}
		}
		return uIWindow;
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
		base.transform.SetAsLastSibling();
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	private void OnEnable()
	{
		if (BlocksInput && !_blocking)
		{
			_blocking = true;
			InputBlocker.Block(state: true);
		}
		FitNow(force: true);
		Opened?.Invoke();
	}

	private void OnDisable()
	{
		if (_blocking)
		{
			_blocking = false;
			InputBlocker.Block(state: false);
		}
		Closed?.Invoke();
	}

	private void Update()
	{
		if (ClosesOnEscape && Input.GetKeyDown(KeyCode.Escape))
		{
			Hide();
		}
		if (FitToScreen)
		{
			FitNow();
		}
	}

	public void FitNow(bool force = false)
	{
		if (!FitToScreen)
		{
			return;
		}
		Vector2 vector = new Vector2(Screen.width, Screen.height);
		if (!force && vector == _fittedFor)
		{
			return;
		}
		_fittedFor = vector;
		bool flag = false;
		foreach (Transform item in base.transform)
		{
			if (item.gameObject.activeInHierarchy && item is RectTransform panel && UILayout.TryVisibleRect(item.gameObject, out var _))
			{
				UILayout.Fit(panel);
				flag = true;
			}
		}
		if (!flag && base.transform is RectTransform panel2)
		{
			UILayout.Fit(panel2);
		}
	}
}

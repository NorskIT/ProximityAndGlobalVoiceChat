using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal class UITabs
{
	private class Tab
	{
		public GameObject Root;

		public Image Selected;

		public Image Hover;

		public Action Chosen;

		public bool Enabled = true;
	}

	private readonly List<Tab> _tabs = new List<Tab>();

	public Color SelectedColor = GameColors.Yellow;

	public Color HoverColor = new Color(1f, 0.889f, 0f, 0.45f);

	public int Index { get; private set; } = -1;

	public int Count => _tabs.Count;

	public event Action<int> Changed;

	public UITabs Add(GameObject tab, Action chosen, Image selected = null, Image hover = null, bool enabled = true)
	{
		if (tab == null)
		{
			return this;
		}
		Tab entry = new Tab
		{
			Root = tab,
			Selected = selected,
			Hover = hover,
			Chosen = chosen,
			Enabled = enabled
		};
		_tabs.Add(entry);
		int index = _tabs.Count - 1;
		if (entry.Selected != null)
		{
			Skin.Highlight(entry.Selected, SelectedColor);
			entry.Selected.enabled = false;
		}
		if (entry.Hover != null)
		{
			Skin.Highlight(entry.Hover, HoverColor);
			entry.Hover.enabled = false;
			UIEvents.OnHover(tab, delegate
			{
				if (entry.Enabled)
				{
					entry.Hover.enabled = true;
				}
			}, delegate
			{
				entry.Hover.enabled = false;
			});
		}
		Button component = tab.GetComponent<Button>();
		if (component != null)
		{
			component.interactable = enabled;
			component.onClick.RemoveAllListeners();
			component.onClick.AddListener(delegate
			{
				Select(index);
			});
			Skin.Sfx(component);
		}
		else
		{
			UIEvents.OnLeftClick(tab, delegate
			{
				Select(index);
			});
		}
		return this;
	}

	public void SetEnabled(int index, bool enabled)
	{
		if (index >= 0 && index < _tabs.Count)
		{
			Tab tab = _tabs[index];
			tab.Enabled = enabled;
			Button button = ((tab.Root != null) ? tab.Root.GetComponent<Button>() : null);
			if (button != null)
			{
				button.interactable = enabled;
			}
			if (!enabled && tab.Hover != null)
			{
				tab.Hover.enabled = false;
			}
		}
	}

	public void SelectSilent(int index)
	{
		if (index >= 0 && index < _tabs.Count && _tabs[index].Enabled)
		{
			Index = index;
			Paint();
		}
	}

	public void Select(int index)
	{
		if (index >= 0 && index < _tabs.Count && _tabs[index].Enabled && index != Index)
		{
			Index = index;
			Paint();
			_tabs[index].Chosen?.Invoke();
			Changed?.Invoke(index);
		}
	}

	public void SelectFirstEnabled()
	{
		for (int i = 0; i < _tabs.Count; i++)
		{
			if (_tabs[i].Enabled)
			{
				Select(i);
				break;
			}
		}
	}

	public void Clear()
	{
		_tabs.Clear();
		Index = -1;
	}

	private void Paint()
	{
		for (int i = 0; i < _tabs.Count; i++)
		{
			Image selected = _tabs[i].Selected;
			if (selected != null)
			{
				selected.enabled = i == Index;
			}
		}
	}
}

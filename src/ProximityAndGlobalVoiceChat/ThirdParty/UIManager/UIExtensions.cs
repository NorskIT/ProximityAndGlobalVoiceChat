using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal static class UIExtensions
{
	public static GameObject Find(GameObject root, string path)
	{
		if (root == null || string.IsNullOrEmpty(path))
		{
			return null;
		}
		if (path.IndexOf('/') >= 0)
		{
			Transform transform = root.transform.Find(path);
			if (transform != null)
			{
				return transform.gameObject;
			}
			string[] array = path.Split(new char[1] { '/' });
			return root.transform.FindDeep(array[array.Length - 1]);
		}
		return root.transform.FindDeep(path);
	}

	public static GameObject FindDeep(this Transform root, string name)
	{
		if (root == null || string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase))
		{
			return root.gameObject;
		}
		for (int i = 0; i < root.childCount; i++)
		{
			GameObject gameObject = root.GetChild(i).FindDeep(name);
			if (gameObject != null)
			{
				return gameObject;
			}
		}
		return null;
	}

	public static GameObject FindDeep(this GameObject root, string name)
	{
		if (!(root != null))
		{
			return null;
		}
		return root.transform.FindDeep(name);
	}

	public static string PathOf(Transform transform)
	{
		if (transform == null)
		{
			return string.Empty;
		}
		string text = transform.name;
		Transform parent = transform.parent;
		while (parent != null)
		{
			text = parent.name + "/" + text;
			parent = parent.parent;
		}
		return text;
	}

	public static void Collect(Transform root, List<Transform> into)
	{
		if (!(root == null) && into != null)
		{
			into.Add(root);
			for (int i = 0; i < root.childCount; i++)
			{
				Collect(root.GetChild(i), into);
			}
		}
	}

	public static T Get<T>(this GameObject root, string path) where T : Component
	{
		GameObject gameObject = Find(root, path);
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.GetComponent<T>();
	}

	public static GameObject SetText(this GameObject root, string path, string value)
	{
		GameObject gameObject = Find(root, path);
		if (gameObject == null)
		{
			return root;
		}
		Text component = gameObject.GetComponent<Text>();
		if (component != null)
		{
			component.text = value;
		}
		TMP_Text component2 = gameObject.GetComponent<TMP_Text>();
		if (component2 != null)
		{
			component2.text = value;
		}
		return root;
	}

	public static GameObject SetSprite(this GameObject root, string path, Sprite sprite)
	{
		Image image = root.Get<Image>(path);
		if (image != null)
		{
			image.sprite = sprite;
			image.enabled = sprite != null;
		}
		return root;
	}

	public static GameObject OnClick(this GameObject root, string path, Action action)
	{
		Button button = root.Get<Button>(path);
		if (button != null && action != null)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(delegate
			{
				action();
			});
		}
		return root;
	}

	public static GameObject CloneTemplate(GameObject template, Transform parent = null, bool active = true)
	{
		if (template == null)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(template, (parent != null) ? parent : template.transform.parent, worldPositionStays: false);
		gameObject.name = template.name + "_clone";
		gameObject.SetActive(active);
		return gameObject;
	}

	public static StyleReport Style(this GameObject root, StyleOptions options = null)
	{
		return Styler.Apply(root, options);
	}
}

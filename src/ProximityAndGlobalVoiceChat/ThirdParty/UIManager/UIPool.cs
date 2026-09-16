using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIManager;

internal class UIPool<T>(GameObject template, Transform parent, Func<GameObject, T> bind) where T : class
{
	private readonly Transform _parent = ((parent != null) ? parent : ((template != null) ? template.transform.parent : null));

	private readonly List<GameObject> _objects = new List<GameObject>();

	private readonly List<T> _views = new List<T>();

	private int _cursor;

	public int Count => _cursor;

	public IReadOnlyList<T> Views => _views;

	public GameObject ObjectAt(int index)
	{
		if (index < 0 || index >= _objects.Count)
		{
			return null;
		}
		return _objects[index];
	}

	public T ViewAt(int index)
	{
		if (index < 0 || index >= _views.Count)
		{
			return null;
		}
		return _views[index];
	}

	public void Begin()
	{
		_cursor = 0;
	}

	public T Take()
	{
		GameObject clone;
		return Take(out clone);
	}

	public T Take(out GameObject clone)
	{
		clone = null;
		if (template == null)
		{
			return null;
		}
		if (_cursor < _objects.Count)
		{
			clone = _objects[_cursor];
			if (clone == null)
			{
				clone = Grow(_cursor);
				if (clone == null)
				{
					return null;
				}
			}
			if (!clone.activeSelf)
			{
				clone.SetActive(value: true);
			}
			return _views[_cursor++];
		}
		clone = Grow(-1);
		if (clone == null)
		{
			return null;
		}
		return _views[_cursor++];
	}

	public void End()
	{
		for (int i = _cursor; i < _objects.Count; i++)
		{
			GameObject gameObject = _objects[i];
			if (gameObject != null && gameObject.activeSelf)
			{
				gameObject.SetActive(value: false);
			}
		}
	}

	public void Clear()
	{
		foreach (GameObject @object in _objects)
		{
			if (@object != null)
			{
				UnityEngine.Object.Destroy(@object);
			}
		}
		_objects.Clear();
		_views.Clear();
		_cursor = 0;
	}

	private GameObject Grow(int replaceAt)
	{
		GameObject gameObject = UIExtensions.CloneTemplate(template, _parent);
		if (gameObject == null)
		{
			return null;
		}
		T val = ((bind != null) ? bind(gameObject) : (gameObject as T));
		if (replaceAt >= 0)
		{
			_objects[replaceAt] = gameObject;
			_views[replaceAt] = val;
		}
		else
		{
			_objects.Add(gameObject);
			_views.Add(val);
		}
		return gameObject;
	}
}
internal class UIPool : UIPool<GameObject>
{
	public UIPool(GameObject template, Transform parent = null)
		: base(template, parent, (Func<GameObject, GameObject>)((GameObject clone) => clone))
	{
	}
}

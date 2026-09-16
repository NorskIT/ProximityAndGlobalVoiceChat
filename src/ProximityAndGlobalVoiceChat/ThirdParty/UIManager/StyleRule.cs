using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UIManager;

internal class StyleRule
{
	public Func<GameObject, bool> Match;

	public ElementType Element;

	public bool Recursive;

	public static StyleRule Named(string pattern, ElementType element, bool recursive = false)
	{
		Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		return new StyleRule
		{
			Match = (GameObject go) => go != null && regex.IsMatch(go.name),
			Element = element,
			Recursive = recursive
		};
	}

	public static StyleRule Path(string path, ElementType element, bool recursive = false)
	{
		return new StyleRule
		{
			Match = (GameObject go) => go != null && UIExtensions.PathOf(go.transform).EndsWith(path, StringComparison.OrdinalIgnoreCase),
			Element = element,
			Recursive = recursive
		};
	}

	public static StyleRule Where(Func<GameObject, bool> predicate, ElementType element, bool recursive = false)
	{
		return new StyleRule
		{
			Match = predicate,
			Element = element,
			Recursive = recursive
		};
	}
}

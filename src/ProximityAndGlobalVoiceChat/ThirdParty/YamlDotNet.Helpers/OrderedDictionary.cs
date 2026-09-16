using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace YamlDotNet.Helpers;

[Serializable]
internal sealed class OrderedDictionary<TKey, TValue> : IOrderedDictionary<TKey, TValue>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : notnull
{
	private class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, IEnumerable
	{
		private readonly OrderedDictionary<TKey, TValue> orderedDictionary;

		public int Count => orderedDictionary.list.Count;

		public bool IsReadOnly => true;

		public void Add(TKey item)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(TKey item)
		{
			return orderedDictionary.dictionary.ContainsKey(item);
		}

		public KeyCollection(OrderedDictionary<TKey, TValue> orderedDictionary)
		{
			this.orderedDictionary = orderedDictionary;
		}

		public void CopyTo(TKey[] array, int arrayIndex)
		{
			for (int i = 0; i < orderedDictionary.list.Count; i++)
			{
				array[i] = orderedDictionary.list[i + arrayIndex].Key;
			}
		}

		public IEnumerator<TKey> GetEnumerator()
		{
			return orderedDictionary.list.Select((KeyValuePair<TKey, TValue> kvp) => kvp.Key).GetEnumerator();
		}

		public bool Remove(TKey item)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable
	{
		private readonly OrderedDictionary<TKey, TValue> orderedDictionary;

		public int Count => orderedDictionary.list.Count;

		public bool IsReadOnly => true;

		public void Add(TValue item)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(TValue item)
		{
			return orderedDictionary.dictionary.ContainsValue(item);
		}

		public ValueCollection(OrderedDictionary<TKey, TValue> orderedDictionary)
		{
			this.orderedDictionary = orderedDictionary;
		}

		public void CopyTo(TValue[] array, int arrayIndex)
		{
			for (int i = 0; i < orderedDictionary.list.Count; i++)
			{
				array[i] = orderedDictionary.list[i + arrayIndex].Value;
			}
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return orderedDictionary.list.Select((KeyValuePair<TKey, TValue> kvp) => kvp.Value).GetEnumerator();
		}

		public bool Remove(TValue item)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	[NonSerialized]
	private Dictionary<TKey, TValue> dictionary;

	private readonly List<KeyValuePair<TKey, TValue>> list;

	private readonly IEqualityComparer<TKey> comparer;

	public TValue this[TKey key]
	{
		get
		{
			return dictionary[key];
		}
		set
		{
			if (dictionary.ContainsKey(key))
			{
				int index = list.FindIndex((KeyValuePair<TKey, TValue> kvp) => comparer.Equals(kvp.Key, key));
				dictionary[key] = value;
				list[index] = new KeyValuePair<TKey, TValue>(key, value);
			}
			else
			{
				Add(key, value);
			}
		}
	}

	public ICollection<TKey> Keys => new KeyCollection(this);

	public ICollection<TValue> Values => new ValueCollection(this);

	public int Count => dictionary.Count;

	public bool IsReadOnly => false;

	public KeyValuePair<TKey, TValue> this[int index]
	{
		get
		{
			return list[index];
		}
		set
		{
			list[index] = value;
		}
	}

	public OrderedDictionary()
		: this((IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
	{
	}

	public OrderedDictionary(IEqualityComparer<TKey> comparer)
	{
		list = new List<KeyValuePair<TKey, TValue>>();
		dictionary = new Dictionary<TKey, TValue>(comparer);
		this.comparer = comparer;
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		if (!TryAdd(item))
		{
			ThrowDuplicateKeyException(item.Key);
		}
	}

	public void Add(TKey key, TValue value)
	{
		if (!TryAdd(key, value))
		{
			ThrowDuplicateKeyException(key);
		}
	}

	private static void ThrowDuplicateKeyException(TKey key)
	{
		throw new ArgumentException($"An item with the same key {key} has already been added.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryAdd(TKey key, TValue value)
	{
		if (DictionaryExtensions.TryAdd(dictionary, key, value))
		{
			list.Add(new KeyValuePair<TKey, TValue>(key, value));
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryAdd(KeyValuePair<TKey, TValue> item)
	{
		if (DictionaryExtensions.TryAdd(dictionary, item.Key, item.Value))
		{
			list.Add(item);
			return true;
		}
		return false;
	}

	public void Clear()
	{
		dictionary.Clear();
		list.Clear();
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return dictionary.Contains(item);
	}

	public bool ContainsKey(TKey key)
	{
		return dictionary.ContainsKey(key);
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		list.CopyTo(array, arrayIndex);
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return list.GetEnumerator();
	}

	public void Insert(int index, TKey key, TValue value)
	{
		dictionary.Add(key, value);
		list.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
	}

	public bool Remove(TKey key)
	{
		if (dictionary.ContainsKey(key))
		{
			int index = list.FindIndex((KeyValuePair<TKey, TValue> kvp) => comparer.Equals(kvp.Key, key));
			list.RemoveAt(index);
			if (!dictionary.Remove(key))
			{
				throw new InvalidOperationException();
			}
			return true;
		}
		return false;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		return Remove(item.Key);
	}

	public void RemoveAt(int index)
	{
		TKey key = list[index].Key;
		dictionary.Remove(key);
		list.RemoveAt(index);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		return dictionary.TryGetValue(key, out value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return list.GetEnumerator();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		dictionary = new Dictionary<TKey, TValue>();
		foreach (KeyValuePair<TKey, TValue> item in list)
		{
			dictionary[item.Key] = item.Value;
		}
	}
}

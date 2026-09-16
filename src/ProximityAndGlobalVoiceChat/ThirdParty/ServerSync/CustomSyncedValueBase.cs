using System;

namespace ServerSync;

internal abstract class CustomSyncedValueBase
{
	public object? LocalBaseValue;

	public readonly string Identifier;

	public readonly Type Type;

	private object? boxedValue;

	protected bool localIsOwner;

	public readonly int Priority;

	public object? BoxedValue
	{
		get
		{
			return boxedValue;
		}
		set
		{
			boxedValue = value;
			ValueChanged?.Invoke();
		}
	}

	public event Action? ValueChanged;

	protected CustomSyncedValueBase(ConfigSync configSync, string identifier, Type type, int priority)
	{
		Priority = priority;
		Identifier = identifier;
		Type = type;
		configSync.AddCustomValue(this);
		localIsOwner = configSync.IsSourceOfTruth;
		configSync.SourceOfTruthChanged += delegate(bool truth)
		{
			localIsOwner = truth;
		};
	}
}

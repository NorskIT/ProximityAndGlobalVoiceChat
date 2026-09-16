using JetBrains.Annotations;

namespace ServerSync;

[PublicAPI]
internal sealed class CustomSyncedValue<T> : CustomSyncedValueBase
{
	public T Value
	{
		get
		{
			return (T)base.BoxedValue;
		}
		set
		{
			base.BoxedValue = value;
		}
	}

	public CustomSyncedValue(ConfigSync configSync, string identifier, T value = default(T), int priority = 0)
		: base(configSync, identifier, typeof(T), priority)
	{
		Value = value;
	}

	public void AssignLocalValue(T value)
	{
		if (localIsOwner)
		{
			Value = value;
		}
		else
		{
			LocalBaseValue = value;
		}
	}
}

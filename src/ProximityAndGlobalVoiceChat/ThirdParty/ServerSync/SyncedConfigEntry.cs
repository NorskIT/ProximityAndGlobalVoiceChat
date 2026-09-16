using BepInEx.Configuration;
using JetBrains.Annotations;

namespace ServerSync;

[PublicAPI]
internal class SyncedConfigEntry<T>(ConfigEntry<T> sourceConfig) : OwnConfigEntryBase
{
	public readonly ConfigEntry<T> SourceConfig = sourceConfig;

	public override ConfigEntryBase BaseConfig => SourceConfig;

	public T Value
	{
		get
		{
			return SourceConfig.Value;
		}
		set
		{
			SourceConfig.Value = value;
		}
	}

	public void AssignLocalValue(T value)
	{
		if (LocalBaseValue == null)
		{
			Value = value;
		}
		else
		{
			LocalBaseValue = value;
		}
	}
}

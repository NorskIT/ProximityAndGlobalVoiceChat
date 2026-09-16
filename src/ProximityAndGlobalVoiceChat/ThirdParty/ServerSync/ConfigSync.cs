using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ServerSync;

[PublicAPI]
internal class ConfigSync
{
	[HarmonyPatch(typeof(ZRpc), "HandlePackage")]
	private static class SnatchCurrentlyHandlingRPC
	{
		public static ZRpc? currentRpc;

		[HarmonyPrefix]
		private static void Prefix(ZRpc __instance)
		{
			currentRpc = __instance;
		}
	}

	[HarmonyPatch(typeof(ZNet), "Awake")]
	internal static class RegisterRPCPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance)
		{
			isServer = __instance.IsServer();
			foreach (ConfigSync configSync2 in configSyncs)
			{
				ZRoutedRpc.instance.Register<ZPackage>(configSync2.Name + " ConfigSync", configSync2.RPC_FromOtherClientConfigSync);
				if (isServer)
				{
					configSync2.InitialSyncDone = true;
					Debug.Log("Registered '" + configSync2.Name + " ConfigSync' RPC - waiting for incoming connections");
				}
			}
			if (isServer)
			{
				__instance.StartCoroutine(WatchAdminListChanges());
			}
			static void SendAdmin(List<ZNetPeer> peers, bool isAdmin)
			{
				ZPackage package = ConfigsToPackage(null, null, new PackageEntry[1]
				{
					new PackageEntry
					{
						section = "Internal",
						key = "lockexempt",
						type = typeof(bool),
						value = isAdmin
					}
				});
				ConfigSync configSync = configSyncs.First();
				if (configSync != null)
				{
					ZNet.instance.StartCoroutine(configSync.sendZPackage(peers, package));
				}
			}
			static IEnumerator WatchAdminListChanges()
			{
				MethodInfo listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId");
				SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
				List<string> CurrentList = new List<string>(adminList.GetList());
				while (true)
				{
					yield return new WaitForSeconds(30f);
					if (!adminList.GetList().SequenceEqual<string>(CurrentList))
					{
						CurrentList = new List<string>(adminList.GetList());
						List<ZNetPeer> adminPeer = ZNet.instance.GetPeers().Where(delegate(ZNetPeer p)
						{
							string hostName = p.m_rpc.GetSocket().GetHostName();
							return ((object)listContainsId == null) ? adminList.Contains(hostName) : ((bool)listContainsId.Invoke(ZNet.instance, new object[2] { adminList, hostName }));
						}).ToList();
						List<ZNetPeer> nonAdminPeer = ZNet.instance.GetPeers().Except<ZNetPeer>(adminPeer).ToList();
						SendAdmin(nonAdminPeer, isAdmin: false);
						SendAdmin(adminPeer, isAdmin: true);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
	private static class RegisterClientRPCPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance, ZNetPeer peer)
		{
			if (__instance.IsServer())
			{
				return;
			}
			foreach (ConfigSync configSync in configSyncs)
			{
				peer.m_rpc.Register<ZPackage>(configSync.Name + " ConfigSync", configSync.RPC_FromServerConfigSync);
			}
		}
	}

	private class ParsedConfigs
	{
		public readonly Dictionary<OwnConfigEntryBase, object?> configValues = new Dictionary<OwnConfigEntryBase, object>();

		public readonly Dictionary<CustomSyncedValueBase, object?> customValues = new Dictionary<CustomSyncedValueBase, object>();
	}

	[HarmonyPatch(typeof(ZNet), "Shutdown")]
	private class ResetConfigsOnShutdown
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			ProcessingServerUpdate = true;
			foreach (ConfigSync configSync in configSyncs)
			{
				configSync.resetConfigsFromServer();
				configSync.IsSourceOfTruth = true;
				configSync.InitialSyncDone = false;
			}
			ProcessingServerUpdate = false;
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
	private class SendConfigsAfterLogin
	{
		private class BufferingSocket(ISocket original) : ZPlayFabSocket, ISocket
		{
			public volatile bool finished = false;

			public volatile int versionMatchQueued = -1;

			public readonly List<ZPackage> Package = new List<ZPackage>();

			public readonly ISocket Original = original;

			public new bool IsConnected()
			{
				return Original.IsConnected();
			}

			public new ZPackage Recv()
			{
				return Original.Recv();
			}

			public new int GetSendQueueSize()
			{
				return Original.GetSendQueueSize();
			}

			public new int GetCurrentSendRate()
			{
				return Original.GetCurrentSendRate();
			}

			public new bool IsHost()
			{
				return Original.IsHost();
			}

			public new void Dispose()
			{
				Original.Dispose();
			}

			public new bool GotNewData()
			{
				return Original.GotNewData();
			}

			public new void Close()
			{
				Original.Close();
			}

			public new string GetEndPointString()
			{
				return Original.GetEndPointString();
			}

			public new void GetAndResetStats(out int totalSent, out int totalRecv)
			{
				Original.GetAndResetStats(out totalSent, out totalRecv);
			}

			public new void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
			{
				Original.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
			}

			public new ISocket Accept()
			{
				return Original.Accept();
			}

			public new int GetHostPort()
			{
				return Original.GetHostPort();
			}

			public new bool Flush()
			{
				return Original.Flush();
			}

			public new string GetHostName()
			{
				return Original.GetHostName();
			}

			public new void VersionMatch()
			{
				if (finished)
				{
					Original.VersionMatch();
				}
				else
				{
					versionMatchQueued = Package.Count;
				}
			}

			public new void Send(ZPackage pkg)
			{
				int pos = pkg.GetPos();
				pkg.SetPos(0);
				int num = pkg.ReadInt();
				if ((num == "PeerInfo".GetStableHashCode() || num == "RoutedRPC".GetStableHashCode() || num == "ZDOData".GetStableHashCode()) && !finished)
				{
					ZPackage zPackage = new ZPackage(pkg.GetArray());
					zPackage.SetPos(pos);
					Package.Add(zPackage);
				}
				else
				{
					pkg.SetPos(pos);
					Original.Send(pkg);
				}
			}
		}

		[HarmonyPriority(800)]
		[HarmonyPrefix]
		private static void Prefix(ref Dictionary<Assembly, BufferingSocket>? __state, ZNet __instance, ZRpc rpc)
		{
			if (!__instance.IsServer())
			{
				return;
			}
			BufferingSocket bufferingSocket = new BufferingSocket(rpc.GetSocket());
			AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket);
			if (AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new Type[1] { typeof(ZRpc) }).Invoke(__instance, new object[1] { rpc }) is ZNetPeer obj && ZNet.m_onlineBackend != OnlineBackendType.Steamworks)
			{
				FieldInfo fieldInfo = AccessTools.DeclaredField(typeof(ZNetPeer), "m_socket");
				if (fieldInfo.GetValue(obj) is ZPlayFabSocket zPlayFabSocket)
				{
					typeof(ZPlayFabSocket).GetField("m_remotePlayerId").SetValue(bufferingSocket, zPlayFabSocket.m_remotePlayerId);
				}
				fieldInfo.SetValue(obj, bufferingSocket);
			}
			if (__state == null)
			{
				__state = new Dictionary<Assembly, BufferingSocket>();
			}
			__state[Assembly.GetExecutingAssembly()] = bufferingSocket;
		}

		[HarmonyPostfix]
		private static void Postfix(Dictionary<Assembly, BufferingSocket> __state, ZNet __instance, ZRpc rpc)
		{
			ZNetPeer peer;
			if (__instance.IsServer())
			{
				object obj = AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new Type[1] { typeof(ZRpc) }).Invoke(__instance, new object[1] { rpc });
				peer = obj as ZNetPeer;
				if (peer == null)
				{
					SendBufferedData();
				}
				else
				{
					__instance.StartCoroutine(sendAsync());
				}
			}
			void SendBufferedData()
			{
				if (rpc.GetSocket() is BufferingSocket bufferingSocket)
				{
					AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket.Original);
					if (AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new Type[1] { typeof(ZRpc) }).Invoke(__instance, new object[1] { rpc }) is ZNetPeer obj2)
					{
						AccessTools.DeclaredField(typeof(ZNetPeer), "m_socket").SetValue(obj2, bufferingSocket.Original);
					}
				}
				BufferingSocket bufferingSocket2 = __state[Assembly.GetExecutingAssembly()];
				bufferingSocket2.finished = true;
				for (int i = 0; i < bufferingSocket2.Package.Count; i++)
				{
					if (i == bufferingSocket2.versionMatchQueued)
					{
						bufferingSocket2.Original.VersionMatch();
					}
					bufferingSocket2.Original.Send(bufferingSocket2.Package[i]);
				}
				if (bufferingSocket2.Package.Count == bufferingSocket2.versionMatchQueued)
				{
					bufferingSocket2.Original.VersionMatch();
				}
			}
			IEnumerator sendAsync()
			{
				foreach (ConfigSync configSync in configSyncs)
				{
					List<PackageEntry> entries = new List<PackageEntry>();
					if (configSync.CurrentVersion != null)
					{
						entries.Add(new PackageEntry
						{
							section = "Internal",
							key = "serverversion",
							type = typeof(string),
							value = configSync.CurrentVersion
						});
					}
					MethodInfo listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId");
					SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
					entries.Add(new PackageEntry
					{
						section = "Internal",
						key = "lockexempt",
						type = typeof(bool),
						value = (((object)listContainsId == null) ? ((object)adminList.Contains(rpc.GetSocket().GetHostName())) : listContainsId.Invoke(ZNet.instance, new object[2]
						{
							adminList,
							rpc.GetSocket().GetHostName()
						}))
					});
					ZPackage package = ConfigsToPackage(configSync.allConfigs.Select((OwnConfigEntryBase c) => c.BaseConfig), configSync.allCustomValues, entries, partial: false);
					yield return __instance.StartCoroutine(configSync.sendZPackage(new List<ZNetPeer> { peer }, package));
				}
				SendBufferedData();
			}
		}
	}

	private class PackageEntry
	{
		public string section = null;

		public string key = null;

		public Type type = null;

		public object? value;
	}

	[HarmonyPatch(typeof(ConfigEntryBase), "GetSerializedValue")]
	private static class PreventSavingServerInfo
	{
		[HarmonyPrefix]
		private static bool Prefix(ConfigEntryBase __instance, ref string __result)
		{
			OwnConfigEntryBase ownConfigEntryBase = configData(__instance);
			if (ownConfigEntryBase == null || isWritableConfig(ownConfigEntryBase))
			{
				return true;
			}
			__result = TomlTypeConverter.ConvertToString(ownConfigEntryBase.LocalBaseValue, __instance.SettingType);
			return false;
		}
	}

	[HarmonyPatch(typeof(ConfigEntryBase), "SetSerializedValue")]
	private static class PreventConfigRereadChangingValues
	{
		[HarmonyPrefix]
		private static bool Prefix(ConfigEntryBase __instance, string value)
		{
			OwnConfigEntryBase ownConfigEntryBase = configData(__instance);
			if (ownConfigEntryBase == null || ownConfigEntryBase.LocalBaseValue == null)
			{
				return true;
			}
			try
			{
				ownConfigEntryBase.LocalBaseValue = TomlTypeConverter.ConvertToValue(value, __instance.SettingType);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"Config value of setting \"{__instance.Definition}\" could not be parsed and will be ignored. Reason: {ex.Message}; Value: {value}");
			}
			return false;
		}
	}

	private class InvalidDeserializationTypeException : Exception
	{
		public string expected = null;

		public string received = null;

		public string field = "";
	}

	public static bool ProcessingServerUpdate;

	public readonly string Name;

	public string? DisplayName;

	public string? CurrentVersion;

	public string? MinimumRequiredVersion;

	public bool ModRequired = false;

	private bool? forceConfigLocking;

	private bool isSourceOfTruth = true;

	private static readonly HashSet<ConfigSync> configSyncs;

	private readonly HashSet<OwnConfigEntryBase> allConfigs = new HashSet<OwnConfigEntryBase>();

	private HashSet<CustomSyncedValueBase> allCustomValues = new HashSet<CustomSyncedValueBase>();

	private static bool isServer;

	private static bool lockExempt;

	private OwnConfigEntryBase? lockedConfig = null;

	private const byte PARTIAL_CONFIGS = 1;

	private const byte FRAGMENTED_CONFIG = 2;

	private const byte COMPRESSED_CONFIG = 4;

	private readonly Dictionary<string, SortedDictionary<int, byte[]>> configValueCache = new Dictionary<string, SortedDictionary<int, byte[]>>();

	private readonly List<KeyValuePair<long, string>> cacheExpirations = new List<KeyValuePair<long, string>>();

	private static long packageCounter;

	public bool IsLocked
	{
		get
		{
			bool? flag = forceConfigLocking;
			bool num;
			if (!flag.HasValue)
			{
				if (lockedConfig == null)
				{
					goto IL_0052;
				}
				num = ((IConvertible)lockedConfig.BaseConfig.BoxedValue).ToInt32(CultureInfo.InvariantCulture) != 0;
			}
			else
			{
				num = flag == true;
			}
			if (!num)
			{
				goto IL_0052;
			}
			int result = ((!lockExempt) ? 1 : 0);
			goto IL_0053;
			IL_0052:
			result = 0;
			goto IL_0053;
			IL_0053:
			return (byte)result != 0;
		}
		set
		{
			forceConfigLocking = value;
		}
	}

	public bool IsAdmin => lockExempt || isSourceOfTruth;

	public bool IsSourceOfTruth
	{
		get
		{
			return isSourceOfTruth;
		}
		private set
		{
			if (value != isSourceOfTruth)
			{
				isSourceOfTruth = value;
				SourceOfTruthChanged?.Invoke(value);
			}
		}
	}

	public bool InitialSyncDone { get; private set; } = false;

	public event Action<bool>? SourceOfTruthChanged;

	private event Action? lockedConfigChanged;

	static ConfigSync()
	{
		ProcessingServerUpdate = false;
		configSyncs = new HashSet<ConfigSync>();
		lockExempt = false;
		packageCounter = 0L;
		RuntimeHelpers.RunClassConstructor(typeof(VersionCheck).TypeHandle);
	}

	public ConfigSync(string name)
	{
		Name = name;
		configSyncs.Add(this);
		new VersionCheck(this);
	}

	public SyncedConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
	{
		OwnConfigEntryBase ownConfigEntryBase = configData(configEntry);
		SyncedConfigEntry<T> syncedEntry = ownConfigEntryBase as SyncedConfigEntry<T>;
		if (syncedEntry == null)
		{
			syncedEntry = new SyncedConfigEntry<T>(configEntry);
			AccessTools.DeclaredField(typeof(ConfigDescription), "<Tags>k__BackingField").SetValue(configEntry.Description, new object[1]
			{
				new ConfigurationManagerAttributes()
			}.Concat(configEntry.Description.Tags ?? Array.Empty<object>()).Concat(new SyncedConfigEntry<T>[1] { syncedEntry }).ToArray());
			configEntry.SettingChanged += delegate
			{
				if (!ProcessingServerUpdate && syncedEntry.SynchronizedConfig)
				{
					Broadcast(0L, configEntry);
				}
			};
			allConfigs.Add(syncedEntry);
		}
		return syncedEntry;
	}

	public SyncedConfigEntry<T> AddLockingConfigEntry<T>(ConfigEntry<T> lockingConfig) where T : IConvertible
	{
		if (lockedConfig != null)
		{
			throw new Exception("Cannot initialize locking ConfigEntry twice");
		}
		lockedConfig = AddConfigEntry(lockingConfig);
		lockingConfig.SettingChanged += delegate
		{
			lockedConfigChanged?.Invoke();
		};
		return (SyncedConfigEntry<T>)lockedConfig;
	}

	internal void AddCustomValue(CustomSyncedValueBase customValue)
	{
		if (allCustomValues.Select((CustomSyncedValueBase v) => v.Identifier).Concat<string>(new string[1] { "serverversion" }).Contains<string>(customValue.Identifier))
		{
			throw new Exception("Cannot have multiple settings with the same name or with a reserved name (serverversion)");
		}
		allCustomValues.Add(customValue);
		allCustomValues = new HashSet<CustomSyncedValueBase>(allCustomValues.OrderByDescending((CustomSyncedValueBase v) => v.Priority));
		customValue.ValueChanged += delegate
		{
			if (!ProcessingServerUpdate)
			{
				Broadcast(0L, customValue);
			}
		};
	}

	private void RPC_FromServerConfigSync(ZRpc rpc, ZPackage package)
	{
		lockedConfigChanged += serverLockedSettingChanged;
		IsSourceOfTruth = false;
		if (HandleConfigSyncRPC(0L, package, clientUpdate: false))
		{
			InitialSyncDone = true;
		}
	}

	private void RPC_FromOtherClientConfigSync(long sender, ZPackage package)
	{
		HandleConfigSyncRPC(sender, package, clientUpdate: true);
	}

	private bool HandleConfigSyncRPC(long sender, ZPackage package, bool clientUpdate)
	{
		try
		{
			if (isServer && IsLocked)
			{
				string text = SnatchCurrentlyHandlingRPC.currentRpc?.GetSocket()?.GetHostName();
				if (text != null)
				{
					MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId");
					SyncedList syncedList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
					if (!(((object)methodInfo == null) ? syncedList.Contains(text) : ((bool)methodInfo.Invoke(ZNet.instance, new object[2] { syncedList, text }))))
					{
						return false;
					}
				}
			}
			cacheExpirations.RemoveAll(delegate(KeyValuePair<long, string> kv)
			{
				if (kv.Key < DateTimeOffset.Now.Ticks)
				{
					configValueCache.Remove(kv.Value);
					return true;
				}
				return false;
			});
			byte b = package.ReadByte();
			if ((b & 2) != 0)
			{
				long num = package.ReadLong();
				string text2 = sender.ToString() + num;
				if (!configValueCache.TryGetValue(text2, out SortedDictionary<int, byte[]> value))
				{
					value = new SortedDictionary<int, byte[]>();
					configValueCache[text2] = value;
					cacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60.0).Ticks, text2));
				}
				int key = package.ReadInt();
				int num2 = package.ReadInt();
				value.Add(key, package.ReadByteArray());
				if (value.Count < num2)
				{
					return false;
				}
				configValueCache.Remove(text2);
				package = new ZPackage(value.Values.SelectMany((byte[] a) => a).ToArray());
				b = package.ReadByte();
			}
			ProcessingServerUpdate = true;
			if ((b & 4) != 0)
			{
				byte[] buffer = package.ReadByteArray();
				MemoryStream stream = new MemoryStream(buffer);
				MemoryStream memoryStream = new MemoryStream();
				using (DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
				{
					deflateStream.CopyTo(memoryStream);
				}
				package = new ZPackage(memoryStream.ToArray());
				b = package.ReadByte();
			}
			if ((b & 1) == 0)
			{
				resetConfigsFromServer();
			}
			ParsedConfigs parsedConfigs = ReadConfigsFromPackage(package);
			ConfigFile configFile = null;
			bool saveOnConfigSet = false;
			foreach (KeyValuePair<OwnConfigEntryBase, object> configValue in parsedConfigs.configValues)
			{
				if (!isServer && configValue.Key.LocalBaseValue == null)
				{
					configValue.Key.LocalBaseValue = configValue.Key.BaseConfig.BoxedValue;
				}
				if (configFile == null)
				{
					configFile = configValue.Key.BaseConfig.ConfigFile;
					saveOnConfigSet = configFile.SaveOnConfigSet;
					configFile.SaveOnConfigSet = false;
				}
				configValue.Key.BaseConfig.BoxedValue = configValue.Value;
			}
			if (configFile != null)
			{
				configFile.SaveOnConfigSet = saveOnConfigSet;
				configFile.Save();
			}
			foreach (KeyValuePair<CustomSyncedValueBase, object> customValue in parsedConfigs.customValues)
			{
				if (!isServer)
				{
					CustomSyncedValueBase key2 = customValue.Key;
					if (key2.LocalBaseValue == null)
					{
						key2.LocalBaseValue = customValue.Key.BoxedValue;
					}
				}
				customValue.Key.BoxedValue = customValue.Value;
			}
			Debug.Log(string.Format("Received {0} configs and {1} custom values from {2} for mod {3}", parsedConfigs.configValues.Count, parsedConfigs.customValues.Count, (isServer | clientUpdate) ? $"client {sender}" : "the server", DisplayName ?? Name));
			if (!isServer)
			{
				serverLockedSettingChanged();
			}
			return true;
		}
		finally
		{
			ProcessingServerUpdate = false;
		}
	}

	private ParsedConfigs ReadConfigsFromPackage(ZPackage package)
	{
		ParsedConfigs parsedConfigs = new ParsedConfigs();
		Dictionary<string, OwnConfigEntryBase> dictionary = allConfigs.Where((OwnConfigEntryBase c) => c.SynchronizedConfig).ToDictionary((OwnConfigEntryBase c) => c.BaseConfig.Definition.Section + "_" + c.BaseConfig.Definition.Key, (OwnConfigEntryBase c) => c);
		Dictionary<string, CustomSyncedValueBase> dictionary2 = allCustomValues.ToDictionary((CustomSyncedValueBase c) => c.Identifier, (CustomSyncedValueBase c) => c);
		int num = package.ReadInt();
		for (int num2 = 0; num2 < num; num2++)
		{
			string text = package.ReadString();
			string text2 = package.ReadString();
			string text3 = package.ReadString();
			Type type = Type.GetType(text3);
			if (text3 == "" || type != null)
			{
				object obj;
				try
				{
					obj = ((text3 == "") ? null : ReadValueWithTypeFromZPackage(package, type));
				}
				catch (InvalidDeserializationTypeException ex)
				{
					Debug.LogWarning("Got unexpected struct internal type " + ex.received + " for field " + ex.field + " struct " + text3 + " for " + text2 + " in section " + text + " for mod " + (DisplayName ?? Name) + ", expecting " + ex.expected);
					continue;
				}
				OwnConfigEntryBase value2;
				if (text == "Internal")
				{
					CustomSyncedValueBase value;
					if (text2 == "serverversion")
					{
						if (obj?.ToString() != CurrentVersion)
						{
							Debug.LogWarning("Received server version is not equal: server version = " + (obj?.ToString() ?? "null") + "; local version = " + (CurrentVersion ?? "unknown"));
						}
					}
					else if (text2 == "lockexempt")
					{
						if (obj is bool flag)
						{
							lockExempt = flag;
						}
					}
					else if (dictionary2.TryGetValue(text2, out value))
					{
						if ((text3 == "" && (!value.Type.IsValueType || Nullable.GetUnderlyingType(value.Type) != null)) || GetZPackageTypeString(value.Type) == text3)
						{
							parsedConfigs.customValues[value] = obj;
							continue;
						}
						Debug.LogWarning("Got unexpected type " + text3 + " for internal value " + text2 + " for mod " + (DisplayName ?? Name) + ", expecting " + value.Type.AssemblyQualifiedName);
					}
				}
				else if (dictionary.TryGetValue(text + "_" + text2, out value2))
				{
					Type type2 = configType(value2.BaseConfig);
					if ((text3 == "" && (!type2.IsValueType || Nullable.GetUnderlyingType(type2) != null)) || GetZPackageTypeString(type2) == text3)
					{
						parsedConfigs.configValues[value2] = obj;
						continue;
					}
					Debug.LogWarning("Got unexpected type " + text3 + " for " + text2 + " in section " + text + " for mod " + (DisplayName ?? Name) + ", expecting " + type2.AssemblyQualifiedName);
				}
				else
				{
					Debug.LogWarning("Received unknown config entry " + text2 + " in section " + text + " for mod " + (DisplayName ?? Name) + ". This may happen if client and server versions of the mod do not match.");
				}
				continue;
			}
			Debug.LogWarning("Got invalid type " + text3 + ", abort reading of received configs");
			return new ParsedConfigs();
		}
		return parsedConfigs;
	}

	private static bool isWritableConfig(OwnConfigEntryBase config)
	{
		ConfigSync configSync = configSyncs.FirstOrDefault((ConfigSync cs) => cs.allConfigs.Contains(config));
		if (configSync == null)
		{
			return true;
		}
		return configSync.IsSourceOfTruth || !config.SynchronizedConfig || config.LocalBaseValue == null || (!configSync.IsLocked && (config != configSync.lockedConfig || lockExempt));
	}

	private void serverLockedSettingChanged()
	{
		foreach (OwnConfigEntryBase allConfig in allConfigs)
		{
			configAttribute<ConfigurationManagerAttributes>(allConfig.BaseConfig).ReadOnly = !isWritableConfig(allConfig);
		}
	}

	private void resetConfigsFromServer()
	{
		ConfigFile configFile = null;
		bool saveOnConfigSet = false;
		foreach (OwnConfigEntryBase item in allConfigs.Where((OwnConfigEntryBase config) => config.LocalBaseValue != null))
		{
			if (configFile == null)
			{
				configFile = item.BaseConfig.ConfigFile;
				saveOnConfigSet = configFile.SaveOnConfigSet;
				configFile.SaveOnConfigSet = false;
			}
			item.BaseConfig.BoxedValue = item.LocalBaseValue;
			item.LocalBaseValue = null;
		}
		if (configFile != null)
		{
			configFile.SaveOnConfigSet = saveOnConfigSet;
		}
		foreach (CustomSyncedValueBase item2 in allCustomValues.Where((CustomSyncedValueBase config) => config.LocalBaseValue != null))
		{
			item2.BoxedValue = item2.LocalBaseValue;
			item2.LocalBaseValue = null;
		}
		lockedConfigChanged -= serverLockedSettingChanged;
		serverLockedSettingChanged();
	}

	private IEnumerator<bool> distributeConfigToPeers(ZNetPeer peer, ZPackage package)
	{
		ZRoutedRpc rpc = ZRoutedRpc.instance;
		if (rpc == null)
		{
			yield break;
		}
		byte[] data = package.GetArray();
		if (data != null && data.LongLength > 250000)
		{
			int fragments = (int)(1 + (data.LongLength - 1) / 250000);
			long packageIdentifier = ++packageCounter;
			int fragment = 0;
			while (fragment < fragments)
			{
				foreach (bool item in waitForQueue())
				{
					yield return item;
				}
				if (peer.m_socket.IsConnected())
				{
					ZPackage fragmentedPackage = new ZPackage();
					fragmentedPackage.Write((byte)2);
					fragmentedPackage.Write(packageIdentifier);
					fragmentedPackage.Write(fragment);
					fragmentedPackage.Write(fragments);
					fragmentedPackage.Write(data.Skip(250000 * fragment).Take(250000).ToArray());
					SendPackage(fragmentedPackage);
					if (fragment != fragments - 1)
					{
						yield return true;
					}
					int num = fragment + 1;
					fragment = num;
					continue;
				}
				break;
			}
			yield break;
		}
		foreach (bool item2 in waitForQueue())
		{
			yield return item2;
		}
		SendPackage(package);
		void SendPackage(ZPackage pkg)
		{
			string text = Name + " ConfigSync";
			if (isServer)
			{
				peer.m_rpc.Invoke(text, pkg);
			}
			else
			{
				rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, text, pkg);
			}
		}
		IEnumerable<bool> waitForQueue()
		{
			float timeout = Time.time + 30f;
			while (peer.m_socket.GetSendQueueSize() > 20000)
			{
				if (Time.time > timeout)
				{
					Debug.Log($"Disconnecting {peer.m_uid} after 30 seconds config sending timeout");
					peer.m_rpc.Invoke("Error", ZNet.ConnectionStatus.ErrorConnectFailed);
					ZNet.instance.Disconnect(peer);
					break;
				}
				yield return false;
			}
		}
	}

	private IEnumerator sendZPackage(long target, ZPackage package)
	{
		if (!ZNet.instance)
		{
			return Enumerable.Empty<object>().GetEnumerator();
		}
		List<ZNetPeer> list = (List<ZNetPeer>)AccessTools.DeclaredField(typeof(ZRoutedRpc), "m_peers").GetValue(ZRoutedRpc.instance);
		if (target != 0)
		{
			list = list.Where((ZNetPeer p) => p.m_uid == target).ToList();
		}
		return sendZPackage(list, package);
	}

	private IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
	{
		if (!ZNet.instance)
		{
			yield break;
		}
		byte[] rawData = package.GetArray();
		if (rawData != null && rawData.LongLength > 10000)
		{
			ZPackage compressedPackage = new ZPackage();
			compressedPackage.Write((byte)4);
			MemoryStream output = new MemoryStream();
			using (DeflateStream deflateStream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
			{
				deflateStream.Write(rawData, 0, rawData.Length);
			}
			compressedPackage.Write(output.ToArray());
			package = compressedPackage;
		}
		List<IEnumerator<bool>> writers = (from p in peers
			where p.IsReady()
			select distributeConfigToPeers(p, package)).ToList();
		writers.RemoveAll((IEnumerator<bool> writer) => !writer.MoveNext());
		while (writers.Count > 0)
		{
			yield return null;
			writers.RemoveAll((IEnumerator<bool> writer) => !writer.MoveNext());
		}
	}

	private void Broadcast(long target, params ConfigEntryBase[] configs)
	{
		if (!IsLocked || isServer)
		{
			ZPackage package = ConfigsToPackage(configs);
			ZNet.instance?.StartCoroutine(sendZPackage(target, package));
		}
	}

	private void Broadcast(long target, params CustomSyncedValueBase[] customValues)
	{
		if (!IsLocked || isServer)
		{
			ZPackage package = ConfigsToPackage(null, customValues);
			ZNet.instance?.StartCoroutine(sendZPackage(target, package));
		}
	}

	private static OwnConfigEntryBase? configData(ConfigEntryBase config)
	{
		return config.Description.Tags?.OfType<OwnConfigEntryBase>().SingleOrDefault();
	}

	public static SyncedConfigEntry<T>? ConfigData<T>(ConfigEntry<T> config)
	{
		return config.Description.Tags?.OfType<SyncedConfigEntry<T>>().SingleOrDefault();
	}

	private static T configAttribute<T>(ConfigEntryBase config)
	{
		return config.Description.Tags.OfType<T>().First();
	}

	private static Type configType(ConfigEntryBase config)
	{
		return configType(config.SettingType);
	}

	private static Type configType(Type type)
	{
		return type.IsEnum ? Enum.GetUnderlyingType(type) : type;
	}

	private static ZPackage ConfigsToPackage(IEnumerable<ConfigEntryBase>? configs = null, IEnumerable<CustomSyncedValueBase>? customValues = null, IEnumerable<PackageEntry>? packageEntries = null, bool partial = true)
	{
		List<ConfigEntryBase> list = configs?.Where((ConfigEntryBase config) => configData(config).SynchronizedConfig).ToList() ?? new List<ConfigEntryBase>();
		List<CustomSyncedValueBase> list2 = customValues?.ToList() ?? new List<CustomSyncedValueBase>();
		ZPackage zPackage = new ZPackage();
		zPackage.Write((byte)(partial ? 1 : 0));
		zPackage.Write(list.Count + list2.Count + (packageEntries?.Count() ?? 0));
		foreach (PackageEntry item in packageEntries ?? Array.Empty<PackageEntry>())
		{
			AddEntryToPackage(zPackage, item);
		}
		foreach (CustomSyncedValueBase item2 in list2)
		{
			AddEntryToPackage(zPackage, new PackageEntry
			{
				section = "Internal",
				key = item2.Identifier,
				type = item2.Type,
				value = item2.BoxedValue
			});
		}
		foreach (ConfigEntryBase item3 in list)
		{
			AddEntryToPackage(zPackage, new PackageEntry
			{
				section = item3.Definition.Section,
				key = item3.Definition.Key,
				type = configType(item3),
				value = item3.BoxedValue
			});
		}
		return zPackage;
	}

	private static void AddEntryToPackage(ZPackage package, PackageEntry entry)
	{
		package.Write(entry.section);
		package.Write(entry.key);
		package.Write((entry.value == null) ? "" : GetZPackageTypeString(entry.type));
		AddValueToZPackage(package, entry.value);
	}

	private static string GetZPackageTypeString(Type type)
	{
		return type.AssemblyQualifiedName;
	}

	private static void AddValueToZPackage(ZPackage package, object? value)
	{
		Type type = value?.GetType();
		if (value is Enum)
		{
			value = ((IConvertible)value).ToType(Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture);
		}
		else
		{
			if (value is ICollection collection)
			{
				package.Write(collection.Count);
				{
					foreach (object item in collection)
					{
						AddValueToZPackage(package, item);
					}
					return;
				}
			}
			if ((object)type != null && type.IsValueType && !type.IsPrimitive)
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				package.Write(fields.Length);
				FieldInfo[] array = fields;
				foreach (FieldInfo fieldInfo in array)
				{
					package.Write(GetZPackageTypeString(fieldInfo.FieldType));
					AddValueToZPackage(package, fieldInfo.GetValue(value));
				}
				return;
			}
		}
		ZRpc.Serialize(new object[1] { value }, ref package);
	}

	private static object ReadValueWithTypeFromZPackage(ZPackage package, Type type)
	{
		if ((object)type != null && type.IsValueType && !type.IsPrimitive && !type.IsEnum)
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int num = package.ReadInt();
			if (num != fields.Length)
			{
				throw new InvalidDeserializationTypeException
				{
					received = $"(field count: {num})",
					expected = $"(field count: {fields.Length})"
				};
			}
			object uninitializedObject = FormatterServices.GetUninitializedObject(type);
			FieldInfo[] array = fields;
			foreach (FieldInfo fieldInfo in array)
			{
				string text = package.ReadString();
				if (text != GetZPackageTypeString(fieldInfo.FieldType))
				{
					throw new InvalidDeserializationTypeException
					{
						received = text,
						expected = GetZPackageTypeString(fieldInfo.FieldType),
						field = fieldInfo.Name
					};
				}
				fieldInfo.SetValue(uninitializedObject, ReadValueWithTypeFromZPackage(package, fieldInfo.FieldType));
			}
			return uninitializedObject;
		}
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<, >))
		{
			int num2 = package.ReadInt();
			IDictionary dictionary = (IDictionary)Activator.CreateInstance(type);
			Type type2 = typeof(KeyValuePair<, >).MakeGenericType(type.GenericTypeArguments);
			FieldInfo field = type2.GetField("key", BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo field2 = type2.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
			for (int j = 0; j < num2; j++)
			{
				object obj = ReadValueWithTypeFromZPackage(package, type2);
				dictionary.Add(field.GetValue(obj), field2.GetValue(obj));
			}
			return dictionary;
		}
		if (type != typeof(List<string>) && type.IsGenericType)
		{
			Type type3 = typeof(ICollection<>).MakeGenericType(type.GenericTypeArguments[0]);
			if ((object)type3 != null && type3.IsAssignableFrom(type))
			{
				int num3 = package.ReadInt();
				object obj2 = Activator.CreateInstance(type);
				MethodInfo method = type3.GetMethod("Add");
				for (int k = 0; k < num3; k++)
				{
					method.Invoke(obj2, new object[1] { ReadValueWithTypeFromZPackage(package, type.GenericTypeArguments[0]) });
				}
				return obj2;
			}
		}
		ParameterInfo parameterInfo = (ParameterInfo)FormatterServices.GetUninitializedObject(typeof(ParameterInfo));
		AccessTools.DeclaredField(typeof(ParameterInfo), "ClassImpl").SetValue(parameterInfo, type);
		List<object> parameters = new List<object>();
		ZRpc.Deserialize(new ParameterInfo[2] { null, parameterInfo }, package, ref parameters);
		return parameters.First();
	}
}

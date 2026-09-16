using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UIManager;

internal static class UIRoot
{
	private static Harmony _harmony;

	private static bool _initialized;

	private static ButtonSfx _sfxTemplate;

	private static UIRootHost _host;

	public const int FrontSortingOrder = 2000;

	public static Transform Front { get; private set; }

	public static Transform Back { get; private set; }

	public static bool IsReady => Front != null;

	public static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

	public static int UILayer
	{
		get
		{
			int num = LayerMask.NameToLayer("UI");
			if (num < 0)
			{
				return 5;
			}
			return num;
		}
	}

	internal static UIRootHost Host
	{
		get
		{
			EnsureHost();
			return _host;
		}
	}

	public static event Action OnReady;

	public static void Init(ManualLogSource log = null, Harmony harmony = null)
	{
		if (!_initialized)
		{
			_initialized = true;
			if (log != null)
			{
				UILog.Use(log);
			}
			if (IsHeadless)
			{
				UILog.Debug("headless - skipping GUI setup");
				return;
			}
			string id = "uimanager." + typeof(UIRoot).Assembly.GetName().Name.ToLowerInvariant();
			_harmony = harmony ?? new Harmony(id);
			_harmony.PatchAll(typeof(UIRoot));
			_harmony.PatchAll(typeof(InputBlocker));
			_harmony.PatchAll(typeof(UIPanels));
			SceneManager.sceneLoaded += OnSceneLoaded;
			EnsureHost();
			TryCreateGui();
		}
	}

	private static void EnsureHost()
	{
		if (!(_host != null))
		{
			GameObject obj = new GameObject("UIManager.Host [" + typeof(UIRoot).Assembly.GetName().Name + "]")
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			UnityEngine.Object.DontDestroyOnLoad(obj);
			_host = obj.AddComponent<UIRootHost>();
		}
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!(scene.name != "start") || !(scene.name != "main"))
		{
			GameAssets.Index(force: true);
			if (_host != null)
			{
				_host.StartCoroutine(WaitAndCreate());
			}
		}
	}

	private static IEnumerator WaitAndCreate()
	{
		for (int i = 0; i < 300; i++)
		{
			if (IsReady)
			{
				break;
			}
			TryCreateGui();
			if (IsReady)
			{
				break;
			}
			yield return null;
		}
	}

	[HarmonyPatch(typeof(FejdStartup), "SetupGui")]
	[HarmonyPostfix]
	private static void FejdStartupSetupGuiPostfix()
	{
		TryCreateGui();
	}

	[HarmonyPatch(typeof(Game), "Start")]
	[HarmonyPostfix]
	private static void GameStartPostfix()
	{
		TryCreateGui();
	}

	public static void TryCreateGui()
	{
		if (IsReady || IsHeadless)
		{
			return;
		}
		Transform transform = FindGuiRoot();
		if (transform == null)
		{
			return;
		}
		GameAssets.Index(force: true);
		VanillaUI.Harvest(force: true);
		string name = typeof(UIRoot).Assembly.GetName().Name;
		Back = CreateCanvas("UIManager.Back [" + name + "]", 0, transform).transform;
		Back.SetAsFirstSibling();
		Front = CreateCanvas("UIManager.Front [" + name + "]", 2000, transform).transform;
		Front.SetAsLastSibling();
		UILog.Debug("GUI canvases created under " + UIExtensions.PathOf(transform));
		try
		{
			OnReady?.Invoke();
		}
		catch (Exception e)
		{
			UILog.Error("a UIRoot.OnReady handler threw", e);
		}
	}

	private static void SetLayerRecursively(GameObject target, int layer)
	{
		if (target == null)
		{
			return;
		}
		target.layer = layer;
		foreach (Transform item in target.transform)
		{
			SetLayerRecursively(item.gameObject, layer);
		}
	}

	private static Transform FindGuiRoot()
	{
		GameObject gameObject = GameObject.Find("GuiRoot/GUI");
		if (gameObject != null)
		{
			return gameObject.transform;
		}
		GameObject gameObject2 = GameObject.Find("_GameMain/LoadingGUI");
		if (gameObject2 != null)
		{
			return gameObject2.transform;
		}
		return null;
	}

	private static GameObject CreateCanvas(string name, int sortingOrder, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GuiPixelFix));
		gameObject.layer = UILayer;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform obj = (RectTransform)gameObject.transform;
		obj.anchorMin = Vector2.zero;
		obj.anchorMax = Vector2.one;
		obj.offsetMin = Vector2.zero;
		obj.offsetMax = Vector2.zero;
		obj.localScale = Vector3.one;
		Canvas component = gameObject.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.overrideSorting = true;
		component.sortingOrder = sortingOrder;
		component.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
		gameObject.GetComponent<CanvasScaler>().referencePixelsPerUnit = 50f;
		return gameObject;
	}

	public static GameObject Instantiate(GameObject prefab, Transform parent = null, bool style = true, StyleOptions options = null)
	{
		if (prefab == null)
		{
			UILog.Error("cannot instantiate a null prefab");
			return null;
		}
		if (parent == null)
		{
			parent = Front;
		}
		if (parent == null)
		{
			UILog.Error("the GUI is not up yet - hook UIRoot.OnReady before instantiating '" + prefab.name + "'");
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays: false);
		gameObject.name = prefab.name;
		SetLayerRecursively(gameObject, UILayer);
		if (style)
		{
			Styler.Apply(gameObject, options);
		}
		return gameObject;
	}

	public static Canvas RaiseAbove(GameObject target, int extra = 100)
	{
		if (target == null)
		{
			return null;
		}
		Canvas obj = target.GetComponent<Canvas>() ?? target.AddComponent<Canvas>();
		obj.overrideSorting = true;
		obj.sortingOrder = 2000 + extra;
		if (target.GetComponent<GraphicRaycaster>() == null)
		{
			target.AddComponent<GraphicRaycaster>();
		}
		return obj;
	}

	public static ButtonSfx FindSfxTemplate()
	{
		if (_sfxTemplate != null)
		{
			return _sfxTemplate;
		}
		ButtonSfx[] array = Resources.FindObjectsOfTypeAll<ButtonSfx>();
		foreach (ButtonSfx buttonSfx in array)
		{
			if (!(buttonSfx == null) && !(buttonSfx.m_sfxPrefab == null))
			{
				_sfxTemplate = buttonSfx;
				break;
			}
		}
		return _sfxTemplate;
	}

	public static Coroutine Run(IEnumerator routine)
	{
		EnsureHost();
		return _host.StartCoroutine(routine);
	}
}

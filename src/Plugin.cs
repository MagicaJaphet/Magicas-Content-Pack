using BepInEx;
using System;
using System.Linq;
using MoreSlugcats;
using static MagicasContentPack.MagicaEnums;
using BepInEx.Logging;
using System.Security.Permissions;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.CompilerServices;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace MagicasContentPack;

[BepInDependency("crs", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(MOD_ID, "Magica's Content Pack", "1.2.0")]

public class Plugin : BaseUnityPlugin
{
	// Update this and the modinfo for new workshop upload
	public const string MOD_ID = "magica.contentpack";
	public static string modPath;
	public static string MOD_NAME = "";
	public static string VERSION = "";
	public static string AUTHORS = "";
	public static new ManualLogSource Logger;

	public static bool IsInit;

	public OptionInterface optionsMenuInstance;
	public static bool isDMSEnabled;
	public static bool isCRSEnabled;
	public static bool debugState = true;
	public static List<string> initalizedMethods = [];

	public void OnEnable()
	{
		On.RainWorld.PreModsInit += PreModsInit;
		On.RainWorld.OnModsInit += OnModsInit;
		On.RainWorld.PostModsInit += PostModsInit;
	}

	private void PreModsInit(On.RainWorld.orig_PreModsInit orig, RainWorld self)
	{
		orig(self);

		// For applying hooks as early as possible
		try
		{
			MenuSceneHooks.PreInit();
		}
		catch (Exception ex)
		{
			HookFail(ex);
		}
	}

	private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);

		if (IsInit) return;

		try
		{
			IsInit = true;

			Logger = base.Logger;

			isDMSEnabled = ModManager.ActiveMods.Exists((ModManager.Mod mod) => mod.id == "dressmyslugcat");
			isCRSEnabled = ModManager.ActiveMods.Exists((ModManager.Mod mod) => mod.id == "crs");

			var mod = ModManager.ActiveMods.FirstOrDefault(mod => mod.id == MOD_ID);

			modPath = mod.path;
			MOD_NAME = mod.name;
			VERSION = mod.version;
			AUTHORS = mod.authors;

			// TODO: ORGANIZE CODE!!!

			On.Menu.Remix.ConfigMenuTab.ButtonManager.SignalSave += ButtonManager_SignalSave;

			GraphicsHooks.Init();
			ObjectHooks.Init();
			IteratorHooks.IteratorHooks.Init();
			MenuSceneHooks.Init();
			WinOrSaveHooks.Init();
			WorldHooks.Init();
			PlayerHooks.Init();

			Application.quitting += QuitDebugLog;

			ModOptions.RegisterOI();
			LoadAtlasResources();

			MagicaEnums.RegisterEnums();

			HookSucceed();
		}
		catch (Exception ex)
		{
			HookFail(ex);
		}
	}

	private void QuitDebugLog()
	{
		if (GraphicsHooks.debugElementsNotChanged.Count > 0 || initalizedMethods.Count > 0)
		{
			string information = "=========== MAGICA'S CONTENT PACK DEBUG INFO ===========";
			if (GraphicsHooks.debugElementsNotChanged.Count > 0)
			{
				information += $"\n\nMISSING ATLAS ELEMENTS: \n{string.Join("\n", GraphicsHooks.debugElementsNotChanged)}";
			}
				
			if (initalizedMethods.Count > 0)
			{
				string[] suceeded = initalizedMethods.Where(x => x.Contains("SUCCEEDED")).ToArray();
				string[] failed = initalizedMethods.Where(x => x.Contains("FAILED")).ToArray();
				information += $"\n\nHOOK STATES ({suceeded.Length}/{initalizedMethods.Count} SUCCEEDED, {initalizedMethods.Where(x => x.Contains("IL")).Count()} IL HOOKS):\n{string.Join("\n", failed)}";
			}

			information += "\n=======================================";

			DebugLog(information);
		}
	}

	private void ButtonManager_SignalSave(On.Menu.Remix.ConfigMenuTab.ButtonManager.orig_SignalSave orig, Menu.Remix.ConfigMenuTab.ButtonManager self, Menu.Remix.MixedUI.UIfocusable trigger)
	{
		orig(self, trigger);

		SceneMaker.allCustomScenes = SceneMaker.AllValidScenes();
	}

	private void PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
	{
		// For other mod patches

		orig(self);

		try
		{
			if (isDMSEnabled)
				DMSHooks.LoadDMSConfigs();

			GraphicsHooks.PostInit();
			ObjectHooks.PostInit();

			HookSucceed();
		}
		catch (Exception ex)
		{
			HookFail(ex);
		}
	}

	public void LoadAtlasResources()
	{
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/MagicaSprites");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/magicarainworldmsc");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/magicauisprites");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/iteratorsprites");

		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/artificer/scarheadleft");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/artificer/scarheadright");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/artificer/scarhead");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/artificer/scarlegsleft");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/artificer/scarlegsright");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/artificer/scarlegs");

		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/red/hunterscars");
		for (int i = 0; i < 4; i++)
		{
			Futile.atlasManager.LoadAtlas($"{modPath}/atlases/slugcats/red/facescar{i}");
			Futile.atlasManager.LoadAtlas($"{modPath}/atlases/slugcats/red/facescarleft{i}");
		}

		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/saint/saintscar");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/saint/saintscarfatique");
		Futile.atlasManager.LoadAtlas(modPath + "/atlases/slugcats/saint/karmarings");

		Futile.atlasManager.LoadImage(modPath + "/projections/SRS_PROJ");

		if (!isDMSEnabled)
		{
			string[] customSlugcatSprites = [MoreSlugcatsEnums.SlugcatStatsName.Spear.value, MoreSlugcatsEnums.SlugcatStatsName.Artificer.value, SlugcatStats.Name.Red.value, MoreSlugcatsEnums.SlugcatStatsName.Saint.value];
			string[] bodyParts = ["body", "hips", "head", "face", "arm", "legs", "tail"];
			for (int i = 0; i < customSlugcatSprites.Length; i++)
			{
				for (int j = 0; j < bodyParts.Length; j++)
				{
					string path = $"{modPath}/atlases/slugcats/{customSlugcatSprites[i]}/{bodyParts[j]}";
					Futile.atlasManager.LoadAtlas(path);
					if (File.Exists($"extras"))
					{
						Futile.atlasManager.LoadAtlas($"extras");
					}
					if (File.Exists($"{path}left"))
					{
						Futile.atlasManager.LoadAtlas($"{path}left");
					}
					if (File.Exists($"{path}right"))
					{
						Futile.atlasManager.LoadAtlas($"{path}right");
					}
				}
			}
			Futile.atlasManager.LoadAtlas($"{modPath}/atlases/slugcats/artificer/faceleft");
		}

		ResourceLoad();
	}

	public static bool DebugLog(string m)
	{
		if (debugState)
		{
			UnityEngine.Debug.Log("[MagicasContentPack] " + m);
		}
		return true;
	}

	public static bool IsVanillaSlugcat(SlugcatStats.Name slugCatClass)
	{
		SlugcatStats.Name[] slugNames =
		[
				MoreSlugcatsEnums.SlugcatStatsName.Saint,
				MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel,
				MoreSlugcatsEnums.SlugcatStatsName.Spear,
				MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
				MoreSlugcatsEnums.SlugcatStatsName.Artificer,
				MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
				SlugcatStats.Name.White,
				SlugcatStats.Name.Yellow,
				SlugcatStats.Name.Red,
		];

		if (slugNames.Contains(slugCatClass))
		{
			return true;
		}
		return false;
	}

	public enum LogStates
	{
		FailILMatch,
		FailILInsert,
		ILSuccess,
		HookFail,
		HooksSucceeded,
		ResourceLoaded,
		MISC
	}

	internal static bool Log(LogStates state, [CallerMemberName] string methodName = "")
	{
		string msg = "";
		msg = AppendLogState(state, msg);

		try
		{
			if (ErrorStates(state) && Logger != null)
			{
				Logger.LogError(string.Join(": ", msg, methodName));
			}
			if (InfoStates(state) && Logger != null)
			{
				Logger.LogInfo(string.Join(": ", msg, methodName));
			}
		}
		catch (Exception ex)
		{
			if (Logger != null)
			{
				Logger.LogError(ex);
			}
		}
		

		return true;
	}

	private static string AppendLogState(LogStates state, string msg)
	{
		switch (state)
		{
			case LogStates.FailILMatch:
				msg = "INVALID IL MATCH";
				break;

			case LogStates.FailILInsert:
				msg = "INVALID IL INSERTION";
				break;

			case LogStates.ILSuccess:
				msg = "SUCCEEDED IL INSERTION";
				break;

			case LogStates.HookFail:
				msg = "HOOK FAILED";
				break;

			case LogStates.HooksSucceeded:
				msg = "HOOKS SUCCEEDED";
				break;

			case LogStates.ResourceLoaded:
				msg = "RESOURCE LOADED";
				break;
		}
		return msg;
	}

	private static bool InfoStates(LogStates state)
	{
		return state == LogStates.ILSuccess
			|| state == LogStates.HooksSucceeded
			|| state == LogStates.ResourceLoaded
			|| state == LogStates.MISC;
	}

	private static bool ErrorStates(LogStates state)
	{
		return state == LogStates.FailILMatch
			|| state == LogStates.FailILInsert
			|| state == LogStates.HookFail;
	}

	private static string GetTypeName(string type)
	{
		return Path.GetFileNameWithoutExtension(type);
	}

	public static void ResourceLoad([CallerMemberName] string method = "")
	{
		Log(LogStates.ResourceLoaded, methodName: method);
	}

	public static void HookSucceed([CallerFilePath] string type = "", [CallerMemberName] string method = "")
	{
		type = GetTypeName(type);
		Log(LogStates.HooksSucceeded, methodName: $"{type}.{method}");

		initalizedMethods.Add($"{type}.{method} HOOK SUCCEEDED");
	}

	public static void HookFail(object exception, [CallerFilePath] string type = "", [CallerMemberName] string method = "")
	{
		type = GetTypeName(type);
		Log(LogStates.HookFail, methodName: $"{type}.{method}");
		Logger.LogError(exception);

		initalizedMethods.Add($"{type}.{method} HOOK FAILED");
	}

	public static void ILSucceed([CallerFilePath] string type = "", [CallerMemberName] string method = "")
	{
		type = GetTypeName(type);
		Log(LogStates.ILSuccess, methodName: method);

		initalizedMethods.Add($"{type}.{method} IL SUCCEEDED");
	}

	public static bool ILMatchFail(bool succeed, [CallerMemberName] string method = "")
	{
		if (!succeed)
		{
			Log(LogStates.FailILMatch, methodName: method);
		}
		return !succeed;
	}

	public static void ILFail(object exception, [CallerFilePath] string type = "", [CallerMemberName] string method = "")
	{
		type = GetTypeName(type);
		Log(LogStates.FailILInsert, methodName: method);
		Logger.LogError(exception);

		initalizedMethods.Add($"{type}.{method} IL FAILED");
	}
}

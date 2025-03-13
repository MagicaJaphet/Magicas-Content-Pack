using BepInEx;
using System;
using System.Linq;
using MoreSlugcats;
using static MagicasContentPack.MagicaEnums;
using BepInEx.Logging;
using System.Security.Permissions;
using System.Collections.Generic;
using Menu;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace MagicasContentPack;

[BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]

[BepInPlugin(MOD_ID, "Magica's Content Pack", "1.0.0")]

public class Plugin : BaseUnityPlugin
{
	// Update this and the modinfo for new workshop upload
	public const string MOD_ID = "magicaanthro.skins";

	public static string MOD_NAME = "";
	public static string VERSION = "";
	public static string AUTHORS = "";
	public static new ManualLogSource Logger;

	public static bool IsInit;

	public OptionInterface optionsMenuInstance;
	public static bool isDMSEnabled;
	public static bool isCRSEnabled;
	public static bool debugState = true;

	public void OnEnable()
	{
		On.RainWorld.PreModsInit += RainWorld_PreModsInit;
		On.RainWorld.OnModsInit += RainWorld_OnModsInit;
		On.RainWorld.PostModsInit += RainWorld_PostModsInit;
	}

	private void RainWorld_PreModsInit(On.RainWorld.orig_PreModsInit orig, RainWorld self)
	{
		orig(self);

		// For applying hooks as early as possible
		try
		{
			MenuSceneHooks.PreInit();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex);
		}
	}

	private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);

		if (IsInit) return;

		try
		{
			IsInit = true;

			Logger = base.Logger;


			// TODO: ORGANIZE CODE!!!

			On.Menu.Remix.ConfigMenuTab.ButtonManager.SignalSave += ButtonManager_SignalSave; ;

			GraphicsHooks.Init();
			ObjectHooks.Init();
			OracleHooks.Init();
			MenuSceneHooks.Init();
			WinOrSaveHooks.Init();
			WorldHooks.Init();
			PlayerHooks.Init();

			ModOptions.RegisterOI();
			LoadResources();

			_ = SlidesShowIDs.ArtificerDreamE;


			var mod = ModManager.ActiveMods.FirstOrDefault(mod => mod.id == MOD_ID);

			MOD_NAME = mod.name;
			VERSION = mod.version;
			AUTHORS = mod.authors;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex);
		}
	}

	private void ButtonManager_SignalSave(On.Menu.Remix.ConfigMenuTab.ButtonManager.orig_SignalSave orig, Menu.Remix.ConfigMenuTab.ButtonManager self, Menu.Remix.MixedUI.UIfocusable trigger)
	{
		orig(self, trigger);

		SceneMaker.allCustomScenes = SceneMaker.AllValidScenes();
	}

	private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
	{
		// For other mod patches

		orig(self);

		try
		{
			isDMSEnabled = ModManager.ActiveMods.Exists((ModManager.Mod mod) => mod.id == "dressmyslugcat");
			isCRSEnabled = ModManager.ActiveMods.Exists((ModManager.Mod mod) => mod.id == "crs");

			if (isDMSEnabled)
				LoadDMSConfigs();

			GraphicsHooks.PostInit();
			ObjectHooks.PostInit();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex);
		}
	}

	private void LoadDMSConfigs()
	{
		//Log("DMS configs loaded");

		//SpriteDefinitions.AvailableSprites.Add(new SpriteDefinitions.AvailableSprite
		//{
		//	Name = "ARTIEXTRAS",
		//	Description = "Arti Extras",
		//	GallerySprite = "ScarHeadA0",
		//	RequiredSprites = new List<string>
		//{
		//	"TailPuff",

		//	"ScarHeadA0",
		//	"ScarHeadA1",
		//	"ScarHeadA2",
		//	"ScarHeadA3",
		//	"ScarHeadA4",
		//	"ScarHeadA5",
		//	"ScarHeadA6",
		//	"ScarHeadA7",
		//	"ScarHeadA8",
		//	"ScarHeadA9",
		//	"ScarHeadA10",
		//	"ScarHeadA11",
		//	"ScarHeadA12",
		//	"ScarHeadA13",
		//	"ScarHeadA14",
		//	"ScarHeadA15",
		//	"ScarHeadA16",
		//	"ScarHeadA17",

		//	"ScarLegsA0"
		//},
		//	Slugcats = new List<string>
		//{
		//	"Artificer"
		//}
		//});

		//SpriteSheet.Get("magica.artificer").ParseAtlases();

		//SpriteDefinitions.AvailableSprites.Add(new SpriteDefinitions.AvailableSprite
		//{
		//	Name = "BRAIDS",
		//	Description = "Braids",
		//	GallerySprite = "Braid",
		//	RequiredSprites = new List<string>
		//{
		//	"Braid"
		//},
		//	Slugcats = new List<string>
		//{
		//	"Spear"
		//}
		//});
	}

	public void LoadResources()
	{
		Futile.atlasManager.LoadAtlas("atlases/MagicaSprites");
		Futile.atlasManager.LoadAtlas("atlases/magicarainworldmsc");
		Futile.atlasManager.LoadAtlas("atlases/magicauisprites");
		Futile.atlasManager.LoadAtlas("atlases/iteratorsprites");

		Futile.atlasManager.LoadAtlas("atlases/artificer/scarheadleft");
		Futile.atlasManager.LoadAtlas("atlases/artificer/scarheadright");
		Futile.atlasManager.LoadAtlas("atlases/artificer/scarhead");
		Futile.atlasManager.LoadAtlas("atlases/artificer/scarlegsleft");
		Futile.atlasManager.LoadAtlas("atlases/artificer/scarlegsright");
		Futile.atlasManager.LoadAtlas("atlases/artificer/scarlegs");

		Futile.atlasManager.LoadAtlas("atlases/hunter/hunterscars");

		Futile.atlasManager.LoadAtlas("atlases/saint/saintscar");
		Futile.atlasManager.LoadAtlas("atlases/saint/saintscarfatique");
		Futile.atlasManager.LoadAtlas("atlases/saint/karmarings");

		Futile.atlasManager.LoadImage("projections/SRS_PROJ");
		DebugLog("Resources loaded");
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
		MISC
	}

	internal static bool Log(LogStates state, object v)
	{
		string msg = "";

		switch (state)
		{
			case LogStates.FailILMatch:
				msg = "INVALID MATCH: ";
				break;

			case LogStates.FailILInsert:
				msg = "INVALID IL: ";
					break;

			case LogStates.ILSuccess:
				msg = "IL SUCCEEDED: ";
				break;

			case LogStates.HookFail:
				msg = "HOOK FAILED: ";
				break;

			case LogStates.HooksSucceeded:
				msg = "HOOKS SUCCEEDED: ";
				break;
		}

		if (ErrorStates(state))
		{
			Logger.LogError(msg + v);
		}
		if (InfoStates(state))
		{
			Logger.LogInfo(msg + v);
		}

		return true;
	}

	private static bool InfoStates(LogStates state)
	{
		return state == LogStates.ILSuccess
			|| state == LogStates.HooksSucceeded
			|| state == LogStates.MISC;
	}

	private static bool ErrorStates(LogStates state)
	{
		return state == LogStates.FailILMatch
			|| state == LogStates.FailILInsert
			|| state == LogStates.HookFail;
	}
}
public static class Extensions
{
	public static void AddOneSpawnForSaint(this VoidSpawnKeeper keeper, PhysicalObject kill)
	{
		if (keeper.toRoom == -1)
		{
			return;
		}
		VoidSpawn voidSpawn = new(new AbstractPhysicalObject(keeper.room.world, AbstractPhysicalObject.AbstractObjectType.VoidSpawn, null, keeper.room.GetWorldCoordinate(kill.firstChunk.pos), keeper.room.game.GetNewID()), keeper.voidMeltInRoom + 0.2f, false);
		voidSpawn.PlaceInRoom(keeper.room);
		voidSpawn.abstractPhysicalObject.Move(keeper.room.GetWorldCoordinate(kill.firstChunk.pos));
		if (keeper.room.abstractRoom.name == "SB_L01")
		{
			voidSpawn.behavior = new VoidSpawn.VoidSeaDive(voidSpawn, keeper.room);
		}
		else if (ModManager.MSC && keeper.room.abstractRoom.name == "SB_E05SAINT")
		{
			voidSpawn.behavior = new VoidSpawn.SwimDown(voidSpawn, keeper.room);
		}
		else if (keeper.room.abstractRoom.name == "SH_D02" || keeper.room.abstractRoom.name == "SH_E02")
		{
			voidSpawn.behavior = new VoidSpawn.MillAround(voidSpawn, keeper.room);
		}
		else
		{
			voidSpawn.behavior = new VoidSpawn.PassThrough(voidSpawn, keeper.toRoom, keeper.room);
		}
		keeper.spawn.Add(voidSpawn);
		Plugin.DebugLog("Spawned voidspawn at: " + voidSpawn.abstractPhysicalObject.pos);
	}

	public static void AddNewScene(this SlideShow self, MenuScene.SceneID sceneID, float fadeIn, float fadeInDone, float fadeOutStart)
	{
		if (self.playList != null)
		{
			self.playList.Add(new(sceneID, fadeIn, fadeInDone, fadeOutStart));
		}
	}
}

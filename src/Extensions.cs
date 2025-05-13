using Menu;
using static MagicasContentPack.WorldHooks;
using System.Linq;
using UnityEngine;
using MoreSlugcats;


#pragma warning restore CS0618


namespace MagicasContentPack;

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

	public static bool ValidSaintWarmthInput(this Player player)
	{
		bool isValidMode = player.animation != Player.AnimationIndex.BellySlide && player.animation != Player.AnimationIndex.CrawlTurn && player.animation != Player.AnimationIndex.CorridorTurn && player.animation != Player.AnimationIndex.Flip && player.animation != Player.AnimationIndex.Roll && player.animation != Player.AnimationIndex.GrapplingSwing && player.animation != Player.AnimationIndex.RocketJump;
		return player.input[0].spec && isValidMode && !player.Stunned && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(player, out var magicaPlayer) && magicaPlayer.warmthLimit > 0;
	}

	public static int MaxAscensionActivationTimer(this Player player)
	{
		return 50;
	}

	public static int MaxAscensionTimer(this Player player)
	{
		int extra = 0;
		if (player.room != null && player.room.world != null && player.room.world.region.name == "HR")
		{
			extra = 2000;
		}
		return (player.KarmaIsReinforced ? 5400 : 5000) + extra;
	}

	public static float SaintAscensionRadius(this Player player)
	{
		return 500f;
	}

	public static float MaxAscensionBuffer(this Player player, BodyChunk obj)
	{
		float extraBuffer = 0f;
		if (obj != null && obj.owner is Creature creature)
		{
			extraBuffer = (GraphicsHooks.GetKarmaOfSpecificCreature(creature, creature.Template.type) + 1) * 15f;
		}
		return 20f + extraBuffer;
	}

	public static float AscensionFatique(this Player player)
	{
		return 20f;
	}

	public static int GetWarmthLimit(this Player player)
	{
		int limit = 1400;
		switch (player.KarmaCap)
		{
			case 0:
			case 1:
			case 2:
			case 3:
				limit = 0;
				break;
			case 4:
			case 5:
				limit = 2000;
				break;
			case 6:
			case 7:
				limit = 2600;
				break;
			case 8:
			case 9:
				limit = 3000;
				break;

		}
		return limit;
	}

	public static void AddNewScene(this SlideShow self, MenuScene.SceneID sceneID, float fadeIn, float fadeInDone, float fadeOutStart)
	{
		self.playList?.Add(new(sceneID, fadeIn, fadeInDone, fadeOutStart));
	}
}

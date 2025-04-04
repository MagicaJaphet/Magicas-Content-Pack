using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MagicasContentPack
{
	internal class WorldHooks
	{
		private static AbstractCreature nshOverseer;

		internal static void Init()
		{
			try
			{
				//On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues; ;

				On.VoidSpawnWorldAI.DirectionFinder.ctor += DirectionFinder_ctor;
				On.VoidSpawnWorldAI.Update += VoidSpawnWorldAI_Update;

				// Changes overseer spawn conditions for spearmaster comms ending
				On.WorldLoader.OverseerSpawnConditions += CustomOverseerSpawnConditions;
				On.WorldLoader.GeneratePopulation += OverseerAddAndKill;

				// To add overseer things and slideshow in spearmaster comms ending
				On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update += PostCommsSetUp;
				IL.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update += CommsEndingSlideshow;
				IL.RainWorldGame.GoToRedsGameOver += EndingPointers;

				// Allows slugcats into OE
				On.RegionGate.Update += SpearmasterOECheck;

				// Adds custom room scripts
				On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += CustomRoomSpecificEvents;
			}
			catch
			{
				Plugin.Log(Plugin.LogStates.HookFail, nameof(WorldHooks));
			}
		}

		private static void WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
		{
			orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);

			// It's as easy as that.
			if (playerCharacter != null && playerCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear && worldName == "LM")
			{
				self.playerCharacter = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
			}
		}

		private static void DirectionFinder_ctor(On.VoidSpawnWorldAI.DirectionFinder.orig_ctor orig, VoidSpawnWorldAI.DirectionFinder self, World world)
		{
			if (ModOptions.CustomMechanics.Value && ModManager.MSC && world.game.IsStorySession && world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				self.world = world;
				self.checkNext = new List<IntVector2>();
				self.matrix = new float[world.NumberOfRooms][];
				for (int i = 0; i < self.matrix.Length; i++)
				{
					self.matrix[i] = new float[world.GetAbstractRoom(i + world.firstRoomIndex).connections.Length];
					for (int j = 0; j < self.matrix[i].Length; j++)
					{
						self.matrix[i][j] = -1f;
					}
				}
				string name = world.region.name;
				string room = world.GetAbstractRoom(world.region.firstRoomIndex).name;
				if (name != null)
				{
					switch (name)
					{
						case "SH":
							room = "SH_D02";
							break;

						case "SB":
							room = "SB_E05SAINT";
							break;
					}
				}
				if (world.GetAbstractRoom(room) != null)
				{
					self.showToRoom = world.GetAbstractRoom(room).index;
				}
				AbstractRoom abstractRoom = world.GetAbstractRoom(self.showToRoom);
				for (int k = 0; k < abstractRoom.connections.Length; k++)
				{
					self.checkNext.Add(new IntVector2(abstractRoom.index - world.firstRoomIndex, k));
					self.matrix[abstractRoom.index - world.firstRoomIndex][k] = 0f;
				}
				return;
			}
			orig(self, world);
		}

		private static void VoidSpawnWorldAI_Update(On.VoidSpawnWorldAI.orig_Update orig, VoidSpawnWorldAI self)
		{
			if (ModOptions.CustomMechanics.Value && ModManager.MSC && self.world.game.IsStorySession && self.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				if (self.directionFinder == null)
				{
					if (!self.triedAddProgressionFinder)
					{
						self.triedAddProgressionFinder = true;
						VoidSpawnWorldAI.DirectionFinder directionFinder = new VoidSpawnWorldAI.DirectionFinder(self.world);
						if (!directionFinder.destroy)
						{
							self.directionFinder = directionFinder;
							return;
						}
					}
				}
				else if (!self.directionFinder.done)
				{
					self.directionFinder.Update();
				}
				return;
			}

			orig(self);
		}

		private static void OverseerAddAndKill(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
		{
			orig(self, fresh);

			if (ModOptions.CustomInGameCutscenes.Value && WinOrSaveHooks.SpearAchievedCommsEnd && self.world != null && self.world.region != null && self.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.world.region.name != "OE")
			{
				WorldCoordinate worldCoordinate = new(self.world.offScreenDen.index, -1, -1, 0);
				AbstractCreature nshOverseer = new(self.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, worldCoordinate, new EntityID(-1, 5));

				// Currently breaks a few spawner mods, need to investigate

				foreach (AbstractWorldEntity abstractEntity in self.world.GetAbstractRoom(worldCoordinate).entities)
				{
					if (abstractEntity != null && abstractEntity is AbstractCreature creature && creature.realizedCreature != null && creature.realizedCreature is Overseer overseer)
					{
						overseer.Destroy();
						break;
					}
				}

				if (self.world.GetAbstractRoom(worldCoordinate).offScreenDen)
				{
					self.world.GetAbstractRoom(worldCoordinate).entitiesInDens.Add(nshOverseer);
				}
				else
				{
					self.world.GetAbstractRoom(worldCoordinate).AddEntity(nshOverseer);
				}

				(nshOverseer.abstractAI as OverseerAbstractAI).SetAsPlayerGuide(2);
			}

		}

		private static bool CustomOverseerSpawnConditions(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugcatStats.Name character)
		{
			if (ModOptions.CustomInGameCutscenes.Value && ModManager.MSC && character == MoreSlugcatsEnums.SlugcatStatsName.Spear && WinOrSaveHooks.SpearAchievedCommsEnd)
			{
				return self.world.region.name != "SS" && self.world.region.name != "DM" && self.game.GetStorySession.saveState.denPosition != "SI_A07";
			}

			return orig(self, character);
		}

		private static void PostCommsSetUp(On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.orig_Update orig, MSCRoomSpecificScript.SpearmasterEnding self, bool eu)
		{
			if (ModOptions.CustomInGameCutscenes.Value && self.room.game.GetStorySession.saveState.denPosition == self.room.abstractRoom.name && self.room.world.rainCycle.timer < 400 && WinOrSaveHooks.SpearAchievedCommsEnd)
			{
				if (nshOverseer == null)
				{
					WorldCoordinate worldCoordinate = new(self.room.world.offScreenDen.index, -1, -1, 0);

					foreach (AbstractWorldEntity abstractWorldEntity in self.room.world.GetAbstractRoom(worldCoordinate).entities)
					{
						if (abstractWorldEntity != null && abstractWorldEntity is AbstractCreature abstractCreature && abstractCreature.creatureTemplate == StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer) && abstractCreature.abstractAI is OverseerAbstractAI aI && aI.ownerIterator == 2)
						{
							nshOverseer = abstractWorldEntity as AbstractCreature;
							break;
						}
					}
				}

				if (nshOverseer != null && nshOverseer.abstractAI is OverseerAbstractAI overseerAI)
				{
					overseerAI.BringToRoomAndGuidePlayer(self.room.abstractRoom.index);
					self.room.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.likesPlayer = 0.84f;
					self.room.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.wantDirectionHandHoldingThisCycle = 0.96f;
					overseerAI.freezeDestination = true;
				}
			}

			orig(self, eu);
		}

		private static void CommsEndingSlideshow(ILContext il)
		{
			ILCursor cursor = new(il);

			bool first = cursor.TryGotoNext(
				MoveType.Before,
			instruction => instruction.MatchStfld<ProcessManager>("statsAfterCredits")
			);

			if (!first)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(CommsEndingSlideshow));
			}

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldarg_1);
			cursor.EmitDelegate((MSCRoomSpecificScript.SpearmasterEnding self, bool eu) =>
			{
				self.room.game.GoToRedsGameOver();
				return;
			});
		}


		// Adds the scene ID for the outro
		private static void EndingPointers(ILContext il)
		{
			ILCursor cursor = new(il);

			bool first = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchStfld<DeathPersistentSaveData>(nameof(DeathPersistentSaveData.redsDeath))
				);

			if (!first)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(EndingPointers) + " #1");
			}

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate((RainWorldGame self) => {
				if (WinOrSaveHooks.HunterOracleID != null)
				{
					if (self.Players[0].realizedCreature != null && (self.Players[0].realizedCreature as Player).redsIllness != null)
					{
						Plugin.DebugLog("Set hunter oracleID to (check): " + WinOrSaveHooks.HunterOracleID);
						self.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<string>(nameof(WinOrSaveHooks.HunterOracleID), WinOrSaveHooks.HunterOracleID);

						self.manager.nextSlideshow = MagicaEnums.SlidesShowIDs.RedsDeath;
						self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow, 0f);
						WinOrSaveHooks.redEndingProcedure = false;
					}
				}
			});

			bool second = cursor.TryGotoNext(
				MoveType.Before,
				instruction => instruction.MatchLdfld<MainLoopProcess>("manager"),
				instruction => instruction.MatchLdsfld<ProcessManager.ProcessID>("Statistics")

			);

			if (!second)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(EndingPointers) + " #2");
			}

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate((RainWorldGame self) =>
			{
				Plugin.DebugLog("Spearmaster alt outro scene successfully initalized");

				if (ModManager.MSC && ModOptions.CustomSlideShows.Value)
				{
					if (self.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						self.manager.statsAfterCredits = true;
						self.manager.nextSlideshow = MagicaEnums.SlidesShowIDs.SpearAltOutro;
						self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
						return;
					}
				}
			});

		}

		private static void CustomRoomSpecificEvents(On.MoreSlugcats.MSCRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
		{
			string name = room.abstractRoom.name;

			if (name == "7S_AI")
			{
				room.AddObject(new SevenRedSunsRoomConditions(room));
			}

			if (name == "7S_E01")
			{
				room.AddObject(new SevenRedSunsMuralRoom(room));
			}

			if (name == "SH_E01" && !ModOptions.CustomInGameCutscenes.Value || (room.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Spear && OracleHooks.CheckIfWhoTookThePearlIsBeforeCurrent(room.game.TimelinePoint)))
			{
				for (int i = 0; i < room.updateList.Count; i++)
				{
					if (room.updateList[i] is HalcyonPearl pearl)
					{
						pearl.Destroy();
						Plugin.DebugLog("RM pearl already shown to FP, deleting from world");
						break;
					}
				}
			}

			if (ModOptions.CustomInGameCutscenes.Value)
			{
				if (name == "OE_FINAL03" && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					Plugin.DebugLog("Spearmaster found Arti!");
					room.AddObject(new OESpearInteraction(room));
					return;
				}

				if (ValidDepthsRooms.Contains(room.abstractRoom.name) && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					Plugin.DebugLog("Spearmaster has abandoned SRS.");
					room.AddObject(new SpearDepthsChat(room));
					return;
				}

				orig(room);

				if (name == "GATE_SB_OE" && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					Plugin.DebugLog("Spearmaster OE cutscene initiated.");
					room.AddObject(new OE_GateOpen(room));
					return;
				}
				return;
			}
			else
			{
				orig(room);
			}

		}

		private static void SpearmasterOECheck(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
		{
			orig(self, eu);

			string name = self.room.abstractRoom.name;

			if (self.room.game != null && WinOrSaveHooks.OEGateOpenedAsSpear && name == "GATE_SB_OE")
			{
				self.Unlock();
			}
		}

		public static readonly List<string> ValidDepthsRooms = new()
		{
			"SB_D01",
			"SB_C08",
			"SB_E01",
			"SB_E06",
			"SB_C09",
			"SB_L01",
		};
	}

	internal class SevenRedSunsMuralRoom : UpdatableAndDeletable
	{
		public SevenRedSunsMuralRoom(Room room)
		{
			this.room = room;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			for (int i = 0; i < room.updateList.Count; i++)
			{
				if (room.updateList[i] != null && room.updateList[i] is Player player && player != null)
				{
					room.gravity = Mathf.InverseLerp(700f, room.PixelHeight - 400f, player.firstChunk.pos.y);
				}
			}
		}
	}

	internal class SevenRedSunsRoomConditions : UpdatableAndDeletable
	{
		private RoomSettings.RoomEffect darkness;

		public SevenRedSunsRoomConditions(Room room)
		{
			this.room = room;
			for (int j = 0; j < room.roomSettings.effects.Count; j++)
			{
				if (room.roomSettings.effects[j].type == RoomSettings.RoomEffect.Type.Darkness)
				{
					darkness = room.roomSettings.effects[j];
				}
			}
		}

		public override void Update(bool eu)
		{
			base.Update(eu);


			for (int i = 0; i < room.updateList.Count; i++)
			{
				if (room.updateList[i] != null && room.updateList[i] is Player player && player != null)
				{
					if (player.firstChunk.pos.y > 740f)
					{
						room.gravity = Mathf.InverseLerp(room.PixelWidth - 1200f, 700f, player.firstChunk.pos.x);
					}

					if (room.game.cameras[0]?.currentCameraPosition > 4 || room.game.cameras[0]?.currentCameraPosition == 0)
					{
						room.roomSettings.Clouds = 0f;
						if (darkness != null) { darkness.amount = 0f; }
					}
					else if (room.game.cameras[0]?.currentCameraPosition != null)
					{
						room.roomSettings.Clouds = 1f;
						if (darkness != null) { darkness.amount = Mathf.Lerp(0f, 1f, (float)room.game.cameras[0].currentCameraPosition / 5f); }
					}
				}
			}
		}
	}

	internal class SpearDepthsChat : UpdatableAndDeletable
	{

		private string currentRoom;
		private readonly ChatlogData.ChatlogID fullChatlog;
		public int chatNum;

		private static string chatA = "Magicachatlog_SBFullA";
		private static string chatB = "Magicachatlog_SBFullB";
		private static string chatC = "Magicachatlog_SBFullC";
		private bool readyForNewChat;

		public SpearDepthsChat(Room room)
		{
			this.room = room;
			currentRoom = "";

			if (room.game.GetStorySession.saveState != null && room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0 && WinOrSaveHooks.SpearAchievedCommsEnd)
			{
				ExtEnumBase.TryParse(typeof(ChatlogData.ChatlogID), chatB, true, out var chatlog);
				fullChatlog = chatlog as ChatlogData.ChatlogID;
			}
			else if (room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0 && !WinOrSaveHooks.SpearAchievedCommsEnd)
			{
				ExtEnumBase.TryParse(typeof(ChatlogData.ChatlogID), chatC, true, out var chatlog);
				fullChatlog = chatlog as ChatlogData.ChatlogID;
			}
			else
			{
				ExtEnumBase.TryParse(typeof(ChatlogData.ChatlogID), chatA, true, out var chatlog);
				fullChatlog = chatlog as ChatlogData.ChatlogID;
			}
		}

		public override void Update(bool eu)
		{
			if (!WorldHooks.ValidDepthsRooms.Contains(room.abstractRoom.name))
			{
				if (room != null && room.game != null && room.game.cameras != null && room.game.cameras[0].hud.chatLog != null)
				{
					room.game.cameras[0].hud.DisposeChatLog();
					room.game.cameras[0].hud.chatLog = null;
				}
				Destroy();
			}

			if (room != null && currentRoom != room.abstractRoom.name && room.game != null && room.game.cameras != null && room.game.cameras[0].hud != null)
			{
				currentRoom = room.abstractRoom.name;
				chatNum++;
				readyForNewChat = true;
			}


			if (room != null && room.game != null && room.game.cameras != null && room.game.cameras[0].hud != null && room.game.cameras[0].hud.chatLog == null && chatNum < 6 && readyForNewChat)
			{
				readyForNewChat = false;
				Plugin.DebugLog("A");
				room.game.cameras[0].hud.InitChatLog(ChatlogData.getChatlog(fullChatlog));
				if (room.game.cameras[0].hud.owner is Player)
				{
					(room.game.cameras[0].hud.owner as Player).abstractCreature.world.game.pauseUpdate = false;
				}
				room.game.cameras[0].hud.chatLog.mainAlpha = 0.6f;
				room.game.cameras[0].hud.chatLog.disable_fastDisplay = true;
				return;
			}
		}
	}

	public class RemoveHalyconPearl : UpdatableAndDeletable
	{
		public RemoveHalyconPearl(Room room)
		{
			this.room = room;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			for (int i = 0; i < room.physicalObjects.Length; i++)
			{
				for (int j = 0; j < room.physicalObjects[i].Count; j++)
				{
					if (room.physicalObjects[i][j] is HalcyonPearl)
					{
						room.physicalObjects[i][j].Destroy();
						Plugin.DebugLog("RM pearl already shown to FP, deleting from world");
						Destroy();

					}
				}
			}

			Destroy();
		}
	}

	public class OESpearInteraction : MSCRoomSpecificScript.ArtificerDream
	{
		private readonly Room room;
		private Player foundPlayer;
		private bool setController;
		private AbstractCreature artiPuppet;
		private Player artiPlayerPuppet;
		private AbstractCreature pupPuppet;
		private Player pupPlayerPuppet;
		private AbstractCreature pupPuppet2;
		private Player pupPlayerPuppet2;

		private AbstractCreature SRSoverseer;

		public OESpearInteraction(Room room)
		{
			this.room = room;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			float num = 1490f;

			if (foundPlayer != null && foundPlayer.firstChunk.pos.x < num && !setController)
			{
				Plugin.DebugLog("Cutscene OE Spear triggered");
				RainWorld.lockGameTimer = true;
				setController = true;
				sceneStarted = true;
			}


			if (artiPlayerPuppet != null && pupPlayerPuppet != null)
			{
				artiPlayerPuppet.standing = true;
				pupPlayerPuppet.standing = true;
				pupPlayerPuppet2.standing = true;
			}
		}

		public override void SceneSetup()
		{

			if (!ModManager.CoopAvailable)
			{
				if (foundPlayer == null && room.game.Players.Count > 0 && room.game.Players[0].realizedCreature != null && room.game.Players[0].realizedCreature.room == room)
				{
					foundPlayer = (room.game.Players[0].realizedCreature as Player);
				}
				if (foundPlayer == null || foundPlayer.inShortcut || room.game.Players[0].realizedCreature.room != room)
				{
					return;
				}
			}
			else
			{
				if (foundPlayer == null && room.PlayersInRoom.Count > 0 && room.PlayersInRoom[0] != null && room.PlayersInRoom[0].room == room)
				{
					foundPlayer = room.PlayersInRoom[0];
				}
				if (foundPlayer == null || foundPlayer.inShortcut || foundPlayer.room != room)
				{
					return;
				}
			}

			if (SRSoverseer == null)
			{
				WorldCoordinate worldCoordinate = new(room.world.offScreenDen.index, -1, -1, 0);
				SRSoverseer = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, worldCoordinate, new EntityID(-1, 5));
				if (room.world.GetAbstractRoom(worldCoordinate).offScreenDen)
				{
					room.world.GetAbstractRoom(worldCoordinate).entitiesInDens.Add(SRSoverseer);
				}
				else
				{
					room.world.GetAbstractRoom(worldCoordinate).AddEntity(SRSoverseer);
				}
			}

			if (artiPuppet == null)
			{
				room.game.wasAnArtificerDream = true;
				Vector2 vector2 = new(428f, 608f);
				artiPuppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, room.ToWorldCoordinate(vector2), room.game.GetNewID());
				artiPuppet.state = new PlayerState(artiPuppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
				(artiPuppet.state as PlayerState).forceFullGrown = true;
				room.abstractRoom.AddEntity(artiPuppet);
				artiPuppet.RealizeInRoom();

				Vector2 vector4 = new(428f, 618f);
				pupPuppet2 = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, room.ToWorldCoordinate(vector4), room.game.GetNewID());
				pupPuppet2.ID.setAltSeed(1000);
				pupPuppet2.state = new PlayerState(pupPuppet2, 0, MoreSlugcatsEnums.SlugcatStatsName.Slugpup, true);
				room.abstractRoom.AddEntity(pupPuppet2);
				pupPuppet2.RealizeInRoom();

				Vector2 vector3 = new(1335f, 200f);
				pupPuppet = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, room.ToWorldCoordinate(vector3), room.game.GetNewID());
				pupPuppet.ID.setAltSeed(1001);
				pupPuppet.state = new PlayerState(pupPuppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Slugpup, true);
				room.abstractRoom.AddEntity(pupPuppet);
				pupPuppet.RealizeInRoom();
			}
			if (artiPuppet != null && artiPlayerPuppet == null && artiPuppet.realizedCreature != null)
			{
				artiPlayerPuppet = (artiPuppet.realizedCreature as Player);
				artiPlayerPuppet.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
				artiPlayerPuppet.controller = new Player.NullController();
			}
			if (pupPuppet != null && pupPlayerPuppet == null && pupPuppet.realizedCreature != null)
			{
				pupPlayerPuppet = (pupPuppet.realizedCreature as Player);
				pupPlayerPuppet.controller = new Player.NullController();
			}
			if (pupPuppet2 != null && pupPlayerPuppet2 == null && pupPuppet2.realizedCreature != null)
			{
				pupPlayerPuppet2 = (pupPuppet2.realizedCreature as Player);
				pupPlayerPuppet2.controller = new Player.NullController();
			}
			if (artiPlayerPuppet != null && pupPuppet2.realizedCreature != null && pupPlayerPuppet2 != null && artiPlayerPuppet.CanPutSlugToBack)
			{
				pupPlayerPuppet2.SuperHardSetPosition(artiPlayerPuppet.firstChunk.pos);
				artiPlayerPuppet.slugOnBack.SlugToBack(pupPlayerPuppet2);
			}
		}

		public override void TimedUpdate(int timer)
		{
			base.TimedUpdate(timer);

			if (foundPlayer.firstChunk.pos.x < 1490f && sceneTimer < 1)
			{
				pupPlayerPuppet.controller = new MSCRoomSpecificScript.ArtificerDream.StartController(this, 0);
				foundPlayer.controller = new Player.NullController();
				if (room.PlayersInRoom.Count > 0)
				{
					for (int i = 0; i < room.PlayersInRoom.Count; i++)
					{
						room.PlayersInRoom[i].controller = new Player.NullController();
					}
				}
				room.game.cameras[0].followAbstractCreature = pupPlayerPuppet.abstractCreature;
				(SRSoverseer.abstractAI as OverseerAbstractAI).PlayerGuideGoAway(300);
			}

			if (sceneTimer == 128)
			{
				artiPlayerPuppet.controller = new MSCRoomSpecificScript.ArtificerDream.StartController(this, 1);
			}

			if ((sceneTimer > 195 && sceneTimer < 302 || sceneTimer > 370) && artiPlayerPuppet.canJump > 0)
			{
				artiPlayerPuppet.Jump();
			}

			if (sceneTimer == 290)
			{
				foundPlayer.standing = false;
			}

			if (sceneTimer == 370)
			{
				room.game.cameras[0].followAbstractCreature = foundPlayer.abstractCreature;
				foundPlayer.standing = true;
				foundPlayer.flipDirection = 1;
			}
			if (sceneTimer == 390)
			{
				SRSoverseer.ignoreCycle = true;
				(SRSoverseer.abstractAI as OverseerAbstractAI).SetAsPlayerGuide(3);
				(SRSoverseer.abstractAI as OverseerAbstractAI).BringToRoomAndGuidePlayer(room.abstractRoom.index);
			}

			if (sceneTimer == 640)
			{
				room.PlaySound(SoundID.Fire_Spear_Explode, artiPlayerPuppet.firstChunk);

				foundPlayer.controller = null;
				if (room.PlayersInRoom.Count > 0)
				{
					for (int i = 0; i < room.PlayersInRoom.Count; i++)
					{
						room.PlayersInRoom[i].controller = null;
					}
				}

			}
			if (sceneTimer > 720)
			{
				artiPlayerPuppet.Destroy();
				pupPlayerPuppet.Destroy();
				pupPlayerPuppet2.Destroy();
				Destroy();
			}
		}

		public override Player.InputPackage GetInput(int index)
		{
			bool jmp = false;
			int x = 0;
			int y = 0;

			if (sceneTimer < 2000)
			{
				if (index == 0)
				{
					if (sceneTimer < 132)
					{
						return new Player.InputPackage(true, Options.ControlSetup.Preset.None, -1, 1, true, false, false, false, false);
					}
				}

				if (index == 1)
				{
					bool grb = false;

					if (sceneTimer < 130)
					{
						x = 1;
						y = 1;
						jmp = true;
						artiPlayerPuppet.Jump();
					}
					if (sceneTimer > 135 && sceneTimer < 140)
					{
						return new Player.InputPackage(true, Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
					}
					if (sceneTimer < 150)
					{
						x = 1;
					}
					if (sceneTimer > 190 && sceneTimer < 195)
					{
						grb = true;
					}
					if (sceneTimer > 195 && sceneTimer < 302)
					{
						x = 1;
						y = 1;
						jmp = true;
					}

					if (sceneTimer > 360 && sceneTimer < 365)
					{
						x = 1;
						y = 1;
						jmp = true;
						artiPlayerPuppet.Jump();
					}

					if (sceneTimer > 365 && sceneTimer < 370)
					{
						return new Player.InputPackage(true, Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
					}
					if (sceneTimer > 370)
					{
						x = 1;
						y = 1;
						jmp = true;
					}

					return new Player.InputPackage(true, Options.ControlSetup.Preset.None, x, y, jmp, false, grb, false, false);
				}
			}
			return new Player.InputPackage(true, Options.ControlSetup.Preset.None, x, y, jmp, false, false, false, false);
		}
	}

	public class OE_GateOpen : UpdatableAndDeletable
	{
		public OE_GateOpen(Room room)
		{
			this.room = room;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
			if (room.game.Players.Count > 0 && firstAlivePlayer != null && firstAlivePlayer.realizedCreature != null && firstAlivePlayer.realizedCreature.room == room)
			{
				Player player = firstAlivePlayer.realizedCreature as Player;

				if (SMGateState == SMOEGateState.START && player.mainBodyChunk.pos.x < 600f)
				{

					if (SRSoverseer == null)
					{
						WorldCoordinate worldCoordinate = new(room.world.offScreenDen.index, -1, -1, 0);
						SRSoverseer = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, worldCoordinate, new EntityID(-1, 5));
						if (room.world.GetAbstractRoom(worldCoordinate).offScreenDen)
						{
							room.world.GetAbstractRoom(worldCoordinate).entitiesInDens.Add(SRSoverseer);
						}
						else
						{
							room.world.GetAbstractRoom(worldCoordinate).AddEntity(SRSoverseer);
						}
					}

					SMEndingPearl = null;
					int grasp = 0;
					for (int k = 0; k < player.grasps.Length; k++)
					{
						if (player.grasps[k] != null && player.grasps[k].grabbed is SpearMasterPearl)
						{
							grasp = k;
							SMEndingPearl = (player.grasps[k].grabbed as SpearMasterPearl);
							if (!(SMEndingPearl.abstractPhysicalObject as SpearMasterPearl.AbstractSpearMasterPearl).broadcastTagged)
							{
								SMEndingPearl = null;
							}
						}
					}
					if (SMEndingPearl != null)
					{
						player.standing = true;
						player.controller = new Player.NullController();
						player.ReleaseGrasp(grasp);
						SMEndingPearl.ChangeCollisionLayer(1);
						SMEndingPearl.SetLocalGravity(0f);
						SMEndingPearl.firstChunk.vel.y = -9f;
						SMGateState = SMOEGateState.GATE;
					}
				}

				if (SMGateState == SMOEGateState.GATE)
				{
					SMOETimer += 1f;
					Vector2 vector = new(570f, 300f);
					float num = Custom.Dist(SMEndingPearl.firstChunk.pos, vector) * 0.01f + Mathf.Max(1f, 20f * (1f - SMOETimer / 240f));
					BodyChunk firstChunk = SMEndingPearl.firstChunk;
					firstChunk.vel.x += (vector.x - SMEndingPearl.firstChunk.pos.x) * (0.0003f + SMOETimer * 0.0002f);
					firstChunk.vel.y += (vector.y - SMEndingPearl.firstChunk.pos.y) * (0.0003f + SMOETimer * 0.0002f);

					Vector2 lightningRange1 = new(UnityEngine.Random.Range(102f, 858f), 356f);
					Vector2 lightningRange2 = new(UnityEngine.Random.Range(102f, 858f), 150f);

					if (firstChunk.pos.y < 530f && firstChunk.pos.x > 445f && firstChunk.pos.x < 500f)
					{
						BodyChunk bodyChunk = firstChunk;
						bodyChunk.vel.x++;
					}
					if (SMEndingPearl.firstChunk.vel.magnitude > num)
					{
						SMEndingPearl.firstChunk.vel = SMEndingPearl.firstChunk.vel.normalized * num;
					}
					if ((SMOETimer > 50f && SMOETimer % 15f == 0f) || (SMOETimer > 150f && SMOETimer % 6f == 0f) || (SMOETimer > 200f && SMOETimer % 3f == 0f))
					{
						LightningBolt lightningBolt = new(lightningRange1, lightningRange2, 0, 0.7f, UnityEngine.Random.Range(0.4f, 2f), 0.3f, 0.3f, true)
						{
							intensity = 1f,
							color = Color.green
						};
						room.AddObject(lightningBolt);
						room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, lightningRange2, 0.5f, 1.4f - UnityEngine.Random.value * 0.4f);
					}
					if (SMOETimer == 65f)
					{
						room.AddObject(new ElectricDeath.SparkFlash(SMEndingPearl.firstChunk.pos, 0.75f + UnityEngine.Random.value));
						room.AddObject(new ShockWave(SMEndingPearl.firstChunk.pos, 50f, 1f, 30, false));
					}
					if ((SMOETimer > 70f && SMOETimer % 12f == 0f) || (SMOETimer > 150f && SMOETimer % 6f == 0f))
					{
						room.AddObject(new MSCRoomSpecificScript.SpearmasterEnding.AttractedGlyph(SMEndingPearl.firstChunk.pos, -SMEndingPearl.firstChunk.vel + Custom.RNV() * ((SMOETimer < 120f) ? 10f : 5f), vector, (SMOETimer < 120f) ? 90 : 30));
						room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Data_Bit, SMEndingPearl.firstChunk.pos, 1f, 0.5f + UnityEngine.Random.value * 2f);
						room.AddObject(new Spark(SMEndingPearl.firstChunk.pos, new(UnityEngine.Random.Range(-5f, 5f), 10f), Color.green, null, 11, 28));
					}
					if (SMOETimer == 230f)
					{
						room.AddObject(new ElectricDeath.SparkFlash(SMEndingPearl.firstChunk.pos, 0.75f + UnityEngine.Random.value));
						room.PlaySound(SoundID.Fire_Spear_Explode, SMEndingPearl.firstChunk.pos, 0.6f, 1f);
						SMGateState = SMOEGateState.OPEN;
					}
				}

				if (SMGateState == SMOEGateState.OPEN)
				{
					SMOETimer += 1f;
					SMEndingPearl.Destroy();

					if (SMOETimer > 250f)
					{
						SMGateState = SMOEGateState.END;
					}

				}

				if (SMGateState == SMOEGateState.END)
				{
					player.controller = null;

					WinOrSaveHooks.OEGateOpenedAsSpear = true;
					(room.world.game.session as StoryGameSession).saveState.miscWorldSaveData.playerGuideState.InfluenceLike(100f, false);
					room.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.wantDirectionHandHoldingThisCycle = 0.96f;


					SRSoverseer.ignoreCycle = true;
					(SRSoverseer.abstractAI as OverseerAbstractAI).SetAsPlayerGuide(3);
					(SRSoverseer.abstractAI as OverseerAbstractAI).BringToRoomAndGuidePlayer(room.abstractRoom.index);

				}
			}
		}

		public enum SMOEGateState
		{
			START,
			GATE,
			OPEN,
			END
		}

		public OE_GateOpen.SMOEGateState SMGateState;

		public float SMOETimer;

		public SpearMasterPearl SMEndingPearl;

		public AbstractCreature SRSoverseer;


	}
}
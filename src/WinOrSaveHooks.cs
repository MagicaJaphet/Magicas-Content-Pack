using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicasContentPack
{
    internal class WinOrSaveHooks
	{
		public static int artiDreamNumber;

		public static bool ArtiKilledScavKing;
		public static bool SpearAchievedCommsEnd;
		public static int HunterScarProgression; // 3 - 0
		internal static bool redEndingProcedure;
		public static bool HunterHasGreenNeuron;

		internal static void Init()
		{
			try
			{
				// Fun statistics!
				On.Menu.StoryGameStatisticsScreen.TickerIsDone += StoryGameStatisticsScreen_TickerIsDone;
				IL.Menu.StoryGameStatisticsScreen.GetDataFromGame += StoryGameStatisticsScreen_ctor;

				//// For menuscene shenanijans and other cycle based stuff
				On.ShelterDoor.ctor += ResetShelterRoomInformation;
				On.ShelterDoor.DoorClosed += GatherShelterRoomInformation;

				// Changes various dream things
				On.DreamsState.StaticEndOfCycleProgress += CheckDreamProgress;
				On.SaveState.ctor += DreamChecks;
				On.RainWorldGame.Win += SpearDreams;
				On.RainWorldGame.ArtificerDreamEnd += ArtiDreamEndSlideshow;

				// Fixes saint not getting the scholar passage given there is a new colored pearl
				On.SlugcatStats.PearlsGivePassageProgress += GiveBackSaintProgress;

				On.PlayerProgression.SaveWorldStateAndProgression += SaveSlugcatData;
				On.PlayerProgression.WipeSaveState += WipeSlugcatData;
				On.PlayerProgression.WipeAll += WipeAllData;

				On.SaveState.LoadGame += SaveState_LoadGame;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}


		private static bool SaveSlugcatData(On.PlayerProgression.orig_SaveWorldStateAndProgression orig, PlayerProgression self, bool malnourished)
		{
			MagicaSaveState.SaveFile(self.currentSaveState.saveStateNumber.value);

			return orig(self, malnourished);
		}

		private static void WipeSlugcatData(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
		{
			MagicaSaveState.WipeSave(saveStateNumber.value);

			if (saveStateNumber == SlugcatStats.Name.Red)
				HunterScarProgression = 3;

			orig(self, saveStateNumber);
		}

		private static void WipeAllData(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
		{
			MagicaSaveState.WipeSave();

			orig(self);
		}

		private static void StoryGameStatisticsScreen_TickerIsDone(On.Menu.StoryGameStatisticsScreen.orig_TickerIsDone orig, StoryGameStatisticsScreen self, StoryGameStatisticsScreen.Ticker ticker)
		{
			orig(self, ticker);

			if (ticker.ID == MagicaEnums.TickerIDs.HelpedBSM)
			{
				self.scoreKeeper.AddScoreAdder(100, 1);
			}
		}

		private static void StoryGameStatisticsScreen_ctor(MonoMod.Cil.ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool succeed = cursor.TryGotoNext(
					x => x.MatchLdfld<Menu.Menu>(nameof(Menu.Menu.pages)),
					x => x.MatchLdcI4(0),
					x => x.MatchCallvirt(out _),
					x => x.MatchLdfld(out _),
					x => x.MatchLdarg(0)
					);

				if (Plugin.ILMatchFail(succeed))
					return;

				cursor.Emit(OpCodes.Ldarg_1);
				cursor.Emit(OpCodes.Ldloc, 0);
				cursor.Emit(OpCodes.Ldloc, 4);
				static void AddCustomPops(StoryGameStatisticsScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package, Vector2 pos, int index)
				{
					if (package.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						Plugin.DebugLog("Custom ticker IDs being checked...");
						if (MagicaSaveState.GetKey(package.saveState.saveStateNumber.value, nameof(SaveValues.CLSeenMoonPearl), out bool _))
						{
							StoryGameStatisticsScreen.Popper helpedMoon = new(self, self.pages[0], pos + new Vector2(0f, -30f * (float)index), "<" + self.Translate("Helped Big Sis Moon") + ">", MagicaEnums.TickerIDs.HelpedBSM);
							self.allTickers.Add(helpedMoon);
							self.pages[0].subObjects.Add(helpedMoon);
							index++;
						}
					}
				}
				cursor.EmitDelegate(AddCustomPops);
				cursor.Emit(OpCodes.Ldarg_0);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void ResetShelterRoomInformation(On.ShelterDoor.orig_ctor orig, ShelterDoor self, Room room)
		{
			SceneMaker.paletteTexture = null;
			SceneMaker.fadePaletteTexture = null;

			if (IteratorHooks.SLOracleBehaviorHooks.moonRevivedThisCycle)
			{
				IteratorHooks.SLOracleBehaviorHooks.moonRevivedThisCycle = false;
			}

			orig(self, room);
		}

		private static void GatherShelterRoomInformation(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
		{
			if (self.room != null && self.room.roomSettings != null && self.room.game != null && self.room.game.cameras[0] != null)
			{
				if (SceneMaker.paletteTexture == null)
				{
					self.room.game.cameras[0].LoadPalette(self.room.roomSettings.Palette, ref SceneMaker.paletteTexture);

					SceneMaker.sleepFadeAmount = 0;
					if (self.room.roomSettings.fadePalette != null)
					{
						self.room.game.cameras[0].LoadPalette(self.room.roomSettings.fadePalette.palette, ref SceneMaker.fadePaletteTexture);

						SceneMaker.sleepFadeAmount = self.room.roomSettings.fadePalette.fades[0];
					}

					RoomSettings.RoomEffect darkness = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness);
					if (darkness != null)
					{
						SceneMaker.roomDarknesss = darkness.GetAmount(0);
					}
					// (-1 = left / 1 = right / 0 neutral, -1 = down / 1 = up / 0 neutral)
					SceneMaker.shelterDirection = self.room.shelterDoor.dir;
					Plugin.DebugLog("main palette: " + SceneMaker.sleepPalette.ToString() + " | fade palette: " + SceneMaker.sleepFadePalette.ToString() + " | fade amount: " + SceneMaker.sleepFadeAmount.ToString() + " | shelterDir: " + SceneMaker.shelterDirection.ToString());
				}
			}

			HunterHasGreenNeuron = false;
			SceneMaker.DreamScenes.slugpupNum = 0;

			if (self.room.game.StoryCharacter == SlugcatStats.Name.Red)
			{
				SceneMaker.DreamScenes.slugpupColors = new();

				for (int m = 0; m < self.room.physicalObjects.Length; m++)
				{
					for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
					{
						if (self.room.physicalObjects[m][n] != null && self.room.physicalObjects[m][n] is Player player && player.isNPC)
						{
							SceneMaker.DreamScenes.slugpupNum++;
							if (player.npcStats != null)
							{
								SceneMaker.DreamScenes.slugpupColors.Add(player.ShortCutColor());
								if (self.room.game.cameras[0] != null)
								{
									Color color = self.room.game.cameras[0].currentPalette.blackColor;
									SceneMaker.DreamScenes.slugpupColors.Add(player.npcStats.Dark ? new Color(1f, 1f, 1f) : new Color(0f, 0f, 0f));
								}
							}
						}
					}
				}
			}

			if (self.room.PlayersInRoom.Count > 0 && self.room.game.StoryCharacter == SlugcatStats.Name.Red)
			{
				if (self.room.game.rainWorld != null && self.room.game.rainWorld.progression != null && self.room.game.rainWorld.progression.currentSaveState != null)
				{
					SceneMaker.DreamScenes.lastCycleCount = RedsIllness.RedsCycles(self.room.game.rainWorld.progression.currentSaveState.redExtraCycles) - self.room.game.rainWorld.progression.currentSaveState.cycleNumber;
				}

				for (int i = 0; i < self.room.PlayersInRoom.Count; i++)
				{
					if (self.room.PlayersInRoom[i].objectInStomach != null && self.room.PlayersInRoom[i].objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
					{
						HunterHasGreenNeuron = true;
						break;
					}
				}
				for (int j = 0; j < self.room.physicalObjects.Length; j++)
				{
					if (HunterHasGreenNeuron) { break; }

					for (int h = 0; h < self.room.physicalObjects[j].Count; h++)
					{
						if (self.room.physicalObjects[j][h] != null && self.room.physicalObjects[j][h].abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
						{
							HunterHasGreenNeuron = true;
							break;
						}
					}
				}
			}

			if (self.room.PlayersInRoom.Count > 0 && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				if (self.room.game.rainWorld != null && self.room.game.rainWorld.progression != null && self.room.game.rainWorld.progression.currentSaveState != null && self.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData != null)
				{
					SceneMaker.DreamScenes.lastKarmaCap = self.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData.karmaCap;
				}

				for (int i = 0; i < self.room.PlayersInRoom.Count; i++)
				{
					if (self.room.PlayersInRoom[i] != null && self.room.PlayersInRoom[i].objectInStomach != null && self.room.PlayersInRoom[i].objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.Lantern)
					{
						SceneMaker.lanternInStomach = true;
						break;
					}
				}
			}

			orig(self);
		}

		private static void CheckDreamProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
		{
			orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);

			if (saveState != null && saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && upcomingDream != DreamsState.DreamID.Pebbles)
			{
				upcomingDream = null;
				Plugin.DebugLog("Spear tried to have a bad dream, yeeteth");
				return;
			}

			// Used for dreamscreen art progression
			artiDreamNumber = familyThread;
		}

		private static void DreamChecks(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
		{
			if (ModOptions.CustomDreams.Value)
			{
				if (ModManager.MSC && saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					// Allows spearmaster to have dreams
					self.dreamsState = new DreamsState();
				}
			}

			orig(self, saveStateNumber, progression);
		}

		private static void SpearDreams(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
		{
			if (ModOptions.CustomDreams.Value)
			{
				// Limits spearmaster dreams to just pebbles
				if ((ModManager.MSC && self.GetStorySession.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear) && !self.GetStorySession.lastEverMetPebbles && self.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0)
				{
					DreamsState dreamsState = self.GetStorySession.saveState.dreamsState;

					if (dreamsState != null)
					{
						dreamsState.InitiateEventDream(DreamsState.DreamID.Pebbles);
						Plugin.DebugLog("Custom dream initialized!");
					}
				}
			}

			orig(self, malnourished, fromWarpPoint);
		}

		private static void ArtiDreamEndSlideshow(On.RainWorldGame.orig_ArtificerDreamEnd orig, RainWorldGame self)
		{
			if (ModOptions.CustomSlideShows.Value)
			{
				if (self.manager.artificerDreamNumber == 4)
				{
					self.manager.artificerDreamNumber = -1;
					self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
					List<AbstractCreature> collection = new(self.session.Players);
					self.session = new StoryGameSession(MoreSlugcatsEnums.SlugcatStatsName.Artificer, self)
					{
						Players = new List<AbstractCreature>(collection)
					};
					self.manager.musicPlayer?.FadeOutAllSongs(20f);

					self.manager.nextSlideshow = MagicaEnums.SlidesShowIDs.ArtificerDreamE;
					self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow, 10f);
					return;
				}
			}

			orig(self);
		}

		private static bool GiveBackSaintProgress(On.SlugcatStats.orig_PearlsGivePassageProgress orig, StoryGameSession session)
		{
			if (ModManager.MSC && ModOptions.CustomInGameCutscenes.Value && session.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				return true;
			}
			return orig(session);
		}

		private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
		{
			orig(self, str, game);

			if (ModManager.MSC && game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
				ArtiKilledScavKing = self.progression.miscProgressionData.artificerEndingID == 1 && self.progression.miscProgressionData.beaten_Artificer;

			if (ModManager.MSC && game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				SpearAchievedCommsEnd = self.progression.miscProgressionData.beaten_SpearMaster_AltEnd;

			if (game.StoryCharacter == SlugcatStats.Name.Red)
				HunterScarProgression = Mathf.RoundToInt(Mathf.Lerp(3f, 0f, ((float)self.cycleNumber) / ((float)RedsIllness.RedsCycles(self.redExtraCycles))));

			Plugin.DebugLog($"Hunter scar progression: {Mathf.RoundToInt(Mathf.Lerp(3f, 0f, ((float)self.cycleNumber) / ((float)RedsIllness.RedsCycles(self.redExtraCycles))))}");
		}

		internal static void BeatGameMode(RainWorldGame game)
		{
			if (game.IsStorySession)
			{
				if (game.GetStorySession.saveState != null)
				{
					string roomName = "";
					if (game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						SaveValues.SpearMetSRS = true;
						game.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
						game.GetStorySession.saveState.deathPersistentSaveData.ascended = false;
						game.GetStorySession.saveState.deathPersistentSaveData.karma = game.GetStorySession.saveState.deathPersistentSaveData.karmaCap;
						roomName = "7S_AI";
					}

					game.GetStorySession.saveState.progression.SaveWorldStateAndProgression(false);

					AbstractCreature abstractCreature = game.FirstAlivePlayer;
					SaveState.forcedEndRoomToAllowwSave = abstractCreature.Room.name;
					game.GetStorySession.saveState.BringUpToDate(game);
					SaveState.forcedEndRoomToAllowwSave = "";
					game.GetStorySession.saveState.AppendCycleToStatistics(abstractCreature.realizedCreature as Player, game.GetStorySession, false, 0);
					if (roomName != "")
					{
						RainWorldGame.ForceSaveNewDenLocation(game, roomName, false);
					}

				}
			}
		}
	}
}
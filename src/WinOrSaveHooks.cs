using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicasContentPack
{
	internal class WinOrSaveHooks
	{
		public static int artiDreamNumber;

		private static bool magicaSaveWipe;
		public static bool OEGateOpenedAsSpear;
		public static string WhoShowedFPThePearl;
		public static bool ArtiKilledScavKing;
		public static bool SpearAchievedCommsEnd;
		public static int HunterScarProgression; // 3 - 0
		public static bool SpearMetSRS;
		internal static bool redEndingProcedure;

		public static bool fpHasSeenMonkAscension;
		public static bool lttmHasSeenMonkAscension;
		public static bool MoonOverWrotePearl;
		public static bool CLSeenMoonPearl;

		public static bool HunterHasGreenNeuron;

		public static string HunterOracleID { get; internal set; }

		internal static void Init()
		{
			try
			{
				// Fun statistics!
				On.Menu.StoryGameStatisticsScreen.TickerIsDone += StoryGameStatisticsScreen_TickerIsDone;
				IL.Menu.StoryGameStatisticsScreen.GetDataFromGame += StoryGameStatisticsScreen_ctor;

				// For menuscene shenanijans and other cycle based stuff
				On.ShelterDoor.ctor += ResetShelterRoomInformation;
				On.ShelterDoor.DoorClosed += GatherShelterRoomInformation;

				// Changes various dream things
				On.DreamsState.StaticEndOfCycleProgress += CheckDreamProgress;
				On.SaveState.ctor += DreamChecks;
				On.RainWorldGame.Win += SpearDreams;
				On.RainWorldGame.ArtificerDreamEnd += ArtiDreamEndSlideshow;

				// Fixes saint not getting the scholar passage given there is a new colored pearl
				On.SlugcatStats.PearlsGivePassageProgress += SlugcatStats_PearlsGivePassageProgress;

				// Updates custom values using slugbase (for now?)
				On.PlayerProgression.SaveProgressionAndDeathPersistentDataOfCurrentState += PlayerProgression_SaveProgressionAndDeathPersistentDataOfCurrentState;
				On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
				On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
				On.WinState.CycleCompleted += WinState_CycleCompleted;
				On.SaveState.LoadGame += SaveState_LoadGame;
			}
			catch
			{
				Plugin.Log(Plugin.LogStates.HookFail, nameof(WinOrSaveHooks));
			}
		}

		private static bool PlayerProgression_SaveProgressionAndDeathPersistentDataOfCurrentState(On.PlayerProgression.orig_SaveProgressionAndDeathPersistentDataOfCurrentState orig, PlayerProgression self, bool saveAsDeath, bool saveAsQuit)
		{
			bool result = orig(self, saveAsDeath, saveAsQuit);

			try
			{
				ResetSave(self, self.PlayingAsSlugcat);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			return result;
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

				if (!succeed)
				{
					Plugin.Log(Plugin.LogStates.FailILMatch, nameof(StoryGameStatisticsScreen_ctor));
					return;
				}

				cursor.Emit(OpCodes.Ldarg_1);
				cursor.Emit(OpCodes.Ldloc, 0);
				cursor.Emit(OpCodes.Ldloc, 4);
				static void AddCustomPops(StoryGameStatisticsScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package, Vector2 pos, int index)
				{
					if (package.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						Plugin.DebugLog("Custom ticker IDs being checked...");
						if (package.saveState.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(CLSeenMoonPearl), out var seenPearl))
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
			}
			catch (Exception e)
			{
				Plugin.Log(Plugin.LogStates.MISC, "ERROR UPDATING SAVE STATE: " + e.ToString());
			}
		}

		private static void ResetShelterRoomInformation(On.ShelterDoor.orig_ctor orig, ShelterDoor self, Room room)
		{
			SceneMaker.paletteTexture = null;
			SceneMaker.fadePaletteTexture = null;

			if (OracleHooks.moonRevivedThisCycle)
			{
				OracleHooks.moonRevivedThisCycle = false;
			}

			orig(self, room);
		}

		private static void GatherShelterRoomInformation(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
		{
			if (self.room != null && self.room.roomSettings != null && self.room.game != null && self.room.game.cameras[0] != null)
			{
				if (SceneMaker.paletteTexture == null)
				{
					SceneMaker.paletteTexture = new Texture2D(32, 8, TextureFormat.ARGB32, false);
					SceneMaker.paletteTexture.anisoLevel = 0;
					SceneMaker.paletteTexture.filterMode = FilterMode.Point;
					SceneMaker.paletteTexture.wrapMode = TextureWrapMode.Clamp;

					self.room.game.cameras[0].LoadPalette(self.room.roomSettings.Palette, ref SceneMaker.paletteTexture);

					SceneMaker.sleepFadeAmount = 0;
					if (self.room.roomSettings.fadePalette != null)
					{
						SceneMaker.fadePaletteTexture = new Texture2D(32, 8, TextureFormat.ARGB32, false);
						SceneMaker.fadePaletteTexture.anisoLevel = 0;
						SceneMaker.fadePaletteTexture.filterMode = FilterMode.Point;
						SceneMaker.fadePaletteTexture.wrapMode = TextureWrapMode.Clamp;

						self.room.game.cameras[0].LoadPalette(self.room.roomSettings.fadePalette.palette, ref SceneMaker.fadePaletteTexture);

						SceneMaker.sleepFadeAmount = self.room.roomSettings.fadePalette.fades[0];
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
						WinOrSaveHooks.HunterHasGreenNeuron = true;
						break;
					}
				}
				for (int j = 0; j < self.room.physicalObjects.Length; j++)
				{
					if (WinOrSaveHooks.HunterHasGreenNeuron) { break; }

					for (int h = 0; h < self.room.physicalObjects[j].Count; h++)
					{
						if (self.room.physicalObjects[j][h] != null && self.room.physicalObjects[j][h].abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
						{
							WinOrSaveHooks.HunterHasGreenNeuron = true;
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
			Debug.Log(artiDreamNumber);
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

		private static void SpearDreams(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
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

			orig(self, malnourished);
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

		private static bool SlugcatStats_PearlsGivePassageProgress(On.SlugcatStats.orig_PearlsGivePassageProgress orig, StoryGameSession session)
		{
			if (ModManager.MSC && session.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				return true;
			}
			return orig(session);
		}

		private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
		{
			orig(self, str, game);
			try
			{
				UpdateSaveBeforeCycle(self);
			}
			catch (Exception e)
			{
				Plugin.Log(Plugin.LogStates.MISC, "ERROR UPDATING SAVE STATE: " + e.ToString());
			}
		}


		private static void WinState_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
		{
			try
			{
				UpdateSaveAfterCycle(game);
			}
			catch (Exception e)
			{
				Plugin.Log(Plugin.LogStates.MISC, "ERROR UPDATING SAVE STATE: " + e.ToString());
			}
			orig(self, game);
		}
		private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
		{
			try
			{
				magicaSaveWipe = true;
				ResetSave(self, saveStateNumber);
			}
			catch (Exception e)
			{
				Plugin.Log(Plugin.LogStates.MISC, "ERROR UPDATING SAVE STATE: " + e.ToString());
			}
			Plugin.DebugLog("Save has been wiped!");
			orig(self, saveStateNumber);
		}

		private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
		{
			try
			{
				if (self != null)
				{
					magicaSaveWipe = true;
					ResetSave(self);
				}
			}
			catch (Exception e)
			{
				Plugin.Log(Plugin.LogStates.MISC, "ERROR UPDATING SAVE STATE: " + e.ToString());
			}
			Plugin.DebugLog("Save has been wiped!");
			orig(self);
		}

		private static void UpdateSaveAfterCycle(RainWorldGame game)
		{
			if (game.IsStorySession)
			{
				if (OEGateOpenedAsSpear)
				{
					OEGateOpenedAsSpear = false;
				}

				if (OracleHooks.hasBeenTouchedBySaintsHalo)
				{
					OracleHooks.hasBeenTouchedBySaintsHalo = false;
				}
				if (OracleHooks.checkedForMoonPearlThisCycle)
				{
					OracleHooks.checkedForMoonPearlThisCycle = false;
				}

				if (!string.IsNullOrEmpty(WhoShowedFPThePearl))
				{
					game.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<string>(nameof(WhoShowedFPThePearl), WhoShowedFPThePearl);
				}

				if (fpHasSeenMonkAscension)
				{
					game.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<bool>(nameof(fpHasSeenMonkAscension), true);
				}
				if (lttmHasSeenMonkAscension)
				{
					game.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<bool>(nameof(lttmHasSeenMonkAscension), true);
				}
				if (MoonOverWrotePearl)
				{
					game.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<bool>(nameof(MoonOverWrotePearl), true);
				}
				if (CLSeenMoonPearl)
				{
					game.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<bool>(nameof(CLSeenMoonPearl), true);
				}
			}
		}

		private static void UpdateSaveBeforeCycle(SaveState self)
		{
			if (self.saveStateNumber == SlugcatStats.Name.Red && !self.deathPersistentSaveData.redsDeath && !Custom.rainWorld.ExpeditionMode)
			{
				HunterScarProgression = Mathf.RoundToInt(Mathf.LerpUnclamped(3f, 0f, (float)self.cycleNumber / (float)RedsIllness.RedsCycles(self.redExtraCycles)));
				if (HunterScarProgression < 0)
				{
					HunterScarProgression = 0;
				}
				Plugin.DebugLog("Hunter prog: " + HunterScarProgression.ToString());
			}

			if (self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.progression.miscProgressionData != null)
			{
				if (self.progression.currentSaveState.deathPersistentSaveData.altEnding)
				{
					ArtiKilledScavKing = true;
					Plugin.DebugLog("Arti has killed the scav king");
				}
			}

			if (self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.progression.miscProgressionData != null)
			{
				if (self.progression.currentSaveState.deathPersistentSaveData.altEnding)
				{
					SpearAchievedCommsEnd = true;
					Plugin.DebugLog("Spear is epic and awesome sauce");
				}

				if (self.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(SpearMetSRS), out bool spearSRS))
				{
					SpearMetSRS = spearSRS;
				}
			}

			if (magicaSaveWipe && self.progression.miscProgressionData != null)
			{
				if (self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(SpearMetSRS));
				}

				//if (self.saveStateNumber != MoreSlugcatsEnums.SlugcatStatsName.Artificer)
				//{
				//	self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(WhoShowedFPThePearl));
				//}

				if (self.saveStateNumber == SlugcatStats.Name.Red)
				{
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(HunterOracleID));
				}

				if (self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
				{
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(fpHasSeenMonkAscension));
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(lttmHasSeenMonkAscension));
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(MoonOverWrotePearl));
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(CLSeenMoonPearl));
				}

				if (self.progression.miscProgressionData.GetSlugBaseData().TryGet<string>(nameof(WhoShowedFPThePearl), out string who) && (who == "false" || string.IsNullOrEmpty(who) || who == self.saveStateNumber.value))
				{
					self.progression.miscProgressionData.GetSlugBaseData().Remove(nameof(WhoShowedFPThePearl)); 
				}

				magicaSaveWipe = false;
			}

			if (!magicaSaveWipe && self.progression.miscProgressionData.GetSlugBaseData().TryGet<string>(nameof(WhoShowedFPThePearl), out string otherPearl) && otherPearl != "false")
			{
				WhoShowedFPThePearl = otherPearl;
				Plugin.DebugLog("Pearl status: " + otherPearl);
			}

			if (!magicaSaveWipe && self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				self.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(fpHasSeenMonkAscension), out fpHasSeenMonkAscension);
				self.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(lttmHasSeenMonkAscension), out lttmHasSeenMonkAscension);
				self.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(MoonOverWrotePearl), out MoonOverWrotePearl);
				self.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(CLSeenMoonPearl), out CLSeenMoonPearl);
			}
		}

		public static void ResetSave(PlayerProgression self)
		{
			OEGateOpenedAsSpear = false;
			ArtiKilledScavKing = false;
			SpearMetSRS = false;
			SpearAchievedCommsEnd = false;
			WhoShowedFPThePearl = "";
			fpHasSeenMonkAscension = false;
			lttmHasSeenMonkAscension = false;
			MoonOverWrotePearl = false;
			CLSeenMoonPearl = false;
		}

		public static void ResetSave(PlayerProgression self, SlugcatStats.Name name)
		{
			if (name == MoreSlugcatsEnums.SlugcatStatsName.Spear)
			{
				OEGateOpenedAsSpear = false;
				SpearAchievedCommsEnd = false;
				SpearMetSRS = false;
			}

			if (name == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			{
				ArtiKilledScavKing = false;
			}

			if (name == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				fpHasSeenMonkAscension = false;
				lttmHasSeenMonkAscension = false;
				MoonOverWrotePearl = false;
				CLSeenMoonPearl = false;
			}
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
						game.GetStorySession.saveState.progression.miscProgressionData.GetSlugBaseData().Set<bool>(nameof(SpearMetSRS), true);
						game.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
						game.GetStorySession.saveState.deathPersistentSaveData.ascended = false;
						game.GetStorySession.saveState.deathPersistentSaveData.karma = game.GetStorySession.saveState.deathPersistentSaveData.karmaCap;
						roomName = "7S_AI";
					}

					game.GetStorySession.saveState.justBeatGame = true;
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
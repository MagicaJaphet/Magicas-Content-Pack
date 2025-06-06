﻿using HUD;
using MagicasContentPack.IteratorHooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MagicasContentPack
{
	internal class WorldHooks
	{
		private static AbstractCreature nshOverseer;
		internal static bool shouldFadeGhostMode;

		public static List<SnowSource> roomSnowSources = [];
		private static MagicaEnums.SaintRainPhases[] warmRainPhase;
		private static int blizzardClearingDuration;
		private static int warmRainPhaseDuration;
		private static float blizzardClearingStart;
		private static object warmRainPhaseStart;
		private static float lastLightAmount;
		private static float lastHeavyAmount;
		private static float lastClouds;
		private static int lastRainCycleMinute;
		private static EntityID warmRainPhaseSeed;

		internal static void Init()
		{
			try
			{
				IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
				//On.RainWorldGame.AllowRainCounterToTick += RainWorldGame_AllowRainCounterToTick;
				//On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
				//On.RainCycle.ctor += RainCycle_ctor;
				//_ = new Hook(typeof(RainCycle).GetProperty(nameof(RainCycle.BlizzardWorldActive), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), SaintBlizzardStatus);

				// For Custom Arti mechanics
				On.MoreSlugcats.CutsceneArtificerRobo.Update += CutsceneArtificerRobo_Update;

				// Change Artificer reputation dynamically
				IL.RainWorldGame.Update += ShouldScavsPursueArti; 
				IL.ScavengerAI.PlayerRelationship += ScavengerAI_PlayerRelationship;
				On.CreatureCommunities.LikeOfPlayer += CreatureCommunities_LikeOfPlayer;

				// Change the way echoes spawn for Artificer
				On.GoldFlakes.GoldFlake.DrawSprites += GoldFlake_DrawSprites;
				//On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
				//On.RoomCamera.Update += RoomCamera_Update;
				On.RoomCamera.UpdateGhostMode += RoomCamera_UpdateGhostMode1;
				IL.RoomCamera.UpdateGhostMode += RoomCamera_UpdateGhostMode;
				On.GhostHunch.Update += GhostHunch_Update;
				On.World.SpawnGhost += World_SpawnGhost;
				//On.RoomRain.DrawSprites += RoomRain_DrawSprites;
				//On.RoomRain.Update += RoomRain_Update;
				//On.MoreSlugcats.ColdRoom.Update += ColdRoom_Update;

				//On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues;

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

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static void RainWorldGame_RawUpdate(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool sooc = cursor.TryGotoNext(
					x => x.MatchStloc(2),
					x => x.MatchLdarg(0),
					x => x.MatchLdarg(0),
					x => x.MatchCallOrCallvirt(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.GamePaused), BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));

				if (Plugin.ILMatchFail(sooc))
					return;

				cursor.Index++;

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldloc_2);
				static float SetFramesForSaint(RainWorldGame game, float frames)
				{
					if (game.IsStorySession && game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						return game.framesPerSecond;
					}
					return frames;
				}
				cursor.EmitDelegate(SetFramesForSaint);
				cursor.Emit(OpCodes.Stloc_2);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static bool RainWorldGame_AllowRainCounterToTick(On.RainWorldGame.orig_AllowRainCounterToTick orig, RainWorldGame self)
		{
			if (!orig(self) && self.IsStorySession && self.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && (self.setupValues.disableRain || (ModManager.MSC && !(self.world.game.rainWorld.safariMode && self.world.game.rainWorld.safariRainDisable)) && self.world.rainCycle.TimeUntilRain / 40 < 400 && self.world.rainCycle.timer > 500))
			{
				return true;
			}

			return orig(self);
		}

		private static void RoomCamera_UpdateGhostMode1(On.RoomCamera.orig_UpdateGhostMode orig, RoomCamera self, Room newRoom, int newCamPos)
		{
			if (self.game.IsStorySession && self.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && self.room != null && self.room.PlayersInRoom.Any(x => x != null && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(x, out var player) && player.magicaSaintAscension))
			{
				self.ghostMode = 1f;
				return;
			}

			orig(self, newRoom, newCamPos);
		}

		private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
		{
			orig(self, cam);

			if (self.owner is Player player && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				self.AddPart(new SaintWarmth(self, self.fContainers[1]));
			}
		}

		private static void RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
		{
			orig(self, world, minutes);

			if (world.game.IsStorySession && world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				lastRainCycleMinute = -1;
				lastLightAmount = -1;
				lastHeavyAmount = -1;
				lastClouds = -1;

				blizzardClearingDuration = Mathf.RoundToInt(self.cycleLength / Mathf.Lerp(1f, 1.4f, UnityEngine.Random.value));
				warmRainPhaseDuration = Mathf.RoundToInt(self.cycleLength / Mathf.Lerp(1.2f, 3f, UnityEngine.Random.value));

				blizzardClearingStart = 0;
				warmRainPhaseStart = 0;

				if (UnityEngine.Random.value < 0.15f)
				{
					blizzardClearingStart = Mathf.Min(UnityEngine.Random.Range(0.5f, self.cycleLength / 5f), self.cycleLength - blizzardClearingDuration);
				}
				if (UnityEngine.Random.value < 0.3f)
				{
					warmRainPhaseStart = Mathf.Min(UnityEngine.Random.Range(1f, self.cycleLength / 3f), self.cycleLength - warmRainPhaseDuration);
				}

				if (world.game.GetStorySession.saveState != null && world.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap >= 4)
				{
					int minoots = Mathf.RoundToInt(minutes);
					warmRainPhase = new MagicaEnums.SaintRainPhases[minoots];
					for (int i = 0; i < minoots; i++)
					{
						
						switch (world.game.SeededRandom(world.game.GetNewID().RandomSeed))
						{
							case < 0.35f:
								warmRainPhase[i] = MagicaEnums.SaintRainPhases.Linear; break;

							case < 0.7f:
								warmRainPhase[i] = MagicaEnums.SaintRainPhases.Wavering; break;

							case < 0.85f:
								warmRainPhase[i] = MagicaEnums.SaintRainPhases.Constant;
								break;

							default:
								warmRainPhase[i] = MagicaEnums.SaintRainPhases.None; break;
						}
					}
				}
			}
		}

		private static void ColdRoom_Update(On.MoreSlugcats.ColdRoom.orig_Update orig, ColdRoom self, bool eu)
		{
			if (PlayerHooks.warmMode)
				return;

			orig(self, eu);
		}

		private static void RoomRain_DrawSprites(On.RoomRain.orig_DrawSprites orig, RoomRain self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (self.slatedForDeletetion)
			{
				sLeaser.CleanSpritesAndRemove();
				self.Destroy();
				return;
			}
			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		private static bool SaintBlizzardStatus(Func<RainCycle, bool> orig, RainCycle self)
		{
			return orig(self) && !PlayerHooks.warmMode;
		}

		private static void CutsceneArtificerRobo_Update(On.MoreSlugcats.CutsceneArtificerRobo.orig_Update orig, CutsceneArtificerRobo self, bool eu)
		{
			if (ModOptions.CustomMechanics.Value)
			{
				if (self.phase == CutsceneArtificerRobo.Phase.End)
				{
					if (self.player != null)
					{
						self.player.controller = null;
					}
					RainWorldGame.ForceSaveNewDenLocation(self.room.game, "GW_A24", true);
					self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("articustomtutorial"), 20, 700, true, true);
					self.Destroy();
					return;
				}
			}
			else if (self.room.updateList.Any(x => x != null && x is FirecrackerPlant))
			{
				for (int i = 0; i < self.room.updateList.Count; i++)
				{
					UpdatableAndDeletable item = self.room.updateList[i];
					if (item != null && item is FirecrackerPlant firecracker)
					{
						firecracker.abstractPhysicalObject.realizedObject.RemoveFromRoom();
						self.room.abstractRoom.RemoveEntity(firecracker.abstractPhysicalObject);
					}
				}
			}

			orig(self, eu);
		}

		private static void ShouldScavsPursueArti(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool sooc = cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)),
					x => x.MatchCall(out _)
					);

				if (Plugin.ILMatchFail(sooc))
					return;

				ILLabel jump = (ILLabel)cursor.Next.Operand;

				cursor.Emit(OpCodes.Brfalse, jump);
				cursor.Emit(OpCodes.Ldarg_0);
				static bool ArtiIsBelow5Cap(RainWorldGame self)
				{
					return ModOptions.CustomMechanics.Value && self.GetStorySession.saveState.deathPersistentSaveData.karmaCap < 4;
				}
				cursor.EmitDelegate(ArtiIsBelow5Cap);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void ScavengerAI_PlayerRelationship(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool sooc = cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)),
					x => x.MatchCall(out _)
					);

				if (Plugin.ILMatchFail(sooc))
					return;

				ILLabel jump = (ILLabel)cursor.Next.Operand;

				cursor.Emit(OpCodes.Brfalse, jump);
				cursor.Emit(OpCodes.Ldarg_1);
				static bool IsLC(RelationshipTracker.DynamicRelationship dynamic)
				{
					return ModOptions.CustomMechanics.Value && ((dynamic.trackerRep.representedCreature.world?.region != null && dynamic.trackerRep.representedCreature.world.region.name == "LC") 
						|| 
						(dynamic.trackerRep.representedCreature.world?.game != null && dynamic.trackerRep.representedCreature.world.game.IsStorySession && dynamic.trackerRep.representedCreature.world.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 0));
				}
				cursor.EmitDelegate(IsLC);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static float CreatureCommunities_LikeOfPlayer(On.CreatureCommunities.orig_LikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber)
		{
			var result = orig(self, commID, region, playerNumber);

			if (ModOptions.CustomMechanics.Value && ModManager.MSC && self.session is StoryGameSession session && commID == CreatureCommunities.CommunityID.Scavengers && session.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			{
				playerNumber = 0;
				if (self.session is ArenaGameSession)
				{
					region = -1;
				}
				float num = self.playerOpinions[commID.Index - 1, region + 1, playerNumber];
				if (region > -1)
				{
					num = Custom.MinusOneToOneRangeFloatInfluence(num, self.playerOpinions[commID.Index - 1, 0, playerNumber]);
				}
				float lowRange = session.saveState.deathPersistentSaveData.karmaCap > 0 ? -1f + (((float)(session.saveState.deathPersistentSaveData.karmaCap + 1) / 10f) * 2f) : -1f;
				return Mathf.Min(lowRange, Custom.ExponentMap(num, -1f, 1f, 1f + 0.2f * self.session.difficulty));
			}

			return result;
		}

		private static void GoldFlake_DrawSprites(On.GoldFlakes.GoldFlake.orig_DrawSprites orig, GoldFlakes.GoldFlake self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (shouldFadeGhostMode)
			{
				sLeaser.sprites[0].alpha = Mathf.Lerp(0f, 1f, self.room.game.cameras[0].ghostMode);
			}
		}

		private static void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
		{
			orig(self, eu);

			if (PlayerHooks.warmMode && self.room != null && self.room.roomRain != null)
			{
				RoomSettings.RoomEffect lightRain = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LightRain);
				RoomSettings.RoomEffect heavyRain = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.HeavyRain);
				if (lightRain != null && heavyRain != null)
				{
					float[] weather = GetWarmRainAmount(self.room);
					float lightAmount = weather[0];
					float heavyAmount = weather[1];
					float clouds = weather[2];

					if (warmRainPhaseSeed.RandomSeed > -1)
					{
						float lerpAmount = Mathf.Clamp(self.room.game.SeededRandom(warmRainPhaseSeed.RandomSeed) * 0.005f, 0.005f, 0.05f);
						if (lightAmount > -1f)
						{
							lastLightAmount = Mathf.Lerp(lastLightAmount, lightAmount, lerpAmount);
							lightRain.amount = lastLightAmount;
						}
						if (heavyAmount > -1f)
						{
							lastHeavyAmount = Mathf.Lerp(lastHeavyAmount, heavyAmount, lerpAmount);
							heavyRain.amount = lastHeavyAmount;
						}
						if (clouds > -1f)
						{
							lastClouds = Mathf.Lerp(lastClouds, clouds, lerpAmount);
							self.room.roomSettings.Clouds = lastClouds;
						}
					}
				}
				else if (self.room.roomSettings.DangerType == RoomRain.DangerType.Rain || self.room.roomSettings.DangerType == RoomRain.DangerType.FloodAndRain)
				{
					if (self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LightRain) == null)
					{
						self.room.roomSettings.effects.Add(new(RoomSettings.RoomEffect.Type.LightRain, GetWarmRainAmount(self.room)[0], false));
					}
					if (self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.HeavyRain) == null)
					{
						self.room.roomSettings.effects.Add(new(RoomSettings.RoomEffect.Type.HeavyRain, GetWarmRainAmount(self.room)[1], false));
					}
				}
			}
		}

		private static void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
		{
			if (newRoom != null && self.game.IsStorySession && self.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				RoomSettings settings = newRoom.roomSettings;
				if (PlayerHooks.warmMode)
				{
					settings = new(newRoom.abstractRoom.name, self.game.world.region, false, false, SlugcatStats.Timeline.Rivulet, self.game);
				}
				else
				{
					settings = new(newRoom.abstractRoom.name, self.game.world.region, false, false, SlugcatStats.Timeline.Saint, self.game);
				}
				if (settings != null)
				{
					newRoom.roomSettings = settings;

					if (PlayerHooks.warmMode && newRoom.roomRain == null && IsRainDanger(settings.DangerType))
					{
						newRoom.roomRain = new(newRoom.game.globalRain, newRoom);
						newRoom.AddObject(newRoom.roomRain);
					}
					else if (newRoom.roomRain != null)
					{
						newRoom.RemoveObject(newRoom.roomRain);
						newRoom.roomRain.slatedForDeletetion = true;
						newRoom.roomRain = null;
					}
				}

				foreach (var source in newRoom.updateList.Where(x => x != null && x is SnowSource).Select(x => x as SnowSource).ToList())
				{
					if (PlayerHooks.warmMode)
					{
						source.visibility = 0;
					}
					else
					{
						source.visibility = source.CheckVisibility(cameraPosition);
					}
				}
			}

			orig(self, newRoom, cameraPosition);
		}

		private static bool IsRainDanger(RoomRain.DangerType dangerType)
		{
			return dangerType == RoomRain.DangerType.Flood || dangerType == RoomRain.DangerType.FloodAndRain || dangerType == RoomRain.DangerType.Rain;
		}

		private static float[] GetWarmRainAmount(Room room)
		{
			float[] amounts = [-1f, -1f, -1f];
			int currentMinute = Mathf.RoundToInt(Mathf.Floor(room.world.rainCycle.timer / 40f / 60f));
			float minuteLerp = (float)(room.world.rainCycle.timer - ((float)currentMinute * 40f * 60f)) / ((float)((currentMinute + 1) * 40f * 60f) - (currentMinute * 40f * 60f));

			if (lastRainCycleMinute != currentMinute)
			{
				Plugin.DebugLog($"New minute! {currentMinute} : {warmRainPhase[currentMinute]}");

				lastRainCycleMinute = currentMinute;
				warmRainPhaseSeed = room.game.GetNewID();
			}

			if (warmRainPhase.Length > currentMinute)
			{
				if (warmRainPhase[currentMinute] == MagicaEnums.SaintRainPhases.None)
				{
					amounts[0] = Mathf.Lerp(0.05f, 0.05f, Mathf.Sin(minuteLerp * 3.1415f));
					amounts[1] = 0f;
					amounts[2] = Mathf.Lerp(lastClouds, 0.05f, minuteLerp);
				}
				else if (warmRainPhase[currentMinute] == MagicaEnums.SaintRainPhases.Linear)
				{
					// Add min and max intensity based on rain cycle
					amounts[0] = Mathf.Lerp(0.05f, 0.05f, Mathf.Sin(minuteLerp * 3.1415f));
					amounts[1] = Mathf.Lerp(0.0f, 0.3f, Mathf.Sin(minuteLerp * 3.1415f));
					amounts[2] = 1f;
				}
				else if (warmRainPhase[currentMinute] == MagicaEnums.SaintRainPhases.Wavering)
				{
					amounts[0] = Custom.LerpBackEaseOutIn(0.1f, 1f, minuteLerp);
					amounts[1] = Custom.LerpBackEaseOutIn(0.1f, 0.1f, minuteLerp);
					amounts[2] = 1f;
				}
				else if (warmRainPhase[currentMinute] == MagicaEnums.SaintRainPhases.Constant)
				{
					amounts[0] = Custom.LerpBackEaseOutIn(0.1f, 1f, minuteLerp);
					amounts[1] = Custom.LerpBackEaseOutIn(0.1f, 0.7f, minuteLerp);
					amounts[2] = 1f;
				}
			}

			return amounts;
		}

		private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
		{
			orig(self);

			if (shouldFadeGhostMode)
			{
				if (self.ghostMode <= 0f)
				{
					shouldFadeGhostMode = false;
					if (self.room.updateList.Count > 0 && self.room.updateList.Any(x => x is GoldFlakes))
					{
						for (int i = 0; i < self.room.updateList.Count; i++)
						{
							UpdatableAndDeletable item = self.room.updateList[i];
							if (item is GoldFlakes gold)
							{
								gold.Destroy();
							}
						}
					}
				}
				else
				{
					self.ghostMode = Mathf.Lerp(self.ghostMode, 0f, 0.005f);
				}
			}
		}

		private static void RoomCamera_UpdateGhostMode(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool succed = cursor.TryGotoNext(
					x => x.MatchRet()
					);

				succed = cursor.TryGotoNext(
					x => x.MatchRet()
					);

				if (Plugin.ILMatchFail(succed))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				static void AddArtiGhostChecks(RoomCamera self)
				{
					if (self.game.IsStorySession && self.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && SaveValues.scavsKilledThisCycle != 0)
					{
						self.ghostMode = 0f;
						(self.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo[self.game.world.worldGhost.ghostID] = 0;
						if (self.game.world.worldGhost.ghostRoom.realizedRoom != null && self.game.world.worldGhost.ghostRoom.realizedRoom.updateList != null)
						{
							for (int i = 0; i < self.game.world.worldGhost.ghostRoom.realizedRoom.updateList.Count; i++)
							{
								UpdatableAndDeletable potentialGhost = self.game.world.worldGhost.ghostRoom.realizedRoom.updateList[i];
								if (potentialGhost != null && potentialGhost is Ghost ghost)
								{
									self.game.world.worldGhost.ghostRoom.realizedRoom.RemoveObject(potentialGhost);
									ghost.Destroy();
								}
							}
						}
						self.game.world.worldGhost = null;
					}
				}
				cursor.EmitDelegate(AddArtiGhostChecks);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void GhostHunch_Update(On.GhostHunch.orig_Update orig, GhostHunch self, bool eu)
		{
			Plugin.DebugLog($"cHECKING... {SaveValues.scavsKilledThisCycle}");
			if (self.room.game.IsStorySession && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && SaveValues.scavsKilledThisCycle != 0)
			{
				Plugin.DebugLog("Arti killed scavengers >:(");
				self.Destroy();
				return;
			}
			orig(self, eu);
		}

		private static void World_SpawnGhost(On.World.orig_SpawnGhost orig, World self)
		{
			if (self.game.setupValues.ghosts < 0)
			{
				return;
			}
			if (!World.CheckForRegionGhost(self.game.StoryCharacter, self.region.name))
			{
				return;
			}
			GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(self.region.name);
			if (ghostID == GhostWorldPresence.GhostID.NoGhost)
			{
				return;
			}

			int encounters = 0;
			if ((self.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo.ContainsKey(ghostID))
			{
				encounters = (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID];
			}
			bool shouldSpawnGhost = self.game.setupValues.ghosts > 0 || GhostWorldPresence.SpawnGhost(ghostID, (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma, (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap, encounters, self.game.StoryCharacter == SlugcatStats.Name.Red);
			if (ModManager.MSC && self.game.IsStorySession && self.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			{
				int scavsKilledSinceLastPrime = SaveValues.scavsKilledThisCycle;
				if (scavsKilledSinceLastPrime > 0)
				{
					(self.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID] = 0;
					SaveValues.scavsKilledThisCycle = 0;
					return;
				}
				else if (encounters == 1 && (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma == (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap || (ModManager.Expedition && self.game.rainWorld.ExpeditionMode && (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma == (self.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap))
				{
					shouldSpawnGhost = true;
				}
			}

			if (shouldSpawnGhost)
			{
				self.worldGhost = new GhostWorldPresence(self, ghostID);
				self.migrationInfluence = self.worldGhost;
				return;
			}

			orig(self);
		}

		private static void WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
		{
			orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);

			// It's as easy as that.
			//if (playerCharacter != null && playerCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear && worldName == "LM")
			//{
			//	self.playerCharacter = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
			//}
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
						abstractEntity.Room.RemoveEntity(overseer.abstractCreature);
						creature.realizedCreature.Destroy();
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
			try
			{
				ILCursor cursor = new(il);

				bool first = cursor.TryGotoNext(
					MoveType.Before,
				instruction => instruction.MatchStfld<ProcessManager>("statsAfterCredits")
				);

				if (Plugin.ILMatchFail(first))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldarg_1);
				cursor.EmitDelegate((MSCRoomSpecificScript.SpearmasterEnding self, bool eu) =>
				{
					self.room.game.GoToRedsGameOver();
					return;
				});

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}


		// Adds the scene ID for the outro
		private static void EndingPointers(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool first = cursor.TryGotoNext(
					MoveType.After,
					instruction => instruction.MatchStfld<DeathPersistentSaveData>(nameof(DeathPersistentSaveData.redsDeath))
					);

				if (Plugin.ILMatchFail(first))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.EmitDelegate((RainWorldGame self) => {
					if (SaveValues.HunterOracleID != null)
					{
						if (self.Players[0].realizedCreature != null && (self.Players[0].realizedCreature as Player).redsIllness != null)
						{
							Plugin.DebugLog("Set hunter oracleID to (check): " + SaveValues.HunterOracleID);

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

				if (Plugin.ILMatchFail(second))
					return;

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

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void CustomRoomSpecificEvents(On.MoreSlugcats.MSCRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
		{
			string name = room.abstractRoom.name;

			if (name == "GW_A25_past")
			{
				if (room.game.IsStorySession && room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && !room.game.GetStorySession.saveState.hasRobo && room.game.GetStorySession.saveState.cycleNumber == 0 && room.game.GetStorySession.saveState.denPosition == "GW_A24")
				{
					room.AddObject(new CutsceneArtificerRobo(room));
				}
			}

			if (name == "7S_AI")
			{
				room.AddObject(new SevenRedSunsRoomConditions(room));
			}

			if (name == "7S_E01")
			{
				room.AddObject(new SevenRedSunsMuralRoom(room));
			}

			if (name == "SH_E01" && !ModOptions.CustomInGameCutscenes.Value || (room.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Spear && SSOracleBehaviorHooks.CheckIfWhoTookThePearlIsBeforeCurrent(room.game.TimelinePoint)))
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

			if (self.room.game != null && SaveValues.OEGateOpenedAsSpear && name == "GATE_SB_OE")
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

	internal class SaintWarmth : HudPart
	{
		private HUD.HUD self;
		private FContainer fContainer;
		private FSprite bigRingSprite;
		private float fade;
		private FSprite blizzardIndicator;
		private float goToBlizzard;
		private float lastBlizzard;
		private int tickCounter;

		public Player Player
		{
			get
			{
				return hud.owner as Player;
			}
		}

		public bool Unlocked
		{
			get
			{
				return Player.KarmaCap >= 4;
			}
		}

		public bool ForceShow
		{
			get
			{
				return Unlocked && (PlayerHooks.warmthActivationTimer > 0 || (PlayerHooks.warmMode && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(Player, out var player) && player.warmthLimit < Player.GetWarmthLimit() / 3 && player.warmthLimit > 0));
			}
		}

		public float Visibility
		{
			get
			{ 
				return ((float)Player.Karma * 1f) / (float)Player.KarmaCap;
			}
		}

		public SaintWarmth(HUD.HUD self, FContainer fContainer) : base(self)
		{
			this.self = self;
			this.fContainer = fContainer;

			bigRingSprite = new FSprite("SaintGuardianRing", true);
			fContainer.AddChild(bigRingSprite);

			blizzardIndicator = new FSprite("SaintGuardianCircleBig");
			fContainer.AddChild(blizzardIndicator);
		}

		public override void ClearSprites()
		{
			base.ClearSprites();
			bigRingSprite?.RemoveFromContainer();
			blizzardIndicator?.RemoveFromContainer();
		}

		public override void Draw(float timeStacker)
		{
			bigRingSprite.SetPosition(hud.karmaMeter.karmaSprite.GetPosition());
			bigRingSprite.scale = hud.karmaMeter.karmaSprite.scale;
			bigRingSprite.alpha = hud.karmaMeter.karmaSprite.alpha * Visibility;
			bigRingSprite.color = Color.Lerp(RainWorld.GoldRGB, RainWorld.SaturatedGold, Visibility);
			
			if (Player.room != null)
			{
				blizzardIndicator.isVisible = true;
				if (tickCounter % 240 == 0)
				{
					goToBlizzard = Player.room.world.rainCycle.AmountLeft;
				}
				lastBlizzard = Mathf.Lerp(lastBlizzard, goToBlizzard, 0.3f);
				blizzardIndicator.SetPosition(Custom.RotateAroundVector(bigRingSprite.GetPosition() + new Vector2(0, hud.karmaMeter.rad + (blizzardIndicator.height / 3.14f)), bigRingSprite.GetPosition(), Mathf.Lerp(0, -360, Mathf.Clamp01(lastBlizzard))));
				blizzardIndicator.alpha = bigRingSprite.alpha;
				blizzardIndicator.scale = bigRingSprite.scale;
				blizzardIndicator.color = bigRingSprite.color;
			}
			else
			{
				blizzardIndicator.isVisible = false;
			}
		}

		public override void Update()
		{
			if (ForceShow)
			{
				hud.karmaMeter.forceVisibleCounter = Mathf.Max(hud.karmaMeter.forceVisibleCounter, 60);
			}
			tickCounter++;
		}
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

			if (room?.game.GetStorySession.saveState != null && room?.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0 && WinOrSaveHooks.SpearAchievedCommsEnd && ExtEnumBase.TryParse(typeof(ChatlogData.ChatlogID), chatB, true, out var chatlog))
			{
				fullChatlog = chatlog as ChatlogData.ChatlogID;
			}
			else if (room?.game.GetStorySession.saveState != null && room?.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0 && !WinOrSaveHooks.SpearAchievedCommsEnd && ExtEnumBase.TryParse(typeof(ChatlogData.ChatlogID), chatC, true, out var chatlog2))
			{
				fullChatlog = chatlog2 as ChatlogData.ChatlogID;
			}
			else if (ExtEnumBase.TryParse(typeof(ChatlogData.ChatlogID), chatA, true, out var chatlog3))
			{
				fullChatlog = chatlog3 as ChatlogData.ChatlogID;
			}
		}

		public override void Update(bool eu)
		{
			if (!WorldHooks.ValidDepthsRooms.Contains(room?.abstractRoom.name))
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

					SaveValues.OEGateOpenedAsSpear = true;
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
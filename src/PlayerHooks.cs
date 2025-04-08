using System.Runtime.CompilerServices;
using UnityEngine;
using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidSea;

namespace MagicasContentPack
{
	internal class PlayerHooks
	{
		private static VoidSpawnKeeper keeper;
		private static readonly float maxTargetTimer = 20f;
		public static readonly float maxAscensionBuffer = 50f;
		public static readonly float maxActivationTimer = 1000f;
		public static readonly float ascensionFatique = 50f;
		public static readonly float saintRadius = 500f;

		public static Dictionary<AbstractPhysicalObject.AbstractObjectType, int> artiCraftables = [];
		public static AbstractPhysicalObject.AbstractObjectType[,] artiResults;

		internal static void Init()
		{
			try
			{
				// Custom arti mechanics
				SetUpArtiCrafts();
				IL.Player.SwallowObject += Player_SwallowObject;
				On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
				On.Player.CraftingResults += Player_CraftingResults;
				On.PlayerSessionRecord.AddKill += PlayerSessionRecord_AddKill;

				// Custom saint mechanics
				On.Player.InitVoidWormCutscene += Player_InitVoidWormCutscene;
				IL.Player.MovementUpdate += AnotherGrabCustomMechanicFix;
				IL.Player.UpdateBodyMode += BodyModeCustomMechanicFix;
				On.Player.SaintTongueCheck += TongueCustomMechanicFix;
				IL.Player.LungUpdate += LungCustomMechanicFix;
				IL.Player.GrabUpdate += GrabCustomMechanicFix;
				On.Player.ctor += Player_ctor;
				On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
				On.Player.DeactivateAscension += Player_DeactivateAscension;
				On.Player.ActivateAscension += Player_ActivateAscension;
			}
			catch
			{
				Plugin.Log(Plugin.LogStates.HookFail);
			}
		}

		private static void Player_SwallowObject(ILContext il)
		{
			try
			{
				static bool MechanicCheck(Player self)
				{
					return !ModOptions.CustomMechanics.Value;
				}

				ILCursor cursor = new(il);

				for (int i = 0; i < 4; i++)
				{
					bool cood = cursor.TryGotoNext(
							MoveType.After,
							x => x.MatchLdfld<AbstractPhysicalObject>(nameof(AbstractPhysicalObject.type)),
							x => x.MatchLdsfld(out _),
							x => x.MatchCall(out _)
						);

					if (Plugin.ILMatchFail(cood))
						return;

					if (i != 2)
					{
						ILLabel jump = (ILLabel)cursor.Next.Operand;

						cursor.Emit(OpCodes.Brfalse, jump);
						cursor.Emit(OpCodes.Ldarg_0);
						cursor.EmitDelegate(MechanicCheck);
					}
				}

				Plugin.ILSucceed();
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError(e);
			}
		}

		private static void SetUpArtiCrafts()
		{
			try
			{
				artiCraftables.Clear();

				int index = 0;
				foreach (var type in AbstractPhysicalObject.AbstractObjectType.values.entries)
				{
					AbstractPhysicalObject.AbstractObjectType actualType = (AbstractPhysicalObject.AbstractObjectType)ExtEnumBase.Parse(typeof(AbstractPhysicalObject.AbstractObjectType), type, false);
					if (CanBeCrafted(actualType))
					{
						artiCraftables.Add(actualType, index);
						index++;
					}
				}

				artiResults = new AbstractPhysicalObject.AbstractObjectType[artiCraftables.Count, artiCraftables.Count];

				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.Spear], artiCraftables[AbstractPhysicalObject.AbstractObjectType.Rock]] = AbstractPhysicalObject.AbstractObjectType.Spear;
				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.Spear], artiCraftables[AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant]] = AbstractPhysicalObject.AbstractObjectType.Spear;
				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.Rock], artiCraftables[AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant]] = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb;
				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.DangleFruit], artiCraftables[AbstractPhysicalObject.AbstractObjectType.SlimeMold]] = AbstractPhysicalObject.AbstractObjectType.Lantern;

				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.Rock], artiCraftables[AbstractPhysicalObject.AbstractObjectType.Spear]] = AbstractPhysicalObject.AbstractObjectType.Spear;
				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant], artiCraftables[AbstractPhysicalObject.AbstractObjectType.Spear]] = AbstractPhysicalObject.AbstractObjectType.Spear;
				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant], artiCraftables[AbstractPhysicalObject.AbstractObjectType.Rock]] = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb;
				artiResults[artiCraftables[AbstractPhysicalObject.AbstractObjectType.SlimeMold], artiCraftables[AbstractPhysicalObject.AbstractObjectType.DangleFruit]] = AbstractPhysicalObject.AbstractObjectType.Lantern;

				Plugin.ResourceLoad();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static bool CanBeCrafted(AbstractPhysicalObject.AbstractObjectType match)
		{
			return
				match == AbstractPhysicalObject.AbstractObjectType.Spear ||
				match == AbstractPhysicalObject.AbstractObjectType.DangleFruit ||
				match == AbstractPhysicalObject.AbstractObjectType.SlimeMold ||
				match == AbstractPhysicalObject.AbstractObjectType.Rock ||
				match == AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant;
		}
		private static void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			{
				AbstractPhysicalObject.AbstractObjectType craftResult = self.CraftingResults();
				if (craftResult != null)
				{
					AbstractPhysicalObject craftedItem = null;
					if (craftResult == AbstractPhysicalObject.AbstractObjectType.Spear)
					{
						bool electric = self.grasps[0].grabbed is Rock || self.grasps[1].grabbed is Rock;
						craftedItem = new AbstractSpear(self.room.world, null, self.abstractPhysicalObject.pos, self.room.game.GetNewID(), !electric);
						if (craftedItem is AbstractSpear spear)
						{
							spear.electric = electric;
							spear.electricCharge = 0;
						}
					}
					else
					{
						craftedItem = new(self.room.world, craftResult, null, self.abstractPhysicalObject.pos, self.room.game.GetNewID());
					}
					self.room.abstractRoom.AddEntity(craftedItem);
					craftedItem.RealizeInRoom();

					for (int i = 0; i < self.grasps.Length; i++)
					{
						AbstractPhysicalObject recipeItem = self.grasps[i].grabbed.abstractPhysicalObject;
						if (self.room.game.session is StoryGameSession session)
						{
							session.RemovePersistentTracker(recipeItem);
						}
						self.ReleaseGrasp(i);
						for (int k = recipeItem.stuckObjects.Count - 1; k >= 0; k--)
						{
							if (recipeItem.stuckObjects[k] is AbstractPhysicalObject.AbstractSpearStick && recipeItem.stuckObjects[k].A.type == AbstractPhysicalObject.AbstractObjectType.Spear && recipeItem.stuckObjects[k].A.realizedObject != null)
							{
								(recipeItem.stuckObjects[k].A.realizedObject as Spear).ChangeMode(Weapon.Mode.Free);
							}
						}
						recipeItem.LoseAllStuckObjects();
						recipeItem.realizedObject.RemoveFromRoom();
						self.room.abstractRoom.RemoveEntity(recipeItem);
						if (self.FreeHand() != -1)
						{
							self.SlugcatGrab(craftedItem.realizedObject, self.FreeHand());
						}
					}
				}
				return;
			}

			orig(self);
		}

		private static AbstractPhysicalObject.AbstractObjectType Player_CraftingResults(On.Player.orig_CraftingResults orig, Player self)
		{
			var result = orig(self);

			if (ModOptions.CustomMechanics.Value && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			{
				return ArtiCraftingRecipes(self.grasps[0], self.grasps[1]);
			}

			return result;
		}

		private static AbstractPhysicalObject.AbstractObjectType ArtiCraftingRecipes(Creature.Grasp left, Creature.Grasp right)
		{
			if (artiResults != null && left != null && right != null)
			{
				AbstractPhysicalObject.AbstractObjectType leftType = left.grabbed.abstractPhysicalObject.type;
				AbstractPhysicalObject.AbstractObjectType rightType = right.grabbed.abstractPhysicalObject.type;

				if (artiCraftables.ContainsKey(leftType) && artiCraftables.ContainsKey(rightType))
				{
					return artiResults[artiCraftables[leftType], artiCraftables[rightType]];
				}
			}
			return null;
		}


		private static void PlayerSessionRecord_AddKill(On.PlayerSessionRecord.orig_AddKill orig, PlayerSessionRecord self, Creature victim)
		{
			orig(self, victim);

			if (victim is Scavenger && Custom.rainWorld.processManager?.currentMainLoop is RainWorldGame game && game != null && game.IsStorySession && game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			{
				WinOrSaveHooks.scavsKilledThisCycle++;
				Plugin.DebugLog($"Arti killed scavenger!: {WinOrSaveHooks.scavsKilledThisCycle}");

				GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(game.world.region.name);
				if (ghostID != GhostWorldPresence.GhostID.NoGhost && game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo.ContainsKey(ghostID))
				{
					game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID] = 0;
					if (game.FirstAlivePlayer != null && game.FirstAlivePlayer.realizedCreature is Player player)
					{
						player.Stun(10);
						player.airInLungs = 0f;
						player.exhausted = true;
						player.drown = 0.8f;
						WorldHooks.shouldFadeGhostMode = true;
					}
				}
			}
		}

		private static void AnotherGrabCustomMechanicFix(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					x => x.MatchLdfld<Player>(nameof(Player.monkAscension))
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Index++;
				cursor.Emit(OpCodes.Ldarg_0);
				bool SaintAscension(bool monkAscension, Player self)
				{
					return monkAscension || (ModOptions.CustomMechanics.Value && MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && player.magicaSaintAscension);
				}
				cursor.EmitDelegate(SaintAscension);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private static void BodyModeCustomMechanicFix(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					x => x.MatchLdfld<Player>(nameof(Player.monkAscension)),
					x => x.MatchBrtrue(out _),
					x => x.MatchLdarg(0),
					x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room))
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Index++;
				cursor.Emit(OpCodes.Ldarg_0);
				bool SaintAscension(bool monkAscension, Player self)
				{
					return monkAscension || !(ModOptions.CustomMechanics.Value && MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && !player.magicaSaintAscension);
				}
				cursor.EmitDelegate(SaintAscension);

				bool success2 = cursor.TryGotoNext(
					x => x.MatchLdfld<Player>(nameof(Player.monkAscension)),
					x => x.MatchBrtrue(out _),
					x => x.MatchLdarg(0),
					x => x.MatchLdsfld<Player.BodyModeIndex>(nameof(Player.BodyModeIndex.ClimbingOnBeam))
					);

				if (Plugin.ILMatchFail(success2))
					return;

				cursor.Index++;
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.EmitDelegate(SaintAscension);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static bool TongueCustomMechanicFix(On.Player.orig_SaintTongueCheck orig, Player self)
		{
			return orig(self) && (!ModOptions.CustomMechanics.Value || MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && !player.magicaSaintAscension);
		}

		private static void LungCustomMechanicFix(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchLdfld<Player>(nameof(Player.monkAscension))
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				bool SaintAscension(bool monkAscension, Player self)
				{
					return monkAscension || !(!ModOptions.CustomMechanics.Value || (MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && !player.magicaSaintAscension));
				}
				cursor.EmitDelegate(SaintAscension);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void GrabCustomMechanicFix(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchLdfld<Player>(nameof(Player.monkAscension))
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				bool SaintAscension(bool monkAscension, Player self)
				{
					return monkAscension || !(!ModOptions.CustomMechanics.Value || (MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && !player.magicaSaintAscension));
				}
				cursor.EmitDelegate(SaintAscension);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
		{
			orig(self, abstractCreature, world);

			var magicaCWT = MagicaPlayer.magicaCWT.GetOrCreateValue(self);
		}

		private static void Player_InitVoidWormCutscene(On.Player.orig_InitVoidWormCutscene orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value)
			{
				self.controller = new SaintAscensionController(self);
				self.voidSceneTimer = 1;
				self.wormCutsceneLockon = false;
				return;
			}
			orig(self);
		}

		private static void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && MagicaPlayer.magicaCWT.TryGetValue(self, out var player))
			{
				bool isSaint = self.abstractCreature.world.game.IsStorySession && self.abstractCreature.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint;
				if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && (self.KarmaCap >= 9 || (self.room.game.session is ArenaGameSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeMeta.ascended)))
				{
					if (keeper != null && keeper.room != self.room)
					{
						if (keeper.room != null && keeper.room.updateList.Count > 0 && keeper.room.updateList.Contains(keeper))
						{
							self.room.updateList.Remove(keeper);
						}
						keeper.Destroy();
						AddVoidSpawnKeeper(self);
					}
					else if (keeper == null)
					{
						AddVoidSpawnKeeper(self);
					}

					if (isSaint && self.AI == null && self.room.game.session is StoryGameSession && !(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.SaintEnlightMessage)
					{
						(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.SaintEnlightMessage = true;
						self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("While in the air, tap jump and pick-up together to take flight."), 240, 640, true, true);
						return;
					}

					if (self.voidSceneTimer > 0 && isSaint)
					{
						self.voidSceneTimer++;
						if (!player.magicaSaintAscension)
						{
							player.activationTimer = 9999f;
							self.ActivateAscension();
						}
						if (self.voidSceneTimer > 60)
						{
							if (player.wormTarget == null)
							{
								for (int i = 0; i < self.room.updateList.Count; i++)
								{
									UpdatableAndDeletable obj = self.room.updateList[i];
									if (obj != null && obj is VoidWorm worm && worm.head != null && worm.mainWorm)
									{
										player.wormTarget = worm.head;
									}
								}
							}
						}
					}
					if (player.ascensionFatique > 0f && !player.magicaSaintAscension)
					{
						player.ascensionFatique = Mathf.Max(player.ascensionFatique - 1f, 0f);
						return;
					}
					if ((self.wantToJump > 0 && player.magicaSaintAscension) || (player.magicaSaintAscension && (player.activationTimer <= 0f || player.ascendedSinceActivation > 4)))
					{
						Plugin.DebugLog(player.ascensionFatique.ToString());
						player.ascensionFatique += Mathf.Min(ascensionFatique * ((float)player.ascendedSinceActivation + 1f) * Mathf.Lerp(1f, 5f, (maxActivationTimer - player.activationTimer) / maxActivationTimer), 800f);
						Plugin.DebugLog(player.ascensionFatique.ToString());
						self.DeactivateAscension();
						self.wantToJump = 0;
						return;
					}
					else if (player.ascensionFatique <= 0f && self.wantToJump > 0 && self.input[0].pckp && self.canJump <= 0 && !player.magicaSaintAscension && !self.tongue.Attached && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.bodyMode != Player.BodyModeIndex.WallClimb && self.bodyMode != Player.BodyModeIndex.Swimming && self.Consious && !self.Stunned && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
					{
						player.activationTimer = maxActivationTimer + (isSaint && self.KarmaIsReinforced ? 400f : 0f);
						self.ActivateAscension();
						return;
					}
				}

				if (player.magicaSaintAscension)
				{
					self.buoyancy = 0f;
					self.animation = Player.AnimationIndex.None;
					self.bodyMode = Player.BodyModeIndex.Default;
					if (self.tongue != null && self.tongue.Attached)
					{
						self.tongue.Release();
					}
					if (self.dead || self.stun >= 20)
					{
						self.DeactivateAscension();
					}

					if (isSaint && self.AI == null && self.room.game.session is StoryGameSession && !(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.KarmicBurstMessage)
					{
						(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.KarmicBurstMessage = true;
						self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Hold pickup and press left or right to change targets, hold throw to perform an ascension."), 80, 800, true, true);
					}

					self.gravity = 0f;
					self.airFriction = 0.7f;

					float swimSpeed = 5.75f;

					if ((player.saintTarget != null && player.saintTarget.room == self.room) || player.wormTarget != null)
					{
						if (player.storedTargetPos.Length == 10 && player.storedTargetPos[9] != null)
						{
							player.saintTargetPos = Vector2.Lerp(player.saintTargetPos, player.storedTargetPos[9], 0.2f);
						}

						for (int i = player.storedTargetPos.Length - 1; i > 0; i--)
						{
							player.storedTargetPos[i] = player.storedTargetPos[i - 1];
						}
						player.storedTargetPos[0] = player.saintTarget == null ? player.wormTarget.HeadPos(self.room.game.myTimeStacker) : player.saintTarget.firstChunk.pos;
					}
					else
					{
						player.storedTargetPos = new Vector2[10];
						Vector2 playerPos = self.graphicsModule is PlayerGraphics graphics ? graphics.head.pos : self.firstChunk.pos;
						for (int i = 0; i < player.storedTargetPos.Length; i++)
						{
							player.storedTargetPos[i] = playerPos;
						}
						player.saintTargetPos = playerPos;
					}

					if (!self.input[0].pckp)
					{
						if (player.wormTarget == null)
						{
							player.activationTimer = Mathf.Max(player.activationTimer - 1f, 0f);
						}

						player.saintTargetMode = false;
						player.scannedForTargets = false;
						if (self.input[0].y > 0)
						{
							self.bodyChunks[0].vel.y = swimSpeed;
							self.bodyChunks[1].vel.y = (swimSpeed - 1f);
						}
						else if (self.input[0].y < 0)
						{
							self.bodyChunks[0].vel.y = -swimSpeed;
							self.bodyChunks[1].vel.y = -(swimSpeed - 1.75f);
						}
						else
						{
							self.bodyChunks[0].vel.y = 0.75f;
							self.bodyChunks[1].vel.y = 0.75f;
						}
						if (self.input[0].x > 0)
						{
							self.bodyChunks[0].vel.x = swimSpeed;
							self.bodyChunks[1].vel.x = (swimSpeed - 1.75f);
						}
						else if (self.input[0].x < 0)
						{
							self.bodyChunks[0].vel.x = -swimSpeed;
							self.bodyChunks[1].vel.x = -(swimSpeed - 1.75f);
						}
						else
						{
							self.bodyChunks[0].vel.x = 0f;
							self.bodyChunks[1].vel.x = 0f;
						}
					}
					else
					{
						player.saintTargetMode = true;
						self.bodyChunks[0].vel = new(0f, 0.8f);
						self.bodyChunks[1].vel = new(0f, 0.8f);

						if (!player.scannedForTargets)
						{
							player.scannedForTargets = true;

							player.validTargets.Clear();

							for (int i = 0; i < self.room.physicalObjects.Length; i++)
							{
								for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
								{
									player.validTargets.AddRange(from x in self.room.physicalObjects[i] where x != self && IsValidEntity(x) && !player.validTargets.Contains(x) select x);
								}
							}
						}
						else
						{
							player.sortedTargets = (from x in player.validTargets where x.room == self.room && Custom.DistLess(self.firstChunk.pos, x.firstChunk.pos, saintRadius) select x).ToList();
							player.sortedTargets.Sort((x, y) => x.firstChunk.pos.x.CompareTo(y.firstChunk.pos.x));

							if (self.input[0].x > 0f && !player.cycledTarget)
							{
								player.cycledTarget = true;
								if (player.saintTarget != null && player.sortedTargets.Count > 0)
								{
									int nextIndex = player.sortedTargets.IndexOf(player.saintTarget) + 1 >= player.sortedTargets.Count ? 0 : player.sortedTargets.IndexOf(player.saintTarget) + 1;
									if (player.sortedTargets[nextIndex].room == self.room && Custom.DistLess(self.firstChunk.pos, player.sortedTargets[nextIndex].firstChunk.pos, saintRadius))
									{
										player.saintTarget = player.sortedTargets[nextIndex];
									}
									else
									{
										player.saintTarget = player.sortedTargets[0];
									}
								}
								else if (player.sortedTargets.Count > 0 && player.sortedTargets[0].room == self.room)
								{
									player.saintTarget = player.sortedTargets[0];
									player.scannedForTargets = false;
								}
							}
							else if (self.input[0].x < 0f && !player.cycledTarget)
							{
								player.cycledTarget = true;
								if (player.saintTarget != null && player.sortedTargets.Count > 0)
								{
									Plugin.DebugLog((player.sortedTargets.IndexOf(player.saintTarget) - 1).ToString());
									int nextIndex = player.sortedTargets.IndexOf(player.saintTarget) - 1 <= -1 ? player.sortedTargets.Count - 1 : player.sortedTargets.IndexOf(player.saintTarget) - 1;
									if (player.sortedTargets[nextIndex].room == self.room && Custom.DistLess(self.firstChunk.pos, player.sortedTargets[nextIndex].firstChunk.pos, saintRadius))
									{
										player.saintTarget = player.sortedTargets[nextIndex];
									}
									else
									{
										player.saintTarget = player.sortedTargets[player.sortedTargets.Count - 1];
									}
								}
								else if (player.sortedTargets.Count > 0 && player.sortedTargets[player.sortedTargets.Count - 1].room == self.room)
								{
									player.saintTarget = player.sortedTargets[player.sortedTargets.Count - 1];
									player.scannedForTargets = false;
								}
								Plugin.DebugLog(player.sortedTargets.IndexOf(player.saintTarget).ToString() + " / " + player.sortedTargets.Count);
							}
							else if (self.input[0].x == 0f)
							{
								player.cycledTarget = false;
							}
						}
					}

					if ((player.saintTarget == null || (player.saintTarget.room != self.room && !player.saintTargetMode || !Custom.DistLess(self.firstChunk.pos, player.saintTarget.firstChunk.pos, saintRadius))) && player.wormTarget == null)
					{
						player.changingTarget = 20f;
						player.saintTarget = null;
						player.karmaCycling = false;
						player.ascendTimer = 0f;

						if (self.graphicsModule is PlayerGraphics graphics && graphics.objectLooker.currentMostInteresting != null && IsValidEntity(graphics.objectLooker.currentMostInteresting))
						{
							player.saintTarget = graphics.objectLooker.currentMostInteresting;
						}
						else
						{
							List<PhysicalObject> canidates = [];
							for (int i = 0; i < self.room.physicalObjects.Length; i++)
							{
								for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
								{
									canidates.AddRange((from x in self.room.physicalObjects[i] where x != self && IsValidEntity(x) && Custom.DistLess(self.firstChunk.pos, x.firstChunk.pos, saintRadius) select x).ToList());
								}
								if (canidates.Count > 0)
								{
									break;
								}
							}

							canidates.Sort((x, y) => Custom.Dist(x.firstChunk.pos, self.firstChunk.pos).CompareTo(Custom.Dist(y.firstChunk.pos, self.firstChunk.pos)));
							player.saintTarget = canidates.FirstOrDefault();
						}
					}


					if (player.ascensionBuffer > 0f)
					{
						player.ascensionBuffer = Mathf.Max(player.ascensionBuffer - 1f, 0f);
					}
					if (self.input[0].thrw && (player.saintTarget != null || player.wormTarget != null) && player.ascensionBuffer <= 0f)
					{
						float karmaLocked = player.saintTargetIsKarmaLocked ? 0.2f : 1f;
						player.ascendTimer = Mathf.Min(Custom.LerpBackEaseOut(player.ascendTimer + (0.15f * karmaLocked), player.ascendTimer + (0.01f * karmaLocked), Custom.Dist(self.firstChunk.pos, (player.saintTarget == null && player.wormTarget != null ? player.wormTarget.HeadPos(self.room.game.myTimeStacker) : player.saintTarget.firstChunk.pos)) / (saintRadius * 1.5f)), 1f);
						
						if (player.saintTarget != null && player.saintTarget is Creature creature)
						{
							if (creature is DaddyLongLegs dLL)
							{
								dLL.firstChunk.vel = Custom.RNV() * Mathf.Lerp(0f, 20f, player.ascendTimer / 1f);
							}
						}
					}
					else
					{
						player.ascendTimer = 0f;
					}
					if (player.ascendTimer >= 1f)
					{
						List<PhysicalObject> killable = [];
						for (int i = 0; i < self.room.physicalObjects.Length; i++)
						{
							killable.AddRange((from x in self.room.physicalObjects[i] where x != self && !killable.Contains(x) && x.room == self.room && IsValidEntity(x) && Custom.DistLess(x.firstChunk.pos, player.saintTargetPos, 50f) select x).ToList());
						}
						bool ascended = false;

						Plugin.DebugLog("Killable: " + killable.Count);

						foreach (var kill in killable)
						{
							if (kill is Creature creature)
							{
								if (!creature.dead)
								{
									ascended = true;
								}
								creature.Die();

								if (keeper != null && keeper.initiated)
								{
									keeper.AddOneSpawnForSaint(kill);
								}

							}
							if (kill is SeedCob cob && !cob.AbstractCob.opened && !cob.AbstractCob.dead)
							{
								cob.spawnUtilityFoods();
								ascended = true;
							}
							if (isSaint && self.room.game.session is StoryGameSession session && kill is Oracle oracle)
							{
								if (oracle.ID == MoreSlugcatsEnums.OracleID.CL && !session.saveState.deathPersistentSaveData.ripPebbles)
								{
									session.saveState.deathPersistentSaveData.ripPebbles = true;
									self.room.PlaySound(SoundID.SS_AI_Talk_1, self.mainBodyChunk, false, 1f, 0.4f);
									Vector2 fpPos = oracle.bodyChunks[0].pos;
									self.room.AddObject(new ShockWave(fpPos, 500f, 0.75f, 18, false));
									self.room.AddObject(new Explosion.ExplosionLight(fpPos, 320f, 1f, 5, Color.white));
									(oracle.oracleBehavior as CLOracleBehavior).dialogBox.Interrupt("...", 1);
									if ((oracle.oracleBehavior as CLOracleBehavior).currentConversation != null)
									{
										(oracle.oracleBehavior as CLOracleBehavior).currentConversation.Destroy();
									}
									oracle.health = 0f;
									ascended = true;
								}
								if (oracle.ID == Oracle.OracleID.SL && !session.saveState.deathPersistentSaveData.ripMoon && oracle.glowers > 0 && oracle.mySwarmers.Count > 0)
								{
									for (int l = 0; l < oracle.mySwarmers.Count; l++)
									{
										oracle.mySwarmers[l].ExplodeSwarmer();
									}
									session.saveState.deathPersistentSaveData.ripMoon = true;
									(oracle.oracleBehavior as SLOracleBehaviorHasMark).dialogBox.Interrupt("...", 1);
									if ((oracle.oracleBehavior as SLOracleBehaviorHasMark).currentConversation != null)
									{
										(oracle.oracleBehavior as SLOracleBehaviorHasMark).currentConversation.Destroy();
									}
									Vector2 lttmPos = oracle.bodyChunks[0].pos;
									self.room.AddObject(new ShockWave(lttmPos, 500f, 0.75f, 18, false));
									self.room.AddObject(new Explosion.ExplosionLight(lttmPos, 320f, 1f, 5, Color.white));
									ascended = true;
								}
							}
							if (kill is Oracle challengeOracle && challengeOracle.ID == MoreSlugcatsEnums.OracleID.ST && challengeOracle.Consious)
							{
								Vector2 challengeOraclePos = challengeOracle.bodyChunks[0].pos;
								self.room.AddObject(new ShockWave(challengeOraclePos, 500f, 0.75f, 18, false));
								(challengeOracle.oracleBehavior as STOracleBehavior).AdvancePhase();
								self.bodyChunks[0].vel = Vector2.zero;
								ascended = true;
							}
						}

						player.saintTarget = null;
						player.ascendTimer = 0f;
						player.ascensionBuffer = maxAscensionBuffer;
						player.ascendedSinceActivation++;
						player.activationTimer = Mathf.Max(player.activationTimer - 200f, 0f);

                        if (ascended || self.voidSceneTimer > 0)
                        {
							self.room.PlaySound(SoundID.Firecracker_Bang, self.mainBodyChunk, false, 1f, 0.75f + UnityEngine.Random.value);
							self.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
						}
						else
						{
							self.room.PlaySound(SoundID.Snail_Pop, self.mainBodyChunk, false, 1f, 1.5f + UnityEngine.Random.value);
						}
						for (int n = 0; n < 20; n++)
						{
							self.room.AddObject(new Spark(player.saintTargetPos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
						}

						if (self.voidSceneTimer > 0)
						{
							self.voidSceneTimer = 0;
							self.DeactivateAscension();
							self.controller = null;
							player.wormTarget = null;
							return;
						}
					}

					return;
				}
			}
			orig(self);
		}

		private static void AddVoidSpawnKeeper(Player self)
		{
			RoomSettings.RoomEffect effect = null;
			for (int i = 0; i < self.room.roomSettings.effects.Count; i++)
			{
				if (self.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidSpawn)
				{
					effect = self.room.roomSettings.effects[i];
					break;
				}
			}
			if (effect == null)
			{
				effect = new(RoomSettings.RoomEffect.Type.VoidSpawn, 0f, false);
				self.room.roomSettings.effects.Add(effect);
			}

			keeper = new(self.room, self.room.roomSettings.effects[self.room.roomSettings.effects.IndexOf(effect)]);
			self.room.AddObject(keeper);
		}

		private static bool IsValidEntity(PhysicalObject entity)
		{
			return entity != null && ((entity is Creature creature && !creature.dead && entity is not Fly) || (entity is SeedCob cob && !cob.AbstractCob.opened && !cob.AbstractCob.dead) || (entity is Oracle oracle && oracle.Consious));
		}

		private static void Player_DeactivateAscension(On.Player.orig_DeactivateAscension orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && MagicaPlayer.magicaCWT.TryGetValue(self, out var player))
			{
				self.room.PlaySound(SoundID.HUD_Pause_Game, self.mainBodyChunk, false, 1f, 0.5f);
				player.magicaSaintAscension = false;
				player.saintTarget = null;

				float timeRatio = (maxActivationTimer - player.activationTimer) / maxActivationTimer;
				if (player.ascendedSinceActivation > 0 || player.activationTimer < maxActivationTimer / 2f)
				{
					self.airInLungs = Mathf.Lerp(0.2f, 0f, ((float)player.ascendedSinceActivation / 4f) - (timeRatio / 1.5f));
					self.Stun((player.ascendedSinceActivation * 10) + Mathf.RoundToInt(timeRatio * 10));
					self.drown = Mathf.Lerp(0.3f, 0.8f, ((float)player.ascendedSinceActivation / 4f) + (timeRatio / 1.5f));
				}
				self.saintWeakness += (player.ascendedSinceActivation * 150) + (Mathf.RoundToInt(timeRatio * 20f));
				self.exhausted = true;

				player.ascendedSinceActivation = 0;
			}
			else
			{
				orig(self);
			}
		}

		private static void Player_ActivateAscension(On.Player.orig_ActivateAscension orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && MagicaPlayer.magicaCWT.TryGetValue(self, out var player))
			{
				self.wantToJump = 0;
				player.magicaSaintAscension = true;
				self.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.mainBodyChunk, false, 1f, 1f);
			}
			else
			{
				orig(self);
			}
		}

		internal static bool CheckForSaintAscension(Player player)
		{
			return player != null && (MagicaPlayer.magicaCWT.TryGetValue(player, out var saint) && saint.magicaSaintAscension) || (player.monkAscension);
		}

		public class MagicaPlayer
		{
			public static readonly ConditionalWeakTable<Player, MagicaPlayer> magicaCWT = new();
			internal bool magicaSaintAscension;
			internal bool zoneInToAscend;
			internal bool saintTargetMode;
			internal PhysicalObject saintTarget;
			internal bool scannedForTargets;
			internal List<PhysicalObject> validTargets = [];
			internal List<PhysicalObject> sortedTargets = [];
			internal bool sortedThroughTargets;
			internal bool cycledTarget;
			internal float ascendTimer;
			internal int ascendedSinceActivation;
			internal float ascensionBuffer;
			internal float activationTimer;
			internal float ascensionFatique;
			internal Vector2 saintTargetPos;
			internal Vector2[] storedTargetPos;
			internal bool saintTargetIsKarmaLocked;
			internal bool karmaCycling;
			internal float karmaCycleTimer;
			internal float changingTarget;
			internal VoidWorm.Head wormTarget;
		}
	}

	internal class SaintAscensionController : Player.PlayerController
	{
		private Player player;

		public SaintAscensionController(Player self)
		{
			player = self;
		}

		public override Player.InputPackage GetInput()
		{
			if (player.voidSceneTimer > 80)
			{
				return new Player.InputPackage(false, null, 0, 0, false, true, false, false, false);
			}
			return default;
		}
	}
}
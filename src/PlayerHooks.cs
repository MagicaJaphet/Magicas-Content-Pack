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
using static MonoMod.InlineRT.MonoModRule;
using MonoMod.RuntimeDetour;

namespace MagicasContentPack
{
	internal class PlayerHooks
	{
		private static VoidSpawnKeeper keeper;

		public static Dictionary<AbstractPhysicalObject.AbstractObjectType, int> artiCraftables = [];
		public static AbstractPhysicalObject.AbstractObjectType[,] artiResults;

		public static int MaxWarmthActivationTimer 
		{ 
			get
			{
				return 150;
			}
		}

		public static int MaxWarmthCooldown 
		{ 
			get
			{
				return 150;
			}	
		}

		public static bool warmMode;
		public static SaintWarmthTransistion transistionWave;
		public static int warmthActivationTimer;

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
				IL.Water.Update += Water_Update;
				IL.Creature.Update += Creature_Update;
				IL.Player.UpdateAnimation += Player_UpdateAnimation1;
				On.Player.LungUpdate += Player_LungUpdate;
				_ = new Hook(typeof(BodyChunk).GetProperty(nameof(BodyChunk.submersion), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetGetMethod(), SaintSubmersion);
				On.Room.PointSubmerged_Vector2_float += Room_PointSubmerged_Vector2_float;
				On.Room.FloatWaterLevel += Room_FloatWaterLevel;
				IL.BodyChunk.Update += BodyChunk_Update;
				On.Player.UpdateAnimation += Player_UpdateAnimation;
				On.Player.UpdateBodyMode += Player_UpdateBodyMode;
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

		private static void Water_Update(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);
				if (!Plugin.ILMatchFail(cursor.TryGotoNext(MoveType.After,
					x => x.MatchLdfld<Water>(nameof(Water.viscosity)),
					x => x.MatchLdcR4(0))))
				{
					ILLabel label = cursor.Next.Operand as ILLabel;

					if (!Plugin.ILMatchFail(cursor.TryGotoPrev(x => x.MatchLdfld<Water>(nameof(Water.viscosity)))))
					{
						cursor.Emit(OpCodes.Ldloc, 8);
						static bool IsSaintAscension(Water self, BodyChunk bodyChunk)
						{
							return bodyChunk.owner != null && bodyChunk.owner is Player player && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(player, out var saint) && saint.magicaSaintAscension;
						}
						cursor.EmitDelegate(IsSaintAscension);
						cursor.Emit(OpCodes.Brtrue, label);
						cursor.Emit(OpCodes.Ldarg_0);

						Plugin.ILSucceed();
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void Creature_Update(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);
				if (!Plugin.ILMatchFail(cursor.TryGotoNext(x => x.MatchLdarg(0),
					x => x.MatchLdcI4(0),
					x => x.MatchStfld<Creature>(nameof(Creature.lavaContact)))))
				{
					ILLabel jumpLabel = cursor.MarkLabel();

					if (!Plugin.ILMatchFail(cursor.TryGotoPrev(x => x.MatchIsinst<Player>(),
						x => x.MatchCallOrCallvirt<Creature>(nameof(Creature.Die)))))
					{
						static void IsSaintAndActuallySubmerged(Creature creature)
						{
							if (creature is Player self)
							{
								if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && self.room != null)
								{
									float waterLevel = 0f;
									if (self.room.waterObject != null)
									{
										waterLevel = self.room.waterObject.DetailedWaterLevel(self.firstChunk.pos);
									}
									else
									{
										waterLevel = self.room.floatWaterLevel;
									}

									if ((self.room.waterInverted && self.firstChunk.pos.y > waterLevel) || (!self.room.waterInverted && self.firstChunk.pos.y < waterLevel))
									{
										self.Die();
									}
								}
								else
								{
									self.Die();
								}
							}
						}
						cursor.EmitDelegate(IsSaintAndActuallySubmerged);
						cursor.Emit(OpCodes.Br, jumpLabel);
						cursor.Emit(OpCodes.Ldarg_0);

						Plugin.ILSucceed();
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void Player_UpdateAnimation1(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);
				if (!Plugin.ILMatchFail(cursor.TryGotoNext(MoveType.After,
					x => x.MatchNewobj<Bubble>(),
					x => x.MatchCallOrCallvirt(out _))))
				{
					cursor.Emit(OpCodes.Ldarg_0);
					static void IsSaintAscension(Player self)
					{
						if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && player.magicaSaintAscension)
						{
							self.airInLungs = 1f;
							player.ascensionTimer -= 10f;

							Plugin.DebugLog($"{player.ascensionTimer}");
						}
					}
					cursor.EmitDelegate(IsSaintAscension);

					Plugin.ILSucceed();
				}
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
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
				SaveValues.scavsKilledThisCycle++;
				Plugin.DebugLog($"Arti killed scavenger!: {SaveValues.scavsKilledThisCycle}");

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

		private static void Player_LungUpdate(On.Player.orig_LungUpdate orig, Player self)
		{
			if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && player.magicaSaintAscension)
				return;

			orig(self);
		}

		private static float SaintSubmersion(Func<BodyChunk, float> orig, BodyChunk self)
		{
			if (self.owner != null && self.owner is Player player && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(player, out var saint) && saint.magicaSaintAscension && player.bodyMode != MagicaEnums.BodyModes.SaintAscension)
			{
				return 1f;
			}

			return orig(self);
		}

		private static bool Room_PointSubmerged_Vector2_float(On.Room.orig_PointSubmerged_Vector2_float orig, Room self, Vector2 pos, float yDisplacement)
		{
			if (pos != null && self.PlayersInRoom != null && self.PlayersInRoom.Any(x => x != null && pos == x.firstChunk.pos && x is Player player && player.bodyMode != MagicaEnums.BodyModes.SaintAscension && MagicaPlayer.magicaCWT.TryGetValue(player, out var saint) && saint.magicaSaintAscension))
			{
				return true;
			}
			return orig(self, pos, yDisplacement);
		}

		private static float Room_FloatWaterLevel(On.Room.orig_FloatWaterLevel orig, Room self, Vector2 pos)
		{
			if (pos != null && self.PlayersInRoom != null && self.PlayersInRoom.Any(x => x != null && pos == x.firstChunk.pos && x is Player player && player.bodyMode != MagicaEnums.BodyModes.SaintAscension && MagicaPlayer.magicaCWT.TryGetValue(player, out var saint) && saint.magicaSaintAscension))
			{
				return 100000f;
			}

			return orig(self, pos);
		}

		private static void BodyChunk_Update(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				if (!Plugin.ILMatchFail(cursor.TryGotoNext(MoveType.After,
					x => x.MatchLdfld<Room>(nameof(Room.water)),
					x => x.MatchLdloc(0),
					x => x.MatchAnd())))
				{
					cursor.Emit(OpCodes.Ldarg_0);
					static bool IsSaintAscension(bool isWater, BodyChunk chunk)
					{
						return isWater || (ModOptions.CustomMechanics.Value && chunk.owner != null && chunk.owner is Player player && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(player, out var saint) && saint.magicaSaintAscension);
					}
					cursor.EmitDelegate(IsSaintAscension);

					Plugin.ILSucceed();
				}
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
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

			if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				magicaCWT.warmthLimit = self.GetWarmthLimit();
			}

			// Set player save values, for tutorials and such
			//MagicaSaveState.GetKey(MoreSlugcatsEnums.SlugcatStatsName.Saint.value, nameof(SaveValues.SaintWarmthMechanicTutorial), out SaveValues.SaintWarmthMechanicTutorial);
		}

		private static void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && self.room != null && self.room.game.IsStorySession && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && player.magicaSaintAscension)
			{
				if ((self.input[0].x == 0 && self.input[0].y == 0) || self.input[0].pckp)
				{
					self.buoyancy = 0f;
					self.gravity = 0f;
					self.airFriction = 0.7f;
					self.animation = Player.AnimationIndex.None;
					return;
				}
			}

			orig(self);
		}

		private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && self.room != null && self.room.game.IsStorySession && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && MagicaPlayer.magicaCWT.TryGetValue(self, out var player) && player.magicaSaintAscension)
			{
				if ((self.input[0].x == 0 && self.input[0].y == 0) || self.input[0].pckp)
				{
					self.bodyMode = MagicaEnums.BodyModes.SaintAscension;
					return;
				}
			}

			orig(self);
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
				//bool storySaint = self.abstractCreature.world.game.IsStorySession && self.abstractCreature.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint;

				//if (storySaint && self.KarmaCap >= 4)
				//{
				//	if (self.room != null && self.room != player.lastRoom)
				//	{
				//		player.lastRoom = self.room;
						
				//	}

				//	if (self.AI == null && self.Hypothermia > 0.6f && !SaveValues.SaintWarmthMechanicTutorial)
				//	{
				//		SaveValues.SaintWarmthMechanicTutorial = true;
				//		self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("saint_warmth_tutorial"), 240, 400, true, true);
				//		return;
				//	}

				//	if (player.warmthCooldown > 0)
				//	{
				//		player.warmthCooldown = Math.Max(0, player.warmthCooldown - 1);
				//	}

				//	if (self.room != null && self.room.game != null && warmthActivationTimer == MaxWarmthActivationTimer / 5)
				//	{
				//		self.room.AddObject(new SaintWarmthTransistion(self.room));
				//	}

				//	if (self.ValidSaintWarmthInput() && player.warmthCooldown <= 0 && warmthActivationTimer < MaxWarmthActivationTimer / 4)
				//	{
				//		warmthActivationTimer++;
				//	}
				//	else if (warmthActivationTimer >= MaxWarmthActivationTimer / 4 && warmthActivationTimer < MaxWarmthActivationTimer)
				//	{
				//		warmthActivationTimer++;
				//	}
				//	else if (warmthActivationTimer >= MaxWarmthActivationTimer)
				//	{
				//		warmthActivationTimer = 0;
				//		player.warmthCooldown = MaxWarmthCooldown;
				//		warmMode = !warmMode;
				//	}
				//	else if (warmthActivationTimer > 0)
				//	{
				//		warmthActivationTimer = Math.Max(0, warmthActivationTimer - 2);
				//	}
				//}

				bool isSaint = self.abstractCreature.world.game.IsStorySession && self.abstractCreature.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint;
				if (isSaint && self.room.world.region != null)
				{
					if (self.lastPingRegion == "")
					{
						self.lastPingRegion = self.room.world.region.name;
					}
					if (self.lastPingRegion != self.room.world.region.name && !self.room.abstractRoom.gate)
					{
						self.lastPingRegion = self.room.world.region.name;
						if (self.room != null && World.CheckForRegionGhost(MoreSlugcatsEnums.SlugcatStatsName.Saint, self.room.world.region.name))
						{
							GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(self.room.world.region.name);
							if (!(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo.ContainsKey(ghostID) || (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID] < 2)
							{
								self.room.AddObject(new GhostPing(self.room));
							}
						}
					}
				}
				if (!MMF.cfgOldTongue.Value && self.input[0].jmp && !self.input[1].jmp && !self.input[0].pckp && self.canJump <= 0 && self.bodyMode != Player.BodyModeIndex.Crawl && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.HangFromBeam && self.SaintTongueCheck())
				{
					Vector2 vector = new Vector2((float)self.flipDirection, 0.7f);
					Vector2 normalized = vector.normalized;
					if (self.input[0].y > 0)
					{
						normalized = new Vector2(0f, 1f);
					}
					normalized = (normalized + self.mainBodyChunk.vel.normalized * 0.2f).normalized;
					self.tongue.Shoot(normalized);
				}

				if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && (self.KarmaCap >= 9 || (self.room.game.session is ArenaGameSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeMeta.ascended)))
				{
					if (self.room.game.IsStorySession)
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
					}
					if (isSaint && self.AI == null && self.room.game.session is StoryGameSession && !(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.SaintEnlightMessage)
					{
						(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.SaintEnlightMessage = true;
						self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("saint_flight_tutorial0"), 240, 640, true, true);
						return;
					}

					if (self.voidSceneTimer > 0 && isSaint)
					{
						self.voidSceneTimer++;
						if (!player.magicaSaintAscension)
						{
							player.ascensionTimer = 9999f;
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
					if (player.ascensionFatique > 0f)
					{
						player.ascensionFatique = Math.Max(player.ascensionFatique - 1, 0);
					}

					if (player.ascensionCooldown > 0f)
					{
						player.ascensionCooldown = Math.Max(player.ascensionCooldown - 1, 0);
					}

					if (player.ascensionCooldown <= 0 && player.ascensionFatique <= 0f && player.ascensionActivationTimer < self.MaxAscensionActivationTimer() && self.ValidSaintWarmthInput())
					{
						player.ascensionActivationTimer++;
					}
					else if (player.magicaSaintAscension && player.ascensionTimer <= 0f)
					{
						player.ascensionActivationTimer = self.MaxAscensionActivationTimer() / 2;
						player.ascensionFatique = 50;
						self.DeactivateAscension();
						self.wantToJump = 0;
						return;
					}
					else if (player.ascensionActivationTimer >= self.MaxAscensionActivationTimer())
					{
						if (!player.magicaSaintAscension)
						{
							player.ascensionActivationTimer = 0;
							self.ActivateAscension();
						}
						else
						{
							player.ascensionActivationTimer = self.MaxAscensionActivationTimer() / 2;
							self.DeactivateAscension();
						}
						return;
					}
					else if (player.ascensionActivationTimer > 0)
					{
						player.ascensionActivationTimer = Mathf.Max(0, player.ascensionActivationTimer - 2);
					}

					if (player.ascensionActivationTimer > 0)
					{
						if (self.room != null && !ModManager.CoopAvailable && self.voidSceneTimer == 0 && !player.magicaSaintAscension)
						{
							player.roomNeedsRefresh = true;

							float defaultGhostMode = self.room == null || self.room.world.worldGhost == null ? 0f : self.room.world.worldGhost.GhostMode(self.room, self.room.game.cameras[0].currentCameraPosition);
							self.room.game.cameras[0].ghostMode = Mathf.Lerp(defaultGhostMode, 1f, (float)(player.ascensionActivationTimer) / (float)self.MaxAscensionActivationTimer());
						}

						self.canJump = 0;
						self.wantToJump = 0;
						self.gravity = 0f;
						foreach (var chunk in self.bodyChunks)
						{
							chunk.vel.y = Mathf.Lerp(0.8f, 3f, (float)player.ascensionActivationTimer / (float)self.MaxAscensionActivationTimer());
							chunk.vel.x /= 1.2f;
							if (chunk == self.firstChunk)
							{
								chunk.vel += Custom.DegToVec(0) * 2f;
							}
						}
					}
					else if (player.roomNeedsRefresh && self.room != null)
					{
						player.roomNeedsRefresh = false;
						self.room.game.cameras[0].UpdateGhostMode(self.room, self.room.game.cameras[0].currentCameraPosition);
					}

				}

				if (player.magicaSaintAscension)
				{
					if (player.ascensionTimer < self.MaxAscensionTimer() / 6)
					{
						if (self.room != null && !ModManager.CoopAvailable && self.voidSceneTimer == 0)
						{
							player.roomNeedsRefresh = true;

							float defaultGhostMode = self.room == null || self.room.world.worldGhost == null ? 0f : self.room.world.worldGhost.GhostMode(self.room, self.room.game.cameras[0].currentCameraPosition);
							self.room.game.cameras[0].ghostMode = Mathf.Lerp(defaultGhostMode, 1f, (player.ascensionTimer / ((float)self.MaxAscensionTimer() / 6f)) + 0.5f);
						}
					}

					if (!self.inShortcut && self.karmaCharging == 0)
					{
						player.ascensionTimer--;
					}
					else if (self.karmaCharging > 0)
					{
						player.ascensionTimer = Mathf.Min(player.ascensionTimer + 1f, self.MaxAscensionTimer());
						self.karmaCharging--;
					}

					if (self.tongue != null && self.tongue.Attached)
					{
						self.tongue.Release();
					}
					if (self.dead || self.stun >= 20)
					{
						player.ascensionActivationTimer = self.MaxAscensionActivationTimer() / 2;
						self.DeactivateAscension();
					}

					if (isSaint && self.AI == null && player.saintTarget != null && self.room.game.session is StoryGameSession session2 && !session2.saveState.deathPersistentSaveData.KarmicBurstMessage)
					{
						(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.KarmicBurstMessage = true;
						self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("saint_flight_tutorial1"), 80, 800, true, true);
					}

					if ((player.saintTarget != null && player.saintTarget.owner.room == self.room) || player.wormTarget != null)
					{
						if (player.storedTargetPos.Length == 10 && player.storedTargetPos[9] != null)
						{
							player.saintTargetPos = Vector2.Lerp(player.saintTargetPos, player.storedTargetPos[9], 0.2f);
						}

						for (int i = player.storedTargetPos.Length - 1; i > 0; i--)
						{
							player.storedTargetPos[i] = player.storedTargetPos[i - 1];
						}
						player.storedTargetPos[0] = player.saintTarget == null ?
							player.wormTarget == null ? default : player.wormTarget.HeadPos(self.room.game.myTimeStacker)
							: player.saintTarget.owner is Centipede pede && player.switchPedeTarget ? pede.bodyChunks[pede.bodyChunks.Length - 1].pos : player.saintTarget.pos;
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

					if (player.ascensionActivationTimer <= 0)
					{
						if (!self.input[0].pckp)
						{
							if (player.wormTarget == null)
							{
								player.ascensionTimer = Mathf.Max(player.ascensionTimer - 1f, 0f);
							}

							player.saintTargetMode = false;
							player.scannedForTargets = false;

							if (self.input[0].x == 0 && self.input[0].y == 0)
							{
								foreach (var chunk in self.bodyChunks)
								{
									chunk.vel.y = Mathf.Lerp(-0.2f, 0.06f, (Mathf.Sin(player.ascensionTimer / 50f) + 1f) / 2f);
									chunk.vel.x /= 1.2f;
									if (chunk == self.firstChunk)
									{
										chunk.vel += Custom.DegToVec(0) * 2f;
									}
								}
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
										player.validTargets.AddRange(from x in self.room.physicalObjects[i] where x != self && IsValidEntity(x) && !player.validTargets.Contains(FindClosestBodyChunk(self, x)) select FindClosestBodyChunk(self, x));
									}
								}
							}
							else
							{
								player.sortedTargets = (from x in player.validTargets where x.owner.room == self.room && (x.owner is not Centipede && Custom.DistLess(self.firstChunk.pos, x.pos, self.SaintAscensionRadius()) || x.owner is Centipede pede && pede.bodyChunks.Any(x => Custom.DistLess(self.firstChunk.pos, x.pos, self.SaintAscensionRadius()))) select x).ToList();
								player.sortedTargets.Sort((x, y) => x.pos.x.CompareTo(y.pos.x));

								if (self.input[0].x > 0f && !player.cycledTarget)
								{
									player.cycledTarget = true;
									if (player.saintTarget != null && player.sortedTargets.Count > 0)
									{
										int nextIndex = player.sortedTargets.IndexOf(player.saintTarget) + 1 >= player.sortedTargets.Count ? 0 : player.sortedTargets.IndexOf(player.saintTarget) + 1;
										if (player.saintTarget.owner is Centipede pede && Custom.DistLess(self.firstChunk.pos, player.switchPedeTarget ? pede.firstChunk.pos : pede.bodyChunks[1].pos, self.SaintAscensionRadius()) && Custom.Dist(self.firstChunk.pos, player.switchPedeTarget ? pede.firstChunk.pos : pede.bodyChunks[1].pos) < Custom.Dist(self.firstChunk.pos, player.sortedTargets[nextIndex].pos))
										{
											player.switchPedeTarget = !player.switchPedeTarget;
										}
										else if (player.sortedTargets[nextIndex].owner.room == self.room && Custom.DistLess(self.firstChunk.pos, player.sortedTargets[nextIndex].pos, self.SaintAscensionRadius()))
										{
											player.saintTarget = player.sortedTargets[nextIndex];
										}
										else
										{
											player.saintTarget = player.sortedTargets[0];
										}
									}
									else if (player.sortedTargets.Count > 0 && player.sortedTargets[0].owner.room == self.room)
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
										if (player.saintTarget.owner is Centipede pede && Custom.DistLess(self.firstChunk.pos, player.switchPedeTarget ? pede.firstChunk.pos : pede.bodyChunks[1].pos, self.SaintAscensionRadius()) && Custom.Dist(self.firstChunk.pos, player.switchPedeTarget ? pede.firstChunk.pos : pede.bodyChunks[1].pos) < Custom.Dist(self.firstChunk.pos, player.sortedTargets[nextIndex].pos))
										{
											player.switchPedeTarget = !player.switchPedeTarget;
										}
										else if (player.sortedTargets[nextIndex].owner.room == self.room && Custom.DistLess(self.firstChunk.pos, player.sortedTargets[nextIndex].pos, self.SaintAscensionRadius()))
										{
											player.saintTarget = player.sortedTargets[nextIndex];
										}
										else
										{
											player.saintTarget = player.sortedTargets[player.sortedTargets.Count - 1];
										}
									}
									else if (player.sortedTargets.Count > 0 && player.sortedTargets[player.sortedTargets.Count - 1].owner.room == self.room)
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
					}

					if ((player.saintTarget == null || (player.saintTarget.owner.room != self.room && !player.saintTargetMode || !Custom.DistLess(self.firstChunk.pos, player.saintTarget.pos, self.SaintAscensionRadius()))) && player.wormTarget == null)
					{
						player.changingTarget = 20f;
						player.saintTarget = null;
						player.karmaCycling = false;
						player.ascendTimer = 0f;

						if (self.graphicsModule is PlayerGraphics graphics && graphics.objectLooker.currentMostInteresting != null && IsValidEntity(graphics.objectLooker.currentMostInteresting))
						{
							player.saintTarget = FindClosestBodyChunk(self, graphics.objectLooker.currentMostInteresting);
						}
						else
						{
							List<BodyChunk> canidates = [];
							for (int i = 0; i < self.room.physicalObjects.Length; i++)
							{
								for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
								{
									canidates.AddRange((from x in self.room.physicalObjects[i] where x != self && IsValidEntity(x) && Custom.DistLess(self.firstChunk.pos, FindClosestBodyChunk(self, x).pos, self.SaintAscensionRadius()) select FindClosestBodyChunk(self, x)).ToList());
								}
								if (canidates.Count > 0)
								{
									break;
								}
							}

							canidates.Sort((x, y) => Custom.Dist(x.pos, self.firstChunk.pos).CompareTo(Custom.Dist(y.pos, self.firstChunk.pos)));
							player.saintTarget = canidates.FirstOrDefault();
						}
					}


					if (player.ascensionBuffer > 0f)
					{
						player.ascensionBuffer = Mathf.Max(player.ascensionBuffer - 1f, 0f);
					}
					if (self.input[0].thrw && (player.saintTarget != null || player.wormTarget != null) && player.ascensionBuffer <= 0f)
					{
						float karmaLocked = player.saintTarget.owner is TempleGuard guard || player.saintTargetIsKarmaLocked ? 0.4f : 1f;
						karmaLocked = player.saintTarget.owner is Creature creature ? karmaLocked * Mathf.Max(0.15f, ((float)GraphicsHooks.GetKarmaOfSpecificCreature(creature, creature.Template.type) / 9f)) : karmaLocked;
						player.ascendTimer = Mathf.Min(Custom.LerpBackEaseOut(player.ascendTimer + (0.15f * karmaLocked), player.ascendTimer + (0.01f * karmaLocked), Custom.Dist(self.firstChunk.pos, (player.saintTarget == null && player.wormTarget != null ? player.wormTarget.HeadPos(self.room.game.myTimeStacker) : player.saintTarget.pos)) / (self.SaintAscensionRadius() * 1.5f)), 1f);

						if (player.saintTarget != null && player.saintTarget.owner is Creature creature2)
						{
							if (creature2 is DaddyLongLegs dLL)
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
							killable.AddRange((from x in self.room.physicalObjects[i] where x != self && !killable.Contains(x) && x.room == self.room && IsValidEntity(x) && x.bodyChunks.Any(y => (y == x.firstChunk || (x is Centipede && y == x.bodyChunks[x.bodyChunks.Length - 1])) && Custom.DistLess(y.pos, player.saintTargetPos, 50f)) select x).ToList());
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

						player.ascensionBuffer = self.MaxAscensionBuffer(player.saintTarget);
						player.ascensionTimer = Mathf.Max(player.ascensionTimer - (self.MaxAscensionBuffer(player.saintTarget) * 5f), 0f);
						player.maxBuffer = player.ascensionBuffer;
						player.saintTarget = null;
						player.ascendTimer = 0f;
						player.ascendedSinceActivation++;

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
						}
					}
				}
				return;
			}
			orig(self);
		}

		private static BodyChunk FindClosestBodyChunk(Player self, PhysicalObject target)
		{
			return target is Centipede pede ? target.bodyChunks.Where(x => x == pede.firstChunk || x == pede.bodyChunks[pede.bodyChunks.Length - 1]).OrderBy(x => Custom.Dist(self.firstChunk.pos, x.pos)).First() : target.firstChunk;
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
			return entity != null && ((entity is Creature creature && (creature is not Player || !ModManager.CoopAvailable || Custom.rainWorld.options.friendlyFire) && !creature.dead && entity is not Fly) || (entity is SeedCob cob && !cob.AbstractCob.opened && !cob.AbstractCob.dead) || (entity is Oracle oracle && oracle.Consious));
		}

		private static void Player_DeactivateAscension(On.Player.orig_DeactivateAscension orig, Player self)
		{
			if (ModOptions.CustomMechanics.Value && MagicaPlayer.magicaCWT.TryGetValue(self, out var player))
			{
				self.room.PlaySound(SoundID.HUD_Pause_Game, self.mainBodyChunk, false, 1f, 0.5f);
				self.room.PlaySound(SoundID.Slugcat_Ghost_Dissappear, self.mainBodyChunk, false, 1f, 0.5f);
				player.magicaSaintAscension = false;
				player.saintTarget = null;
				player.ascensionCooldown = 60;

				if (self.voidSceneTimer == 0)
				{
					//player.ascensionFatique = 50;
				}

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
				self.room.PlaySound(SoundID.Slugcat_Ghost_Appear, self.mainBodyChunk, false, 1f, 0.5f);
				self.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.mainBodyChunk, false, 1f, 1f);
				player.ascensionTimer = self.MaxAscensionTimer();
				player.ascensionCooldown = 60;
				player.magicaSaintAscension = true;

				if (!ModManager.CoopAvailable && self.room != null)
				{
					self.room.game.cameras[0].ghostMode = 1f;
				}
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
			internal BodyChunk saintTarget;
			internal bool scannedForTargets;
			internal List<BodyChunk> validTargets = [];
			internal List<BodyChunk> sortedTargets = [];
			internal bool sortedThroughTargets;
			internal bool cycledTarget;
			internal float ascendTimer;
			internal int ascendedSinceActivation;
			internal float ascensionBuffer;
			internal float ascensionTimer;
			internal int ascensionFatique;
			internal Vector2 saintTargetPos;
			internal Vector2[] storedTargetPos;
			internal bool saintTargetIsKarmaLocked;
			internal bool karmaCycling;
			internal float karmaCycleTimer;
			internal float changingTarget;
			internal VoidWorm.Head wormTarget;

			// For new warmth mechanic
			internal int warmthLimit;
			internal int warmthCooldown;
			internal Room lastRoom;
			internal int ascensionActivationTimer;
			internal float maxBuffer;
			internal bool roomNeedsRefresh;
			internal int ascensionCooldown;
			internal bool switchPedeTarget;
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
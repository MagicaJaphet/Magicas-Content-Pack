using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static MagicasContentPack.IteratorHooks.IteratorHooks;

namespace MagicasContentPack.IteratorHooks
{

	/// <summary>
	/// For everything used in the standard Oracle class. Covers all iterators within the game standardly and graphics-wise.
	/// </summary>
	public class OracleHooks
	{
		public enum OracleColor
		{
			SRSPearls,
			SRSCloak,
			SRSCloakDark,
		}

		public static Dictionary<OracleColor, Color> oracleColor = new()
		{
			{ OracleColor.SRSPearls, new Color(0.659f, 0.133f, 0.067f) },
			{ OracleColor.SRSCloak, new(0.314f, 0.212f, 0.239f) },
			{ OracleColor.SRSCloakDark, new(0.196f, 0.153f, 0.188f) },
		};

		public static void Init()
		{
			try
			{
				// FIX THE STUPID FUCKING METHOD OF SETTING THE THIRD EYE SPRITE STUPID ASS CODE WHY THE HELL IS THIS NOT JUST SET IN THE CTOR!!!!!!!!
				_ = new Hook(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.MoonThirdEyeSprite), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<OracleGraphics, int> orig, OracleGraphics oracle) =>
				{
					if (oracle.IsPebbles)
					{
						return oracle.killSprite + 1;
					}
					return orig(oracle);
				});

				// Tells the game to spawn an oracle here
				On.Room.ReadyForAI += Room_ReadyForAI;

				// Changes basic oracle things
				IL.Oracle.ctor += Oracle_ctor;
				On.Oracle.ctor += TellFPToShutUp;
				IL.Oracle.SetUpMarbles += Oracle_SetUpMarbles;
				On.Oracle.SetUpMarbles += YeetAnyUnwantedObjectsNow;

				// Oracle graphics
				IL.OracleGraphics.ctor += ThirdEye;
				IL.OracleGraphics.InitiateSprites += InitiateCustomSprites;
				IL.OracleGraphics.DrawSprites += ThirdEyeDrawSprites;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static void TellFPToShutUp(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
		{
			allowFpToCheckHisPearl = false;

			orig(self, abstractPhysicalObject, room);

			Plugin.DebugLog(self.ID.value);
			if (self.ID == MagicaEnums.Oracles.SRS)
			{
				self.arm = new Oracle.OracleArm(self)
				{
					isActive = true
				};
				self.oracleBehavior = new MagicaOracleBehavior(self);
				self.myScreen = new OracleProjectionScreen(room, self.oracleBehavior);
				self.marbles = new List<PebblesPearl>();
				self.SetUpMarbles();
				room.gravity = 0f;
				for (int m = 0; m < room.updateList.Count; m++)
				{
					if (room.updateList[m] is AntiGravity)
					{
						(room.updateList[m] as AntiGravity).active = false;
						return;
					}
				}
				return;
			}
		}
		private static void YeetAnyUnwantedObjectsNow(On.Oracle.orig_SetUpMarbles orig, Oracle self)
		{
			orig(self);

			if (self.ID == Oracle.OracleID.SS && self.room.world.name == "SS")
			{
				MagicaSaveState.GetKey(MagicaSaveState.anySave, nameof(SaveValues.WhoShowedFPThePearl), out SaveValues.WhoShowedFPThePearl);
				SSOracleBehaviorHooks.halcyonPearl = null;

				int halcyonNum = 0;
				for (int i = 0; i < self.room.updateList.Count; i++)
				{
					UpdatableAndDeletable item = self.room.updateList[i];

					if (item != null && item is HalcyonPearl pearl && pearl.abstractPhysicalObject != null)
					{
						Plugin.DebugLog("Found halcyon!");
						halcyonNum++;

						if (halcyonNum > 1)
						{
							Plugin.DebugLog("Extra Halcyon found, removing... " + pearl.abstractPhysicalObject.ID);
							item.RemoveFromRoom();
							item.Destroy();
							continue;
						}

						if (self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear || !ModOptions.CustomInGameCutscenes.Value || string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl) || SSOracleBehaviorHooks.CheckIfWhoTookThePearlIsBeforeCurrent(self.room.game.TimelinePoint))
						{
							Plugin.DebugLog("Invalid Halcyon found, removing... " + pearl.abstractPhysicalObject.ID);
							if (self.oracleBehavior is SSOracleBehavior behav && behav.readDataPearlOrbits.Contains(pearl.AbstractPearl))
							{
								behav.readDataPearlOrbits.Remove(pearl.AbstractPearl);
								behav.readPearlGlyphs.Remove(pearl.AbstractPearl);
							}

							item.RemoveFromRoom();
							item.Destroy();
						}
					}
				}
			}

			if (self.ID == MoreSlugcatsEnums.OracleID.CL)
			{
				int moonPearlNum = 0;
				for (int i = 0; i < self.room.updateList.Count; i++)
				{
					if (self.room.updateList[i] is DataPearl moonPearl && moonPearl.AbstractPearl.dataPearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl)
					{
						moonPearl.firstChunk.pos = new(2600f, 200f);

						moonPearlNum++;
						if (moonPearlNum > 1)
						{
							Plugin.DebugLog("Extra Moon pearl found, removing... " + moonPearl.abstractPhysicalObject.ID);
							moonPearl.Destroy();
							self.room.updateList[i].RemoveFromRoom();
							continue;
						}

						if (!string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl))
						{
							moonPearl.Destroy();
							self.room.updateList[i].RemoveFromRoom();
						}
					}
				}
			}

			allowFpToCheckHisPearl = true;
		}

		private static void Oracle_SetUpMarbles(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool succeed = cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchLdcI4(0)
					);

				if (Plugin.ILMatchFail(succeed))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldloc, 0);
				static void SRSInjection(Oracle self, Vector2 startPos)
				{
					if (self.room.world.name == "7S")
					{
						startPos = new(450f, 100f);
					}
				}
				cursor.EmitDelegate(SRSInjection);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}
		private static void Oracle_ctor(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool succeed = cursor.TryGotoNext(
					x => x.MatchNewarr<PhysicalObject.BodyChunkConnection>()
					);

				if (Plugin.ILMatchFail(succeed))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldarg_2);
				static void SRSInjection(Oracle self, Room room)
				{
					if (room.world.name == "7S")
					{
						self.ID = MagicaEnums.Oracles.SRS;
						for (int i = 0; i < self.bodyChunks.Length; i++)
						{
							self.bodyChunks[i] = new BodyChunk(self, i, new Vector2(500f, 300f), 6f, 0.5f);
						}
					}
				}
				cursor.EmitDelegate(SRSInjection);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}
		private static void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
		{
			orig(self);

			if (self.game != null && self.game.IsStorySession && self.abstractRoom.name == "7S_AI")
			{
				Oracle srsObject = new(new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(self.abstractRoom.index, 15, 15, -1), self.game.GetNewID()), self);
				self.AddObject(srsObject);
			}
			self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80);
		}



		private static void ThirdEye(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					move => move.MatchLdarg(0),
					move => move.MatchCall(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsMoon)).GetGetMethod()),
					move => move.MatchBrtrue(out _),
					move => move.MatchLdarg(0),
					move => move.MatchCall(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsPastMoon)).GetGetMethod()),
					move => move.MatchBrtrue(out _),
					move => move.MatchLdarg(0),
					move => move.MatchCall(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsStraw)).GetGetMethod())
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Index += 2;
				ILLabel label = (ILLabel)cursor.Next.Operand;
				cursor.Index += 6;
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(MagicaOracleGraphics).GetMethod(nameof(MagicaOracleGraphics.IsSRS)));
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsPebbles)).GetGetMethod());
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsRottedPebbles)).GetGetMethod());
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsSaintPebbles)).GetGetMethod());

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void ThirdEyeDrawSprites(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					move => move.MatchLdarg(0),
					move => move.MatchCall(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsMoon)).GetGetMethod()),
					move => move.MatchBrtrue(out _),
					move => move.MatchLdarg(0),
					move => move.MatchCall(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsPastMoon)).GetGetMethod()),
					move => move.MatchBrtrue(out _),
					move => move.MatchLdarg(0),
					move => move.MatchCall(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsStraw)).GetGetMethod())
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Index += 2;
				ILLabel label = (ILLabel)cursor.Next.Operand;
				cursor.Index += 6;
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(MagicaOracleGraphics).GetMethod(nameof(MagicaOracleGraphics.IsSRS)));
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsPebbles)).GetGetMethod());
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsRottedPebbles)).GetGetMethod());
				cursor.Emit(OpCodes.Brtrue_S, label);
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsSaintPebbles)).GetGetMethod());

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void InitiateCustomSprites(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					MoveType.Before,
					move => move.MatchCallvirt<GraphicsModule>(nameof(GraphicsModule.AddToContainer))
					);

				if (Plugin.ILMatchFail(success))
					return;

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldarg_1);
				cursor.Emit(OpCodes.Ldarg_2);
				static void SRSDetails(OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
				{
					if (self.IsMoon || self.IsPastMoon)
					{
						sLeaser.sprites[self.neckSprite].scaleX = 6f;
						sLeaser.sprites[self.HeadSprite].SetElementByName("MoonHead");
						sLeaser.sprites[self.ChinSprite].SetElementByName("MoonHead");

						for (int i = 0; i < 2; i++)
						{
							sLeaser.sprites[self.EyeSprite(i)].SetElementByName("MoonEye");
							sLeaser.sprites[self.PhoneSprite(i, 2)].SetElementByName("MoonEar");
						}
					}
					else if (self.IsPebbles || self.IsSaintPebbles || self.IsRottedPebbles)
					{
						sLeaser.sprites[self.MoonThirdEyeSprite] = new("FPMark", true)
						{
							shader = rCam.game.rainWorld.Shaders["Hologram"]
						};

						sLeaser.sprites[self.neckSprite].scaleX = 5f;
						sLeaser.sprites[self.HeadSprite].SetElementByName("FPHead");
						sLeaser.sprites[self.HeadSprite].anchorY = 0.4f;
						sLeaser.sprites[self.ChinSprite].SetElementByName("FPHead");

						for (int i = 0; i < 2; i++)
						{
							if (!self.IsRottedPebbles)
							{
								sLeaser.sprites[self.EyeSprite(i)].SetElementByName("FPEyeOpen");
							}
							else
							{
								sLeaser.sprites[self.EyeSprite(i)].SetElementByName("FPEye");
							}
							sLeaser.sprites[self.PhoneSprite(i, 2)].SetElementByName("FPEar");
						}
					}
					else if (self.IsSRS())
					{
						sLeaser.sprites[self.MoonThirdEyeSprite] = new("SRSMark", true)
						{
							shader = rCam.game.rainWorld.Shaders["Hologram"]
						};

						sLeaser.sprites[self.neckSprite].scaleX = 0.5f;
						sLeaser.sprites[self.neckSprite].SetElementByName("SRSNeck");

						sLeaser.sprites[self.HeadSprite].SetElementByName("SRSHead");
						sLeaser.sprites[self.ChinSprite].SetElementByName("SRSHead");

						for (int i = 0; i < 2; i++)
						{
							sLeaser.sprites[self.EyeSprite(i)].SetElementByName("SRSEye");
							sLeaser.sprites[self.PhoneSprite(i, 2)].SetElementByName("SRSEar");
						}

						Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 6);

						sLeaser.sprites[sLeaser.sprites.Length - 6] = new("pixel", true)
						{
							anchorY = 0,
							scaleX = 1.2f
						};
						sLeaser.sprites[sLeaser.sprites.Length - 5] = new("pixel", true)
						{
							anchorY = 0,
							scaleX = 1.2f
						};
						sLeaser.sprites[sLeaser.sprites.Length - 4] = new("SRSCloak3", true)
						{
							anchorY = 0.9f,
							scaleX = 1.2f
						};

						sLeaser.sprites[sLeaser.sprites.Length - 3] = new("SRSCloak1", true)
						{
							scale = 0.9f
						};
						sLeaser.sprites[sLeaser.sprites.Length - 2] = new("SRSCloak2", true)
						{
							scale = 0.8f,
							anchorX = 1,
							anchorY = 0
						};
						sLeaser.sprites[sLeaser.sprites.Length - 1] = new("SRSCloak2", true)
						{
							scale = 0.8f,
							anchorX = 1,
							anchorY = 0
						};
					}
				}
				cursor.EmitDelegate(SRSDetails);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}
	}
}
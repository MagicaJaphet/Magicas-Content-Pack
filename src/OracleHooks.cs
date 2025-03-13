using IL;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using On;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MagicasContentPack
{
	public class OracleHooks
	{
		private static int handPose;
		private static int handTimer;
		public static int handTimerMax = 30;

		public static Dictionary<OracleColor, Color> oracleColor = new()
		{
			{ OracleColor.SRSPearls, new Color(0.659f, 0.133f, 0.067f) },
			{ OracleColor.SRSCloak, new(0.314f, 0.212f, 0.239f) },
			{ OracleColor.SRSCloakDark, new(0.196f, 0.153f, 0.188f) },
		};

		public enum OracleColor
		{
			SRSPearls,
			SRSCloak,
			SRSCloakDark,
		}

		private static List<SSOracleBehavior.Action> ListOfCustomActions()
		{
			return
			[
				MagicaEnums.OracleActions.ArtiFPPearlInit,
				MagicaEnums.OracleActions.ArtiFPPearlInspect,
				MagicaEnums.OracleActions.ArtiFPPearlPlay,
				MagicaEnums.OracleActions.ArtiFPPearlStop,
				MagicaEnums.OracleActions.ArtiFPPearlGrabbed,
				MagicaEnums.OracleActions.FPMusicPearlInit,
				MagicaEnums.OracleActions.FPMusicPearlPlay,
				MagicaEnums.OracleActions.FPMusicPearlStop,
				MagicaEnums.OracleActions.ArtiFPAllPearls,
				MagicaEnums.OracleActions.SRSSeesPurple,
				MagicaEnums.OracleActions.RedDiesInFP
			];
		}

		private static HalcyonPearl halcyonPearl;
		private static bool allowFpToCheckHisPearl;
		public static bool moonHugRed;
		public static bool moonRevivedThisCycle;
		internal static bool hasBeenTouchedBySaintsHalo;
		internal static bool checkedForMoonPearlThisCycle;
		private static DataPearl moonPearlObj;
		private static bool inspectObjectIsMoonPearl;

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

				// Moon / FP "hugging" hunter
				_ = new Hook(typeof(SLOracleBehaviorHasMark).GetProperty(nameof(SLOracleBehaviorHasMark.OracleGetToPos), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<SLOracleBehaviorHasMark, Vector2> orig, SLOracleBehaviorHasMark oracle) =>
				{
					if (moonHugRed && oracle.player != null)
					{
						return oracle.player.firstChunk.pos + new Vector2(12f, 0f);
					}
					return orig(oracle);
				});

				_ = new Hook(typeof(SLOracleBehavior).GetProperty(nameof(SLOracleBehavior.EyesClosed), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<SLOracleBehavior, bool> orig, SLOracleBehavior oracle) =>
				{
					return orig(oracle) || moonHugRed;
				});

				_ = new Hook(typeof(SSOracleBehavior).GetProperty(nameof(SSOracleBehavior.EyesClosed), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<SSOracleBehavior, bool> orig, SSOracleBehavior oracle) =>
				{
					return orig(oracle) || moonHugRed;
				});

				_ = new Hook(typeof(Inspector).GetProperty(nameof(Inspector.OwneriteratorColor), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<Inspector, Color> orig, Inspector inspector) =>
				{
					var result = orig(inspector);
					if (inspector.ownerIterator == 3)
					{
						return new(1f, 0f, 0f);
					}
					return result;
				});

				// Tells the game to spawn an oracle here
				On.MoreSlugcats.Inspector.InitiateGraphicsModule += Inspector_InitiateGraphicsModule;
				On.Room.ReadyForAI += Room_ReadyForAI;

				// Changes basic oracle things
				IL.Oracle.ctor += Oracle_ctor;
				On.Oracle.ctor += TellFPToShutUp;
				IL.Oracle.SetUpMarbles += Oracle_SetUpMarbles;
				On.Oracle.SetUpMarbles += YeetAnyUnwantedObjectsNow;
				On.Oracle.OracleArm.ctor += CustomArmRestraints;
				On.Oracle.OracleArm.BaseDir += CustomArmBaseDir;
				On.Oracle.OracleArm.OnFramePos += CustomArmOnFramePos;

				// Custom pearl colors
				On.DataPearl.ApplyPalette += CustomPearlColors;

				// Oracle graphics
				IL.OracleGraphics.ctor += ThirdEye;
				On.OracleGraphics.Update += CustomGraphicsUpdate;
				IL.OracleGraphics.InitiateSprites += InitiateCustomSprites;
				On.OracleGraphics.AddToContainer += CustomAddToContainer;
				IL.OracleGraphics.DrawSprites += ThirdEyeDrawSprites;
				On.OracleGraphics.DrawSprites += CustomDrawSprites;
				On.OracleGraphics.ApplyPalette += CustomApplyPalette;
				On.OracleGraphics.Gown.Color += CustomGownColors;

				// Adds custom oracles and behaviors
				On.SSOracleBehavior.HandTowardsPlayer += CustomHandTowardPlayerCheck;

				// Moon
				On.SLOracleBehavior.Update += SLOracleBehavior_Update;
				On.SLOracleBehavior.InitCutsceneObjects += SLOracleBehavior_InitCutsceneObjects;
				On.SLOracleBehaviorHasMark.GrabObject += SLOracleBehaviorHasMark_GrabObject;
				On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMark_Update;
				On.SLOracleWakeUpProcedure.ctor += SLOracleWakeUpProcedure_ctor;
				On.SLOracleBehaviorHasMark.SpecialEvent += SLOracleBehaviorHasMark_SpecialEvent;
				On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
				On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;

				// Five Pebbles
				On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
				On.SSOracleBehavior.Update += ItemRecognitions;
				On.SSOracleBehavior.SpecialEvent += CustomSpecialEvent;
				On.SSOracleBehavior.NewAction += CustomFPActions;
				On.SSOracleBehavior.StartItemConversation += AddPearlRecognition;
				On.SSOracleBehavior.PebblesConversation.AddEvents += CustomPebblesConversationEvents; // Also used for custom oracle readings, since they use the PebblesConversation class

				// Saint FP
				On.MoreSlugcats.CLOracleBehavior.InitateConversation += CLOracleBehavior_InitateConversation;
				On.MoreSlugcats.CLOracleBehavior.Update += CLOracleBehavior_Update;
			}
			catch
			{
				Plugin.Log(Plugin.LogStates.HookFail, nameof(OracleHooks));
			}
		}

		private static void Oracle_SetUpMarbles(ILContext il)
		{
			ILCursor cursor = new(il);

			bool succeed = cursor.TryGotoNext(
				MoveType.After,
				x => x.MatchLdcI4(0)
				);

			if (!succeed)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(Oracle_SetUpMarbles));
				return;
			}

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
		}

		private static void Oracle_ctor(ILContext il)
		{
			ILCursor cursor = new(il);

			bool succeed = cursor.TryGotoNext(
				x => x.MatchNewarr<PhysicalObject.BodyChunkConnection>()
				);

            if (!succeed)
            {
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(Oracle_ctor));
				return;
            }

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
        }

		private static void Inspector_InitiateGraphicsModule(On.MoreSlugcats.Inspector.orig_InitiateGraphicsModule orig, Inspector self)
		{
			if (self.room.game.IsStorySession && self.room.world.region != null)
			{
				if (self.room.world.region.name == "7S")
				{
					self.ownerIterator = 3;
				}
			}

			orig(self);
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


		private static bool CustomHandTowardPlayerCheck(On.SSOracleBehavior.orig_HandTowardsPlayer orig, SSOracleBehavior self)
		{
			if (MagicaOracleBehavior.HandTowardPlayer)
			{
				return true;
			}
			return orig(self);
		}

		private static void CustomPearlColors(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			// Fucked up and evil code if real
			if (rCam.game != null && rCam.game.IsStorySession && rCam.room.abstractRoom.name == "7S_AI")
			{
				if (self.color == new Color(1f, 0.47843137f, 0.007843138f))
				{
					self.color = oracleColor[OracleColor.SRSPearls];
				}
			}
		}

		private static Color CustomGownColors(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
		{
			if (self.owner.IsSRS())
			{
				return Color.Lerp(oracleColor[OracleColor.SRSCloak], oracleColor[OracleColor.SRSCloakDark], Mathf.Lerp(0.4f, 0.75f, f));
			}
			return orig(self, f);
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

				if (!success)
				{
					Plugin.Log(Plugin.LogStates.FailILMatch, nameof(ThirdEye));
				}

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
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
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

				if (!success)
				{
					Plugin.Log(Plugin.LogStates.FailILMatch, nameof(ThirdEyeDrawSprites));
				}

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
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
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

				if (!success)
				{
					Plugin.Log(Plugin.LogStates.FailILMatch, nameof(InitiateCustomSprites));
				}

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldarg_1);
				cursor.Emit(OpCodes.Ldarg_2);
				cursor.EmitDelegate((OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) =>
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
				});
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
		private static void CustomAddToContainer(On.OracleGraphics.orig_AddToContainer orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);

			if (self.IsSRS())
			{
				for (int i = 0; i < 6; i++)
				{
					rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[sLeaser.sprites.Length - (i + 1)]);
					if (i == 2)
					{
						sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].MoveBehindOtherNode(sLeaser.sprites[self.neckSprite]);
					}
					else
					{
						sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].MoveBehindOtherNode(sLeaser.sprites[self.firstHandSprite]);
					}
				}
			}
		}

		private static void CustomDrawSprites(On.OracleGraphics.orig_DrawSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.IsPebbles || self.IsRottedPebbles || self.IsSaintPebbles)
			{
				sLeaser.sprites[self.MoonThirdEyeSprite].MoveBehindOtherNode(sLeaser.sprites[self.EyeSprite(0)]);
				sLeaser.sprites[self.MoonThirdEyeSprite].scale += 0.4f;
				sLeaser.sprites[self.MoonThirdEyeSprite].y = Vector2.Lerp(new Vector2(sLeaser.sprites[self.MoonThirdEyeSprite].x, sLeaser.sprites[self.MoonThirdEyeSprite].y), new Vector2(sLeaser.sprites[self.EyeSprite(0)].x, sLeaser.sprites[self.EyeSprite(0)].y), 0.3f).y;
			}

			if (self.IsSRS())
			{
				sLeaser.sprites[self.neckSprite].scaleY = 0.55f;
				sLeaser.sprites[self.MoonThirdEyeSprite].scale += 0.4f;

				Vector2 neckPos = Vector2.Lerp(self.head.pos, self.oracle.firstChunk.pos, 0.7f);
				float rotatoe = Custom.VecToDeg(Custom.DirVec(Vector2.Lerp(self.owner.firstChunk.lastPos, self.owner.firstChunk.pos, timeStacker), Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker)));

				// Fluffs
				sLeaser.sprites[sLeaser.sprites.Length - 3].x = neckPos.x - camPos.x;
				sLeaser.sprites[sLeaser.sprites.Length - 3].y = neckPos.y - camPos.y;
				sLeaser.sprites[sLeaser.sprites.Length - 3].rotation = rotatoe;

				// Necktie
				sLeaser.sprites[sLeaser.sprites.Length - 4].x = self.oracle.firstChunk.pos.x - camPos.x;
				sLeaser.sprites[sLeaser.sprites.Length - 4].y = self.oracle.firstChunk.pos.y - camPos.y;


				neckPos = Vector2.Lerp(self.head.pos, self.oracle.firstChunk.pos, 0.9f);
				Vector2 fluffPos = Custom.RotateAroundVector(self.oracle.firstChunk.pos, neckPos, rotatoe) - camPos;
				for (int i = 0; i < 2; i++)
				{
					sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].x = fluffPos.x + Custom.PerpendicularVector(fluffPos).x * 5f;
					sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].y = fluffPos.y;
					sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].scaleX = (i == 0 ? -1f : 1f);
					sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].rotation = sLeaser.sprites[self.HeadSprite].rotation + 180f;
				}

				// Rope sprites
				sLeaser.sprites[sLeaser.sprites.Length - 6].x = sLeaser.sprites[sLeaser.sprites.Length - 1].x;
				sLeaser.sprites[sLeaser.sprites.Length - 6].y = sLeaser.sprites[sLeaser.sprites.Length - 1].y;

				sLeaser.sprites[sLeaser.sprites.Length - 5].x = sLeaser.sprites[sLeaser.sprites.Length - 2].x;
				sLeaser.sprites[sLeaser.sprites.Length - 5].y = sLeaser.sprites[sLeaser.sprites.Length - 2].y;
			}

			if (self.IsMoon || self.IsPastMoon || self.IsPebbles || self.IsSaintPebbles || self.IsRottedPebbles || self.IsSRS())
			{
				for (int i = 0; i < 2; i++)
				{
					sLeaser.sprites[self.EyeSprite(i)].scale /= 2.8f;
					sLeaser.sprites[self.EyeSprite(i)].scaleX *= i == 0 ? 1f : -1f;

					if (self.IsPebbles)
					{
						if (self.oracle.oracleBehavior is SSOracleBehavior && (self.oracle.oracleBehavior as SSOracleBehavior).action == SSOracleBehavior.Action.General_Idle)
						{
							sLeaser.sprites[self.EyeSprite(i)].SetElementByName("FPEyeOpen");
						}
						else
						{
							sLeaser.sprites[self.EyeSprite(i)].SetElementByName("FPEye");
						}

						sLeaser.sprites[self.EyeSprite(i)].scaleY *= -1f;
					}

					if (self.eyesOpen < 0.4f)
					{
						sLeaser.sprites[self.EyeSprite(i)].scaleY = 0.2f;
					}
					sLeaser.sprites[self.PhoneSprite(i, 2)].scaleX = (Mathf.Min(0.5f, Mathf.Abs(self.RelativeLookDir(timeStacker).x)) * self.RelativeLookDir(timeStacker).x < 0f ? -1.1f : 1.1f) * (i == 0 ? -1f : 1f);
				}
			}
		}

		private static void CustomApplyPalette(On.OracleGraphics.orig_ApplyPalette orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			if (self.IsPastMoon || self.IsMoon || self.IsPebbles || self.IsRottedPebbles || self.IsSaintPebbles || self.IsSRS())
			{
				Color bodyColor = new();
				Color sigilColor = new();
				Color earPhoneColor = new();
				if (self.IsSRS())
				{
					bodyColor = Custom.hexToColor("BC7C38");
					sigilColor = Custom.hexToColor("FF1800");
					earPhoneColor = Custom.hexToColor("5B4843");
				}

				if (self.IsPastMoon)
				{
					bodyColor = Custom.hexToColor("34859C");
					sigilColor = Custom.hexToColor("AD3D4A");
					earPhoneColor = Custom.hexToColor("5B4843");
				}
				if (self.IsMoon)
				{
					bodyColor = Custom.hexToColor("46747B");
					sigilColor = Custom.hexToColor("955551");
					earPhoneColor = Custom.hexToColor("4C4644");
				}
				if (self.IsMoon && rCam.game.IsStorySession && rCam.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
				{
					bodyColor = Color.Lerp(Custom.hexToColor("46747B"), new(1f, 1f, 1f), 0.2f);
					sigilColor = Color.Lerp(Custom.hexToColor("955551"), new(1f, 1f, 1f), 0.2f);
					earPhoneColor = Color.Lerp(Custom.hexToColor("4C4644"), new(1f, 1f, 1f), 0.2f);
				}

				float rottedAmount = 0.6f;
				if (rCam.game.IsStorySession)
				{
					SlugcatStats.Name[] timeLine = SlugcatStats.SlugcatTimelineOrder().ToArray();

					rottedAmount = Mathf.Lerp(0.2f, 0.75f, (float)timeLine.IndexOf(rCam.game.StoryCharacter) / (float)timeLine.Length);

					if (timeLine.IndexOf(MoreSlugcatsEnums.SlugcatStatsName.Spear) != 0 && timeLine.IndexOf(rCam.game.StoryCharacter) < timeLine.IndexOf(MoreSlugcatsEnums.SlugcatStatsName.Spear))
					{
						rottedAmount = 0f;
					}
				}
				if (self.IsPebbles)
				{
					bodyColor = Color.Lerp(Custom.hexToColor("FF6974"), Custom.hexToColor("FF8891"), rottedAmount);
					sigilColor = Custom.hexToColor("FFA75A");
					earPhoneColor = Color.Lerp(Custom.hexToColor("5D4742"), Custom.hexToColor("6E615E"), rottedAmount);
				}
				if (self.IsRottedPebbles)
				{
					bodyColor = Color.Lerp(Custom.hexToColor("FF8891"), new(1f, 1f, 1f), 0.2f);
					sigilColor = Color.Lerp(Custom.hexToColor("FFA75A"), new(1f, 1f, 1f), 0.2f);
					earPhoneColor = Color.Lerp(Custom.hexToColor("6E615E"), new(1f, 1f, 1f), 0.2f);
				}
				if (self.IsSaintPebbles)
				{
					bodyColor = Custom.hexToColor("D09DA1");
					sigilColor = Custom.hexToColor("DABEA5");
					earPhoneColor = Custom.hexToColor("555150");
					sLeaser.sprites[self.EyeSprite(1)].color = rCam.currentPalette.blackColor;
					sLeaser.sprites[sLeaser.sprites.Length - 1].color = earPhoneColor;
				}

				for (int j = 0; j < self.owner.bodyChunks.Length; j++)
				{
					sLeaser.sprites[self.firstBodyChunkSprite + j].color = bodyColor;
				}
				sLeaser.sprites[self.neckSprite].color = bodyColor;
				sLeaser.sprites[self.HeadSprite].color = bodyColor;
				sLeaser.sprites[self.ChinSprite].color = bodyColor;
				for (int k = 0; k < 2; k++)
				{
					sLeaser.sprites[self.PhoneSprite(k, 0)].color = earPhoneColor;
					sLeaser.sprites[self.PhoneSprite(k, 1)].color = earPhoneColor;
					sLeaser.sprites[self.PhoneSprite(k, 2)].color = earPhoneColor;

					sLeaser.sprites[self.HandSprite(k, 0)].color = bodyColor;
					if (self.gown == null)
					{
						sLeaser.sprites[self.HandSprite(k, 1)].color = bodyColor;
					}
					sLeaser.sprites[self.FootSprite(k, 0)].color = bodyColor;
					sLeaser.sprites[self.FootSprite(k, 1)].color = bodyColor;

					sLeaser.sprites[self.FootSprite(k, 1)].color = bodyColor;
				}

				sLeaser.sprites[self.MoonThirdEyeSprite].color = sigilColor;

				if (self.IsSRS())
				{
					sLeaser.sprites[self.neckSprite].color = oracleColor[OracleColor.SRSCloak];

					Color fluffColor = new(0.612f, 0.498f, 0.471f);
					sLeaser.sprites[sLeaser.sprites.Length - 3].color = fluffColor;
					sLeaser.sprites[sLeaser.sprites.Length - 2].color = fluffColor;
					sLeaser.sprites[sLeaser.sprites.Length - 1].color = fluffColor;

					Color neckColor = new(0.102f, 0.024f, 0.027f);
					sLeaser.sprites[sLeaser.sprites.Length - 6].color = neckColor;
					sLeaser.sprites[sLeaser.sprites.Length - 5].color = neckColor;
					sLeaser.sprites[sLeaser.sprites.Length - 4].color = neckColor;
				}
			}
		}
		private static void CustomGraphicsUpdate(On.OracleGraphics.orig_Update orig, OracleGraphics self)
		{
			orig(self);

			if (self.IsSRS() && self.MeditationPose())
			{
				for (int i = 0; i < 2; i++)
				{
					self.hands[i].vel += (self.oracle.firstChunk.pos + new Vector2(5f, 2f)) * (i == 0 ? -1f : 1f);
					self.feet[i].vel += (self.oracle.firstChunk.pos + new Vector2(3f, 5f)) * (i == 0 ? -1f : 1f);
				}
			}

			if (self.IsSRS() && self.SittingPose())
			{
				for (int i = 0; i < 2; i++)
				{
					self.feet[i].vel += self.feet[i].pos + new Vector2(-3f, 0f);

					if (!(self.oracle.oracleBehavior as SSOracleBehavior).HandTowardsPlayer() && !MagicaOracleBehavior.SRSSeesPurple.SRSSigning)
					{
						self.hands[i].vel += Vector2.ClampMagnitude(self.knees[i, 1] - self.hands[i].pos, 10f) / 3f;
					}

				}

				if (MagicaOracleBehavior.SRSSeesPurple.SRSSigning)
				{
					if (handTimer == 0)
					{
						handPose = Mathf.RoundToInt(UnityEngine.Random.value * 5);
					}
					if (handTimer > handTimerMax)
					{
						handTimer = 0;
						handPose = Mathf.RoundToInt(UnityEngine.Random.value * 5);
					}


					switch (handPose)
					{
						case 0:
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.head.pos, self.oracle.firstChunk.pos, (float)handTimer / (float)handTimerMax) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos + new Vector2(-1f, 0f) - self.hands[1].pos, 10f) / 3f;
							break;

						case 1:
							self.eyesOpen = 0.6f;
							self.hands[0].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos + new Vector2(-1f, -1f) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos, self.hands[0].pos, ((float)handTimer / (float)handTimerMax) / 1.2f) - self.hands[1].pos, 10f) / 3f;
							break;

						case 2:
							self.hands[0].vel += Vector2.ClampMagnitude(self.head.pos + new Vector2(-2f, 1f) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos - self.hands[1].pos, 10f) / 3f;
							break;

						case 3:
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos, self.hands[1].pos, ((float)handTimer / (float)handTimerMax) / 1.5f) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.head.pos, self.head.pos + new Vector2(3f, 2f), ((float)handTimer / (float)handTimerMax)) - self.hands[1].pos, 10f) / 3f;
							break;

						case 4:
							self.eyesOpen = 0.3f;
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos, self.hands[1].pos, 0.3f) - self.hands[0].pos, 10f) / 3f;
							if (self.oracle.oracleBehavior.player != null)
							{
								self.hands[1].vel += Vector2.ClampMagnitude(self.oracle.oracleBehavior.player.mainBodyChunk.pos - self.hands[1].pos, 10f) / 3f;
							}
							break;

						case 5:
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos + new Vector2(-1f, -1f), self.oracle.firstChunk.pos + new Vector2(-1f, -1f), ((float)handTimer / (float)handTimerMax)) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos - self.hands[1].pos, 10f) / 3f;
							break;
					}
					handTimer++;
				}
				else
				{
					handTimer = 0;
				}
			}
		}

		private static Vector2 CustomArmBaseDir(On.Oracle.OracleArm.orig_BaseDir orig, Oracle.OracleArm self, float timeStacker)
		{
			if (self.oracle.ID == MagicaEnums.Oracles.SRS)
			{
				return new(-1f, 0f);
			}

			return orig(self, timeStacker);
		}

		private static Vector2 CustomArmOnFramePos(On.Oracle.OracleArm.orig_OnFramePos orig, Oracle.OracleArm self, float timeStacker)
		{
			if (self.oracle.ID == MagicaEnums.Oracles.SRS)
			{
				return new(1012f, 328f);
			}

			return orig(self, timeStacker);
		}

		private static void CustomArmRestraints(On.Oracle.OracleArm.orig_ctor orig, Oracle.OracleArm self, Oracle oracle)
		{
			orig(self, oracle);

			if (oracle.ID == MagicaEnums.Oracles.SRS)
			{
				self.cornerPositions[0] = oracle.room.MiddleOfTile(19, 31);
				self.cornerPositions[1] = oracle.room.MiddleOfTile(49, 31);
				self.cornerPositions[2] = oracle.room.MiddleOfTile(19, 3);
				self.cornerPositions[3] = oracle.room.MiddleOfTile(49, 3);
			}
		}

		private static void CLOracleBehavior_InitateConversation(On.MoreSlugcats.CLOracleBehavior.orig_InitateConversation orig, CLOracleBehavior self)
		{
			if (WinOrSaveHooks.fpHasSeenMonkAscension)
			{
				self.dialogBox.NewMessage(self.Translate("..."), 60);
				self.dialogBox.NewMessage(self.Translate("...Little... green?"), 60);
				self.dialogBox.NewMessage(self.Translate("...What is... this... energy?"), 80);
				self.dialogBox.NewMessage(self.Translate("...It is... warm..."), 60);
				self.dialogBox.NewMessage(self.Translate("...Thank... you..."), 80);
				return;
			}

			if (!WinOrSaveHooks.CLSeenMoonPearl && moonPearlObj != null)
			{
				WinOrSaveHooks.CLSeenMoonPearl = true;

				self.dialogBox.NewMessage(self.Translate("...What is..."), 60);
				self.dialogBox.NewMessage(self.Translate("..."), 120);
				self.dialogBox.NewMessage(self.Translate("...This sphere..."), 60);
				self.dialogBox.NewMessage(self.Translate("...It is... odd."), 60);
				self.dialogBox.NewMessage(self.Translate("...Why does... Give me..."), 60);
				self.dialogBox.NewMessage(self.Translate("...Lost feeling..."), 90);
				self.dialogBox.NewMessage(self.Translate("...Where... got...?"), 60);
				self.dialogBox.NewMessage(self.Translate("...It is... warm... sad..."), 90);
				self.dialogBox.NewMessage(self.Translate("...No more... lonely..."), 120);
				self.dialogBox.NewMessage(self.Translate("...Feel... loved..."), 60);
				return;
			}

			orig(self);
		}

		private static void CLOracleBehavior_Update(On.MoreSlugcats.CLOracleBehavior.orig_Update orig, CLOracleBehavior self, bool eu)
		{
			if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.CheckForSaintAscension(self.player) && !WinOrSaveHooks.fpHasSeenMonkAscension)
			{
				WinOrSaveHooks.fpHasSeenMonkAscension = true;
				self.InitateConversation();
			}

			if (moonPearlObj == null && !WinOrSaveHooks.CLSeenMoonPearl)
			{
				for (int j = 0; j < self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Count; j++)
				{
					if (self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item is DataPearl checkPearl && checkPearl.AbstractPearl.dataPearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl && Custom.DistLess(checkPearl.firstChunk.pos, self.oracle.firstChunk.pos, 100f))
					{
						moonPearlObj = checkPearl;
						if (self.currentConversation != null)
						{
							self.currentConversation.Destroy();
						}
						self.currentConversation = null;
						self.dialogBox.Interrupt(self.Translate("..."), 40);
						self.InitateConversation();
						break;
					}
				}
			}

			orig(self, eu);

			if (moonPearlObj != null && WinOrSaveHooks.CLSeenMoonPearl && !self.FocusedOnHalcyon)
			{
				Vector2? pearlTargetPos = new Vector2?(self.oracle.bodyChunks[0].pos + new Vector2(10f, 20f));
				float num = Custom.Dist((Vector2)pearlTargetPos, moonPearlObj.firstChunk.pos);

				self.lookPoint = moonPearlObj.firstChunk.pos;

				moonPearlObj.firstChunk.vel *= Custom.LerpMap(moonPearlObj.firstChunk.vel.magnitude, 1f, 6f, 0.999f, 0.9f);
				moonPearlObj.firstChunk.vel += Vector2.ClampMagnitude(pearlTargetPos.Value - moonPearlObj.firstChunk.pos, 100f) / 100f * 0.4f;
				moonPearlObj.gravity = 0f;
			}
			else if (moonPearlObj != null)
			{
				moonPearlObj.gravity = 0.9f;
			}
		}
		private static void SLOracleBehaviorHasMark_GrabObject(On.SLOracleBehaviorHasMark.orig_GrabObject orig, SLOracleBehaviorHasMark self, PhysicalObject item)
		{
			if (self.throwAwayObjects)
			{
				return;
			}

			inspectObjectIsMoonPearl = item is DataPearl pearl && pearl.AbstractPearl.dataPearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl;
			orig(self, item);
		}

		private static void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
		{
			if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.CheckForSaintAscension(self.player) && (Custom.Dist(self.oracle.bodyChunks[0].pos, new Vector2(self.player.mainBodyChunk.pos.x + self.player.burstX, self.player.mainBodyChunk.pos.y + self.player.burstY + 60f)) < 30f || (ModOptions.CustomMechanics.Value && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(self.player, out var saint) && saint.ascendTimer > 0f)) && !hasBeenTouchedBySaintsHalo)
			{
				self.dialogBox.Interrupt("What are you doing...?", 80);
				hasBeenTouchedBySaintsHalo = true;
				if (self.currentConversation != null)
				{
					self.currentConversation.paused = true;
					self.resumeConversationAfterCurrentDialoge = true;
				}
			}
			if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.CheckForSaintAscension(self.player) && !WinOrSaveHooks.lttmHasSeenMonkAscension)
			{
				WinOrSaveHooks.lttmHasSeenMonkAscension = true;
				self.InitateConversation();
			}

			orig(self, eu);
		}

		private static void SLOracleWakeUpProcedure_ctor(On.SLOracleWakeUpProcedure.orig_ctor orig, SLOracleWakeUpProcedure self, Oracle SLOracle)
		{
			orig(self, SLOracle);
			moonRevivedThisCycle = true;
		}

		private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
		{
			if (!WinOrSaveHooks.redEndingProcedure && self.player != null && self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == SlugcatStats.Name.Red && self.oracle.room.game.GetStorySession.RedIsOutOfCycles)
			{
				Plugin.DebugLog("Hunter ending with Pebbles started");
				self.NewAction(MagicaEnums.OracleActions.RedDiesInFP);
				WinOrSaveHooks.redEndingProcedure = true;
				return;
			}

			if (WinOrSaveHooks.redEndingProcedure)
			{
				return;
			}

			orig(self);
		}

		private static void SLOracleBehavior_Update(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
		{
			if (moonHugRed)
			{
				self.dontHoldKnees = 5;
			}

			orig(self, eu);
		}

		private static void SLOracleBehaviorHasMark_SpecialEvent(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
		{
			orig(self, eventName);

			if (eventName == "redhug")
			{
				moonHugRed = true;
			}
			if (eventName == "MoonRewritesSaintPearl")
			{
				if (self.holdingObject != null && self.holdingObject is DataPearl)
				{
					LerpPearlColors(self.holdingObject as DataPearl);
					(self.holdingObject as DataPearl).AbstractPearl.dataPearlType = MagicaEnums.DataPearlIDs.MoonRewrittenPearl;
					for (int i = 0; i < 20; i++)
					{
						self.oracle.room.AddObject(new Spark(self.holdingObject.firstChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
					}
					self.oracle.room.PlaySound(SoundID.Moon_Wake_Up_Swarmer_Ping, 0f, 1f, 1f);
				}
			}
		}

		private static void LerpPearlColors(DataPearl dataPearl)
		{
			DataPearl.AbstractDataPearl.DataPearlType dataPearlType = dataPearl.AbstractPearl.dataPearlType;

			if (dataPearl != null)
			{
				dataPearl.color = DataPearl.UniquePearlMainColor(MagicaEnums.DataPearlIDs.MoonRewrittenPearl);
				dataPearl.highlightColor = DataPearl.UniquePearlHighLightColor(MagicaEnums.DataPearlIDs.MoonRewrittenPearl);
			}
		}

		private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
		{
			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_PearlBleaching && !WinOrSaveHooks.MoonOverWrotePearl && inspectObjectIsMoonPearl)
			{
				self.LoadEventsFromFile(413);
				return;
			}
			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_PearlBleaching && !WinOrSaveHooks.MoonOverWrotePearl)
			{
				WinOrSaveHooks.MoonOverWrotePearl = true;
				self.LoadEventsFromFile(411);
				return;
			}
			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_PearlBleaching && inspectObjectIsMoonPearl)
			{
				self.LoadEventsFromFile(412);
				return;
			}

			orig(self);

			if (self.id == MagicaEnums.ConversationIDs.RedDiesMoon)
			{
				self.LoadEventsFromFile(405);
				return;
			}
			if (self.id == MagicaEnums.ConversationIDs.RedRevivedMoonButDiesTragically)
			{
				self.LoadEventsFromFile(409);
				return;
			}
			if (self.id == MagicaEnums.ConversationIDs.SaintLTTMMonkAscension)
			{
				self.LoadEventsFromFile(410);
				return;
			}
		}

		private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
		{
			if (WinOrSaveHooks.lttmHasSeenMonkAscension)
			{
				if (self.currentConversation != null)
				{
					self.currentConversation.Destroy();
				}
				self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(MagicaEnums.ConversationIDs.SaintLTTMMonkAscension, self, SLOracleBehaviorHasMark.MiscItemType.NA);
				return;
			}

			if (self.State.SpeakingTerms && self.oracle.room.game.StoryCharacter == SlugcatStats.Name.Red && WinOrSaveHooks.redEndingProcedure)
			{
				if (moonRevivedThisCycle)
				{
					self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(MagicaEnums.ConversationIDs.RedRevivedMoonButDiesTragically, self, SLOracleBehaviorHasMark.MiscItemType.NA);
				}
				else
				{
					self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(MagicaEnums.ConversationIDs.RedDiesMoon, self, SLOracleBehaviorHasMark.MiscItemType.NA);
				}
				return;
			}

			orig(self);
		}

		private static void SLOracleBehavior_InitCutsceneObjects(On.SLOracleBehavior.orig_InitCutsceneObjects orig, SLOracleBehavior self)
		{
			orig(self);

			if (self.oracle.room.world.game.GetStorySession != null && self.oracle.room.world.game.GetStorySession.saveState.miscWorldSaveData.moonRevived && !WinOrSaveHooks.redEndingProcedure && self.player != null && self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == SlugcatStats.Name.Red && self.State.neuronsLeft == 5 && self.oracle.room.game.GetStorySession.RedIsOutOfCycles)
			{
				Plugin.DebugLog("Hunter ending with Moon started");
				self.oracle.room.AddObject(new RedDyingEnding(self.oracle));
				WinOrSaveHooks.redEndingProcedure = true;
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

			if (self.ID == Oracle.OracleID.SS && self.room.world.name != "RM")
			{
				for (int i = 0; i < self.room.updateList.Count; i++)
				{
					int halcyonNum = 0;
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

						if (self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear || !ModOptions.CustomInGameCutscenes.Value || string.IsNullOrEmpty(WinOrSaveHooks.WhoShowedFPThePearl) || CheckIfWhoTookThePearlIsBeforeCurrent(self.room.game.StoryCharacter))
						{
							Plugin.DebugLog("Invalid Halcyon found, removing... " + pearl.abstractPhysicalObject.ID);
							if (self.oracleBehavior is SSOracleBehavior behav && behav.readDataPearlOrbits.Contains(pearl.abstractPhysicalObject))
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

			if (!WinOrSaveHooks.CLSeenMoonPearl && self.ID == MoreSlugcatsEnums.OracleID.CL)
			{
				for (int i = 0; i < self.room.updateList.Count; i++)
				{
					if (self.room.updateList[i] is DataPearl moonPearl && moonPearl.AbstractPearl.dataPearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl)
					{
						moonPearl.Destroy();
						self.room.updateList[i].RemoveFromRoom();
					}
				}
			}

			allowFpToCheckHisPearl = true;
		}

		internal static bool CheckIfWhoTookThePearlIsBeforeCurrent(SlugcatStats.Name currentSlug)
		{
			SlugcatStats.Name[] names = SlugcatStats.SlugcatTimelineOrder().ToArray();
			return !string.IsNullOrEmpty(WinOrSaveHooks.WhoShowedFPThePearl) && SlugcatStats.Name.TryParse(typeof(SlugcatStats.Name), WinOrSaveHooks.WhoShowedFPThePearl, true, out var slug) && slug is SlugcatStats.Name whoTookItSlug && names.Contains(whoTookItSlug) && names.Contains(currentSlug) && names.IndexOf(whoTookItSlug) < names.IndexOf(currentSlug) && Plugin.DebugLog(whoTookItSlug.value + " is " + (names.IndexOf(whoTookItSlug) < names.IndexOf(currentSlug) ? "sooner than " : "later than ") + currentSlug.value);
		}

		public static bool IsIntroAction(SSOracleBehavior owner)
		{
			if (owner.action == SSOracleBehavior.Action.General_GiveMark || owner.action == SSOracleBehavior.Action.General_MarkTalk || owner.action == SSOracleBehavior.Action.GetNeuron_GetOutOfStomach || owner.action == SSOracleBehavior.Action.GetNeuron_Init || owner.action == SSOracleBehavior.Action.GetNeuron_InspectNeuron || owner.action == SSOracleBehavior.Action.GetNeuron_TakeNeuron || owner.action == SSOracleBehavior.Action.MeetRed_Init || owner.action == SSOracleBehavior.Action.MeetWhite_Talking || owner.action == SSOracleBehavior.Action.MeetYellow_Init || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_StartDialog) { return true; }
			if (ModManager.MSC && (owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.ThrowOut_Singularity || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetArty_Init) || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetArty_Talking || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_SecondImages || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_StartDialog || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_ThirdCurious || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Rubicon) { return true; }

			return false;
		}

		private static void ItemRecognitions(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
		{
			if (ModOptions.CustomInGameCutscenes.Value && allowFpToCheckHisPearl && self.timeSinceSeenPlayer > 20 && halcyonPearl == null)
			{
				if (self.oracle.ID == Oracle.OracleID.SS && !IsIntroAction(self) && self.conversation == null && self.oracle.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData.theMark && (self.action != MagicaEnums.OracleActions.FPMusicPearlInit || self.action != MagicaEnums.OracleActions.FPMusicPearlPlay || self.action != MagicaEnums.OracleActions.FPMusicPearlStop))
				{
					for (int i = 0; i < self.oracle.room.updateList.Count; i++)
					{
						UpdatableAndDeletable item = self.oracle.room.updateList[i];
						if (item is HalcyonPearl pearl)
						{
							Plugin.DebugLog("Found pearl! : " + pearl.abstractPhysicalObject.ID);
							halcyonPearl = pearl;
							break;
						}
					}
				}

				if (halcyonPearl != null && string.IsNullOrEmpty(WinOrSaveHooks.WhoShowedFPThePearl))
				{
					self.NewAction(MagicaEnums.OracleActions.FPMusicPearlInit);
				}
			}

			if (halcyonPearl != null && !string.IsNullOrEmpty(WinOrSaveHooks.WhoShowedFPThePearl))
			{
				halcyonPearl.firstChunk.HardSetPosition(self.oracle.firstChunk.pos + new Vector2(20f, -9f));
			}

			// Add trigger that checks if the ending has already been played
			if (self.readDataPearlOrbits.Count >= 22)
			{
				Plugin.DebugLog("yuooee");
				//self.NewAction(MagicaEnums.ArtiFPAllPearls);
			}

			orig(self, eu);
		}

		private static void CustomSpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
		{
			orig(self, eventName);

			if (eventName == "music")
			{
				if (self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
				{
					self.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
				}
				else
				{
					self.NewAction(MagicaEnums.OracleActions.FPMusicPearlPlay);
				}
			}

			if (eventName == "endmusic")
			{
				if (self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
				{
					self.NewAction(MagicaEnums.OracleActions.ArtiFPPearlStop);
				}
				else
				{
					self.NewAction(MagicaEnums.OracleActions.FPMusicPearlStop);
				}
			}

			if (eventName == "spearsign")
			{
				GraphicsHooks.spearSigning = true;
				if (self.conversation != null)
				{
					self.conversation.paused = true;
				}
			}

			if (eventName == "hug")
			{
				MagicaOracleBehavior.SRSSeesPurple.hugState = true;
			}
		}

		private static void CustomPebblesConversationEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
		{
			// RM pearl
			if (self.id == MagicaEnums.ConversationIDs.MusicPearlDialogue)
			{
				self.LoadEventsFromFile(402);
			}
			if (self.id == MagicaEnums.ConversationIDs.MusicPearlDialogue2)
			{
				self.LoadEventsFromFile(403);
			}

			// SRS seeing spearmaster
			if (self.id == MagicaEnums.ConversationIDs.SRSMeetsSpear)
			{
				self.LoadEventsFromFile(404);
			}

			// Hunter cycle 1 endings
			if (self.id == MagicaEnums.ConversationIDs.RedDiesPebbles1)
			{
				self.LoadEventsFromFile(406);
			}

			if (self.id == MagicaEnums.ConversationIDs.RedDiesPebbles2)
			{
				self.LoadEventsFromFile(407);
			}

			if (self.id == MagicaEnums.ConversationIDs.GreenNeuronTooLate)
			{
				self.LoadEventsFromFile(408);
			}

			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_HR)
			{
				self.LoadEventsFromFile(414);
				return;
			}

			if (self.id == MoreSlugcatsEnums.ConversationID.Pebbles_HR)
			{
				self.LoadEventsFromFile(415);
				return;
			}

			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_Pebbles_HR)
			{
				self.LoadEventsFromFile(416);
				return;
			}

			orig(self);
		}

		private static void CustomFPActions(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
		{
			if (ListOfCustomActions().Contains(nextAction))
			{
				CustomNewAction(nextAction, self);
			}

			orig(self, nextAction);
		}

		private static void AddPearlRecognition(On.SSOracleBehavior.orig_StartItemConversation orig, SSOracleBehavior self, DataPearl item)
		{
			if ((ModManager.MSC && self.oracle.ID == Oracle.OracleID.SS) && self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && item.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.RM)
			{
				Conversation.ID id = Conversation.DataPearlToConversation(item.AbstractPearl.dataPearlType);

				self.pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(id, self, SLOracleBehaviorHasMark.MiscItemType.NA);
				if (WinOrSaveHooks.WhoShowedFPThePearl == MoreSlugcatsEnums.SlugcatStatsName.Artificer.value)
				{
					self.NewAction(MagicaEnums.OracleActions.ArtiFPPearlInspect);
					Plugin.DebugLog("FP has already seen RM pearl!");
				}
				else
				{
					self.NewAction(MagicaEnums.OracleActions.ArtiFPPearlInit);
				}
			}

			orig(self, item);
		}

		private static void CustomNewAction(SSOracleBehavior.Action nextAction, SSOracleBehavior self)
		{
			if (nextAction == self.action)
			{
				return;
			}

			SSOracleBehavior.SubBehavior.SubBehavID subBehavID = SSOracleBehavior.SubBehavior.SubBehavID.General;

			if (nextAction == MagicaEnums.OracleActions.ArtiFPPearlInit || nextAction == MagicaEnums.OracleActions.ArtiFPPearlInspect || nextAction == MagicaEnums.OracleActions.ArtiFPPearlPlay || nextAction == MagicaEnums.OracleActions.ArtiFPPearlStop || nextAction == MagicaEnums.OracleActions.ArtiFPPearlGrabbed)
			{
				subBehavID = MagicaEnums.OracleActions.MusicPearlInspect;
			}
			if (nextAction == MagicaEnums.OracleActions.FPMusicPearlInit || nextAction == MagicaEnums.OracleActions.FPMusicPearlPlay || nextAction == MagicaEnums.OracleActions.FPMusicPearlStop)
			{
				subBehavID = MagicaEnums.OracleActions.MusicPearlInspect2;
			}
			if (nextAction == MagicaEnums.OracleActions.ArtiFPAllPearls)
			{
				subBehavID = MagicaEnums.OracleActions.ArtiFPEnding;
			}
			if (nextAction == MagicaEnums.OracleActions.SRSSeesPurple)
			{
				subBehavID = MagicaEnums.OracleActions.SRSPurple;
			}
			if (nextAction == MagicaEnums.OracleActions.RedDiesInFP)
			{
				subBehavID = MagicaEnums.OracleActions.RedDies;
			}
			if (subBehavID != SSOracleBehavior.SubBehavior.SubBehavID.General && subBehavID != self.currSubBehavior.ID)
			{
				SSOracleBehavior.SubBehavior subBehavior = null;
				for (int i = 0; i < self.allSubBehaviors.Count; i++)
				{
					if (self.allSubBehaviors[i].ID == subBehavID)
					{
						subBehavior = self.allSubBehaviors[i];
						break;
					}
				}
				if (subBehavior == null)
				{
					self.LockShortcuts();
					if (subBehavID == MagicaEnums.OracleActions.MusicPearlInspect)
					{
						subBehavior = new SSFPReadsMusicPearl(self);
					}
					if (subBehavID == MagicaEnums.OracleActions.MusicPearlInspect2)
					{
						subBehavior = new SSFPReadsMusicPearlNonArti(self);
					}
					if (subBehavID == MagicaEnums.OracleActions.ArtiFPEnding)
					{
						subBehavior = new SSFPArtiEndingTrigger(self);
					}
					if (subBehavID == MagicaEnums.OracleActions.SRSPurple)
					{
						subBehavior = new MagicaOracleBehavior.SRSSeesPurple(self);
					}
					if (subBehavID == MagicaEnums.OracleActions.RedDies)
					{
						subBehavior = new SSReactsToRedDying(self);
					}
					self.allSubBehaviors.Add(subBehavior);
				}
				subBehavior.Activate(self.action, nextAction);
				self.currSubBehavior.Deactivate();
				self.currSubBehavior = subBehavior;
			}
			self.inActionCounter = 0;
			self.action = nextAction;
		}

		public class SSFPReadsMusicPearl : SSOracleBehavior.ConversationBehavior
		{
			public OracleChatLabel chatLabel;
			public HalcyonPearl halcyon;
			private int numGrabbedHalcyon;
			private bool finishedFlag;
			private bool startedConverstation;
			public new Player player;
			private float playerAng;

			public Vector2 GrabPos
			{
				get
				{
					if (oracle.graphicsModule == null)
					{
						return oracle.firstChunk.pos;
					}
					return (oracle.graphicsModule as OracleGraphics).hands[1].pos;
				}
			}

			public SSFPReadsMusicPearl(SSOracleBehavior owner) : base(owner, MagicaEnums.OracleActions.MusicPearlInspect, Conversation.ID.None)
			{
				halcyon = null;

				startedConverstation = false;
			}

			public override void Update()
			{
				base.Update();

				if (finishedFlag)
				{
					return;
				}

				if (halcyon == null)
				{
					for (int i = 0; i < oracle.room.updateList.Count; i++)
					{
						if (oracle.room.updateList[i] != null && oracle.room.updateList[i] is HalcyonPearl pearl)
						{
							halcyon = pearl;
							break;
						}
					}
				}

				if (owner.action == MagicaEnums.OracleActions.ArtiFPPearlInit)
				{
					owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

					if (!startedConverstation)
					{
						owner.dialogBox.Interrupt(Translate("... Hm? What is this, little ruffian?"), 50);
						owner.dialogBox.NewMessage(Translate("Let's see whats on it."), 40);
						owner.dialogBox.NewMessage(Translate("..."), 60);
						startedConverstation = true;
					}

					if (startedConverstation)
					{
						if (owner.dialogBox.messages.Count == 0)
						{
							owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlInspect);
						}
					}
				}

				if (owner.action == MagicaEnums.OracleActions.ArtiFPPearlInspect)
				{
					numGrabbedHalcyon = 0;
					owner.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
					owner.lookPoint = GrabPos;

					if (halcyon != null)
					{
						Vector2 vector = GrabPos - halcyon.firstChunk.pos;
						float num = Custom.Dist(GrabPos, halcyon.firstChunk.pos);
						if (num < 30f)
						{
							halcyon.firstChunk.vel += Vector2.ClampMagnitude(vector, 2f) / 20f * Mathf.Clamp(16f - num / 100f * 16f, 4f, 16f);
							if (halcyon.firstChunk.vel.magnitude < 1f || num < 8f)
							{
								halcyon.firstChunk.vel = Vector2.zero;
								halcyon.firstChunk.HardSetPosition(GrabPos);
							}
						}
					}

					if (owner.nextPos != oracle.room.MiddleOfTile(24, 17))
					{
						owner.SetNewDestination(oracle.room.MiddleOfTile(24, 17));
					}
				}

				if (owner.action == MagicaEnums.OracleActions.ArtiFPPearlPlay)
				{
					owner.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
					oracle.marbleOrbiting = true;

					if (halcyon != null)
					{
						halcyon.hoverPos = new Vector2?(oracle.firstChunk.pos + new Vector2(40f, 20f));
						owner.lookPoint = halcyon.firstChunk.pos;
						halcyon.firstChunk.vel.y = Mathf.Sin(4f);
					}

					if (oracle.graphicsModule != null && oracle.graphicsModule is OracleGraphics oracleGraphics && halcyon != null)
					{
						oracleGraphics.hands[1].vel += Custom.DirVec(oracleGraphics.hands[1].pos, halcyon.firstChunk.pos) * 3f;
					}

					if (halcyon != null)
					{
						if (inActionCounter < 1500 && halcyon.Carried)
						{
							owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlGrabbed);
							numGrabbedHalcyon++;
							return;
						}

						if (inActionCounter > 1500 && !halcyon.Carried)
						{
							owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

							if (owner.conversation == null)
							{
								owner.InitateConversation(MagicaEnums.ConversationIDs.MusicPearlDialogue, this);
							}
						}

						if (numGrabbedHalcyon >= 3)
						{
							if (player == null)
							{
								player = (oracle.room.game.session.Players[0].realizedCreature as Player);
								playerAng = Custom.Angle(player.firstChunk.pos, new Vector2(oracle.room.PixelWidth / 2f, oracle.room.PixelHeight / 2f));
							}

							playerAng += 0.4f;
							Vector2 b = new Vector2(oracle.room.PixelWidth / 2f, oracle.room.PixelHeight / 2f) + Custom.DegToVec(playerAng) * 140f;
							player.firstChunk.pos = Vector2.Lerp(player.firstChunk.pos, b, 0.16f);
						}
					}
				}

				if (owner.action == MagicaEnums.OracleActions.ArtiFPPearlGrabbed)
				{
					owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

					if (owner.dialogBox.messages.Count == 0)
					{
						startedConverstation = false;
					}

					switch (numGrabbedHalcyon)
					{
						case 1:
							if (!startedConverstation && inActionCounter < 20)
							{
								Plugin.DebugLog("Grabbed FP's Music pearl, increment: " + numGrabbedHalcyon);
								startedConverstation = true;
								owner.dialogBox.Interrupt(Translate("Ah, do you not like the sound?"), 50);
							}
							if (halcyon != null && !halcyon.Carried)
							{
								owner.dialogBox.Interrupt(Translate("Thank you, now where were we..."), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
								return;
							}

							if (inActionCounter == 500)
							{
								owner.dialogBox.NewMessage(Translate("I suppose you won't be giving that back."), 50);
							}

							if (inActionCounter == 1000)
							{
								owner.dialogBox.NewMessage(Translate("If you intend on wasting my time, this is not the place for it little beast. I will be getting back to my work now."), 50);
								if (owner.conversation != null)
								{
									owner.conversation.slatedForDeletion = true;
								}
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlStop);
							}
							break;

						case 2:
							if (!startedConverstation && inActionCounter < 20)
							{
								Plugin.DebugLog("Grabbed FP's Music pearl, increment: " + numGrabbedHalcyon);
								startedConverstation = true;
								owner.dialogBox.Interrupt(Translate("... What are you doing?"), 50);
							}
							if (halcyon != null && !halcyon.Carried)
							{
								owner.dialogBox.Interrupt(Translate("Thank you, now where were we..."), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
								return;
							}

							if (inActionCounter == 500)
							{
								owner.dialogBox.NewMessage(Translate("I suppose you won't be giving that back."), 50);
							}

							if (inActionCounter == 1000)
							{
								owner.dialogBox.NewMessage(Translate("If you intend on wasting my time, this is not the place for it little beast. I will be getting back to my work now."), 50);
								if (owner.conversation != null)
								{
									owner.conversation.slatedForDeletion = true;
								}
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlStop);
							}
							break;

						case 3:
							if (!startedConverstation && inActionCounter < 20)
							{
								Plugin.DebugLog("Grabbed FP's Music pearl, increment: " + numGrabbedHalcyon);
								startedConverstation = true;
								owner.dialogBox.Interrupt(Translate("I'm growing tired of your shenanijans, little ruffian."), 50);
							}
							if (halcyon != null && !halcyon.Carried)
							{
								owner.dialogBox.Interrupt(Translate("..."), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
								return;
							}

							if (inActionCounter == 500)
							{
								owner.dialogBox.NewMessage(Translate("I've had enough, get out."), 50);
								owner.dialogBox.NewMessage(Translate("OUT."), 50);
								if (owner.conversation != null)
								{
									owner.conversation.slatedForDeletion = true;
								}
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlStop);
							}
							break;

						case 4:
							if (!startedConverstation && inActionCounter < 20)
							{
								Plugin.DebugLog("Grabbed FP's Music pearl, increment: " + numGrabbedHalcyon);
								startedConverstation = true;
								owner.dialogBox.Interrupt(Translate("That's it, enough with your antics. If you cannot spare me the patience of allowing me some peace, then GO."), 50);
								owner.dialogBox.NewMessage(Translate("OUT."), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlStop);
							}
							break;
					}
				}


				if (owner.action == MagicaEnums.OracleActions.ArtiFPPearlStop)
				{
					owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
					Deactivate();
				}

				// End

			}

			public override void Deactivate()
			{
				WinOrSaveHooks.WhoShowedFPThePearl = MoreSlugcatsEnums.SlugcatStatsName.Artificer.value;
				owner.UnlockShortcuts();
				oracle.marbleOrbiting = false;
				finishedFlag = true;
				if (halcyon != null)
				{
					halcyon.hoverPos = null;
				}
			}

		}

		public class SSFPReadsMusicPearlNonArti : SSOracleBehavior.ConversationBehavior
		{
			public HalcyonPearl halcyon;
			private bool startedConverstation;
			private bool grabbedRM;

			public Vector2 GrabPos
			{
				get
				{
					if (oracle.graphicsModule == null)
					{
						return oracle.firstChunk.pos;
					}
					return (oracle.graphicsModule as OracleGraphics).hands[0].pos;
				}
			}

			public SSFPReadsMusicPearlNonArti(SSOracleBehavior owner) : base(owner, MagicaEnums.OracleActions.MusicPearlInspect2, Conversation.ID.None)
			{
				halcyon = null;

				if (halcyon == null)
				{
					for (int i = 0; i < oracle.room.physicalObjects.Length; i++)
					{
						for (int j = 0; j < oracle.room.physicalObjects[i].Count; j++)
						{
							if (oracle.room.physicalObjects[i][j] is HalcyonPearl)
							{
								halcyon = (oracle.room.physicalObjects[i][j] as HalcyonPearl);
								break;
							}
						}
					}
				}

				startedConverstation = false;
			}

			public override void Update()
			{
				base.Update();

				if (owner.action == MagicaEnums.OracleActions.FPMusicPearlInit)
				{
					if (inActionCounter > 40)
					{
						owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

						if (!startedConverstation)
						{
							owner.dialogBox.Interrupt(Translate("... What do you have there?"), 10);
							startedConverstation = true;
						}
					}

					if (inActionCounter == 100)
					{
						if (halcyon != null && halcyon.Carried && player != null)
						{
							for (int k = 0; k < player.grasps.Length; k++)
							{
								if (player.grasps[k] != null && player.grasps[k].grabbed is HalcyonPearl)
								{
									player.ReleaseGrasp(k);
									break;
								}
							}
						}
					}

					if (inActionCounter > 100)
					{
						if (halcyon != null)
						{
							owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
							owner.lookPoint = halcyon.firstChunk.pos;

							Vector2 vector = GrabPos - halcyon.firstChunk.pos;
							float num = Custom.Dist(GrabPos, halcyon.firstChunk.pos);

							if (!grabbedRM)
							{
								if (halcyon.firstChunk.vel.y < 2 && owner.getToWorking == 0f)
								{
									halcyon.firstChunk.vel.y += 1f;
								}

								halcyon.firstChunk.vel += Vector2.ClampMagnitude(vector, 40f) / 40f * Mathf.Clamp(2f - num / 200f * 2f, 0.5f, 2f);
								if (num < 15f)
								{
									owner.getToWorking = 0f;
									halcyon.firstChunk.vel = Vector2.zero;
									halcyon.firstChunk.HardSetPosition(GrabPos);
									grabbedRM = true;
									return;
								}
								if (halcyon.firstChunk.vel.magnitude > 8f)
								{
									halcyon.firstChunk.vel /= 2f;
								}
							}
							else
							{
								halcyon.firstChunk.vel = Vector2.zero;
								halcyon.firstChunk.HardSetPosition(GrabPos);
							}

							if (num > 64f)
							{
								if (oracle.graphicsModule != null)
								{
									OracleGraphics oracleGraphics = (oracle.graphicsModule as OracleGraphics);
									oracleGraphics.hands[1].vel += Custom.DirVec(oracleGraphics.hands[1].pos, halcyon.firstChunk.pos) * 3f;
								}
							}
						}

						if (grabbedRM)
						{
							if (owner.nextPos != oracle.room.MiddleOfTile(24, 17))
							{
								owner.SetNewDestination(oracle.room.MiddleOfTile(24, 17));
							}

							if (owner.conversation == null)
							{
								owner.InitateConversation(MagicaEnums.ConversationIDs.MusicPearlDialogue2, this);
							}
						}
					}
				}

				if (owner.action == MagicaEnums.OracleActions.FPMusicPearlPlay)
				{
					if (inActionCounter < 1700)
					{
						owner.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;

						if (halcyon != null)
						{
							halcyon.hoverPos = new Vector2?(oracle.firstChunk.pos + new Vector2(40f, 20f));
						}

						if (oracle.graphicsModule != null)
						{
							OracleGraphics oracleGraphics = (oracle.graphicsModule as OracleGraphics);
							oracleGraphics.hands[1].vel += Custom.DirVec(oracleGraphics.hands[1].pos, halcyon.firstChunk.pos) * 3f;
						}

						if (inActionCounter == 1000)
						{
							owner.dialogBox.NewMessage(Translate("Hm..."), 50);
						}
					}

					if (inActionCounter == 1600)
					{
						owner.dialogBox.NewMessage(Translate("..."), 50);
						owner.dialogBox.NewMessage(Translate("I think that is enough for now, I have a problem to focus on."), 50);
						owner.dialogBox.NewMessage(Translate("I suppose I will say thanks. But don't get any ideas in bringing me more things, beast."), 50);
					}

					if (inActionCounter > 1700)
					{
						owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

						if (owner.dialogBox.messages.Count == 0)
						{
							owner.NewAction(MagicaEnums.OracleActions.FPMusicPearlStop);
						}
					}
				}

				if (owner.action == MagicaEnums.OracleActions.FPMusicPearlStop)
				{
					if (halcyon != null)
					{
						halcyon.hoverPos = null;
					}

					Deactivate();
					owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
				}

				// End
			}

			public override void Deactivate()
			{
				WinOrSaveHooks.WhoShowedFPThePearl = oracle.room.game.StoryCharacter.value;
				owner.getToWorking = 1f;
			}
		}

		public class SSReactsToRedDying : SSOracleBehavior.ConversationBehavior
		{
			private RedPhases phase;
			private int circleTimer;
			public Player foundPlayer;
			private ProjectionCircle projectionCircle;
			private NSHSwarmer greenNeuron;
			private bool dialogueSet;
			private bool holdingNeuron;
			private FadeOut fadeOut;

			public Vector2 GrabPos
			{
				get
				{
					if (oracle.graphicsModule == null)
					{
						return oracle.firstChunk.pos;
					}
					return (oracle.graphicsModule as OracleGraphics).hands[1].pos;
				}
			}

			public Vector2 holdPlayerPos
			{
				get
				{
					return new Vector2(488f, 350f + Mathf.Sin((float)inActionCounter / 70f * 3.1415927f * 2f) * 4f);
				}
			}

			public SSReactsToRedDying(SSOracleBehavior self) : base(self, MagicaEnums.OracleActions.RedDies, self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.moonRevived ? MagicaEnums.ConversationIDs.RedDiesPebbles1 : MagicaEnums.ConversationIDs.RedDiesPebbles2)
			{
				WinOrSaveHooks.HunterOracleID = oracle.ID.value;
				Plugin.DebugLog("Set hunter oracleID to: " + oracle.ID.value);
				phase = RedPhases.WAIT;
			}

			public override void Update()
			{
				base.Update();

				if (foundPlayer == null)
				{
					if (oracle.room.game.Players.Count > 0 && oracle.room.game.Players[0].realizedCreature != null && oracle.room.game.Players[0].realizedCreature.room == oracle.room)
					{
						foundPlayer = (oracle.room.game.Players[0].realizedCreature as Player);
					}
				}

				if (phase == RedPhases.WAIT)
				{
					owner.LockShortcuts();

					if (oracle.oracleBehavior is SSOracleBehavior oracleBehavior)
					{
						movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

						if (inActionCounter > 80 && inActionCounter < 150)
						{
							if (!ModManager.CoopAvailable)
							{
								foundPlayer.mainBodyChunk.vel += Custom.DirVec(foundPlayer.mainBodyChunk.pos, oracle.room.MiddleOfTile(28, 32)) * 0.6f * (1f - oracle.room.gravity);
								if (oracle.room.GetTilePosition(foundPlayer.mainBodyChunk.pos) == new IntVector2(28, 32) && foundPlayer.enteringShortCut == null)
								{
									foundPlayer.enteringShortCut = new IntVector2?(oracle.room.ShortcutLeadingToNode(1).StartTile);
									return;
								}
								return;
							}
							else
							{
								using (List<Player>.Enumerator enumerator2 = oracle.oracleBehavior.PlayersInRoom.GetEnumerator())
								{
									while (enumerator2.MoveNext())
									{
										Player player = enumerator2.Current;
										player.mainBodyChunk.vel += Custom.DirVec(foundPlayer.mainBodyChunk.pos, oracle.room.MiddleOfTile(28, 32)) * 0.6f * (1f - oracle.room.gravity);
										if (oracle.room.GetTilePosition(player.mainBodyChunk.pos) == new IntVector2(28, 32) && foundPlayer.enteringShortCut == null)
										{
											foundPlayer.enteringShortCut = new IntVector2?(oracle.room.ShortcutLeadingToNode(1).StartTile);
										}
									}
									return;
								}
							}
						}

						if (inActionCounter == 50)
						{
							dialogBox.Interrupt(Translate("I do not have time for you right now little creature."), 0);
						}

						if (inActionCounter > 130)
						{
							foundPlayer.standing = false;
							foundPlayer.Stun(10);
							if (foundPlayer.objectInStomach != null)
							{
								foundPlayer.Regurgitate();
							}
						}
						if (inActionCounter == 150)
						{
							dialogBox.Interrupt("...", 30);
						}

						if (inActionCounter > 250 && foundPlayer.objectInStomach == null)
						{

							for (int i = 0; i < oracle.room.physicalObjects.Length; i++)
							{
								for (int j = 0; j < oracle.room.physicalObjects[i].Count; j++)
								{
									if (oracle.room.physicalObjects[i][j].abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
									{
										greenNeuron = oracle.room.physicalObjects[i][j] as NSHSwarmer;
										break;
									}
								}

								if (greenNeuron != null)
								{
									break;
								}
							}

							if (greenNeuron != null)
							{
								greenNeuron.firstChunk.vel = Vector2.zero;
							}

							phase = RedPhases.START;
							return;
						}
					}
				}

				if (greenNeuron != null && holdingNeuron)
				{
					if (greenNeuron.graphicsModule != null)
					{
						greenNeuron.firstChunk.MoveFromOutsideMyUpdate(true, GrabPos);
					}
					else
					{
						greenNeuron.firstChunk.MoveFromOutsideMyUpdate(false, GrabPos);
					}
					greenNeuron.firstChunk.vel *= 0f;
					greenNeuron.direction = Custom.PerpendicularVector(oracle.firstChunk.pos, greenNeuron.firstChunk.pos);
				}

				if (phase == RedPhases.START)
				{
					if (oracle.oracleBehavior is SSOracleBehavior oracleBehavior)
					{
						oracleBehavior.getToWorking = 0f;
						oracleBehavior.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

						if (greenNeuron != null && !holdingNeuron)
						{
							greenNeuron.storyFly = true;
							greenNeuron.storyFlyTarget = GrabPos;
							if (Custom.DistLess(GrabPos, greenNeuron.firstChunk.pos, 10f))
							{
								holdingNeuron = true;
								greenNeuron.storyFly = false;
							}
						}

						if (dialogueSet && oracleBehavior.dialogBox.messages.Count == 0)
						{
							phase = RedPhases.TALK;
							return;
						}

						if (oracleBehavior.dialogBox.messages.Count == 0 && !dialogueSet)
						{
							dialogueSet = true;

							if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap)
							{
								oracleBehavior.dialogBox.NewMessage(Translate("What brings you back? You've wasted the extra time I've provided you."), 30);
							}
							else
							{
								oracleBehavior.dialogBox.NewMessage(Translate("Why do you come here in such a terrible state? Do you believe I have something to provide you?"), 30);
							}

							if (greenNeuron != null)
							{
								if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron)
								{
									oracleBehavior.dialogBox.NewMessage(Translate("And you've brought back that which you were meant to deliver to Moon."), 20);
								}
								else
								{
									oracleBehavior.dialogBox.NewMessage(Translate("And it seems you've brought something along with you that is important to your destination."), 20);
									oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron = true;
								}
							}
						}

						if (owner.nextPos != oracle.room.MiddleOfTile(10, 22))
						{
							owner.SetNewDestination(oracle.room.MiddleOfTile(10, 22));
						}

						if (foundPlayer != null)
						{
							foundPlayer.mainBodyChunk.vel *= Custom.LerpMap((float)inActionCounter, 0f, 30f, 1f, 0.95f);
							foundPlayer.bodyChunks[1].vel *= Custom.LerpMap((float)inActionCounter, 0f, 30f, 1f, 0.95f);
							foundPlayer.mainBodyChunk.vel += Custom.DirVec(foundPlayer.mainBodyChunk.pos, holdPlayerPos) * Mathf.Lerp(0.5f, Custom.LerpMap(Vector2.Distance(foundPlayer.mainBodyChunk.pos, holdPlayerPos), 30f, 150f, 2.5f, 7f), oracle.room.gravity) * Mathf.InverseLerp(0f, 10f, (float)inActionCounter) * Mathf.InverseLerp(0f, 30f, Vector2.Distance(player.mainBodyChunk.pos, holdPlayerPos));

							if (projectionCircle == null)
							{
								projectionCircle = new ProjectionCircle(foundPlayer.bodyChunks[0].pos, 0f, 3f);
								oracle.room.AddObject(projectionCircle);
							}
							else
							{
								float radiusSize = Mathf.Lerp(0f, 1f, ((float)inActionCounter - 300f) / 150f);
								projectionCircle.radius = 12f * Mathf.Clamp(radiusSize * 2f, 0f, 1f);
								projectionCircle.pos = foundPlayer.bodyChunks[0].pos;
							}

							if (UnityEngine.Random.value > 0.9f)
							{
								foundPlayer.Stun((int)(UnityEngine.Random.value * 5f));
							}
						}
					}
				}

				if (phase == RedPhases.TALK)
				{

					if (projectionCircle != null)
					{
						projectionCircle.pos = foundPlayer.bodyChunks[0].pos;
					}

					if (owner.pathProgression == 1f && owner.conversation == null)
					{
						if (greenNeuron != null && !oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron)
						{
							owner.InitateConversation(MagicaEnums.ConversationIDs.GreenNeuronTooLate, this);
						}
						else
						{
							owner.InitateConversation(convoID, this);
						}
					}

					if (owner.conversation != null && owner.conversation.slatedForDeletion)
					{
						if (greenNeuron != null && holdingNeuron)
						{
							holdingNeuron = false;
						}
						if (projectionCircle != null)
						{
							projectionCircle.Destroy();
							projectionCircle = null;
						}

						OracleHooks.moonHugRed = true;
						phase = RedPhases.END;
						circleTimer = inActionCounter;
					}

					if (foundPlayer != null)
					{
						foundPlayer.mainBodyChunk.vel *= Custom.LerpMap((float)inActionCounter, 0f, 30f, 1f, 0.95f);
						foundPlayer.bodyChunks[1].vel *= Custom.LerpMap((float)inActionCounter, 0f, 30f, 1f, 0.95f);
						foundPlayer.mainBodyChunk.vel += Custom.DirVec(foundPlayer.mainBodyChunk.pos, holdPlayerPos) * Mathf.Lerp(0.5f, Custom.LerpMap(Vector2.Distance(foundPlayer.mainBodyChunk.pos, holdPlayerPos), 30f, 150f, 2.5f, 7f), oracle.room.gravity) * Mathf.InverseLerp(0f, 10f, (float)inActionCounter) * Mathf.InverseLerp(0f, 30f, Vector2.Distance(player.mainBodyChunk.pos, holdPlayerPos));

						if (UnityEngine.Random.value > 0.95f)
						{
							foundPlayer.aerobicLevel = 0.2f;
							foundPlayer.Stun((int)(UnityEngine.Random.value * 10f));
						}
						if (foundPlayer.FoodInStomach > 0)
						{
							foundPlayer.SubtractFood(foundPlayer.FoodInStomach);
						}
					}
				}

				if (phase == RedPhases.END)
				{
					if (owner.nextPos != oracle.room.MiddleOfTile(20, 10))
					{
						owner.SetNewDestination(oracle.room.MiddleOfTile(20, 10));
					}

					if (greenNeuron != null && !holdingNeuron)
					{
						if (Custom.DistLess(foundPlayer.firstChunk.pos, greenNeuron.firstChunk.pos, 20f))
						{
							greenNeuron.storyFly = false;
						}
						else
						{
							greenNeuron.storyFly = true;
							greenNeuron.storyFlyTarget = foundPlayer.firstChunk.pos;
						}
					}

					if (projectionCircle != null)
					{
						float radiusSize = Mathf.Lerp(1f, 0f, ((float)inActionCounter - 300f) / circleTimer);
						projectionCircle.radius = 12f * Mathf.Clamp(radiusSize * 2f, 0f, 1f);
						projectionCircle.pos = foundPlayer.bodyChunks[0].pos;

						if (projectionCircle.radius <= 0f)
						{
							projectionCircle.Destroy();
							projectionCircle = null;
						}
					}

					if (foundPlayer != null)
					{
						foundPlayer.firstChunk.vel += new Vector2(0.0005f, 0f);
						foundPlayer.Stun(10);
						foundPlayer.standing = false;
					}

					if (inActionCounter == circleTimer + 50f)
					{
						if (fadeOut == null)
						{
							fadeOut = new FadeOut(oracle.room, Color.black, 300f, false);
							oracle.room.AddObject(fadeOut);
						}
					}

					if (fadeOut != null && fadeOut.fade == 1f)
					{
						foundPlayer.abstractCreature.world.game.GameOver(null);
						OracleHooks.moonHugRed = false;
					}
				}
			}

			public enum RedPhases
			{
				WAIT,
				START,
				TALK,
				END
			}
		}

		public class RedDyingEnding : UpdatableAndDeletable
		{
			private Oracle oracle;
			private RedPhases phase;
			private int timer;
			public Player foundPlayer;
			private FadeOut fadeOut;


			public RedDyingEnding(Oracle oracle)
			{
				RainWorld.lockGameTimer = true;
				this.oracle = oracle;
				phase = RedPhases.WAIT;

				WinOrSaveHooks.HunterOracleID = oracle.ID.value;
				Plugin.DebugLog("Set hunter oracleID to: " + oracle.ID.value);
			}

			public override void Update(bool eu)
			{
				base.Update(eu);

				timer++;

				if (foundPlayer == null)
				{
					if (room.game.Players.Count > 0 && room.game.Players[0].realizedCreature != null && room.game.Players[0].realizedCreature.room == room)
					{
						foundPlayer = (room.game.Players[0].realizedCreature as Player);
					}
				}

				if (phase == RedPhases.WAIT && oracle.oracleBehavior.player.room == oracle.room)
				{
					if (oracle != null && oracle.ID == Oracle.OracleID.SL && oracle.Consious)
					{
						if (foundPlayer.firstChunk.pos.x > 800f)
						{
							foundPlayer.controller = new RedEndingController(this);
							timer = 0;
						}
						if (foundPlayer.firstChunk.pos.x > 1195 || (timer > 600 && oracle.oracleBehavior is SLOracleBehaviorHasMark moon && moon.currentConversation != null))
						{
							phase = RedPhases.START;
							timer = 0;
						}
					}
				}

				if (phase == RedPhases.START)
				{
					if (oracle != null && oracle.ID == Oracle.OracleID.SL && oracle.Consious)
					{
						if (timer == 30)
						{
							foundPlayer.standing = false;
							foundPlayer.aerobicLevel = 0;
							foundPlayer.Stun(50);

							if (foundPlayer.FoodInStomach > 0)
							{
								foundPlayer.SubtractFood(foundPlayer.FoodInStomach);
							}
						}

						if (foundPlayer.firstChunk.pos.x >= 1511f)
						{
							foundPlayer.standing = false;
							foundPlayer.Stun(500);
						}

						if (timer > 1600)
						{
							if (fadeOut == null && oracle.room != null)
							{
								fadeOut = new FadeOut(oracle.room, Color.black, 850f, false);
								oracle.room.AddObject(fadeOut);
							}

							if (fadeOut != null && fadeOut.fade == 1f)
							{
								phase = RedPhases.END;
							}
						}
					}
				}

				if (phase == RedPhases.END)
				{
					if ((oracle.oracleBehavior is SLOracleBehaviorHasMark moon && moon.currentConversation == null))
					{
						foundPlayer.abstractCreature.world.game.GameOver(null);
						fadeOut.Destroy();
						OracleHooks.moonHugRed = false;
						Destroy();
					}
				}
			}

			public enum RedPhases
			{
				WAIT,
				START,
				END
			}

			internal Player.InputPackage GetInput()
			{
				if (foundPlayer != null)
				{
					if (phase == RedPhases.WAIT)
					{
						if (oracle.ID == Oracle.OracleID.SL)
						{
							if (foundPlayer.firstChunk.pos.x < 1196f)
							{
								return new Player.InputPackage(true, null, 1, 1, false, false, false, false, false);
							}
							if (foundPlayer.firstChunk.pos.x > 1195f)
							{
								return new Player.InputPackage(true, null, 0, -1, false, false, false, false, false);
							}
						}
					}

					if (phase == RedPhases.START)
					{
						if (oracle.ID == Oracle.OracleID.SL)
						{
							if (timer > 120 && foundPlayer.firstChunk.pos.x < 1515f)
							{
								return new Player.InputPackage(true, null, 1, 1, false, false, false, false, false);
							}
							else
							{
								return new Player.InputPackage(true, null, 0, -1, false, false, false, false, false);
							}
						}
					}
				}
				return default;
			}
		}

		internal class RedEndingController : Player.PlayerController
		{
			private RedDyingEnding owner;

			public RedEndingController(RedDyingEnding owner)
			{
				this.owner = owner;
			}

			public override Player.InputPackage GetInput()
			{
				return owner.GetInput();
			}
		}

		internal class SSFPArtiEndingTrigger : SSOracleBehavior.ConversationBehavior
		{
			public SSFPArtiEndingTrigger(SSOracleBehavior owner) : base(owner, MagicaEnums.OracleActions.ArtiFPEnding, Conversation.ID.None)
			{

			}

			public override void Update()
			{
				base.Update();
			}
		}
	}


	public static class MagicaOracleGraphics
	{
		public static bool IsSRS(this OracleGraphics oracle)
		{
			return oracle.oracle.ID == MagicaEnums.Oracles.SRS;
		}

		public static bool MeditationPose(this OracleGraphics oracle)
		{
			if (oracle.oracle.oracleBehavior != null && oracle.oracle.oracleBehavior is MagicaOracleBehavior behavior && behavior.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate && behavior.pathProgression == 1f)
			{
				return true;
			}
			return false;
		}

		public static bool SittingPose(this OracleGraphics oracle)
		{
			if (oracle.oracle.oracleBehavior != null && oracle.oracle.oracleBehavior is MagicaOracleBehavior behavior && behavior.movementBehavior == MagicaEnums.OracleMovementIDs.SitAndFocus && behavior.pathProgression == 1f)
			{
				return true;
			}
			return false;
		}
	}

	internal class MagicaOracleBehavior : SSOracleBehavior, Conversation.IOwnAConversation
	{
		public MagicaOracleBehavior(Oracle oracle) : base(oracle)
		{
			currentGetTo = oracle.firstChunk.pos;
			lastPos = oracle.firstChunk.pos;
			nextPos = oracle.firstChunk.pos;
			pathProgression = 1f;
			investigateAngle = UnityEngine.Random.value * 360f;
			allSubBehaviors = new List<SubBehavior>();
			currSubBehavior = new NoSubBehavior(this);
			allSubBehaviors.Add(currSubBehavior);
			working = 1f;
			getToWorking = 1f;
			movementBehavior = MovementBehavior.Meditate;
			playerEnteredWithMark = oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark;
			talkedAboutThisSession = new List<EntityID>();
		}

		public static bool HandTowardPlayer { get; internal set; }

		public override void Update(bool eu)
		{
			if (ModManager.MSC)
			{
				if (inspectPearl != null)
				{
					movementBehavior = MovementBehavior.Meditate;
					if (inspectPearl.grabbedBy.Count > 0)
					{
						for (int i = 0; i < inspectPearl.grabbedBy.Count; i++)
						{
							Creature grabber = inspectPearl.grabbedBy[i].grabber;
							if (grabber != null)
							{
								for (int j = 0; j < grabber.grasps.Length; j++)
								{
									if (grabber.grasps[j].grabbed != null && grabber.grasps[j].grabbed == inspectPearl)
									{
										grabber.ReleaseGrasp(j);
									}
								}
							}
						}
					}
					Vector2 vector = oracle.firstChunk.pos - inspectPearl.firstChunk.pos;
					float num = Custom.Dist(oracle.firstChunk.pos, inspectPearl.firstChunk.pos);
					if (num < 64f)
					{
						inspectPearl.firstChunk.vel += Vector2.ClampMagnitude(vector, 2f) / 20f * Mathf.Clamp(16f - num / 100f * 16f, 4f, 16f);
						if (inspectPearl.firstChunk.vel.magnitude < 1f || num < 8f)
						{
							inspectPearl.firstChunk.vel = Vector2.zero;
							inspectPearl.firstChunk.HardSetPosition((oracle.graphicsModule as OracleGraphics).hands[0].pos);
						}
					}
					if (num < 100f && pearlConversation == null && conversation == null)
					{
						// StartItemConversation(inspectPearl);
					}
				}
				//UpdateStoryPearlCollection();
			}
			if (timeSinceSeenPlayer >= 0)
			{
				timeSinceSeenPlayer++;
			}
			if (conversation != null)
			{
				if (restartConversationAfterCurrentDialoge && conversation.paused && action != Action.General_GiveMark && dialogBox.messages.Count == 0 && (!ModManager.MSC || player.room == oracle.room))
				{
					conversation.paused = false;
					restartConversationAfterCurrentDialoge = false;
					conversation.RestartCurrent();
				}
			}
			else if (ModManager.MSC && pearlConversation != null)
			{
				if (pearlConversation.slatedForDeletion)
				{
					pearlConversation = null;
					if (inspectPearl != null)
					{
						inspectPearl.firstChunk.vel = Custom.DirVec(inspectPearl.firstChunk.pos, player.mainBodyChunk.pos) * 3f;
						readDataPearlOrbits.Add(inspectPearl.AbstractPearl);
						inspectPearl = null;
					}
				}
				else
				{
					pearlConversation.Update();
					if (player.room != oracle.room)
					{
						if (player.room != null && !pearlConversation.paused)
						{
							pearlConversation.paused = true;
							InterruptPearlMessagePlayerLeaving();
						}
					}
					else if (pearlConversation.paused && !restartConversationAfterCurrentDialoge)
					{
						ResumePausedPearlConversation();
					}
					if (pearlConversation.paused && restartConversationAfterCurrentDialoge && dialogBox.messages.Count == 0)
					{
						pearlConversation.paused = false;
						restartConversationAfterCurrentDialoge = false;
						pearlConversation.RestartCurrent();
					}
				}
			}
			else
			{
				restartConversationAfterCurrentDialoge = false;
			}
			for (int l = 0; l < oracle.room.game.cameras.Length; l++)
			{
				if (oracle.room.game.cameras[l].room == oracle.room)
				{
					oracle.room.game.cameras[l].virtualMicrophone.volumeGroups[2] = 1f - oracle.room.gravity;
				}
				else
				{
					oracle.room.game.cameras[l].virtualMicrophone.volumeGroups[2] = 1f;
				}
			}
			if (!oracle.Consious)
			{
				return;
			}
			unconciousTick = 0f;
			currSubBehavior.Update();
			if (oracle.slatedForDeletetion)
			{
				return;
			}
			if (conversation != null)
			{
				conversation.Update();
			}
			if (!currSubBehavior.CurrentlyCommunicating && (!ModManager.MSC || pearlConversation == null))
			{
				pathProgression = Mathf.Min(1f, pathProgression + 1f / Mathf.Lerp(40f + pathProgression * 80f, Vector2.Distance(lastPos, nextPos) / 5f, 0.5f));
			}
			currentGetTo = Custom.Bezier(lastPos, ClampVectorInRoom(lastPos + lastPosHandle), nextPos, ClampVectorInRoom(nextPos + nextPosHandle), pathProgression);
			floatyMovement = false;
			investigateAngle += invstAngSpeed;
			inActionCounter++;
			if (pathProgression >= 1f && consistentBasePosCounter > 100 && !oracle.arm.baseMoving)
			{
				allStillCounter++;
			}
			else
			{
				allStillCounter = 0;
			}
			if (action == Action.General_Idle)
			{
				if (movementBehavior != MovementBehavior.Idle && movementBehavior != MovementBehavior.Meditate)
				{
					movementBehavior = MovementBehavior.Idle;
				}
				if (player != null && player.room == oracle.room)
				{
					discoverCounter++;
					if (oracle.room.GetTilePosition(player.mainBodyChunk.pos).y < 32 && (discoverCounter > 220 || Custom.DistLess(player.mainBodyChunk.pos, oracle.firstChunk.pos, 150f) || !Custom.DistLess(player.mainBodyChunk.pos, oracle.room.MiddleOfTile(oracle.room.ShortcutLeadingToNode(1).StartTile), 150f)))
					{
						CustomSeePlayer();
					}
				}
			}
			CustomMove();
			if (working != getToWorking)
			{
				working = Custom.LerpAndTick(working, getToWorking, 0.05f, 0.033333335f);
			}
			if (oracle.room.world.name != "HR")
			{
				float cycleTimer = 1320f;
				float duskpoint = 1.47f;
				float nightpoint = 1.92f;

				if (oracle.room.game.cameras.Length > 0 && oracle.room.game.cameras[0].room == oracle.room && !oracle.room.game.cameras[0].AboutToSwitchRoom && oracle.room.game.cameras[0].paletteBlend != working || (oracle.room.world.rainCycle.dayNightCounter > 0))
				{
					int mainPalette = 25;
					int fadePalette = 26;

					int duskPalette = 3;
					int nightPalette = 100;

					if (oracle.ID == MagicaEnums.Oracles.SRS && oracle.room.game.cameras[0].currentCameraPosition == 6)
					{
						if (oracle.room.world != null && oracle.room.world.rainCycle.dayNightCounter > 0)
						{

							if ((float)oracle.room.world.rainCycle.dayNightCounter < cycleTimer * duskpoint)
							{
								if (working == 0f)
								{
									oracle.room.game.cameras[0].ChangeBothPalettes(mainPalette, duskPalette, Mathf.InverseLerp(cycleTimer, cycleTimer * duskpoint, (float)oracle.room.world.rainCycle.dayNightCounter));
								}
								else
								{
									oracle.room.game.cameras[0].ChangeBothPalettes(fadePalette, duskPalette, Mathf.InverseLerp(cycleTimer, cycleTimer * duskpoint, (float)oracle.room.world.rainCycle.dayNightCounter));
								}
							}
							else if ((float)oracle.room.world.rainCycle.dayNightCounter < cycleTimer * nightpoint)
							{
								oracle.room.game.cameras[0].ChangeBothPalettes(duskPalette, nightPalette, Mathf.InverseLerp(cycleTimer * duskpoint, cycleTimer * nightpoint, (float)oracle.room.world.rainCycle.dayNightCounter) * (oracle.room.game.cameras[0].effect_dayNight * 0.99f));
							}

						}
						else
						{
							oracle.room.game.cameras[0].ChangeBothPalettes(mainPalette, fadePalette, working / 1.2f);
						}
					}
				}
			}
			if (ModManager.MSC)
			{
				if (player != null && player.room == oracle.room)
				{
					List<PhysicalObject>[] physicalObjects = oracle.room.physicalObjects;
					for (int num6 = 0; num6 < physicalObjects.Length; num6++)
					{
						for (int num7 = 0; num7 < physicalObjects[num6].Count; num7++)
						{
							PhysicalObject physicalObject = physicalObjects[num6][num7];
							if (physicalObject is Weapon)
							{
								Weapon weapon = physicalObject as Weapon;
								if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(weapon.firstChunk.pos, oracle.firstChunk.pos) < 100f)
								{
									weapon.ChangeMode(Weapon.Mode.Free);
									weapon.SetRandomSpin();
									weapon.firstChunk.vel *= -0.2f;
									for (int num8 = 0; num8 < 5; num8++)
									{
										oracle.room.AddObject(new Spark(weapon.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
									}
									oracle.room.AddObject(new Explosion.ExplosionLight(weapon.firstChunk.pos, 150f, 1f, 8, Color.white));
									oracle.room.AddObject(new ShockWave(weapon.firstChunk.pos, 60f, 0.1f, 8, false));
									oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, weapon.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value * 0.5f);
								}
							}
						}
					}
				}
				if (currSubBehavior.LowGravity >= 0f)
				{
					oracle.room.gravity = currSubBehavior.LowGravity;
					return;
				}
			}
			if (!currSubBehavior.Gravity)
			{
				oracle.room.gravity = Custom.LerpAndTick(oracle.room.gravity, 0f, 0.05f, 0.02f);
				return;
			}

			if (!ModManager.MSC || oracle.room.world.name != "HR" || !oracle.room.game.IsStorySession || !oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.ripMoon || oracle.ID != Oracle.OracleID.SS)
			{
				oracle.room.gravity = 1f - working;
			}
		}

		private void CustomMove()
		{
			if (movementBehavior == MovementBehavior.Idle)
			{
				invstAngSpeed = 1f;
				if (investigateMarble == null && oracle.marbles.Count > 0)
				{
					investigateMarble = oracle.marbles[UnityEngine.Random.Range(0, oracle.marbles.Count)];
				}
				if (investigateMarble != null && (investigateMarble.orbitObj == oracle || Custom.DistLess(new Vector2(250f, 150f), investigateMarble.firstChunk.pos, 100f)))
				{
					investigateMarble = null;
				}
				if (investigateMarble != null)
				{
					lookPoint = investigateMarble.firstChunk.pos;
					if (Custom.DistLess(nextPos, investigateMarble.firstChunk.pos, 100f))
					{
						floatyMovement = true;
						nextPos = investigateMarble.firstChunk.pos - Custom.DegToVec(investigateAngle) * 50f;
					}
					else
					{
						SetNewDestination(investigateMarble.firstChunk.pos - Custom.DegToVec(investigateAngle) * 50f);
					}
					if (pathProgression == 1f && UnityEngine.Random.value < 0.005f)
					{
						investigateMarble = null;
					}
				}
				if (ModManager.MSC && oracle.ID == MoreSlugcatsEnums.OracleID.DM && UnityEngine.Random.value < 0.001f)
				{
					movementBehavior = MovementBehavior.Meditate;
				}
			}
			else if (movementBehavior == MovementBehavior.Meditate)
			{
				if (nextPos != oracle.room.MiddleOfTile(40, 4) && oracle.ID == MagicaEnums.Oracles.SRS)
				{
					SetNewDestination(oracle.room.MiddleOfTile(40, 4));
				}
				investigateAngle = 0f;
				lookPoint = oracle.firstChunk.pos + new Vector2(0f, -40f);
				if (ModManager.MMF && UnityEngine.Random.value < 0.001f)
				{
					movementBehavior = MovementBehavior.Idle;
				}
			}
			else if (movementBehavior == MovementBehavior.KeepDistance)
			{
				if (player == null)
				{
					movementBehavior = MovementBehavior.Idle;
				}
				else
				{
					lookPoint = player.DangerPos;
					Vector2 vector = new Vector2(UnityEngine.Random.value * oracle.room.PixelWidth, UnityEngine.Random.value * oracle.room.PixelHeight);
					if (!oracle.room.GetTile(vector).Solid && oracle.room.aimap.getTerrainProximity(vector) > 2 && Vector2.Distance(vector, player.DangerPos) > Vector2.Distance(nextPos, player.DangerPos) + 100f)
					{
						SetNewDestination(vector);
					}
				}
			}
			else if (movementBehavior == MovementBehavior.Investigate)
			{
				if (player == null)
				{
					movementBehavior = MovementBehavior.Idle;
				}
				else
				{
					lookPoint = player.DangerPos;
					if (investigateAngle < -90f || investigateAngle > 90f || (float)oracle.room.aimap.getTerrainProximity(nextPos) < 2f)
					{
						investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
						invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1f : 1f);
					}
					Vector2 vector = player.DangerPos + Custom.DegToVec(investigateAngle) * 150f;
					if ((float)oracle.room.aimap.getTerrainProximity(vector) >= 2f)
					{
						if (pathProgression > 0.9f)
						{
							if (Custom.DistLess(oracle.firstChunk.pos, vector, 30f))
							{
								floatyMovement = true;
							}
							else if (!Custom.DistLess(nextPos, vector, 30f))
							{
								SetNewDestination(vector);
							}
						}
						nextPos = vector;
					}
				}
			}
			else if (movementBehavior == MovementBehavior.Talk)
			{
				if (player == null)
				{
					movementBehavior = MovementBehavior.Idle;
				}
				else
				{
					lookPoint = player.DangerPos;
					Vector2 vector = new Vector2(UnityEngine.Random.value * oracle.room.PixelWidth, UnityEngine.Random.value * oracle.room.PixelHeight);
					if (CommunicatePosScore(vector) + 40f < CommunicatePosScore(nextPos) && !Custom.DistLess(vector, nextPos, 30f))
					{
						SetNewDestination(vector);
					}
				}
			}
			else if (movementBehavior == MovementBehavior.ShowMedia)
			{
				if (currSubBehavior is SSOracleMeetWhite)
				{
					(currSubBehavior as SSOracleMeetWhite).ShowMediaMovementBehavior();
				}
			}
			else if (movementBehavior == MagicaEnums.OracleMovementIDs.SitAndFocus)
			{
				investigateAngle = 0f;

				if (nextPos != oracle.room.MiddleOfTile(40, 3) && oracle.ID == MagicaEnums.Oracles.SRS)
				{
					SetNewDestination(oracle.room.MiddleOfTile(40, 3));
				}
				if (pathProgression == 1f)
				{
					if (oracle.firstChunk.ContactPoint.x != 0)
					{
						oracle.firstChunk.vel.y = Mathf.Lerp(oracle.firstChunk.vel.y, 1.2f, 0.5f) + 1.2f;
					}
					if (oracle.bodyChunks[1].ContactPoint.x != 0)
					{
						oracle.firstChunk.vel.y = Mathf.Lerp(oracle.firstChunk.vel.y, 1.2f, 0.5f) + 1.2f;
					}
					oracle.WeightedPush(0, 1, new Vector2(0f, 1f), 4f * Mathf.InverseLerp(60f, 20f, Mathf.Abs(OracleGetToPos.x - oracle.firstChunk.pos.x)));
				}
			}
			if (currSubBehavior != null && currSubBehavior.LookPoint != null)
			{
				lookPoint = currSubBehavior.LookPoint.Value;
			}
			consistentBasePosCounter++;
			if (oracle.room.readyForAI)
			{
				Vector2 vector = new(UnityEngine.Random.value * oracle.room.PixelWidth, UnityEngine.Random.value * oracle.room.PixelHeight);
				if (!oracle.room.GetTile(vector).Solid && BasePosScore(vector) + 40f < BasePosScore(baseIdeal))
				{
					baseIdeal = vector;
					consistentBasePosCounter = 0;
					return;
				}
			}
			else
			{
				baseIdeal = nextPos;
			}
		}

		private void CustomSeePlayer()
		{
			if (conversation == null && !WinOrSaveHooks.SpearMetSRS && action != MagicaEnums.OracleActions.SRSSeesPurple)
			{
				NewAction(MagicaEnums.OracleActions.SRSSeesPurple);
			}
		}

		public class SRSSeesPurple : ConversationBehavior
		{
			private SSOracleBehavior self;
			private int timer;
			public static bool SRSSigning { get; private set; }

			private SRSCutsceneState cutsceneState;
			private FadeOut fadeOut;

			public static bool hugState { get; set; }

			public SRSSeesPurple(SSOracleBehavior self) : base(self, MagicaEnums.OracleActions.SRSPurple, MagicaEnums.ConversationIDs.SRSMeetsSpear)
			{
				this.Deactivate();
				this.self = self as MagicaOracleBehavior;
				cutsceneState = SRSCutsceneState.START;
				if (player != null)
				{
					player.controller = new SpearmasterController(this);

					if (player.graphicsModule is PlayerGraphics graphics)
					{
						graphics.markBaseAlpha = 0f;
					}
				}
			}

			public override void Update()
			{
				base.Update();
				timer++;

				if (cutsceneState == SRSCutsceneState.START)
				{
					if (timer == 30)
					{
						self.getToWorking = 0.7f;
						oracle.room.PlaySound(SoundID.SS_AI_Exit_Work_Mode, 0f, 1f, 1f);
					}

					if (timer > 150 && timer < 620)
					{
						self.getToWorking = 0f;
						movementBehavior = MagicaEnums.OracleMovementIDs.SitAndFocus;
						if (owner.pathProgression == 1f)
						{
							SRSSigning = true;

						}
					}

					if (timer == 580)
					{
						OracleHooks.handTimerMax = 80;
					}

					if (timer > 480)
					{
						if (player.graphicsModule is PlayerGraphics graphics)
						{
							graphics.markBaseAlpha = Mathf.Lerp(0f, 1f, (float)(timer - 480) / 100f);
						}
					}

					if (timer > 510)
					{
						if (player != null)
						{
							oracle.oracleBehavior.lookPoint = player.firstChunk.pos + new Vector2(0f, 20f);
						}
					}
					else
					{
						if (player != null && owner.pathProgression == 1f)
						{
							oracle.oracleBehavior.lookPoint = player.firstChunk.pos;
						}
					}

					if (timer > 620)
					{
						SRSSigning = false;
						cutsceneState = SRSCutsceneState.SHOCK;
						timer = 0;
					}
				}

				if (cutsceneState == SRSCutsceneState.SHOCK)
				{
					if (owner.conversation == null)
					{
						owner.InitateConversation(convoID, this);
					}

					if (player != null)
					{
						oracle.oracleBehavior.lookPoint = player.firstChunk.pos;
					}

					if (timer > 1050 && GraphicsHooks.spearSigning)
					{
						GraphicsHooks.spearSigning = false;
						if (owner.conversation != null)
						{
							owner.conversation.paused = false;
						}
					}

					if (timer > 1050 && hugState)
					{
						cutsceneState = SRSCutsceneState.HUG;
						timer = 0;
					}
				}

				if (cutsceneState == SRSCutsceneState.HUG)
				{
					if (player != null && player.firstChunk.pos.x >= oracle.firstChunk.pos.x - 20f)
					{
						(oracle.graphicsModule as OracleGraphics).head.vel += Vector2.ClampMagnitude((oracle.firstChunk.pos - new Vector2(3f, 0f)) - (oracle.graphicsModule as OracleGraphics).head.pos, 10f) / 3f;
						(oracle.graphicsModule as OracleGraphics).hands[0].vel += Vector2.ClampMagnitude((player.firstChunk.pos + new Vector2(-20f, 10f)) - (oracle.graphicsModule as OracleGraphics).hands[0].pos, 10f) / 3f;
						player.sleepCurlUp = 0.4f;

						if (player.graphicsModule is PlayerGraphics pg)
						{
							pg.blink = 5;
							pg.hands[1].mode = Limb.Mode.HuntRelativePosition;
							pg.hands[1].relativeHuntPos = oracle.firstChunk.pos;

							pg.BringSpritesToFront();
						}
					}

					if (timer > 500 && fadeOut == null)
					{
						WinOrSaveHooks.BeatGameMode(oracle.room.game);
						fadeOut = new(oracle.room, Color.black, 250f, false);
						oracle.room.AddObject(fadeOut);
					}
					if (fadeOut != null && fadeOut.IsDoneFading())
					{
						oracle.room.game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Credits);
						fadeOut = null;
					}
				}

			}
			private Player.InputPackage GetInput()
			{
				if (cutsceneState == SRSCutsceneState.START)
				{
					if (player.firstChunk.pos.x > oracle.room.MiddleOfTile(33, 0).x || player.firstChunk.pos.x < oracle.room.MiddleOfTile(31, 0).x)
					{
						return new(true, Options.ControlSetup.Preset.None, (player.firstChunk.pos.x > oracle.room.MiddleOfTile(33, 0).x ? -1 : 1), (player.bodyMode == Player.BodyModeIndex.Crawl ? 1 : 0), false, false, false, false, false);
					}
					else if (player.lastFlipDirection != 1)
					{
						return new(true, Options.ControlSetup.Preset.None, 1, (player.bodyMode == Player.BodyModeIndex.Crawl ? 1 : 0), false, false, false, false, false);
					}
					return new(true, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
				}
				if (cutsceneState == SRSCutsceneState.SHOCK)
				{
					if (timer > 340 && timer < 345)
					{
						return new(true, Options.ControlSetup.Preset.None, 0, 1, true, false, false, false, false);
					}
				}
				if (cutsceneState == SRSCutsceneState.HUG)
				{
					if (player.firstChunk.pos.x < oracle.firstChunk.pos.x - 20f)
					{
						return new(true, Options.ControlSetup.Preset.None, 1, (player.bodyMode == Player.BodyModeIndex.Crawl ? 1 : 0), false, false, false, false, false);
					}
					else if (player.bodyMode != Player.BodyModeIndex.Crawl)
					{
						return new(true, Options.ControlSetup.Preset.None, 0, -1, false, false, false, false, false);
					}
				}
				return default;
			}

			public enum SRSCutsceneState
			{
				START,
				SHOCK,
				HUG
			}

			public class SpearmasterController : Player.PlayerController
			{
				private SRSSeesPurple owner;

				public SpearmasterController(SRSSeesPurple owner)
				{
					this.owner = owner;
				}

				public override Player.InputPackage GetInput()
				{
					return owner.GetInput();
				}
			}
		}
	}
}
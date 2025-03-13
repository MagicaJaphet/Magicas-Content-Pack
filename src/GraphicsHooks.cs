using DressMySlugcat;
using DressMySlugcat.Hooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MagicasContentPack
{
	public class GraphicsHooks
	{
		private static Color saintScarColor;
		private static int handTimer;
		private static int handPose;
		private static int handTimerMax = 40;
		private static FLabel debugLabel;
		private static FSprite[] debugSprites;
		internal static float saintGlowTimerMax = 30f;

		public static Dictionary<CustomColorValues, Color> customColorDict = new()
		{
			{ CustomColorValues.HunterArm, Custom.hexToColor("FFCD6A") },
			{ CustomColorValues.SaintTongue, new(0.631f, 0.333f, 0.294f) },
		};

		public enum CustomColorValues
		{
			HunterArm,
			SaintTongue
		}

		public static bool spearSigning { get; set; }

		public static void Init()
		{
			try
			{
				// Fix vulture mask positions for when using custom graphics
				IL.VultureMask.DrawSprites += VultureMask_DrawSprites;
				On.MoreSlugcats.VultureMaskGraphics.DrawSprites += UpdatePostArtiKingMask;

				// Adds custom body parts and colors
				On.PlayerGraphics.SaintFaceCondition += PlayerGraphics_SaintFaceCondition;
				IL.PlayerGraphics.ColoredBodyPartList += PlayerGraphics_ColoredBodyPartList;
				IL.PlayerGraphics.DefaultBodyPartColorHex += PlayerGraphics_DefaultBodyPartColorHex;

				On.PlayerGraphics.ctor += PlayerGraphics_ctor;
				On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
				On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
				On.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
				On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
				On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;

				// Update for skins
				On.TailSegment.Update += TailSegment_Update;
				On.PlayerGraphics.CosmeticPearl.InitiateSprites += CosmeticPearl_InitiateSprites;
			}
			catch
			{
				Plugin.Log(Plugin.LogStates.HookFail, nameof(GraphicsHooks));
			}
		}

		internal static void PostInit()
		{
			try
			{
				if (Plugin.isDMSEnabled)
					ApplyDMSHooks();

				Plugin.Log(Plugin.LogStates.HooksSucceeded, nameof(GraphicsHooks) + " post");
			}
			catch (Exception ex)
			{
				Plugin.Log(Plugin.LogStates.HookFail, nameof(GraphicsHooks));
				Plugin.Logger.LogError(ex);
			}
		}

		// This is in a method in case the dependancy isn't enabled, so the assembly doesn't shit itself
		private static void ApplyDMSHooks()
		{
			_ = new Hook(typeof(PlayerGraphicsDummy).GetMethod("UpdateSprites", BindingFlags.NonPublic | BindingFlags.Instance), (Action<PlayerGraphicsDummy> orig, PlayerGraphicsDummy dummy) =>
			{
				orig(dummy);

				dummy?.UpdateSpritePositions();
			});

			_ = new Hook(typeof(PlayerGraphicsDummy).GetMethod(nameof(PlayerGraphicsDummy.UpdateSpritePositions), BindingFlags.Public | BindingFlags.Instance), (Action<PlayerGraphicsDummy> orig, PlayerGraphicsDummy dummy) =>
			{
				orig(dummy);

				if (dummy != null)
				{
					FancyMenu owner = (FancyMenu)typeof(PlayerGraphicsDummy).GetField("owner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dummy);

					if (owner != null && Customization.For(owner.selectedSlugcat, owner.selectedPlayerIndex, true) != null)
					{
						CustomSprite customSprite = Customization.For(owner.selectedSlugcat, owner.selectedPlayerIndex, true).CustomSprite("HEAD", false);

						if (customSprite != null && MagicaSprites.GetKey(customSprite.SpriteSheetID) != null && MagicaSprites.GetKey(customSprite.SpriteSheetID).faceLift)
						{
							dummy.Sprites[9].y = 3f + dummy.Sprites[0].y;
						}
					}
				}
			});

			_ = new Hook(typeof(AtlasHooks).GetMethod(nameof(AtlasHooks.LoadAtlasesInternal), BindingFlags.Public | BindingFlags.Static), (Action<string> orig, string directory = "dressmyslugcat") =>
			{
				orig(directory);

				List<string> source = Utils.ListDirectory(directory, false, true).Distinct<string>().ToList<string>();
				string text = source.FirstOrDefault((string f) => "metadata.json".Equals(Path.GetFileName(f), StringComparison.InvariantCultureIgnoreCase));
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						Dictionary<string, object> json = File.ReadAllText(text).dictionaryFromJson();

						if (json != null && json.TryGetValue("id", out object idObj))
						{
							string id = idObj.ToString();

							if (json != null && json.TryGetValue("taller", out object faceLift) && MagicaSprites.GetKey(id) != null)
							{
								string face = faceLift.ToString();
								MagicaSprites.GetKey(id).faceLift = bool.TryParse(face, out _);
								Plugin.DebugLog("taller found for " + id + ", adding: " + bool.TryParse(face, out _).ToString());
							}
						}
					}
					catch (Exception e)
					{
						Plugin.Logger.LogError(e);
					}
				}
			});

			// CODE DOESNT WORK BECAUSE IT BREAKS THE ASSEMBLY FOR SYSTEM COLLECTIONS APPARENTLY? IDK LOL
			//ILHook loadDMSAtlasHook = new(typeof(AtlasHooks).GetMethod(nameof(AtlasHooks.LoadAtlasesInternal), BindingFlags.Public | BindingFlags.Static), (ILContext il) =>
			//{
			//	try
			//	{
			//		ILCursor cursor = new(il);

			//		bool success = cursor.TryGotoNext(
			//			MoveType.After,
			//			x => x.MatchLdstr("author")
			//			);

			//		if (!success)
			//		{
			//			Plugin.Log(Plugin.LogStates.FailILMatch, nameof(loadDMSAtlasHook));
			//		}

			//		cursor.Emit(OpCodes.Nop);
			//		cursor.Emit(OpCodes.Ldloc_3);
			//		cursor.Emit(OpCodes.Ldloc, 5);
			//		static void AddExtraHeadAnchorCheck(SpriteSheet spriteSheet, Dictionary<string, object> json)
			//		{
			//			if (spriteSheet != null && !string.IsNullOrEmpty(spriteSheet.ID) && (json != null && json.TryGetValue("id", out object idObj)))
			//			{
			//				string id = string.IsNullOrEmpty(spriteSheet.ID) ? idObj.ToString() : spriteSheet.ID;

			//				if (json != null && json.TryGetValue("taller", out object faceLift))
			//				{
			//					string face = faceLift.ToString();
			//					MagicaSprites.qualifiedForChange.Add(id, bool.TryParse(face, out _));
			//					Plugin.DebugLog(MagicaSprites.qualifiedForChange.TryGetValue(id, out _).ToString());
			//				}
			//			}

			//		}
			//		cursor.EmitDelegate(AddExtraHeadAnchorCheck);
			//	}
			//	catch (Exception ex)
			//	{
			//		Plugin.Log(Plugin.LogStates.FailILInsert, ex);
			//	}

			//	Plugin.Log(Plugin.LogStates.ILSuccess, nameof(loadDMSAtlasHook));
			//});
		}

		private static void PlayerGraphics_ColoredBodyPartList(ILContext il)
		{
			ILCursor cursor = new(il);

			bool success = cursor.TryGotoNext(
				MoveType.Before,
				move => move.MatchRet()
				);

			if (!success)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(PlayerGraphics_ColoredBodyPartList));
			}

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc_0);
			static void AddToList(SlugcatStats.Name slugcatID, List<string> list)
			{
				if (slugcatID == SlugcatStats.Name.Red)
				{
					list.Add("Arm");
				}
			}
			cursor.EmitDelegate(AddToList);

		}
		private static void PlayerGraphics_DefaultBodyPartColorHex(ILContext il)
		{
			ILCursor cursor = new(il);

			bool saintTongue = cursor.TryGotoNext(
				x => x.MatchLdstr("FF80A6")
				);

			if (!saintTongue)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(PlayerGraphics_DefaultBodyPartColorHex) + " I don't even know how this one would fail");
			}

			cursor.Next.Operand = Custom.colorToHex(customColorDict[CustomColorValues.SaintTongue]);

			bool success = cursor.TryGotoNext(
				MoveType.Before,
				move => move.MatchRet()
				);

			if (!success)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(PlayerGraphics_DefaultBodyPartColorHex));
			}

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc_0);
			static void AddToList(SlugcatStats.Name slugcatID, List<string> list)
			{
				if (slugcatID == SlugcatStats.Name.Red)
				{
					list.Add(Custom.colorToHex(customColorDict[CustomColorValues.HunterArm]));
				}
			}
			cursor.EmitDelegate(AddToList);

			Plugin.Log(Plugin.LogStates.ILSuccess, nameof(PlayerGraphics_DefaultBodyPartColorHex));
		}

		private static void VultureMask_DrawSprites(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool success = cursor.TryGotoNext(
					MoveType.After,
					move => move.MatchStfld<VultureMaskGraphics>(nameof(VultureMaskGraphics.overrideAnchorVector))
					);

				if (!success)
				{
					Plugin.Log(Plugin.LogStates.FailILMatch, nameof(VultureMask_DrawSprites));
				}

				cursor.Emit(OpCodes.Ldarg_0);
				static void FixVultureMaskPosition(VultureMask self)
				{
					if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player && self.grabbedBy[0].grabber.graphicsModule is PlayerGraphics pGraphics && CheckForDMS(pGraphics, "HEAD", DMSCheck.FaceLift))
					{
						self.maskGfx.overrideDrawVector += new Vector2(0f, 4f);
					}
				}
				cursor.EmitDelegate(FixVultureMaskPosition);
			}
			catch (Exception ex)
			{
				Plugin.Log(Plugin.LogStates.FailILInsert, ex);
			}
		}

		private static void UpdatePostArtiKingMask(On.MoreSlugcats.VultureMaskGraphics.orig_DrawSprites orig, VultureMaskGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (WinOrSaveHooks.ArtiKilledScavKing && self.ScavKing && ModOptions.CustomGraphics.Value)
			{
				for (int i = 0; i < 3; i++)
				{
					string vultureMaskName = sLeaser.sprites[self.firstSprite + i].element.name;

					if (!vultureMaskName.Contains("Magica"))
						sLeaser.sprites[self.firstSprite + i].SetElementByName("Magica" + vultureMaskName);
				}
			}
		}
		private static bool PlayerGraphics_SaintFaceCondition(On.PlayerGraphics.orig_SaintFaceCondition orig, PlayerGraphics self)
		{
			if (ModOptions.CustomMechanics.Value)
			{
				return self.player.room != null && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(self.player, out var player) && !player.magicaSaintAscension;
			}
			return orig(self);
		}

		private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
		{
			orig(self, ow);

			if (ModOptions.CustomGraphics.Value)
			{
				var magicaCWT = MagicaSprites.magicaCWT.GetOrCreateValue(self);
				if (magicaCWT != null)
				{
					List<Braids> braids = [];

					int rows = 0;
					int length = 0;
					int startSprite = ModManager.MSC ? 13 : 12;

					// Add a mod option for the number of braids/rows for each scug, for now it'll just be hardcoded cuz im lazy

					if (ModManager.MSC)
					{
						if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
						{
							rows = 2;
							length = 8;
						}

						if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
						{
							rows = 6;
							length = 18;
						}
					}

					for (int i = 0; i < length; i++)
					{
						int rowNum = Mathf.FloorToInt((float)i / ((float)length / (float)rows));
						if (i % (length / rows) == 0)
						{
							braids.Add(new(self, rowNum, 5f, 6f, null, 0.7f, 0.9f));
						}
						else
						{
							braids.Add(new(self, rowNum, 5f, 6f, braids[i - 1], 0.7f, 0.9f));
						}
					}

					if (braids.Count > 0)
					{
						foreach (Braids braid in braids)
						{
							Plugin.DebugLog(string.Join(" | ",
								"NUM " + braids.IndexOf(braid),
								"CONNECTED TO " + (braid.connectedSegment == null ? "null" : braids.IndexOf(braid.connectedSegment)),
								"ROW " + braid.row.ToString()));
						}
						magicaCWT.braidRows = rows;
						magicaCWT.braids = braids.ToArray();
						magicaCWT.headBraids = (from braid in magicaCWT.braids where magicaCWT.braids.IndexOf(braid) % (magicaCWT.braids.Length / magicaCWT.braidRows) == 0 select braid).ToArray();
						magicaCWT.lastBraidPos = new Vector2[braids.Count];
					}
				}
			}
		}

		private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);

			if (ModOptions.CustomGraphics.Value && MagicaSprites.magicaCWT.TryGetValue(self, out var magicaCWT))
			{
				magicaCWT.bodySprite = sLeaser.sprites[0];
				magicaCWT.hipSprite = sLeaser.sprites[1];
				magicaCWT.tailSprite = sLeaser.sprites[2];
				magicaCWT.headSprite = sLeaser.sprites[3];
				magicaCWT.legSprite = sLeaser.sprites[4];
				magicaCWT.faceSprite = sLeaser.sprites[9];

				if (magicaCWT.braids != null && magicaCWT.braids.Length > 0)
				{
					magicaCWT.braidStartSprite = sLeaser.sprites.Length;
					Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + magicaCWT.braids.Length);
					for (int i = magicaCWT.braidStartSprite; i < sLeaser.sprites.Length; i++)
					{
						sLeaser.sprites[i] = new("Braid")
						{
							scaleX = 0.8f
						};
					}
				}

				if (ModManager.MSC)
				{
					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
					{
						magicaCWT.artiStartSprite = sLeaser.sprites.Length;

						if (self.player.DreamState)
						{
							Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
							sLeaser.sprites[magicaCWT.artiStartSprite] = new("TailPuff");

							magicaCWT.artiTailSprite = sLeaser.sprites[magicaCWT.artiStartSprite];
						}
						else
						{
							Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 3);
							sLeaser.sprites[magicaCWT.artiStartSprite] = new("ScarHeadA0");
							sLeaser.sprites[magicaCWT.artiStartSprite + 1] = new("ScarLegsA0");
							sLeaser.sprites[magicaCWT.artiStartSprite + 2] = new("TailPuff");

							magicaCWT.artiScarSprite = sLeaser.sprites[magicaCWT.artiStartSprite];
							magicaCWT.artiLegSprite = sLeaser.sprites[magicaCWT.artiStartSprite + 1];
							magicaCWT.artiTailSprite = sLeaser.sprites[magicaCWT.artiStartSprite + 2];
						}
					}

					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						magicaCWT.saintStartSprite = sLeaser.sprites.Length;

						Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 8);
						sLeaser.sprites[magicaCWT.saintStartSprite] = new("SaintFaceB0");
						sLeaser.sprites[magicaCWT.saintStartSprite + 1] = new("FatiqueSaintFaceB0");
						for (int i = 0; i < 4; i++)
						{
							sLeaser.sprites[magicaCWT.saintStartSprite + i + 2] = new("pixel") { isVisible = false, anchorY = 0f };
						}
						sLeaser.sprites[magicaCWT.saintStartSprite + 6] = new("SaintKarmaRing") { scale = 0.5f, isVisible = false };
						sLeaser.sprites[magicaCWT.saintStartSprite + 7] = new("SaintKarmaRing0") { scale = 0.5f, isVisible = false };

						magicaCWT.saintScarSprite = sLeaser.sprites[magicaCWT.saintStartSprite];
						magicaCWT.saintFatiqueSprite = sLeaser.sprites[magicaCWT.saintStartSprite + 1];
						magicaCWT.saintAcensionLines = [
							sLeaser.sprites[magicaCWT.saintStartSprite + 2],
							sLeaser.sprites[magicaCWT.saintStartSprite + 3],
							sLeaser.sprites[magicaCWT.saintStartSprite + 4],
							sLeaser.sprites[magicaCWT.saintStartSprite + 5]
							];
						magicaCWT.saintCreatureCircle = sLeaser.sprites[magicaCWT.saintStartSprite + 6];
						magicaCWT.saintCreatureKarma = sLeaser.sprites[magicaCWT.saintStartSprite + 7];
					}
				}

				if (self.player.SlugCatClass == SlugcatStats.Name.Red)
				{
					if (rCam.room != null && rCam.room.game != null && rCam.room.game.IsArenaSession)
					{
						WinOrSaveHooks.HunterScarProgression = 3;
					}
					float scarAlpha = 1f - (((float)(WinOrSaveHooks.HunterScarProgression + 1) / 4f) * 0.6f);

					magicaCWT.hunterStartSprite = sLeaser.sprites.Length;

					Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 6);

					sLeaser.sprites[magicaCWT.hunterStartSprite] = new("HunterFace" + WinOrSaveHooks.HunterScarProgression.ToString()) { alpha = scarAlpha };
					sLeaser.sprites[magicaCWT.hunterStartSprite + 1] = new("HunterHip" + WinOrSaveHooks.HunterScarProgression.ToString()) { alpha = scarAlpha };
					sLeaser.sprites[magicaCWT.hunterStartSprite + 2] = new("HunterLeg" + WinOrSaveHooks.HunterScarProgression.ToString()) { alpha = scarAlpha };
					sLeaser.sprites[magicaCWT.hunterStartSprite + 3] = new("HunterBody" + WinOrSaveHooks.HunterScarProgression.ToString()) { alpha = scarAlpha };
					sLeaser.sprites[magicaCWT.hunterStartSprite + 4] = new("HunterTail" + WinOrSaveHooks.HunterScarProgression.ToString()) { alpha = scarAlpha };
					sLeaser.sprites[magicaCWT.hunterStartSprite + 5] = new("HunterTip" + WinOrSaveHooks.HunterScarProgression.ToString()) { alpha = scarAlpha };

					magicaCWT.hunterFaceSprite = sLeaser.sprites[magicaCWT.hunterStartSprite];
					magicaCWT.hunterHipSprite = sLeaser.sprites[magicaCWT.hunterStartSprite + 1];
					magicaCWT.hunterLegSprite = sLeaser.sprites[magicaCWT.hunterStartSprite + 2];
					magicaCWT.hunterBodySprite = sLeaser.sprites[magicaCWT.hunterStartSprite + 3];
					magicaCWT.hunterTailSprite = sLeaser.sprites[magicaCWT.hunterStartSprite + 4];
					magicaCWT.hunterTailTipSprite = sLeaser.sprites[magicaCWT.hunterStartSprite + 5];
				}

				self.AddToContainer(sLeaser, rCam, null);
			}
		}


		private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);

			if (ModOptions.CustomGraphics.Value && MagicaSprites.magicaCWT.TryGetValue(self, out var magicaCWT))
			{
				newContatiner ??= rCam.ReturnFContainer("Midground");

				if (magicaCWT.braids != null && sLeaser.sprites.Length > magicaCWT.braidStartSprite)
				{
					for (int i = magicaCWT.braidStartSprite; i < sLeaser.sprites.Length; i++)
					{
						if (sLeaser.sprites[i] != null)
						{
							sLeaser.sprites[i].RemoveFromContainer();
							newContatiner.AddChild(sLeaser.sprites[i]);
							sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[0]);
						}
					}
				}

				if (ModManager.MSC)
				{
					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && sLeaser.sprites.Length > magicaCWT.artiStartSprite)
					{
						if (magicaCWT.artiScarSprite != null)
						{
							magicaCWT.artiScarSprite.RemoveFromContainer();
							newContatiner.AddChild(magicaCWT.artiScarSprite);
							magicaCWT.artiScarSprite.MoveBehindOtherNode(sLeaser.sprites[9]);
						}
						if (magicaCWT.artiLegSprite != null)
						{
							magicaCWT.artiLegSprite.RemoveFromContainer();
							newContatiner.AddChild(magicaCWT.artiLegSprite);
							magicaCWT.artiLegSprite.MoveBehindOtherNode(sLeaser.sprites[5]);
						}
						if (magicaCWT.artiTailSprite != null)
						{
							magicaCWT.artiTailSprite.RemoveFromContainer();
							newContatiner.AddChild(magicaCWT.artiTailSprite);
							magicaCWT.artiTailSprite.MoveBehindOtherNode(sLeaser.sprites[2]);
						}
					}
				}

				if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && sLeaser.sprites.Length > magicaCWT.saintStartSprite)
				{
					if (magicaCWT.saintScarSprite != null)
					{
						magicaCWT.saintScarSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.saintScarSprite);
						magicaCWT.saintScarSprite.MoveBehindOtherNode(sLeaser.sprites[9]);
					}
					if (magicaCWT.saintFatiqueSprite != null)
					{
						magicaCWT.saintFatiqueSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.saintFatiqueSprite);
						magicaCWT.saintFatiqueSprite.MoveBehindOtherNode(sLeaser.sprites[9]);
					}
					if (magicaCWT.saintAcensionLines != null)
					{
						foreach (var sprite in magicaCWT.saintAcensionLines)
						{
							sprite.RemoveFromContainer();
							rCam.ReturnFContainer("HUD2").AddChild(sprite);
						}
					}
					if (magicaCWT.saintCreatureCircle != null)
					{
						magicaCWT.saintCreatureCircle.RemoveFromContainer();
						rCam.ReturnFContainer("HUD2").AddChild(magicaCWT.saintCreatureCircle);
					}
					if (magicaCWT.saintCreatureKarma != null)
					{
						magicaCWT.saintCreatureKarma.RemoveFromContainer();
						rCam.ReturnFContainer("HUD2").AddChild(magicaCWT.saintCreatureKarma);
						magicaCWT.saintCreatureKarma.MoveBehindOtherNode(magicaCWT.saintCreatureCircle);
					}
				}

				if (self.player.SlugCatClass == SlugcatStats.Name.Red && sLeaser.sprites.Length > magicaCWT.hunterStartSprite)
				{
					if (magicaCWT.hunterFaceSprite != null)
					{
						magicaCWT.hunterFaceSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.hunterFaceSprite);
						magicaCWT.hunterFaceSprite.MoveBehindOtherNode(sLeaser.sprites[9]);
					}
					if (magicaCWT.hunterHipSprite != null)
					{
						magicaCWT.hunterHipSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.hunterHipSprite);
						magicaCWT.hunterHipSprite.MoveBehindOtherNode(sLeaser.sprites[3]);
					}
					if (magicaCWT.hunterLegSprite != null)
					{
						magicaCWT.hunterLegSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.hunterLegSprite);
						magicaCWT.hunterLegSprite.MoveBehindOtherNode(sLeaser.sprites[5]);
					}
					if (magicaCWT.hunterBodySprite != null)
					{
						magicaCWT.hunterBodySprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.hunterBodySprite);
						magicaCWT.hunterBodySprite.MoveBehindOtherNode(sLeaser.sprites[3]);
					}
					if (magicaCWT.hunterTailSprite != null)
					{
						magicaCWT.hunterTailSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.hunterTailSprite);
						magicaCWT.hunterTailSprite.MoveBehindOtherNode(sLeaser.sprites[1]);
					}
					if (magicaCWT.hunterTailTipSprite != null)
					{
						magicaCWT.hunterTailTipSprite.RemoveFromContainer();
						newContatiner.AddChild(magicaCWT.hunterTailTipSprite);
						magicaCWT.hunterTailTipSprite.MoveBehindOtherNode(sLeaser.sprites[1]);
					}
				}
			}
		}

		private static void PlayerGraphics_MSCUpdate(On.PlayerGraphics.orig_MSCUpdate orig, PlayerGraphics self)
		{
			orig(self);

			//if (spearSigning)
			//{
			//	self.hands[0].reachingForObject = true;
			//	self.hands[1].reachingForObject = true;
			//	self.hands[0].mode = Limb.Mode.HuntRelativePosition;
			//	self.hands[1].mode = Limb.Mode.HuntRelativePosition;

			//	if (handTimer > handTimerMax)
			//	{
			//		handTimer = 0;
			//		handPose = Mathf.RoundToInt(UnityEngine.Random.value * 5);
			//	}


			//	switch (handPose)
			//	{
			//		case 0:
			//			self.hands[0].relativeHuntPos += Vector2.ClampMagnitude(Vector2.Lerp(self.head.pos, self.player.firstChunk.pos, (float)handTimer / (float)handTimerMax) - self.hands[0].pos, 10f) / 3f;
			//			self.hands[1].relativeHuntPos += Vector2.ClampMagnitude(self.player.firstChunk.pos + new Vector2(-4f, 0f) - self.hands[1].pos, 10f) / 3f;
			//			break;

			//		case 1:
			//			self.blink = 5;
			//			self.hands[0].relativeHuntPos += Vector2.ClampMagnitude(self.player.firstChunk.pos + new Vector2(-4f, -4f) - self.hands[0].pos, 10f) / 3f;
			//			self.hands[1].relativeHuntPos += Vector2.ClampMagnitude(Vector2.Lerp(self.player.firstChunk.pos, self.hands[0].pos, ((float)handTimer / (float)handTimerMax) / 1.2f) - self.hands[1].pos, 10f) / 3f;
			//			break;

			//		case 2:
			//			self.hands[0].relativeHuntPos += Vector2.ClampMagnitude(self.head.pos + new Vector2(-8f, 4f) - self.hands[0].pos, 10f) / 3f;
			//			self.hands[1].relativeHuntPos += Vector2.ClampMagnitude(self.player.firstChunk.pos - self.hands[1].pos, 10f) / 3f;
			//			break;

			//		case 3:
			//			self.hands[0].relativeHuntPos += Vector2.ClampMagnitude(Vector2.Lerp(self.player.firstChunk.pos, self.hands[1].pos, ((float)handTimer / (float)handTimerMax) / 1.5f) - self.hands[0].pos, 10f) / 3f;
			//			self.hands[1].relativeHuntPos += Vector2.ClampMagnitude(Vector2.Lerp(self.head.pos, self.head.pos + new Vector2(6f, 4f), ((float)handTimer / (float)handTimerMax)) - self.hands[1].pos, 10f) / 3f;
			//			break;

			//		case 4:
			//			self.blink = 5;
			//			self.hands[0].relativeHuntPos += Vector2.ClampMagnitude(Vector2.Lerp(self.player.firstChunk.pos, self.hands[1].pos, 0.3f) - self.hands[0].pos, 10f) / 3f;
			//			if (self.objectLooker != null)
			//			{
			//				self.hands[1].relativeHuntPos += Vector2.ClampMagnitude(self.objectLooker.lookAtPoint.Value - self.hands[1].pos, 10f) / 3f;
			//			}
			//			break;

			//		case 5:
			//			self.hands[0].relativeHuntPos += Vector2.ClampMagnitude(Vector2.Lerp(self.player.firstChunk.pos + new Vector2(-4f, -4f), self.player.firstChunk.pos + new Vector2(-4f, -4f), ((float)handTimer / (float)handTimerMax)) - self.hands[0].pos, 10f) / 3f;
			//			self.hands[1].relativeHuntPos += Vector2.ClampMagnitude(self.player.firstChunk.pos - self.hands[1].pos, 10f) / 3f;
			//			break;
			//	}
			//	handTimer++;
			//}

			if (ModOptions.CustomMechanics.Value && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(self.player, out var player))
			{
				if (player.magicaSaintAscension && player.saintTargetMode)
				{
					if (player.saintTarget != null)
					{
						int hand = player.saintTarget.firstChunk.pos.x > self.player.firstChunk.pos.x ? 1 : 0;
						self.hands[hand].reachingForObject = true;
						self.hands[hand].mode = Limb.Mode.HuntAbsolutePosition;
						self.hands[hand].absoluteHuntPos = player.saintTarget.firstChunk.pos;
					}
				}
			}

			if (ModOptions.CustomGraphics.Value && MagicaSprites.magicaCWT.TryGetValue(self, out var magicaCWT))
			{
				if (magicaCWT.braids != null && magicaCWT.braids.Length > 0 && magicaCWT.headSprite != null && magicaCWT.headBraids != null)
				{
					// Set connected point based on head sprite itself

					// Right = 1f : Left = -1f
					float flip = magicaCWT.headSprite.scaleX;
					Vector2 headDisplacement = Vector2.Lerp(magicaCWT.headSpritePos, magicaCWT.faceSpritePos, 0.3f);
					Vector2 move = new();

					float minAng = 0f;
					float maxAng = 0f;

					string name = magicaCWT.headSprite.element.name;

					if (name.Length > 6 && name.IndexOf("Head") != -1)
					{
						switch (name.Substring(name.IndexOf("Head")))
						{
							case "HeadA0":
							case "HeadA1":
							case "HeadA2":
							case "HeadA3":
								minAng = 100f;
								maxAng = 140f;
								move = flip > 0 ? new(-8f, 2f) : new(8f, 2f);
								break;

							case "HeadA4":
							case "HeadA5":
							case "HeadA6":
								minAng = 50f + (flip > 0f ? 180 : 0);
								maxAng = 80f + (flip > 0f ? 180 : 0);
								move = new(-8f * flip, 2f);
								break;

							case "HeadA7":
							case "HeadA8":
							case "HeadA9":
								minAng = flip > 0 ? -180f : -90f;
								maxAng = flip > 0 ? -200f : -120f;
								break;

							case "HeadA10":
							case "HeadA11":
							case "HeadA12":
							case "HeadA13":
							case "HeadA14":
							case "HeadA15":
							case "HeadA16":
								minAng = 300f;
								maxAng = 230f;
								break;

							case "HeadA17":
								minAng = 180f;
								maxAng = 230f;
								break;
						}
					}

					if (self.lookDirection != Vector2.zero)
					{
						minAng = Custom.Angle(magicaCWT.headSpritePos, magicaCWT.faceSpritePos) - 20f;
						maxAng = Custom.Angle(magicaCWT.headSpritePos, magicaCWT.faceSpritePos) + 40f;
						move = flip > 0 ? new(2f, 0f) : new(-2f, 0f);
					}

					Vector2 velocity = Vector2.zero;

					Player.BodyModeIndex bodyMode = self.player.bodyMode;
					Player.AnimationIndex animMode = self.player.animation;

					if (bodyMode == Player.BodyModeIndex.ZeroG || bodyMode == Player.BodyModeIndex.Swimming)
					{
						velocity = new(self.head.vel.x, -self.head.vel.y);
					}
					else if (bodyMode == Player.BodyModeIndex.Crawl || bodyMode == Player.BodyModeIndex.WallClimb)
					{
						velocity = new(flip, -(-0.8f - self.head.vel.y));
					}
					else if (animMode == Player.AnimationIndex.HangUnderVerticalBeam)
					{
						velocity = new(self.head.vel.x, -0.8f - self.head.vel.y);
					}
					else if (animMode == Player.AnimationIndex.ClimbOnBeam)
					{
						velocity = new(self.legs.vel.x, -(-0.8f - self.head.vel.y));
					}
					else
					{
						velocity = new(self.head.vel.x, -(-0.8f - self.head.vel.y));
					}

					for (int i = 0; i < magicaCWT.braids.Length; i++)
					{
						if (magicaCWT.headBraids.Contains(magicaCWT.braids[i]))
						{
							int index = magicaCWT.headBraids.IndexOf(magicaCWT.braids[i]);
							float braidLerp = index == 0 ? 0f : (float)index / (float)(magicaCWT.headBraids.Length - 1);
							Vector2 pos = Custom.RotateAroundVector(magicaCWT.headSpritePos + move, headDisplacement + move, (Mathf.Lerp(minAng, maxAng, braidLerp) + Mathf.Abs(magicaCWT.headSprite.rotation)) * flip);
							magicaCWT.headBraids[index].connectedPoint = pos;
							magicaCWT.headBraids[index].vel = Vector2.Lerp(Custom.DirVec(headDisplacement + move, pos) + (self.player.bodyMode == Player.BodyModeIndex.Crawl ? Custom.PerpendicularVector(headDisplacement + move, pos) : new()), magicaCWT.headBraids[index].vel - velocity, 0.05f);

							if (Custom.Dist(magicaCWT.braids[i].pos, magicaCWT.headSpritePos) > 40f && self.head.vel.magnitude < 2f)
							{
								magicaCWT.braids[i].Reset(pos);
							}
						}
						else
						{
							float braidNum = i - ((magicaCWT.braids.Length / magicaCWT.braidRows) * magicaCWT.braids[i].row);
							magicaCWT.braids[i].vel = Vector2.Lerp(magicaCWT.headBraids[magicaCWT.braids[i].row].vel + Vector2.Lerp(new(), (Custom.PerpendicularVector(self.player.bodyChunks[1].pos, magicaCWT.braids[i].pos) * (self.lookDirection == Vector2.zero ? -flip : -self.lookDirection.x) * 10f), (float)(magicaCWT.braids[i].row + 1) / (float)magicaCWT.braidRows), magicaCWT.braids[i].vel - velocity, Custom.LerpCircEaseOut(0f, 0.8f, braidNum / (float)(magicaCWT.braids.Length / magicaCWT.braidRows)));
						}

						if (bodyMode != Player.BodyModeIndex.ZeroG && bodyMode != Player.BodyModeIndex.Crawl && bodyMode != Player.BodyModeIndex.WallClimb && self.head.vel.y < 0f)
						{
							velocity.x += UnityEngine.Random.value - 0.5f;
						}

						magicaCWT.braids[i]?.Update();
					}
				}
			}
		}

		private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			var headAngle = sLeaser.sprites[0].rotation;
			string spriteFace;
			int spriteScale;

			if (CheckForDMS(self, "HEAD", DMSCheck.FaceLift))
			{
				sLeaser.sprites[9].anchorY = 0.43f;
			}

			if ((headAngle is > 0 and < 18) || (headAngle is < 0 and > -18) || headAngle is 0)
			{
				spriteFace = "Right";
				spriteScale = 1;
			}
			else if ((headAngle is > 18 and < 130) || (headAngle is < -18 and > -130))
			{
				if (headAngle is < -18 and > -120)
				{
					spriteFace = "Left";
					spriteScale = -1;
				}
				else
				{
					spriteFace = "Right";
					spriteScale = 1;
				}
			}
			else
			{
				spriteFace = "";
				spriteScale = 1;
			}

			if (ModOptions.CustomGraphics.Value && MagicaSprites.magicaCWT.TryGetValue(self, out var magicaCWT))
			{
				magicaCWT.headSpritePos = sLeaser.sprites[3].GetPosition() + rCam.pos;
				magicaCWT.faceSpritePos = sLeaser.sprites[9].GetPosition() + rCam.pos;

				if (ModManager.MSC)
				{
					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && sLeaser.sprites.Length > magicaCWT.artiStartSprite)
					{
						if (magicaCWT.artiScarSprite != null && magicaCWT.headSprite != null)
						{
							if ((!magicaCWT.artiScarSprite.element.name.Contains(magicaCWT.headSprite.element.name) || !magicaCWT.artiScarSprite.element.name.Contains(spriteFace)) && magicaCWT.headSprite.element.name.Length == 6)
							{
								magicaCWT.artiScarSprite.SetElementByName(spriteFace + "Scar" + magicaCWT.headSprite.element.name);
							}
							CopyOverSpriteAttributes(magicaCWT.artiScarSprite, magicaCWT.headSprite);
							magicaCWT.artiScarSprite.scaleX = spriteScale;
						}

						if (magicaCWT.artiLegSprite != null && magicaCWT.legSprite != null)
						{
							if (!magicaCWT.artiLegSprite.element.name.Contains(magicaCWT.legSprite.element.name) || !magicaCWT.artiLegSprite.element.name.Contains(self.player.flipDirection == 1 ? "Right" : "Left"))
							{
								magicaCWT.artiLegSprite.SetElementByName((self.player.flipDirection == 1 ? "Right" : "Left") + "Scar" + magicaCWT.legSprite.element.name);
							}
							CopyOverSpriteAttributes(magicaCWT.artiLegSprite, magicaCWT.legSprite);
						}

						if (magicaCWT.artiTailSprite != null && magicaCWT.tailSprite != null)
						{
							Color baseColor = magicaCWT.tailSprite.color;

							magicaCWT.artiTailSprite.SetPosition(self.tail[3].pos - camPos);

							if (magicaCWT.tailSprite is TriangleMesh tailMesh)
							{
								magicaCWT.tailSprite.MoveBehindOtherNode(sLeaser.sprites[1]);
								tailMesh.customColor = true;
								if (tailMesh.verticeColors == null || tailMesh.verticeColors.Length != tailMesh.vertices.Length)
								{
									tailMesh.verticeColors = new Color[tailMesh.vertices.Length];
								}

								for (int j = tailMesh.verticeColors.Length - 1; j >= 0; j--)
								{
									if (j < (self.player.DreamState ? 12 : 9))
									{
										tailMesh.verticeColors[j] = baseColor;
									}
									else if (magicaCWT.colorTimer > 0f)
									{
										tailMesh.verticeColors[j] = Color.LerpUnclamped(magicaCWT.scarColor, Color.white, magicaCWT.colorTimer / magicaCWT.colorTimerMax);
									}
									else
									{
										tailMesh.verticeColors[j] = magicaCWT.scarColor;
									}
								}
								tailMesh.Refresh();

								magicaCWT.artiTailSprite.SetPosition(tailMesh.vertices[tailMesh.vertices.Length - 1]);
							}

							if (self.player.pyroJumpped && !magicaCWT.flashSet)
							{
								magicaCWT.colorTimer = magicaCWT.colorTimerMax;
								magicaCWT.flashSet = true;
							}
							if (magicaCWT.colorTimer == 0f && !self.player.pyroJumpped)
							{
								magicaCWT.flashSet = false;
								sLeaser.sprites[sLeaser.sprites.Length - 1].color = magicaCWT.scarColor;
							}
							if (magicaCWT.colorTimer > 0f)
							{
								magicaCWT.colorTimer--;
								if (!self.player.DreamState)
								{
									magicaCWT.artiTailSprite.color = Color.LerpUnclamped(magicaCWT.scarColor, Color.white, magicaCWT.colorTimer / magicaCWT.colorTimerMax);
								}
							}
						}
					}

					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && sLeaser.sprites.Length > magicaCWT.saintStartSprite && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(self.player, out var player))
					{
						if (!magicaCWT.saintScarSprite.element.name.Contains(magicaCWT.faceSprite.element.name))
						{
							magicaCWT.saintScarSprite.SetElementByName("Saint" + magicaCWT.faceSprite.element.name);
						}
						if (!magicaCWT.saintFatiqueSprite.element.name.Contains(magicaCWT.faceSprite.element.name))
						{
							magicaCWT.saintFatiqueSprite.SetElementByName("FatiqueSaint" + magicaCWT.faceSprite.element.name);
						}
						CopyOverSpriteAttributes(magicaCWT.saintScarSprite, magicaCWT.faceSprite);
						CopyOverSpriteAttributes(magicaCWT.saintFatiqueSprite, magicaCWT.faceSprite);

						if (magicaCWT.saintScarSprite != null)
						{
							magicaCWT.saintScarSprite.isVisible = true;

							magicaCWT.saintCreatureCircle.isVisible = ModOptions.CustomMechanics.Value && player.magicaSaintAscension && player.saintTarget != null;
							magicaCWT.saintCreatureKarma.isVisible = magicaCWT.saintCreatureCircle.isVisible;
							foreach (var line in magicaCWT.saintAcensionLines)
							{
								line.isVisible = magicaCWT.saintCreatureCircle.isVisible;
							}

							if ((ModOptions.CustomMechanics.Value && player.magicaSaintAscension) || self.player.monkAscension)
							{
								if (magicaCWT.saintGlowTimer == 0)
								{
									magicaCWT.originalColors = [
										sLeaser.sprites[0].color,
										sLeaser.sprites[1].color,
										sLeaser.sprites[2].color,
										sLeaser.sprites[3].color,
										sLeaser.sprites[4].color,
										sLeaser.sprites[5].color
										];
								}

								if (magicaCWT.saintGlowTimer < saintGlowTimerMax)
								{
									magicaCWT.saintGlowTimer++;
								}
								ChangeSaintBodyColors(sLeaser.sprites, magicaCWT.originalColors, self, magicaCWT);

								if (magicaCWT.faceSprite != null)
								{
									if (player.zoneInToAscend)
									{
										magicaCWT.faceSprite.color = Color.white;
									}
									else
									{
										magicaCWT.faceSprite.color = UnityEngine.Random.value > 0.3f ? RainWorld.SaturatedGold : Color.cyan;
									}

									if (ModOptions.CustomMechanics.Value && player.ascensionBuffer > 0f)
									{
										magicaCWT.saintScarSprite.color = Color.Lerp(RainWorld.SaturatedGold, Color.red, player.ascensionBuffer / PlayerHooks.maxAscensionBuffer);
									}
									else
									{
										magicaCWT.saintScarSprite.color = Color.Lerp(RainWorld.SaturatedGold, Color.cyan, ModOptions.CustomMechanics.Value ? player.ascendTimer / 1f : self.player.killFac / 1f);
									}

									if (!ModOptions.CustomMechanics.Value)
									{
										sLeaser.sprites[14].color = magicaCWT.saintScarSprite.color;

										for (int m = 0; m < self.numGodPips; m++)
										{
											if (self.player.karmaCharging > 0)
											{
												sLeaser.sprites[15 + m].color = magicaCWT.faceSprite.color;
											}
											else
											{
												sLeaser.sprites[15 + m].color = PlayerGraphics.SlugcatColor(self.CharacterForColor);
											}
										}
									}
									else
									{
										magicaCWT.saintCreatureCircle.scale = Custom.LerpCircEaseOut(0.5f, 0.7f, player.ascendTimer / 1f);
										magicaCWT.saintCreatureKarma.scale = Custom.LerpCircEaseOut(0.5f, 0.55f, player.ascendTimer / 1f);

										if (player.changingTarget > 0f)
										{
											player.changingTarget = Mathf.Max(player.changingTarget - 1f, 0f);
										}

										if (player.saintTarget != null)
										{
											if (player.karmaCycleTimer > 0f)
											{
												player.karmaCycleTimer = Mathf.Max (player.karmaCycleTimer - 1f, 0f);
											}
											if (!player.karmaCycling || (player.karmaCycling && player.karmaCycleTimer <= 0f) || player.ascendTimer > 0f)
											{
												magicaCWT.saintCreatureKarma.SetElementByName(GetCreatureKarma(player.saintTarget, player, self.player.room.game.IsStorySession && self.player.room.world.name == "HR"));
											}

											float alphaLerp = Custom.LerpExpEaseInOut(0f, 1f, (PlayerHooks.saintRadius - Custom.Dist(self.player.firstChunk.pos, player.saintTargetPos)) / PlayerHooks.saintRadius);
											magicaCWT.saintCreatureCircle.alpha = (magicaCWT.saintGlowTimer < saintGlowTimerMax) ? alphaLerp * Custom.LerpBackEaseIn(0f, 1f, magicaCWT.saintGlowTimer / saintGlowTimerMax) : player.changingTarget > 0f ? alphaLerp * Custom.LerpBackEaseIn(0f, 1f, player.changingTarget / 20f) : alphaLerp;
											magicaCWT.saintCreatureKarma.alpha = magicaCWT.saintCreatureCircle.alpha;
											magicaCWT.saintCreatureCircle.SetPosition(player.saintTargetPos - rCam.pos);
											magicaCWT.saintCreatureKarma.SetPosition(magicaCWT.saintCreatureCircle.GetPosition());
										}

										magicaCWT.saintCreatureCircle.color = magicaCWT.saintScarSprite.color;
										magicaCWT.saintCreatureKarma.color = player.saintTargetIsKarmaLocked ? Color.Lerp(Color.white, Color.red, Mathf.Sin(player.activationTimer / 5f) - (player.ascendTimer / 1f)) : Color.white;
										for (int i = 0; i < magicaCWT.saintAcensionLines.Length; i++)
										{
											FSprite line = magicaCWT.saintAcensionLines[i];

											line.color = magicaCWT.saintScarSprite.color;
											line.alpha = magicaCWT.saintCreatureCircle.alpha;

											//float numOfRotations = 5f;
											float rotationLerp = (player.activationTimer % 50f) / 50f;
											Vector2 facePos = GetFacePos(magicaCWT.saintScarSprite, i);
											Vector2 circlePos = Custom.RotateAroundVector(magicaCWT.saintCreatureCircle.GetPosition() + (new Vector2(magicaCWT.saintCreatureCircle.width, 0f) / 2f), magicaCWT.saintCreatureCircle.GetPosition(), Mathf.Lerp(0, 360f, (float)(i + 1) / 4f) + Mathf.Lerp(-45f, 315f, rotationLerp));

											line.SetPosition(facePos);
											line.scaleY = Custom.Dist(facePos, circlePos);
											line.rotation = Custom.AimFromOneVectorToAnother(facePos, circlePos);
										}
									}
								}
							}
							else
							{
								if (magicaCWT.saintGlowTimer > 0)
								{
									magicaCWT.saintGlowTimer--;
									ChangeSaintBodyColors(sLeaser.sprites, magicaCWT.originalColors, self, magicaCWT);
								}

								if (rCam.ghostMode > 0f)
								{
									magicaCWT.saintScarSprite.color = Color.Lerp(magicaCWT.scarColor, RainWorld.SaturatedGold, rCam.ghostMode);
								}
								else if (player.ascensionFatique > 0f)
								{
									magicaCWT.saintScarSprite.color = Color.Lerp(magicaCWT.scarColor, Color.red, player.ascensionFatique / PlayerHooks.ascensionFatique);
									magicaCWT.saintFatiqueSprite.color = magicaCWT.saintScarSprite.color;
									magicaCWT.saintFatiqueSprite.alpha = Mathf.Lerp(0f, 1f, player.ascensionFatique / PlayerHooks.ascensionFatique);
								}
								else
								{
									magicaCWT.saintScarSprite.color = magicaCWT.scarColor;
									magicaCWT.saintFatiqueSprite.alpha = 0f;
								}
							}
						}
					}
				}

				if (self.player.SlugCatClass == SlugcatStats.Name.Red && sLeaser.sprites.Length > magicaCWT.hunterStartSprite)
				{
					if (magicaCWT.hunterFaceSprite != null && magicaCWT.faceSprite != null)
					{
						CopyOverSpriteAttributes(magicaCWT.hunterFaceSprite, magicaCWT.faceSprite);
					}
					if (magicaCWT.hunterHipSprite != null && magicaCWT.hipSprite != null)
					{
						CopyOverSpriteAttributes(magicaCWT.hunterHipSprite, magicaCWT.hipSprite);
					}
					if (magicaCWT.hunterLegSprite != null && magicaCWT.legSprite != null)
					{
						CopyOverSpriteAttributes(magicaCWT.hunterLegSprite, magicaCWT.legSprite);
					}
					if (magicaCWT.hunterBodySprite != null && magicaCWT.bodySprite != null)
					{
						CopyOverSpriteAttributes(magicaCWT.hunterBodySprite, magicaCWT.bodySprite);
					}
					if (magicaCWT.hunterTailSprite != null && magicaCWT.hunterTailTipSprite != null && magicaCWT.tailSprite != null)
					{
						if (sLeaser.sprites[2] is TriangleMesh tailMesh)
						{
							if (spriteFace == "Left")
							{
								magicaCWT.hunterTailSprite.scaleX = -1f;
							}
							else
							{
								magicaCWT.hunterTailSprite.scaleX = 1f;
							}

							float rotatte = Custom.AimFromOneVectorToAnother(new(tailMesh.vertices[tailMesh.vertices.Length - 1].x, tailMesh.vertices[tailMesh.vertices.Length - 1].y), new(tailMesh.vertices[0].x, tailMesh.vertices[0].y));
							magicaCWT.hunterTailSprite.x = Mathf.Lerp(tailMesh.vertices[tailMesh.vertices.Length / 2].x, sLeaser.sprites[1].x, 0.5f);
							magicaCWT.hunterTailSprite.y = Mathf.Lerp(tailMesh.vertices[tailMesh.vertices.Length / 2].y, sLeaser.sprites[1].y, 0.5f);
							magicaCWT.hunterTailSprite.rotation = rotatte;

							magicaCWT.hunterTailTipSprite.x = Mathf.Lerp(tailMesh.vertices[tailMesh.vertices.Length - 4].x, sLeaser.sprites[1].x, 0.2f);
							magicaCWT.hunterTailTipSprite.y = Mathf.Lerp(tailMesh.vertices[tailMesh.vertices.Length - 4].y, sLeaser.sprites[1].y, 0.2f);
							magicaCWT.hunterTailTipSprite.rotation = rotatte + 90f;
						}
					}
				}

				if (magicaCWT.braids != null && magicaCWT.braids.Length > 0 && sLeaser.sprites.Length > magicaCWT.braidStartSprite)
				{
					for (int i = 0; i < magicaCWT.braids.Length; i++)
					{
						FSprite braid = sLeaser.sprites[magicaCWT.braidStartSprite + i];
						int lengthOfRow = magicaCWT.braids.Length / magicaCWT.braidRows;
						float lerp = 1f - ((((float)(i - magicaCWT.braids.IndexOf(magicaCWT.headBraids[magicaCWT.braids[i].row])) / (float)lengthOfRow) + 0.2f) * 0.8f);

						braid.SetPosition(Vector2.Lerp(magicaCWT.lastBraidPos[i], Vector2.Lerp(magicaCWT.braids[i].pos - camPos, magicaCWT.headSprite.GetPosition(), lerp), 0.8f));
						magicaCWT.lastBraidPos[i] = braid.GetPosition();

						if (i % (magicaCWT.braids.Length / magicaCWT.braidRows) == 0)
						{
							braid.rotation = Custom.AimFromOneVectorToAnother(braid.GetPosition(), magicaCWT.headSprite.GetPosition());
						}
						else
						{
							braid.rotation = Custom.AimFromOneVectorToAnother(braid.GetPosition(), sLeaser.sprites[magicaCWT.braidStartSprite + i - 1].GetPosition());
							braid.scaleY = Mathf.Lerp(1f, 2.2f, magicaCWT.braids[i].vel.magnitude / 6.5f);
						}

						bool flipped = self.player.flipDirection == -1f;
						float lerpAgain = GetBraidLerp(magicaCWT, (float)magicaCWT.braids[i].row, flipped);
						braid.color = Color.Lerp(magicaCWT.braidColor, rCam.currentPalette.blackColor, lerpAgain);

						braid.MoveBehindOtherNode(sLeaser.sprites[0]);
						if (flipped && i != 0)
						{
							braid.MoveBehindOtherNode(sLeaser.sprites[magicaCWT.braidStartSprite + i - 1]);
						}

						if (ModManager.MSC && rCam.game.IsStorySession && rCam.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && magicaCWT.headSprite != null)
						{
							magicaCWT.braidColor = magicaCWT.headSprite.color;
						}
					}
				}
			}
		}

		private static float GetBraidLerp(MagicaSprites magicaCWT, float row, bool flipped)
		{
			return ((flipped ? row / (float)magicaCWT.braidRows : (float)(magicaCWT.braidRows - row) / (float)magicaCWT.braidRows) - (1f / (float)magicaCWT.braidRows)) * 0.3f;
		}

		private static Vector2 GetFacePos(FSprite scar, int i)
		{
			string element = scar.element.name;
			Vector2 centralPos = scar.GetPosition();

			if (element.Length > 0 && element.Contains("Face") && int.TryParse(element.Substring(element.IndexOf("Face") + 5), out int faceNum))
			{
				Vector2 offset = new();
				switch (faceNum)
				{
					case 0:
						offset = new Vector2(0f, 10f) / 2f;
						break;

					case 1:
						offset = new Vector2(2f, 9f) / 2f;
						break;

					case 2:
						offset = new Vector2(3f, 8f) / 2f;
						break;

					case 3:
						offset = new Vector2(6f, 5f) / 2f;
						break;

					case 4:
						offset = new Vector2(7f, 7f) / 2f;
						break;

					case 5:
						offset = new Vector2(3f, 4f) / 2f;
						break;

					case 6:
						offset = new Vector2(4f, 2f) / 2f;
						break;

					case 7:
						offset = new Vector2(2f, 4f) / 2f;
						break;

					case 8:
						offset = new Vector2(0f, 4f) / 2f;
						break;
				}
				offset.x *= scar.scaleX;
				centralPos += offset;
			}
			Vector2 cornerPos = Custom.RotateAroundVector(centralPos + new Vector2(3f, 0f), centralPos, Mathf.Lerp(-45f, 315f, (float)(i + 1) / 4f));

			return cornerPos;
		}

		private static string GetCreatureKarma(PhysicalObject entity, PlayerHooks.MagicaPlayer player, bool isHR)
		{
			int karma = 0;

			if (entity != null)
			{
				if (entity is SeedCob)
				{
					karma = 3;
				}

				if (entity is Creature creature)
				{
					CreatureTemplate.Type type = creature.abstractCreature.creatureTemplate.type;
					karma = GetKarmaOfSpecificCreature(creature, type);
					player.saintTargetIsKarmaLocked = ApexPredatorsIncludes(type);
					player.karmaCycling = type == CreatureTemplate.Type.DaddyLongLegs || type == CreatureTemplate.Type.BrotherLongLegs || type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs;
					player.karmaCycleTimer = type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs ? 5f : type == CreatureTemplate.Type.DaddyLongLegs ? 10f : 20f;
				}

				if (entity is Oracle oracle)
				{
					if (oracle.ID == Oracle.OracleID.SL)
					{
						karma = 8;
					}
					if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
					{
						karma = 5;
					}
				}
			}

			if (isHR)
			{
				karma = Math.Min(karma + 5, 9);
			}

			if (player.ascendTimer > 0f && karma != 9)
			{
				karma = Mathf.RoundToInt(Mathf.Lerp((float)karma, 9f, player.ascendTimer / 1f));
			}

			return "SaintKarmaRing" + karma.ToString();
		}

		private static bool ApexPredatorsIncludes(CreatureTemplate.Type type)
		{
			return type == CreatureTemplate.Type.RedLizard || type == CreatureTemplate.Type.KingVulture || type == CreatureTemplate.Type.RedCentipede || type == MoreSlugcatsEnums.CreatureTemplateType.MirosVulture;
		}

		private static int GetKarmaOfSpecificCreature(Creature creature, CreatureTemplate.Type type)
		{
			if (creatureKarma.ContainsKey(type))
			{
				return creatureKarma[type];
			}

			if (type == CreatureTemplate.Type.Scavenger || type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite || type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing)
			{
				return creature.abstractCreature.karmicPotential;
			}

			if (type == CreatureTemplate.Type.DaddyLongLegs || type == CreatureTemplate.Type.BrotherLongLegs || type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs)
			{
				return UnityEngine.Random.Range(0, 9 - (type == CreatureTemplate.Type.DaddyLongLegs ? 4 : type == CreatureTemplate.Type.BrotherLongLegs ? 7 : 0));
			}

			return 0;
		}

		public static Dictionary<CreatureTemplate.Type, int> creatureKarma = new()
		{
			{ CreatureTemplate.Type.StandardGroundCreature, 0 },
			{ CreatureTemplate.Type.LizardTemplate, 0 },
			{ CreatureTemplate.Type.PinkLizard, 0 },
			{ CreatureTemplate.Type.GreenLizard, 3 },
			{ CreatureTemplate.Type.BlueLizard, 4 },
			{ CreatureTemplate.Type.YellowLizard, 2 },
			{ CreatureTemplate.Type.WhiteLizard, 4 },
			{ CreatureTemplate.Type.RedLizard, 0 },
			{ CreatureTemplate.Type.BlackLizard, 5 },
			{ CreatureTemplate.Type.Salamander, 2 },
			{ CreatureTemplate.Type.CyanLizard, 3 },
			{ CreatureTemplate.Type.Fly, 5 },
			{ CreatureTemplate.Type.Leech, 3 },
			{ CreatureTemplate.Type.SeaLeech, 2 },
			{ CreatureTemplate.Type.Snail, 4 },
			{ CreatureTemplate.Type.Vulture, 1 },
			{ CreatureTemplate.Type.GarbageWorm, 4 },
			{ CreatureTemplate.Type.LanternMouse, 5 },
			{ CreatureTemplate.Type.CicadaA, 1 },
			{ CreatureTemplate.Type.CicadaB, 2 },
			{ CreatureTemplate.Type.Spider, 1 },
			{ CreatureTemplate.Type.JetFish, 4 },
			{ CreatureTemplate.Type.BigEel, 3 },
			{ CreatureTemplate.Type.Deer, 7 },
			{ CreatureTemplate.Type.TubeWorm, 6 },
			{ CreatureTemplate.Type.TentaclePlant, 7 },
			{ CreatureTemplate.Type.PoleMimic, 8 },
			{ CreatureTemplate.Type.MirosBird, 2 },
			{ CreatureTemplate.Type.TempleGuard, 9 },
			{ CreatureTemplate.Type.Centipede, 1 },
			{ CreatureTemplate.Type.RedCentipede, 0 },
			{ CreatureTemplate.Type.Centiwing, 3 },
			{ CreatureTemplate.Type.SmallCentipede, 4 },
			{ CreatureTemplate.Type.Overseer, 7 },
			{ CreatureTemplate.Type.VultureGrub, 5 },
			{ CreatureTemplate.Type.EggBug, 1 },
			{ CreatureTemplate.Type.BigSpider, 3 },
			{ CreatureTemplate.Type.SpitterSpider, 0 },
			{ CreatureTemplate.Type.SmallNeedleWorm, 4 },
			{ CreatureTemplate.Type.BigNeedleWorm, 1 },
			{ CreatureTemplate.Type.DropBug, 3 },
			{ CreatureTemplate.Type.KingVulture, 0 },
			{ CreatureTemplate.Type.Hazer, 4 },
			{ MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, 0 },
			{ MoreSlugcatsEnums.CreatureTemplateType.SpitLizard, 3 },
			{ MoreSlugcatsEnums.CreatureTemplateType.EelLizard, 0 },
			{ MoreSlugcatsEnums.CreatureTemplateType.MotherSpider, 1 },
			{ MoreSlugcatsEnums.CreatureTemplateType.AquaCenti, 0 },
			{ MoreSlugcatsEnums.CreatureTemplateType.FireBug, 8 },
			{ MoreSlugcatsEnums.CreatureTemplateType.StowawayBug, 3 },
			{ MoreSlugcatsEnums.CreatureTemplateType.Inspector, 4 },
			{ MoreSlugcatsEnums.CreatureTemplateType.Yeek, 4 },
			{ MoreSlugcatsEnums.CreatureTemplateType.BigJelly, 3 },
			{ MoreSlugcatsEnums.CreatureTemplateType.JungleLeech, 1 },
			{ MoreSlugcatsEnums.CreatureTemplateType.ZoopLizard, 4 },
			{ MoreSlugcatsEnums.CreatureTemplateType.TrainLizard, 0 },
		};

		private static void ChangeSaintBodyColors(FSprite[] sprites, Color[] originalColors, PlayerGraphics graphics, MagicaSprites magicaCWT)
		{
			Color blackColor = new(0.21f, 0.2f, 0.2f);
			float lerp = Mathf.Min((magicaCWT.saintGlowTimer / saintGlowTimerMax) - 0.3f, 1f);
			if (graphics.player != null && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(graphics.player, out var player) && player.activationTimer > 0f && magicaCWT.saintGlowTimer >= saintGlowTimerMax)
			{
				lerp = Mathf.Min((player.activationTimer / PlayerHooks.maxActivationTimer) - 0.3f, 1f);
			}

			sprites[0].color = Color.Lerp(originalColors[0], blackColor, lerp);
			sprites[1].color = Color.Lerp(originalColors[1], blackColor, lerp);
			sprites[2].color = Color.Lerp(originalColors[2], blackColor, lerp);
			sprites[3].color = Color.Lerp(originalColors[3], blackColor, lerp);
			sprites[4].color = Color.Lerp(originalColors[4], blackColor, lerp);
			for (int i = 0; i < 2; i++)
			{
				sprites[5 + i].color = Color.Lerp(originalColors[5], blackColor, lerp);
				sprites[7 + i].color = Color.Lerp(originalColors[5], blackColor, lerp);
			}
		}

		private static void CopyOverSpriteAttributes(FSprite copyTo, FSprite copyFrom)
		{
			if (copyTo != null && copyFrom != null)
			{
				copyTo.SetPosition(copyFrom.GetPosition());
				copyTo.SetAnchor(copyFrom.GetAnchor());
				copyTo.scaleX = copyFrom.scaleX;
				copyTo.scaleY = copyFrom.scaleY;
				copyTo.rotation = copyFrom.rotation;
			}
		}

		private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			if (ModOptions.CustomGraphics.Value && MagicaSprites.magicaCWT.TryGetValue(self, out var magicaCWT))
			{
				if (magicaCWT.braids != null && magicaCWT.braids.Length > 0 && sLeaser.sprites.Length > magicaCWT.braidStartSprite)
				{
					magicaCWT.braidColor = GetPossibleColors(self);
					if (rCam.game.IsArenaSession)
					{
						magicaCWT.braidColor = sLeaser.sprites[3].color;
					}
					for (int i = magicaCWT.braidStartSprite; i < sLeaser.sprites.Length; i++)
					{
						sLeaser.sprites[i].color = magicaCWT.braidColor;
					}
				}

				if (ModManager.MSC)
				{
					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && sLeaser.sprites.Length > magicaCWT.artiStartSprite)
					{
						magicaCWT.scarColor = GetScarColor(self, rCam);
						if (sLeaser.sprites.Length >= 12 && !self.player.DreamState)
						{
							sLeaser.sprites[12].color = magicaCWT.scarColor;
						}

						if (magicaCWT.artiScarSprite != null)
						{
							magicaCWT.artiScarSprite.color = magicaCWT.scarColor;
						}
						if (magicaCWT.artiLegSprite != null)
						{
							magicaCWT.artiLegSprite.color = magicaCWT.scarColor;
						}
						if (magicaCWT.artiTailSprite != null)
						{
							magicaCWT.artiTailSprite.color = magicaCWT.scarColor;
						}
					}

					if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && sLeaser.sprites.Length > magicaCWT.saintStartSprite)
					{
						magicaCWT.originalColors = [
							sLeaser.sprites[0].color,
							sLeaser.sprites[1].color,
							sLeaser.sprites[2].color,
							sLeaser.sprites[3].color,
							sLeaser.sprites[4].color,
							sLeaser.sprites[5].color
							];

						magicaCWT.scarColor = GetScarColor(self, rCam);
						if (magicaCWT.saintScarSprite != null && magicaCWT.headSprite != null && !rCam.game.IsArenaSession && rCam.game.GetStorySession.saveState != null)
						{
							magicaCWT.saintScarSprite.color = magicaCWT.scarColor;
							magicaCWT.saintScarSprite.alpha = rCam.game.IsStorySession || (rCam.game.session is ArenaGameSession && rCam.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge && rCam.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeMeta.ascended) ? (float)(rCam.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap + 1) / 10f : 0f;
							magicaCWT.saintFatiqueSprite.color = magicaCWT.scarColor;
							magicaCWT.saintFatiqueSprite.alpha = 0f;
						}
						if (magicaCWT.saintAcensionLines != null && magicaCWT.saintCreatureCircle != null)
						{
							foreach (var sprite in magicaCWT.saintAcensionLines)
							{
								sprite.color = RainWorld.SaturatedGold;
							}
							magicaCWT.saintCreatureCircle.color = RainWorld.SaturatedGold;
						}

						if (sLeaser.sprites[12] is TriangleMesh tongueMesh)
						{
							for (var i = 0; i < tongueMesh.verticeColors.Length; i++)
							{
								tongueMesh.verticeColors[i] = magicaCWT.scarColor;
							}
						}
					}
				}

				if (self.player.SlugCatClass == SlugcatStats.Name.Red && sLeaser.sprites.Length > magicaCWT.hunterStartSprite)
				{
					for (int i = 0; i < 6; i++)
					{
						sLeaser.sprites[magicaCWT.hunterStartSprite + i].color = Color.Lerp(Color.white, sLeaser.sprites[3].color, 0.5f);
					}
					Color baseColor = customColorDict[CustomColorValues.HunterArm];

					if (self.useJollyColor)
					{
						baseColor = PlayerGraphics.JollyColor(self.player.playerState.playerNumber, 2);
					}
					if (PlayerGraphics.CustomColorsEnabled())
					{
						baseColor = PlayerGraphics.CustomColorSafety(2);
					}

					sLeaser.sprites[6].color = baseColor;
					sLeaser.sprites[8].color = baseColor;
				}
			}
		}

		private static Color GetScarColor(PlayerGraphics self, RoomCamera rCam)
		{
			Color scarColor = new(0.27059f, 0.15686f, 0.23529f);

			if (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				scarColor = customColorDict[CustomColorValues.SaintTongue];
			}

			if (rCam.room.game.IsArenaSession && !rCam.room.game.setupValues.arenaDefaultColors)
			{
				switch (self.player.playerState.playerNumber)
				{
					case 0:
						if (rCam.room.game.IsArenaSession && rCam.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType != MoreSlugcatsEnums.GameTypeID.Challenge)
						{
							scarColor = new(0.78f, 0.604f, 0.604f);
						}
						break;
					case 1:
						scarColor = new(0.682f, 0.49f, 0.404f);
						break;
					case 2:
						scarColor = new(0.643f, 0.263f, 0.361f);
						break;
					case 3:
						scarColor = new(0.259f, 0.09f, 0.129f);
						break;
				}
			}

			if (PlayerGraphics.CustomColorsEnabled())
			{
				scarColor = PlayerGraphics.CustomColorSafety(2);
			}
			scarColor = Color.Lerp(scarColor, self.HypothermiaColorBlend(scarColor), 0.6f);
			return scarColor;
		}

		private static Color GetPossibleColors(PlayerGraphics self)
		{
			if (self != null)
			{
				if (Plugin.isDMSEnabled && TryGetDMSColor(self, "HEAD", out var color))
				{
					return color;
				}
				else
				{
					return PlayerGraphics.SlugcatColor(self.CharacterForColor);
				}
			}
			return new(1f, 1f, 1f);
		}

		private static bool TryGetDMSColor(PlayerGraphics self, string v, out Color color)
		{
			if (Customization.For(self.player) != null && Customization.For(self.player).CustomSprite(v) != null && Customization.For(self.player).CustomSprite(v).Color != default)
			{
				color = Customization.For(self.player).CustomSprite(v).Color;
				return true;
			}
			color = PlayerGraphics.SlugcatColor(self.CharacterForColor);
			return false;
		}

		public enum DMSCheck
		{
			IsNotEmpty,
			FaceLift
		}

		private static bool CheckForDMS(PlayerGraphics self, string spriteName, DMSCheck check)
		{
			if (!Plugin.isDMSEnabled && ModOptions.CustomGraphics.Value)
			{
				return true;
			}
			if (self != null && Plugin.isDMSEnabled)
			{
				return CautionDMSCheck(self, spriteName, check);
			}
			return false;
		}

		private static bool CautionDMSCheck(PlayerGraphics self, string spriteName, DMSCheck check)
		{
			return check switch
			{
				DMSCheck.IsNotEmpty => Customization.For(self.player, true) != null && Customization.For(self.player, true).CustomSprite(spriteName) != null && !Customization.For(self.player, true).CustomSprite(spriteName).SpriteSheetID.ToLowerInvariant().Contains("empty"),
				DMSCheck.FaceLift => Customization.For(self.player, true) != null && Customization.For(self.player, true).CustomSprite(spriteName) != null && MagicaSprites.GetKey(Customization.For(self.player, true).CustomSprite(spriteName).SpriteSheetID) != null && MagicaSprites.GetKey(Customization.For(self.player, true).CustomSprite(spriteName).SpriteSheetID).faceLift,
				_ => false,
			};
		}

		private static void TailSegment_Update(On.TailSegment.orig_Update orig, TailSegment self)
		{
			if (ModOptions.CustomGraphics.Value && self.owner is PlayerGraphics pGraphics && self.connectedSegment == null && Plugin.IsVanillaSlugcat((self.owner as PlayerGraphics).player.SlugCatClass) && CheckForDMS(pGraphics, "HEAD", DMSCheck.FaceLift))
			{
				self.connectedPoint = Vector2.Lerp(pGraphics.drawPositions[1, 0], pGraphics.drawPositions[0, 0], .3f);
			}

			orig(self);
		}

		private static void CosmeticPearl_InitiateSprites(On.PlayerGraphics.CosmeticPearl.orig_InitiateSprites orig, PlayerGraphics.CosmeticPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);

			if (CheckForDMS(self.pGraphics, "HEAD", DMSCheck.FaceLift))
			{
				sLeaser.sprites[self.startSprite].anchorY = -0.6f;
			}
			else
			{
				sLeaser.sprites[self.startSprite].anchorX = 0.5f;
				sLeaser.sprites[self.startSprite].anchorY = 0.5f;
			}
			sLeaser.sprites[self.startSprite + 1].anchorX = sLeaser.sprites[self.startSprite].anchorX;
			sLeaser.sprites[self.startSprite + 1].anchorY = sLeaser.sprites[self.startSprite].anchorY;

			sLeaser.sprites[self.startSprite + 2].anchorX = sLeaser.sprites[self.startSprite].anchorX;
			sLeaser.sprites[self.startSprite + 2].anchorY = sLeaser.sprites[self.startSprite].anchorY;
		}

	}

	public class MagicaSprites
	{
		public static readonly ConditionalWeakTable<PlayerGraphics, MagicaSprites> magicaCWT = new();

		public float colorTimer;
		public float colorTimerMax = 15f;
		public bool flashSet;
		public Braids[] braids;

		public static Dictionary<string, MagicaDMSThings> qualifiedForChange = [];
		internal int braidRows;
		internal int braidStartSprite;
		internal Braids[] headBraids;
		internal FSprite headSprite;
		internal Color braidColor;
		internal FSprite faceSprite;
		internal Vector2 headSpritePos;
		internal Vector2 faceSpritePos;
		internal int artiStartSprite;
		internal FSprite artiTailSprite;
		internal FSprite artiLegSprite;
		internal FSprite artiScarSprite;
		internal FSprite legSprite;
		internal FSprite tailSprite;
		internal Color scarColor;
		internal int hunterStartSprite;
		internal FSprite hunterFaceSprite;
		internal FSprite hunterHipSprite;
		internal FSprite hunterLegSprite;
		internal FSprite hunterBodySprite;
		internal FSprite hunterTailSprite;
		internal FSprite hunterTailTipSprite;
		internal FSprite hipSprite;
		internal FSprite bodySprite;
		internal int saintStartSprite;
		internal FSprite saintScarSprite;
		internal FSprite[] saintAcensionLines;
		internal FSprite saintCreatureCircle;
		internal Color[] originalColors;
		internal float saintGlowTimer;
		internal FSprite saintFatiqueSprite;
		internal FSprite saintCreatureKarma;
		internal Vector2[] lastBraidPos;

		public class MagicaDMSThings
		{
			public bool faceLift;

			public MagicaDMSThings() { }
		}

		public static MagicaDMSThings GetKey(string key)
		{
			if (qualifiedForChange != null && qualifiedForChange.Count > 0)
			{
				if (qualifiedForChange.ContainsKey(key))
				{
					return qualifiedForChange[key];
				}
				else
				{
					qualifiedForChange.Add(key, new());
					return GetKey(key);
				}
			}
			else
			{
				qualifiedForChange = [];
				qualifiedForChange.Add(key, new());
				return qualifiedForChange[key];
			}
		}
	}

	public class Braids : BodyPart
	{
		public Braids connectedSegment;
		public float stretched;
		public int row;
		public float connectionRad;
		public float affectPrevious;
		public bool pullInPreviousPosition;
		public Vector2? connectedPoint;


		public Braids(GraphicsModule ow, int row, float rad, float connectionRad, Braids joinedSeg, float sfFric, float aFric) : base(ow)
		{
			base.rad = rad;
			this.row = row;
			this.connectionRad = connectionRad;
			surfaceFric = sfFric;
			airFriction = aFric;
			connectedSegment = joinedSeg;
			connectedPoint = null;
			Reset(owner.owner.bodyChunks[0].pos);
		}

		public override void Update()
		{
			lastPos = pos;
			pos += vel;
			vel *= airFriction;
			stretched = 1f;
			if (connectedSegment != null)
			{
				if (!Custom.DistLess(pos, connectedSegment.pos, connectionRad))
				{
					float num = Vector2.Distance(pos, connectedSegment.pos) - (UnityEngine.Random.value * 0.5f);
					Vector2 a = Custom.DirVec(pos, connectedSegment.pos);
					pos -= (connectionRad - num) * a * (1f - affectPrevious);
					vel -= (connectionRad - num) * a * (1f - affectPrevious);
					if (pullInPreviousPosition)
					{
						connectedSegment.pos += (connectionRad - num) * a * affectPrevious;
					}
					connectedSegment.vel += (connectionRad - num) * a * affectPrevious;
					stretched = Mathf.Clamp((connectionRad / (num * 0.5f) + 2f) / 3f, 0.2f, 3f);
				}
			}

			else if (connectedPoint != null && !Custom.DistLess(pos, connectedPoint.Value, connectionRad))
			{
				float num2 = Vector2.Distance(pos, connectedPoint.Value);
				Vector2 a2 = Custom.DirVec(pos, connectedPoint.Value);
				pos -= (connectionRad - num2) * a2 * 1f;
				vel -= (connectionRad - num2) * a2 * 1f;
				stretched = Mathf.Clamp((connectionRad / (num2 * 0.5f) + 2f) / 3f, 0.2f, 1f);
			}
			if (connectedSegment != null)
			{
				PushOutOfTerrain(owner.owner.room, connectedSegment.pos);
				return;
			}
			if (connectedPoint != null)
			{
				PushOutOfTerrain(owner.owner.room, connectedPoint.Value);
			}
		}
	}
}
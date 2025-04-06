using DressMySlugcat;
using DressMySlugcat.Hooks;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static MagicasContentPack.MagicaSprites;

namespace MagicasContentPack
{
	internal class DMSHooks
	{

		// This is in a method in case the dependancy isn't enabled, so the assembly doesn't shit itself
		public static void ApplyDMSHooks()
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

						if (customSprite != null && GetKey(customSprite.SpriteSheetID) != null && GetKey(customSprite.SpriteSheetID).faceLift)
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

							if (json != null && json.TryGetValue("taller", out object faceLift) && GetKey(id) != null)
							{
								string face = faceLift.ToString();
								GetKey(id).faceLift = bool.TryParse(face, out _);
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
			//					qualifiedForChange.Add(id, bool.TryParse(face, out _));
			//					Plugin.DebugLog(qualifiedForChange.TryGetValue(id, out _).ToString());
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

		public enum DMSCheck
		{
			IsNotEmpty,
			FaceLift
		}

		public static bool CheckForDMS(PlayerGraphics self, string spriteName, DMSCheck check)
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
				DMSCheck.FaceLift => Customization.For(self.player, true) != null && Customization.For(self.player, true).CustomSprite(spriteName) != null && GetKey(Customization.For(self.player, true).CustomSprite(spriteName).SpriteSheetID) != null && GetKey(Customization.For(self.player, true).CustomSprite(spriteName).SpriteSheetID).faceLift,
				_ => false,
			};
		}

		public static bool TryGetDMSColor(PlayerGraphics self, string v, out Color color)
		{
			if (Customization.For(self.player) != null && Customization.For(self.player).CustomSprite(v) != null && Customization.For(self.player).CustomSprite(v).Color != default)
			{
				color = Customization.For(self.player).CustomSprite(v).Color;
				return true;
			}
			color = PlayerGraphics.SlugcatColor(self.CharacterForColor);
			return false;
		}


		public static Dictionary<string, MagicaDMSThings> qualifiedForChange = [];



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
}
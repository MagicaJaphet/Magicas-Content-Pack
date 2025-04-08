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
		public static void LoadDMSConfigs()
		{
			//Log("DMS configs loaded");

			//SpriteDefinitions.AvailableSprites.Add(new SpriteDefinitions.AvailableSprite
			//{
			//	Name = "ARTIEXTRAS",
			//	Description = "Arti Extras",
			//	GallerySprite = "ScarHeadA0",
			//	RequiredSprites = new List<string>
			//{
			//	"TailPuff",

			//	"ScarHeadA0",
			//	"ScarHeadA1",
			//	"ScarHeadA2",
			//	"ScarHeadA3",
			//	"ScarHeadA4",
			//	"ScarHeadA5",
			//	"ScarHeadA6",
			//	"ScarHeadA7",
			//	"ScarHeadA8",
			//	"ScarHeadA9",
			//	"ScarHeadA10",
			//	"ScarHeadA11",
			//	"ScarHeadA12",
			//	"ScarHeadA13",
			//	"ScarHeadA14",
			//	"ScarHeadA15",
			//	"ScarHeadA16",
			//	"ScarHeadA17",

			//	"ScarLegsA0"
			//},
			//	Slugcats = new List<string>
			//{
			//	"Artificer"
			//}
			//});

			//SpriteSheet.Get("magica.artificer").ParseAtlases();

			//SpriteDefinitions.AvailableSprites.Add(new SpriteDefinitions.AvailableSprite
			//{
			//	Name = "BRAIDS",
			//	Description = "Braids",
			//	GallerySprite = "Braid",
			//	RequiredSprites = new List<string>
			//{
			//	"Braid"
			//},
			//	Slugcats = new List<string>
			//{
			//	"Spear"
			//}
			//});
		}

		// This is in a method in case the dependancy isn't enabled, so the assembly doesn't shit itself
		public static void ApplyDMSHooks()
		{
			try
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

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
			
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
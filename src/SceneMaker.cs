using Menu;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MagicasContentPack
{
	using static MagicasContentPack.MagicaEnums;
	using static MagicasContentPack.SceneMaker.SpearScenes;
	using SlideShowID = SlideShow.SlideShowID;

	public class SceneMaker
	{
		// Used for sleep screen
		public static int sleepPalette;
		public static int sleepFadePalette;
		public static float sleepFadeAmount;
		public static Vector2 shelterDirection;
		public static Texture2D paletteTexture;
		public static Texture2D fadePaletteTexture;

		// Class variables
		private static MenuScene menuScene;
		private static string sceneFolder;
		private static bool flat = false;
		private static List<float> idleDepths;

		private static List<MenuIllustration> multiplyScenes;
		private static List<Color> multiplyColors;
		internal static bool lanternInStomach;

		// Attempt to sort all of the scene enum objects and return both if a custom scene exists and which menu it belongs to

		public static bool ReturnCustomScene(MenuScene.SceneID match)
		{
			foreach (SceneKey key in allCustomScenes.Keys)
			{
				if (allCustomScenes.ContainsKey(key) && allCustomScenes[key].Count > 0 && allCustomScenes[key].Contains(match))
				{
					return true;
				}
			}

			return false;
		}

		public static Dictionary<SceneKey, List<MenuScene.SceneID>> allCustomScenes = AllValidScenes();
		internal static float roomDarknesss;

		public static Dictionary<SceneKey, List<MenuScene.SceneID>> AllValidScenes()
		{
			Dictionary<SceneKey, List<MenuScene.SceneID>> sceneDict = [];

			List<MenuScene.SceneID> dreams = [];

			List<MenuScene.SceneID> spearmaster = [];
			List<MenuScene.SceneID> artificer = [];
			List<MenuScene.SceneID> hunter = [];
			List<MenuScene.SceneID> saint = [];

			if (ModOptions.CustomDreams.Value)
			{
				dreams.AddRange([
					MenuScene.SceneID.SleepScreen,
					MenuScene.SceneID.Dream_Pebbles,
					MoreSlugcatsEnums.MenuSceneID.SaintMaxKarma,
					]);
			}

			if (ModOptions.CustomSlugcatPages.Value)
			{
				spearmaster.AddRange([
					MoreSlugcatsEnums.MenuSceneID.Slugcat_Spear,
					MagicaEnums.SceneIDs.CustomSlugcat_SpearPearl,
					MoreSlugcatsEnums.MenuSceneID.End_Spear,
					MoreSlugcatsEnums.MenuSceneID.AltEnd_Spearmaster,
					MagicaEnums.SceneIDs.CustomSlugcat_SpearSRS,
					]);

				artificer.AddRange([
					MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer,
					MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo,
					MoreSlugcatsEnums.MenuSceneID.End_Artificer,
					MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_Portrait,
					]);

				hunter.AddRange([
					MenuScene.SceneID.Slugcat_Red,
					MenuScene.SceneID.Slugcat_Dead_Red,
					]);

				saint.AddRange([
					MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint,
					MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max,
					MoreSlugcatsEnums.MenuSceneID.End_Saint,
					]);
			}

			if (ModOptions.CustomSlideShows.Value)
			{
				spearmaster.AddRange([
					MagicaEnums.SceneIDs.Intro_Spearmasterlook,
					MagicaEnums.SceneIDs.Intro_Spearmasterentrance,
					MagicaEnums.SceneIDs.Intro_Spearmasteroverseer,
					MagicaEnums.SceneIDs.Intro_Spearmasterdownlook,
					MagicaEnums.SceneIDs.Intro_Spearmasterleap,
					MagicaEnums.SceneIDs.Intro_SpearmasterSRS,

					MagicaEnums.SceneIDs.Outro_SpearmasterSwimLeft,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroSwim,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroPause,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroLook,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroHop,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroEmbrace,

					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltComms,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltDescent,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltChimney,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltMice,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltWaterfront,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltLuna,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltChamber,
					MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltCollapse,
					]);

				artificer.AddRange([
					MoreSlugcatsEnums.MenuSceneID.Outro_Artificer1,
					MoreSlugcatsEnums.MenuSceneID.Outro_Artificer2,
					MoreSlugcatsEnums.MenuSceneID.Outro_Artificer3,
					MagicaEnums.SceneIDs.Outro_Artificer6,
					MoreSlugcatsEnums.MenuSceneID.Outro_Artificer4,
					MoreSlugcatsEnums.MenuSceneID.Outro_Artificer5,

					MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_1,
					MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_2,
					MagicaEnums.SceneIDs.AltEnd_Artificer_4,
					MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_3,
					]);

				hunter.AddRange([
					MenuScene.SceneID.Outro_Hunter_1_Swim,
					MenuScene.SceneID.Outro_Hunter_2_Sink,
					MenuScene.SceneID.Outro_Hunter_3_Embrace,
					]);

				saint.AddRange([
					MoreSlugcatsEnums.MenuSceneID.Intro_S1,
					MoreSlugcatsEnums.MenuSceneID.Intro_S2,
					MoreSlugcatsEnums.MenuSceneID.Intro_S3,
					MoreSlugcatsEnums.MenuSceneID.Intro_S4,
					]);
			}

			sceneDict.Add(SceneKey.Dream, dreams);
			sceneDict.Add(SceneKey.Spearmaster, spearmaster);
			sceneDict.Add(SceneKey.Artificer, artificer);
			sceneDict.Add(SceneKey.Hunter, hunter);
			sceneDict.Add(SceneKey.Saint, saint);

			return sceneDict;
		}

		public enum SceneKey
		{
			Dream,
			Spearmaster,
			Artificer,
			Hunter,
			Saint
		}

		public static bool IsCustomSlideShow(SlideShowID slideShowID)
		{
			List<SlideShowID> list = new();

			if (ModManager.MSC)
			{
				list.Add(SlidesShowIDs.SpearAltOutro);
				list.Add(SlidesShowIDs.SpearIntro);
				list.Add(MoreSlugcatsEnums.SlideShowID.SpearmasterOutro);

				list.Add(SlidesShowIDs.ArtificerDreamE);
				list.Add(MoreSlugcatsEnums.SlideShowID.ArtificerOutro);
				list.Add(MoreSlugcatsEnums.SlideShowID.ArtificerAltEnd);

				list.Add(SlidesShowIDs.RedsDeath);

				list.Add(MoreSlugcatsEnums.SlideShowID.SaintIntro);
			}

			if (list.Contains(slideShowID))
			{
				return true;
			}
			return false;
		}

		// returns folder paths
		public static string SceneFolder(string subDirectory, string name, string scene)
		{
			string result = "magicascenes" + Path.DirectorySeparatorChar.ToString() + subDirectory + Path.DirectorySeparatorChar.ToString() + name + Path.DirectorySeparatorChar.ToString() + scene;

			if (menuScene != null)
			{
				menuScene.sceneFolder = result;
			}
			return result;
		}

		public static string SceneFolder(string name, string scene)
		{
			string result = "magicascenes" + Path.DirectorySeparatorChar.ToString() + name + Path.DirectorySeparatorChar.ToString() + scene;

			if (menuScene != null)
			{
				menuScene.sceneFolder = result;
			}
			return result;
		}
		private static MenuIllustration CreateIllus(string fileName, float depth, MenuDepthIllustration.MenuShader shader)
		{
			return new MenuDepthIllustration(menuScene.menu, menuScene, sceneFolder, fileName, (menuScene.owner is InteractiveMenuScene ? new(0f, 0f) : new(71f, 49f)) , depth, shader);
		}

		private static MenuIllustration CreateIllus(string fileName, Vector2 position = default, bool anchorCenter = true)
		{
			if (position == default)
				position = new(683f, 384f);

			return new MenuIllustration(menuScene.menu, menuScene, sceneFolder, fileName, position, false, anchorCenter);
		}

		// Basic methods to build scenes
		public static void ApplySceneSpecificAlphas(SlideShowMenuScene self)
		{
			float fadeNum;
			if (!flat)
			{

				if (self.sceneID == MagicaEnums.SceneIDs.Outro_SpearmasterOutroEmbrace)
				{
					self.depthIllustrations[self.depthIllustrations.Count - 5].alpha = Mathf.InverseLerp(0.05f, 0.44f, self.displayTime);
					fadeNum = Mathf.Pow(Custom.SCurve(Mathf.InverseLerp(0.21f, 0.44f, self.displayTime), 0.65f), 1.5f);
					self.depthIllustrations[self.depthIllustrations.Count - 4].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 1].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 3].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 3);
					self.depthIllustrations[self.depthIllustrations.Count - 2].alpha = 0f + 1f * Mathf.InverseLerp(0f, 1f, self.displayTime * 3);
				}

				if (self.sceneID == MagicaEnums.SceneIDs.Intro_Spearmasterdownlook)
				{
					self.depthIllustrations[self.depthIllustrations.Count - 3].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 3 - 1);
					self.depthIllustrations[self.depthIllustrations.Count - 4].alpha = 1f * Mathf.InverseLerp(0f, 1f, self.displayTime * 3 - 1);
				}

				if (self.sceneID == MagicaEnums.SceneIDs.Intro_SpearmasterSRS)
				{
					fadeNum = Mathf.Lerp(0.8f, 1f, Mathf.Sin(self.displayTime * 50));

					self.depthIllustrations[self.depthIllustrations.Count - 1].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 3].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 5].alpha = fadeNum;
				}

				if (self.sceneID == MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltComms)
				{
					self.depthIllustrations[self.depthIllustrations.Count - 1].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 5 - 2);
					self.depthIllustrations[self.depthIllustrations.Count - 2].alpha = 1f * Mathf.InverseLerp(0f, 1f, self.displayTime * 5 - 2);
				}

				if (self.sceneID == MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltDescent)
				{
					self.depthIllustrations[self.depthIllustrations.Count - 3].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 6 - 2);
					self.depthIllustrations[self.depthIllustrations.Count - 2].alpha = 1f * Mathf.InverseLerp(1f, 0f, 1 - (self.displayTime * 6 - 2));
				}

				if (self.sceneID == MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltChamber)
				{
					self.depthIllustrations[self.depthIllustrations.Count - 3].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 5 - 2);
					self.depthIllustrations[self.depthIllustrations.Count - 5].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 5 - 2);
					self.depthIllustrations[self.depthIllustrations.Count - 8].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 5 - 2);

					self.depthIllustrations[self.depthIllustrations.Count - 2].alpha = 1f * Mathf.InverseLerp(0f, 1f, self.displayTime * 5 - 2);
					self.depthIllustrations[self.depthIllustrations.Count - 4].alpha = 1f * Mathf.InverseLerp(0f, 1f, self.displayTime * 5 - 2);
					self.depthIllustrations[self.depthIllustrations.Count - 7].alpha = 1f * Mathf.InverseLerp(0f, 1f, self.displayTime * 5 - 2);


					self.depthIllustrations[self.depthIllustrations.Count - 1].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 5 - 1);
					self.depthIllustrations[self.depthIllustrations.Count - 6].alpha = 1f * Mathf.InverseLerp(1f, 0f, self.displayTime * 5 - 1);
				}

				if (self.sceneID == MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltCollapse)
				{
					fadeNum = Mathf.Lerp(1f, 0f, 1 + -Mathf.Sin((self.displayTime * 2f) * (40f / (self.displayTime + 0.17f))) - (self.displayTime / 20f));
					self.depthIllustrations[self.depthIllustrations.Count - 2].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 4].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 6].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 8].alpha = fadeNum;
					self.depthIllustrations[self.depthIllustrations.Count - 10].alpha = fadeNum;

					self.depthIllustrations[self.depthIllustrations.Count - 1].alpha = Mathf.Lerp(0f, 1f, self.displayTime);
				}

				if (self.sceneID == MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_3)
				{
					float firstOffset = 1f;
					float secondOffset = 2.1f;
					self.depthIllustrations[self.depthIllustrations.Count - 6].alpha = Mathf.InverseLerp(1f, 0f, (self.displayTime * 6f) - firstOffset);
					self.depthIllustrations[self.depthIllustrations.Count - 5].alpha = self.displayTime > secondOffset / 6f ? Mathf.InverseLerp(1f, 0f, (self.displayTime * 5f) - secondOffset) : Mathf.InverseLerp(0f, 1f, (self.displayTime * 6f) - firstOffset);
					self.depthIllustrations[self.depthIllustrations.Count - 4].alpha = Mathf.InverseLerp(0f, 1f, (self.displayTime * 5f) - secondOffset);
				}
			}
		}

		public static void BuildScene(MenuScene self)
		{
			menuScene = self;
			flat = self.flatMode;

			multiplyScenes = new();
			multiplyColors = new();

			List<MenuIllustration> scenes = FindSceneStuff(self);

			foreach(MenuIllustration illus in scenes)
			{
				menuScene.AddIllustration(illus);
				Plugin.DebugLog(illus.fileName + " loaded");
			}
			scenes.Clear();
			self.RefreshPositions();

			if (menuScene is InteractiveMenuScene menu)
			{
				menu.idleDepths.Clear();
				if (idleDepths != null && idleDepths.Count > 0)
				{
					menu.idleDepths = idleDepths;
				}
			}

			if (multiplyScenes != null && multiplyScenes.Count > 0)
			{
				for (int i = 0; i < multiplyScenes.Count; i++)
				{
					ApplyTheFuckingMultiplyBitch(multiplyScenes[i], multiplyColors[i]);
				}
			}
		}

		private static void ApplyMultiplyOnDepthMaps(MenuIllustration scene, Color color)
		{
			multiplyScenes.Add(scene);
			multiplyColors.Add(color);
		}

		private static void ApplyTheFuckingMultiplyBitch(MenuIllustration illus, Color multiplyColor)
		{
			if (illus.texture.width > 0)
			{
				if (illus is MenuDepthIllustration depth)
				{
					try
					{
						Color[] pixels = depth.texture.GetPixels();
						for (int i = pixels.Length / 2; i < pixels.Length; i++)
						{
							pixels[i] = pixels[i] * multiplyColor;
						}
						depth.texture.SetPixels(pixels, 0);
						depth.texture.Apply(false);

						pixels = null;
					}
					catch
					{
						Plugin.Logger.LogError("Unable to apply multiply to menu illustrations oops");
					}
				}
				else if (illus.sprite != null)
				{
					illus.sprite.color = multiplyColor;
				}
			}
		}

		private static List<MenuIllustration> FindSceneStuff(MenuScene self)
		{
			SlugcatStats.Name currentSlugcat;
			if (self.menu.manager.currentMainLoop is RainWorldGame game)
			{
				currentSlugcat = game.StoryCharacter;
			}
			else
			{
				currentSlugcat = self.menu.manager.rainWorld.progression.PlayingAsSlugcat;
			}

			List<MenuIllustration> menuIllustrations = new();
			MenuScene.SceneID name = self.sceneID;

			if (allCustomScenes.ContainsKey(SceneKey.Dream) && allCustomScenes[SceneKey.Dream].Contains(self.sceneID))
			{
				return DreamScenes.SelectScene(name, currentSlugcat);
			}

			if (allCustomScenes.ContainsKey(SceneKey.Spearmaster) && allCustomScenes[SceneKey.Spearmaster].Contains(self.sceneID))
			{
				return SpearScenes.SelectScene(name);
			}

			if (allCustomScenes.ContainsKey(SceneKey.Artificer) && allCustomScenes[SceneKey.Artificer].Contains(self.sceneID))
			{
				return ArtificerScenes.SelectScene(name);
			}

			if (allCustomScenes.ContainsKey(SceneKey.Hunter) && allCustomScenes[SceneKey.Hunter].Contains(self.sceneID))
			{
				return HunterScenes.SelectScene(name);
			}

			if (allCustomScenes.ContainsKey(SceneKey.Saint) && allCustomScenes[SceneKey.Saint].Contains(self.sceneID))
			{
				return SaintScenes.SelectScene(name);
			}

			return menuIllustrations;
		}

		internal static List<SlugcatStats.Name> SleepSlugcats()
		{
			return new()
			{
				MoreSlugcatsEnums.SlugcatStatsName.Spear,
				MoreSlugcatsEnums.SlugcatStatsName.Artificer,
				SlugcatStats.Name.Red,
				MoreSlugcatsEnums.SlugcatStatsName.Saint
			};
		}

		// Actual scene pulling

		public class DreamScenes
		{
			public static int lastKarmaCap;
			public static int lastCycleCount;
			public static bool greenNeuronFlash;
			public static int dotFadeStart;
			internal static int dotFade;
			public static int slugpupNum;
			internal static List<Color> slugpupColors;

			internal static List<MenuIllustration> SelectScene(MenuScene.SceneID name, SlugcatStats.Name currentSlugcat)
			{
				List<MenuIllustration> scenes = new();

				scenes = name switch
				{
					var _ when name == MenuScene.SceneID.SleepScreen && SleepSlugcats().Contains(currentSlugcat) => SleepScreen(ref scenes, currentSlugcat),
					var _ when name == MenuScene.SceneID.Dream_Pebbles => PebblesDream(ref scenes, currentSlugcat),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.SaintMaxKarma => SaintMaxKarma(ref scenes),
				};

				return scenes;
			}

			private static List<MenuIllustration> SaintMaxKarma(ref List<MenuIllustration> scenes)
			{
				sceneFolder = SceneFolder("dreams", "dream - karma");

				if (!flat)
				{
					menuScene.blurMin = 0.05f;
					menuScene.blurMax = 0.35f;

					scenes.Add(CreateIllus("dream - karma - 0", 9f, MenuDepthIllustration.MenuShader.Normal));
					scenes.Add(CreateIllus("dream - karma - 1", 6f, MenuDepthIllustration.MenuShader.Lighten));
					scenes.Add(CreateIllus("dream - karma - 2", 5f, MenuDepthIllustration.MenuShader.Lighten));
					scenes.Add(CreateIllus("dream - karma - 3", 4.5f, MenuDepthIllustration.MenuShader.LightEdges));
					scenes.Add(CreateIllus("dream - karma - 4", 4.7f, MenuDepthIllustration.MenuShader.LightEdges));
					scenes.Add(CreateIllus("dream - karma - 5", 4f, MenuDepthIllustration.MenuShader.LightEdges));
					scenes.Add(CreateIllus("dream - karma - 6", 8f, MenuDepthIllustration.MenuShader.Lighten));
					scenes[scenes.Count - 1].setAlpha = 0.2f;
					scenes.Add(CreateIllus("dream - karma - 7", 5f, MenuDepthIllustration.MenuShader.Lighten));
					scenes[scenes.Count - 1].setAlpha = 0.7f;
					scenes.Add(CreateIllus("dream - karma - 8", 3.2f, MenuDepthIllustration.MenuShader.Lighten));
					scenes[scenes.Count - 1].setAlpha = 0.5f;
					scenes.Add(CreateIllus("dream - karma - 9", 3f, MenuDepthIllustration.MenuShader.Normal));
				}
				else
				{
					scenes.Add(CreateIllus("dream - karma - flat"));
				}

				return scenes;
			}

			private static List<MenuIllustration> SleepScreen(ref List<MenuIllustration> scenes, SlugcatStats.Name currentSlugcat)
			{
				sceneFolder = SceneFolder("slugcats", currentSlugcat.value, "sleep screen - " + currentSlugcat.value);

				Color gateColor = Custom.hexToColor("061F2C");
				Color grassColor = Custom.hexToColor("1E6188");
				Color slugColor = Color.white;
				// here we fuck up some colors
				if (paletteTexture != null)
				{
					gateColor = paletteTexture.GetPixel(1, 10);
					grassColor = paletteTexture.GetPixel(5, 11);
					slugColor = paletteTexture.GetPixel(29, 10);

					if (fadePaletteTexture != null)
					{
						gateColor = Color.Lerp(gateColor, fadePaletteTexture.GetPixel(1, 10), sleepFadeAmount);
						grassColor = Color.Lerp(grassColor, fadePaletteTexture.GetPixel(5, 11), sleepFadeAmount);
						slugColor = Color.Lerp(slugColor, grassColor, 0.4f);
					}

					if (roomDarknesss > 0f)
					{
						Color offBlack = new(0.001f, 0.001f, 0.001f);
						gateColor = Color.Lerp(gateColor, offBlack, roomDarknesss);
						grassColor = Color.Lerp(grassColor, offBlack, roomDarknesss);
						slugColor = Color.Lerp(slugColor, offBlack, roomDarknesss);
					}

					slugColor = Color.Lerp(slugColor, Color.white, 0.4f);
				}

				// setting the actual menuscenes
				string value = currentSlugcat.value;

				if (!flat)
				{
					menuScene.blurMax = 0.5f;
					menuScene.blurMin = 0.1f;
					if (currentSlugcat == SlugcatStats.Name.Red || currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						menuScene.blurMax = 0.2f;
						menuScene.blurMin = 0f;
					}

					// (-1 = left / 1 = right / 0 neutral, -1 = down / 1 = up / 0 neutral)

					string direction = "E";
					if (shelterDirection != null)
					{
						if (shelterDirection == new Vector2(0f, 1f))
						{
							direction = "N";
						}
						else if (shelterDirection == new Vector2(0f, -1f))
						{
							direction = "S";
						}
					}

					string version = "A";
					if (currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						version = "B";
					}

					scenes.Add(CreateIllus("3 - gate background", 3.5f, MenuDepthIllustration.MenuShader.Normal));
					ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], gateColor);

					scenes.Add(CreateIllus("2 - back grass " + direction, 2.8f, MenuDepthIllustration.MenuShader.Normal));
					scenes[scenes.Count - 1].setAlpha = new float?(0.24f);
					ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], grassColor);

					scenes.Add(CreateIllus("1" + version + " - rest spot " + direction, 2.2f, MenuDepthIllustration.MenuShader.Normal));
					ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], grassColor);

					if (ModManager.MSC && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						int num = 1;
						int num2 = 1;
						string path2 = AssetManager.ResolveFilePath(string.Concat(new string[]
						{
								menuScene.sceneFolder,
								Path.DirectorySeparatorChar.ToString(),
								"Sleep - D",
								num2.ToString(),
								".png"
						}));
						while (num2 < 999 && File.Exists(path2))
						{
							num = num2;
							num2++;
							path2 = AssetManager.ResolveFilePath(string.Concat(new string[]
							{
									menuScene.sceneFolder,
									Path.DirectorySeparatorChar.ToString(),
									"Sleep - D",
									num2.ToString(),
									".png"
							}));
						}
						string fileslugcatName = "Sleep - D" + UnityEngine.Random.Range(1, num + 1).ToString();
						scenes.Add(CreateIllus(fileslugcatName, 2.2f, MenuDepthIllustration.MenuShader.Basic));
					}

					if (ModManager.MSC && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
					{
						scenes.Add(CreateIllus("4 - artiglow", 1.2f, MenuDepthIllustration.MenuShader.Multiply));
						scenes[scenes.Count - 1].setAlpha = new float?(WinOrSaveHooks.artiDreamNumber > 0 ? (float)(WinOrSaveHooks.artiDreamNumber + 1) / 6f : 0f);

						string[] letter = new[]{
							"a",
							"b",
							"c",
							"d",
							"e",
							"f"
						};

						for (int i = 0; i < 6; i++)
						{
							if (i == WinOrSaveHooks.artiDreamNumber)
							{
								scenes.Add(CreateIllus("sleep - 2" + letter[WinOrSaveHooks.artiDreamNumber] + " - artificer", 2.2f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);
							}
							else
							{
								scenes.Add(CreateIllus("Sleep - empty", 2.2f, MenuDepthIllustration.MenuShader.Normal));
							}
						}
					}
					else if (currentSlugcat == SlugcatStats.Name.Red)
					{
						string slugPups = slugpupNum > 0 ? slugpupNum.ToString() : "";

						scenes.Add(CreateIllus("Sleep - 2" + slugPups + " - " + value, 1.7f, MenuDepthIllustration.MenuShader.Normal));
						ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);

						float? scarProgression = 0f;
						if (menuScene.menu != null && menuScene.menu is SleepAndDeathScreen menu && menu.manager.rainWorld != null && menu.manager.rainWorld.progression != null && menu.manager.rainWorld.progression.currentSaveState != null)
						{
							if (RedsIllness.RedsCycles(menu.manager.rainWorld.progression.currentSaveState.redExtraCycles) - menu.manager.rainWorld.progression.currentSaveState.cycleNumber < 0)
							{
								scarProgression = 1f;
							}
							else
							{
								scarProgression = new float?((float)(menu.manager.rainWorld.progression.currentSaveState.cycleNumber / (float)RedsIllness.RedsCycles(menu.manager.rainWorld.progression.currentSaveState.redExtraCycles))) - 0.1f;
							}
							Plugin.DebugLog(scarProgression.ToString());
						}

						scenes.Add(CreateIllus("Sleep - 2b" + slugPups + " - " + value, 1.8f, MenuDepthIllustration.MenuShader.Multiply));
						ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);
						scenes[scenes.Count - 1].setAlpha = scarProgression;

						switch (slugpupNum)
						{
							case 0:
								scenes.Add(CreateIllus("empty", 1.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("empty", 1.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("empty", 1.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("empty", 1.7f, MenuDepthIllustration.MenuShader.Basic));
								break;

							case 1:
								scenes.Add(CreateIllus("sleep - 5a - red", 1.7f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugpupColors[0] * slugColor);
								scenes.Add(CreateIllus("sleep - 5b - red", 1.7f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugpupColors[1] * slugColor);
								scenes.Add(CreateIllus("empty", 1.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("empty", 1.7f, MenuDepthIllustration.MenuShader.Basic));
								break;

							case 2:
								scenes.Add(CreateIllus("sleep - 5a - red", 1.7f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugpupColors[0] * slugColor);
								scenes.Add(CreateIllus("sleep - 5b - red", 1.7f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugpupColors[1] * slugColor);
								scenes.Add(CreateIllus("sleep - 6a - red", 1.7f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugpupColors[2] * slugColor);
								scenes.Add(CreateIllus("sleep - 6b - red", 1.7f, MenuDepthIllustration.MenuShader.Normal));
								ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugpupColors[3] * slugColor);
								break;
						}

						if (WinOrSaveHooks.HunterHasGreenNeuron)
						{

							scenes.Add(CreateIllus("Sleep - 3 - " + value, 1.8f, MenuDepthIllustration.MenuShader.Normal));
							ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);

							scenes.Add(CreateIllus("sleep - 4" + slugPups + " - red", 1.69f, MenuDepthIllustration.MenuShader.Lighten));
							scenes[scenes.Count - 1].setAlpha = 0.65f;

							scenes.Add(CreateIllus("sleep - neuron - red", 1.4f, MenuDepthIllustration.MenuShader.Basic));

							int cycleCount = 0;

							if (lastCycleCount != 0)
							{
								cycleCount = lastCycleCount;
							}

							if (menuScene.menu != null && menuScene.menu is SleepAndDeathScreen screen)
							{
								if (screen.manager.rainWorld != null && screen.manager.rainWorld.progression != null && screen.manager.rainWorld.progression.currentSaveState != null)
								{
									cycleCount = RedsIllness.RedsCycles(screen.manager.rainWorld.progression.currentSaveState.redExtraCycles) - screen.manager.rainWorld.progression.currentSaveState.cycleNumber;
								}
								lastCycleCount = cycleCount;
							}

							if (lastCycleCount < 0)
							{
								lastCycleCount = -1;
							}

							Plugin.DebugLog("Cycle count: " + cycleCount.ToString() + " " + greenNeuronFlash);

							dotFadeStart = scenes.Count - 1;
							for (int i = 0; i < 25; i++)
							{
								if (i < lastCycleCount + 1)
								{
									scenes.Add(CreateIllus("sleep - dot - red", 1.39f, MenuDepthIllustration.MenuShader.Basic));
								}
								else
								{
									scenes.Add(CreateIllus("empty", 1.39f, MenuDepthIllustration.MenuShader.Basic));
								}
							}
							dotFade = 0;
						}
						else
						{
							scenes.Add(CreateIllus("Sleep - 3b - " + value, 1.8f, MenuDepthIllustration.MenuShader.Normal));
							ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);

							// Green neuron
							scenes.Add(CreateIllus("empty", 1.65f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 1.65f, MenuDepthIllustration.MenuShader.Basic));

							// All da dots
							for (int i = 0; i < 25; i++)
							{
								scenes.Add(CreateIllus("empty", 1.65f, MenuDepthIllustration.MenuShader.Basic));
							}
						}

					}
					else if (ModManager.MSC && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						int karmaCap = 1;
						string face = "A";

						if (lastKarmaCap != 0)
						{
							karmaCap = lastKarmaCap;
						}

						scenes.Add(CreateIllus("sleep - 2 - saint", 1.7f, MenuDepthIllustration.MenuShader.Normal));
						ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);
						if (menuScene.menu != null && menuScene.menu is SleepAndDeathScreen screen)
						{
							if (screen.manager.rainWorld != null && screen.manager.rainWorld.progression != null && screen.manager.rainWorld.progression.currentSaveState != null && screen.manager.rainWorld.progression.currentSaveState.deathPersistentSaveData != null)
							{
								karmaCap = screen.manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.karmaCap;
							}
							if (karmaCap == 9)
							{
								face = "B";
							}
							lastKarmaCap = karmaCap;
						}
						scenes.Add(CreateIllus("sleep - scar", 1.65f, MenuDepthIllustration.MenuShader.Normal));
						ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);
						scenes[scenes.Count - 1].setAlpha = new float?((float)(karmaCap + 1) / 9f) - 0.3f;
						scenes.Add(CreateIllus("sleep - face " + face, 1.65f, MenuDepthIllustration.MenuShader.Normal));
						if (face != "B")
						{
							ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);
						}

						scenes.Add(CreateIllus("sleep - arms " + karmaCap, 1.6f, MenuDepthIllustration.MenuShader.Normal));
						ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);

						if (lanternInStomach)
						{
							scenes.Add(CreateIllus("sleep - glow", 1.4f, MenuDepthIllustration.MenuShader.SoftLight));
						}
						else
						{
							scenes.Add(CreateIllus("Sleep - empty", 2.2f, MenuDepthIllustration.MenuShader.Normal));
						}
					}
					else
					{
						scenes.Add(CreateIllus("Sleep - 2 - " + value, 1.7f, MenuDepthIllustration.MenuShader.Normal));
						ApplyMultiplyOnDepthMaps(scenes[scenes.Count - 1], slugColor);
					}

					scenes.Add(CreateIllus("0" + version + " - grass shadow " + direction, 1.2f, MenuDepthIllustration.MenuShader.Normal));

					idleDepths = new()
					{
						3.3f,
						2.7f,
						1.8f,
						1.7f
					};
					if (currentSlugcat == SlugcatStats.Name.Red)
					{
						idleDepths.Add(1.7f);
						idleDepths.Add(1.7f);
						if (WinOrSaveHooks.HunterHasGreenNeuron)
						{
							idleDepths.Add(1.65f);
						}
					}
					if (ModManager.MSC && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						idleDepths.Add(1.65f);
					}
					idleDepths.Add(1.6f);
					idleDepths.Add(1.2f);
				}
				else
				{
					if (ModManager.MSC && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
					{
						scenes.Add(CreateIllus("Sleep Screen - White - FlatB"));
					}
					else if (currentSlugcat == SlugcatStats.Name.Red)
					{
						string slugPups = slugpupNum > 0 ? slugpupNum.ToString() : "";
						float? scarProgression = 0f;
						if (menuScene.menu != null && menuScene.menu is SleepAndDeathScreen menu && menu.manager.rainWorld != null && menu.manager.rainWorld.progression != null && menu.manager.rainWorld.progression.currentSaveState != null)
						{
							if (RedsIllness.RedsCycles(menu.manager.rainWorld.progression.currentSaveState.redExtraCycles) - menu.manager.rainWorld.progression.currentSaveState.cycleNumber < 0)
							{
								scarProgression = 1f;
							}
							else
							{
								scarProgression = new float?((float)(menu.manager.rainWorld.progression.currentSaveState.cycleNumber / (float)RedsIllness.RedsCycles(menu.manager.rainWorld.progression.currentSaveState.redExtraCycles))) - 0.1f;
							}
							Plugin.DebugLog(scarProgression.ToString());
						}
						scenes.Add(CreateIllus($"sleep screen - red{(WinOrSaveHooks.HunterHasGreenNeuron ? " neuron" : "")}{slugPups} - flat"));

						if (WinOrSaveHooks.HunterHasGreenNeuron)
						{

							int cycleCount = 0;
							if (lastCycleCount != 0)
							{
								cycleCount = lastCycleCount;
							}

							if (menuScene.menu != null && menuScene.menu is SleepAndDeathScreen screen)
							{
								if (screen.manager.rainWorld != null && screen.manager.rainWorld.progression != null && screen.manager.rainWorld.progression.currentSaveState != null)
								{
									cycleCount = RedsIllness.RedsCycles(screen.manager.rainWorld.progression.currentSaveState.redExtraCycles) - screen.manager.rainWorld.progression.currentSaveState.cycleNumber;
								}
								lastCycleCount = cycleCount;
							}

							if (lastCycleCount < 0)
							{
								lastCycleCount = -1;
							}

							Plugin.DebugLog("Cycle count: " + cycleCount.ToString() + " " + greenNeuronFlash);

							dotFadeStart = scenes.Count - 1;
							Vector2[] positions = GetPositions(sceneFolder);
							if (positions != null && positions.Length > 11)
							{
								scenes.Add(CreateIllus("sleep - neuron - red", position: positions[11], anchorCenter: false));
								for (int i = 0; i < 25; i++)
								{
									if (i < lastCycleCount + 1)
									{
										scenes.Add(CreateIllus("sleep - dot - red", position: positions[i + 12], anchorCenter: false));
									}
								}
							}
							dotFade = 0;
						}
					}
					else if (ModManager.MSC && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint)
					{
						int karmaCap = 1;
						if (lastKarmaCap != 0)
						{
							karmaCap = lastKarmaCap;
						}

						scenes.Add(CreateIllus("Sleep Screen - Saint" + karmaCap.ToString() + " - Flat"));
					}
					else
					{
						scenes.Add(CreateIllus("Sleep Screen - " + value + " - Flat"));
					}
				}

				return scenes;
			}

			private static List<MenuIllustration> PebblesDream(ref List<MenuIllustration> scenes, SlugcatStats.Name currentSlugcat)
			{
				sceneFolder = SceneFolder("dreams", "dream - pebbles");

				if (!flat)
				{
					menuScene.blurMin = 0.05f;
					menuScene.blurMax = 0.35f;

					scenes.Add(CreateIllus("Pebbles - 10", 8f, MenuDepthIllustration.MenuShader.Normal));
					scenes.Add(CreateIllus("Pebbles - 9", 5f, MenuDepthIllustration.MenuShader.Lighten));
					scenes.Add(CreateIllus("Pebbles - 9", 5f, MenuDepthIllustration.MenuShader.SoftLight));
					scenes.Add(CreateIllus("Pebbles - 8", 4f, MenuDepthIllustration.MenuShader.Normal));
					scenes.Add(CreateIllus("Pebbles - 7", 2.7f, MenuDepthIllustration.MenuShader.Normal));
					scenes.Add(CreateIllus("Pebbles - 6", 2.2f, MenuDepthIllustration.MenuShader.Normal));
					scenes.Add(CreateIllus("Pebbles - 5", 1.1f, MenuDepthIllustration.MenuShader.Normal));
					scenes.Add(CreateIllus("Pebbles - 4", 2.9f, MenuDepthIllustration.MenuShader.Normal));

					if (currentSlugcat == SlugcatStats.Name.Yellow)
					{
						scenes.Add(CreateIllus("Pebbles - 3b", 1.6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Pebbles - 2b", 1.4f, MenuDepthIllustration.MenuShader.Lighten));
					}
					else if (currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						scenes.Add(CreateIllus("Pebbles - 3c", 1.6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Pebbles - 2c", 1.4f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("Pebbles - 3", 1.6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Pebbles - 2", 1.4f, MenuDepthIllustration.MenuShader.Lighten));
					}
					scenes.Add(CreateIllus("Pebbles - 1", 0.8f, MenuDepthIllustration.MenuShader.Normal));
				}
				else
				{
					scenes.Add(CreateIllus("Dream - Pebbles - Flat"));
				}

				return scenes;
			}
		}

		private static Vector2[] GetPositions(string sceneFolder)
		{
			if (string.IsNullOrEmpty(sceneFolder))
			{
				return null;
			}

			string path;
			if (!string.IsNullOrEmpty(menuScene.positionsFile))
			{
				path = AssetManager.ResolveFilePath(sceneFolder + Path.DirectorySeparatorChar.ToString() + menuScene.positionsFile);
			}
			else
			{
				path = AssetManager.ResolveFilePath(sceneFolder + Path.DirectorySeparatorChar.ToString() + "positions_ims.txt");
			}
			if (!File.Exists(path))
			{
				path = AssetManager.ResolveFilePath(sceneFolder + Path.DirectorySeparatorChar.ToString() + "positions.txt");
			}
			if (File.Exists(path))
			{
				string[] array = File.ReadAllLines(path);
				Vector2[] positions = new Vector2[array.Length];
				for (int num = 0; num < array.Length; num++) 
				{
					positions[num] = new(float.Parse(Regex.Split(Custom.ValidateSpacedDelimiter(array[num], ","), ", ")[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(Regex.Split(Custom.ValidateSpacedDelimiter(array[num], ","), ", ")[1], NumberStyles.Any, CultureInfo.InvariantCulture));
				}
				return positions;
			}
			return null;
		}

		public class SpearScenes
		{
			public static readonly string subDirectory = "slugcats";
			public static readonly string slugcatName = "spear";

			internal static List<MenuIllustration> SelectScene(MenuScene.SceneID name)
			{
				List<MenuIllustration> scenes = [];

				scenes = name switch
				{
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Slugcat_Spear || name == SceneIDs.CustomSlugcat_SpearPearl => SelectMenu.SelectScreen(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.End_Spear => SelectMenu.Ghost(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.AltEnd_Spearmaster => SelectMenu.AltEndingBroadcast(ref scenes),
					var _ when name == SceneIDs.CustomSlugcat_SpearSRS => SelectMenu.AltEndingSRS(ref scenes),

					var _ when name == SceneIDs.Intro_Spearmasterentrance => IntroScenes.Entrance(ref scenes),
					var _ when name == SceneIDs.Intro_Spearmasteroverseer => IntroScenes.Walk(ref scenes),
					var _ when name == SceneIDs.Intro_Spearmasterlook => IntroScenes.Overseer(ref scenes),
					var _ when name == SceneIDs.Intro_Spearmasterdownlook => IntroScenes.LookDown(ref scenes),
					var _ when name == SceneIDs.Intro_Spearmasterleap => IntroScenes.Leap(ref scenes),
					var _ when name == SceneIDs.Intro_SpearmasterSRS => IntroScenes.SRS(ref scenes),

					var _ when name == SceneIDs.Outro_SpearmasterSwimLeft => GhostScenes.ZoomedOut(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroSwim => GhostScenes.Swim(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroPause => GhostScenes.Pause(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroLook => GhostScenes.Look(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroHop => GhostScenes.Leap(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroEmbrace => GhostScenes.Embrace(ref scenes),

					var _ when name == SceneIDs.Outro_SpearmasterOutroAltComms => AltEndingScenes.Comms(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltDescent => AltEndingScenes.Descent(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltChimney => AltEndingScenes.Chimney(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltMice => AltEndingScenes.Mice(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltWaterfront => AltEndingScenes.Shoreline(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltLuna => AltEndingScenes.Luna(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltChamber => AltEndingScenes.Fall(ref scenes),
					var _ when name == SceneIDs.Outro_SpearmasterOutroAltCollapse => AltEndingScenes.Floor(ref scenes),
				};

				return scenes;
			}

			internal class IntroScenes
			{
				internal static List<MenuIllustration> Entrance(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 1");

					if (!flat)
					{
						scenes.Add(CreateIllus("7 - backcolor", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("6 - backbackgrounddd", 9f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("5 - backgroundpipes", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4 - foregroundpipes", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - pipe", 3f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2 - foreground", 1.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - spearmasterstand", 0.5f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("intro spearmaster 1 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Leap(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 5");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - sky", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("4 - backkground", 6f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("3 - obverseer", 9f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("2 - tunnele", 20f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1e - spearmaster", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("intro spearmaster 5 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> LookDown(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 4");

					if (!flat)
					{
						scenes.Add(CreateIllus("7 - colorb", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("6b - background", 9f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("5 - plants", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4b - dudes", 7.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4a - dudes", 7.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3d - foreground", 3.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - abyss", 0.5f, MenuDepthIllustration.MenuShader.Basic));
					}
					else
					{
						scenes.Add(CreateIllus("intro spearmaster 4 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Overseer(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 3");

					if (!flat)
					{
						scenes.Add(CreateIllus("6 - outside", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("5 - tunnel", 1.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4 - ground", 5.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3b - overseer", 4f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2c - foreground", 12f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1c - spearmaster", 0.5f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("intro spearmaster 3 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> SRS(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 6");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - hologram", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("4 - hand", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - flicker", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - srs", 9f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - flicker", 9f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("intro spearmaster 6 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Walk(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 2");

					if (!flat)
					{
						scenes.Add(CreateIllus("6 - color", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("5b - backgroundie", 10f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("4b - backgroundpipes", 5.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("3 - spearmasterwalk", 6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - overseer", 4f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1b - foreground", 10f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("intro spearmaster 2 - flat"));
					}

					return scenes;
				}
			}

			internal class GhostScenes
			{
				internal static List<MenuIllustration> Embrace(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro spearmaster 5");

					if (!flat)
					{
						scenes.Add(CreateIllus("4b - void", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("3 - glow", 9.5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("2 - SRS", 3f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1d - spearmaster", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1e - spearmaster", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("0 - hands", 1.5f, MenuDepthIllustration.MenuShader.LightEdges));
					}
					else
					{
						scenes.Add(CreateIllus("outro spearmaster 6 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Leap(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 5");

					if (!flat)
					{
						scenes.Add(CreateIllus("4 - background", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("3 - voidbackground", 9f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2 - void glow", 4.5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("1c - spearmaster", 3f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outro spearmaster 5 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Look(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 4");

					if (!flat)
					{
						scenes.Add(CreateIllus("6 - background", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("5 - voidbloom", 10f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("4 - void", 4.5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("3 - hand", 3.2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2b - spearmasterglow", 2.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1b - spearmaster", 2f, MenuDepthIllustration.MenuShader.LightEdges));
					}
					else
					{
						scenes.Add(CreateIllus("outro spearmaster 4 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Pause(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 3");

					if (!flat)
					{
						scenes.Add(CreateIllus("4 - background", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("3 - leavingslugcats", 4f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2 - spearmasterglow", 1.1f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - spearmaster", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outro spearmaster 3 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Swim(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 2");

					if (!flat)
					{
						scenes.Add(CreateIllus("6 - background", 8f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("5 - void", 7.8f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("4 - background slugcats", 3f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("3 - slugcatsglow", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2 - slugcatsforeground", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - spearmasterglow", 1.05f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("0 - spearmaster", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outro spearmaster 2 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> ZoomedOut(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 1");
					
					if (!flat)
					{
						scenes.Add(CreateIllus("4 - background", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("3 - voidglow", 9f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2 - slugcatglow", 5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - slugcats", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - spear", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outro spearmaster 1 - flat"));
					}

					return scenes;
				}
			}

			internal class AltEndingScenes
			{
				internal static List<MenuIllustration> Chimney(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 3_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - color", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("4 - bckstructure", 8.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - rebarb", 3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - tower", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - lizard", 0.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - rain", 1.5f, MenuDepthIllustration.MenuShader.SoftLight));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 3 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Comms(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 1_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - clouds", 8.5f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("4 - building", 5.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - comms", 5.2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - ground", 5.2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - pole", 5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - speargrabb", 3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - speargraba", 3f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 1 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Descent(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 2_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("7 - sky", 15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("5 - fgclouds", 7.5f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("4 - fp", 6f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("3 - structure", 2.8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - bckpoles", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - poles", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - spearclimb", 1.5f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 2 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Fall(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 7_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("4b - chamber", 10f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4a - chamber", 10f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - glow", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2b - pearls", 5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2a - pearls", 5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0b - moon", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0a - moon", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - halo", 2.5f, MenuDepthIllustration.MenuShader.LightEdges));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 7a - flat"));
						scenes.Add(CreateIllus("outrob spearmaster 7b - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Floor(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 8_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - chamber2", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("5 - chamber2b", 4.5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("4 - body", 4.3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4 - bodyb", 4.3f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("3 - shoulders", 3.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - shouldersb", 3.5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("2 - head", 3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - headb", 3f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("0 - arms", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - armsb", 2.5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("6 - multiply", 2.5f, MenuDepthIllustration.MenuShader.Overlay));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 8 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Luna(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 6_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - chamber", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("4 - glow", 3.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("3 - halo", 4.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("2 - wires", 5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - moon", 3.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - moonhand", 1.4f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("0 - luna", 1f, MenuDepthIllustration.MenuShader.Lighten));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 6 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Mice(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 4_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("3 - den", 10f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - lanternmice", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("0 - glow", 2f, MenuDepthIllustration.MenuShader.SoftLight));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 4 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Shoreline(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro spearmaster 5_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("4 - grey", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("3 - horizon", 6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - backstructure", 3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - pipe", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - rainpipe", 1.8f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outrob spearmaster 5 - flat"));
					}

					return scenes;
				}
			}

			internal class SelectMenu
			{
				internal static List<MenuIllustration> AltEndingSRS(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat end_c - spearmaster");

					if (!flat)
					{
						scenes.Add(CreateIllus("Spearmaster End C - Flat", 4.5f, MenuDepthIllustration.MenuShader.Basic));

						idleDepths = new()
							{
								2.8f
							};
					}
					else
					{
						scenes.Add(CreateIllus("Spearmaster End C - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> AltEndingBroadcast(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat end_b - spearmaster");

					if (!flat)
					{
						scenes.Add(CreateIllus("Spearmaster End Halo", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Spearmaster End Bkg", 3.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("Spearmaster End SRS", 2.7f, MenuDepthIllustration.MenuShader.Basic));

						idleDepths = new()
							{
								3.1f,
								2.8f
							};
					}
					else
					{
						scenes.Add(CreateIllus("Spearmaster End B - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Ghost(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat end - spearmaster");

					if (!flat)
					{
						scenes.Add(CreateIllus("Spearmaster Bkg", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Spearmaster A", 2.85f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Spearmaster B", 2.7f, MenuDepthIllustration.MenuShader.Overlay));

						idleDepths = new()
							{
								3.1f,
								2.8f
							};
					}
					else
					{
						scenes.Add(CreateIllus("Spearmaster - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> SelectScreen(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat - spearmaster");

					if (!flat)
					{
						scenes.Add(CreateIllus("Spearmaster Background - 4", 3.2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Spearmaster Pipes - 3", 3.1f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Spearmaster Vines - 2", 2.8f, MenuDepthIllustration.MenuShader.Normal));
						if (menuScene.owner is SlugcatSelectMenu.SlugcatPage menu)
						{
							menu.AddGlow();
						}
						if (menuScene.UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Spear))
						{
							if (menuScene.sceneID == MagicaEnums.SceneIDs.CustomSlugcat_SpearPearl)
							{
								if (MenuSceneHooks.magicaGameData != null && MenuSceneHooks.magicaGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Spear) && MenuSceneHooks.magicaGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear] != null && MenuSceneHooks.magicaGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].pearlBroadcastTagged)
								{
									scenes.Add(CreateIllus("spearmaster slugcat - 1c", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								}
								else
								{
									scenes.Add(CreateIllus("spearmaster slugcat - 1b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								}
								scenes.Add(CreateIllus("spearmaster slugcat - 2", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 2b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 3", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 4b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 5", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 6", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 6b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							}
							else
							{
								scenes.Add(CreateIllus("spearmaster slugcat - 1", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 2", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 2b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 3", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 4", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 6", 2.7f, MenuDepthIllustration.MenuShader.Basic));
								scenes.Add(CreateIllus("spearmaster slugcat - 6b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							}
						}
						else
						{
							scenes.Add(CreateIllus("Spearmaster Slugcat - 1 - Dark", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.7f, MenuDepthIllustration.MenuShader.Basic));
						}

						idleDepths = new()
							{
								3.6f,
								2.8f,
								2.7f,
								2.6f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
							};
					}
					else
					{
						if (menuScene.UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Spear))
						{
							if (menuScene.sceneID == MagicaEnums.SceneIDs.CustomSlugcat_SpearPearl)
							{
								scenes.Add(CreateIllus("Slugcat - Spearmaster - Flatb"));
							}
							else
							{
								scenes.Add(CreateIllus("Slugcat - Spearmaster - Flat"));
							}
						}
						else
						{
							scenes.Add(CreateIllus("Slugcat - Spearmaster Dark - Flat"));
						}
					}

					return scenes;
				}
			}
		}
		public class ArtificerScenes
		{
			public static readonly string subDirectory = "slugcats";
			public static readonly string slugcatName = "artificer";

			internal static List<MenuIllustration> SelectScene(MenuScene.SceneID name)
			{
				List<MenuIllustration> scenes = [];

				scenes = name switch
				{
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer || name == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo => SelectMenu.SelectScreen(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.End_Artificer => SelectMenu.Ghost(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_Portrait => SelectMenu.AltEndingScavKing(ref scenes),

					var _ when name == MoreSlugcatsEnums.MenuSceneID.Outro_Artificer1 => GhostScenes.Swim(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Outro_Artificer2 => GhostScenes.Zoom(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Outro_Artificer3 => GhostScenes.See(ref scenes),
					var _ when name == MagicaEnums.SceneIDs.Outro_Artificer6 => GhostScenes.Shock(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Outro_Artificer4 => GhostScenes.Embrace(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Outro_Artificer5 => GhostScenes.Fade(ref scenes),

					var _ when name == MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_1 => AltEndingScenes.Beast(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_2 => AltEndingScenes.Wrath(ref scenes),
					var _ when name == MagicaEnums.SceneIDs.AltEnd_Artificer_4 => AltEndingScenes.Mask(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_3 => AltEndingScenes.Victory(ref scenes),
				};

				return scenes;
			}

			internal class AltEndingScenes
			{
				internal static List<MenuIllustration> Beast(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 1_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("Outro Artificer 1_B - 8", 10f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 7", 7.2f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 6", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 5", 3.2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 4", 2.3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 3", 2.1f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 2", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Outro Artificer 1_B - 1", 1.8f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 1_B - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Mask(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 4_b");

					if (!flat)
					{
						scenes.Add(CreateIllus("outro artificer 4_b - 1", 10.5f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("outro artificer 4_b - 2", 6.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer 4_b - 3", 3f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer 4_b - 4", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer 4_b - 5", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 4_B - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Victory(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 3_b");

					if (!flat)
					{
						menuScene.blurMin = 0.05f;
						menuScene.blurMax = 0.3f;

						scenes.Add(CreateIllus("outro artificer - backgroundpillar", 11f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("outro artificer - pillaaar", 7.9f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - lightcameraaction", 7f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("outro artificer - body", 4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - head1", 4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - head2", 4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - head3", 4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - robobobo", 3.95f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - scavsforeground1", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro artificer - scavsforeground2", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 3_B - Flat"));
						scenes.Add(CreateIllus("Outro Artificer 3_B - Flatb"));
						scenes.Add(CreateIllus("Outro Artificer 3_B - Flatc"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Wrath(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 2_b");

					if (!flat)
					{
						menuScene.blurMin = -0.2f;
						menuScene.blurMax = 0.1f;
						scenes.Add(CreateIllus("Outro Artificer 2_B - 7", 10f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 2_B - 6", 4.5f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 2_B - 5", 3.8f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 2_B - 4", 2.4f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 2_B - 3", 1.8f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 2_B - 2", 1.4f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Outro Artificer 2_B - 1", 10f, MenuDepthIllustration.MenuShader.Basic));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 2_B - Flat"));
					}

					return scenes;
				}
			}

			internal class GhostScenes
			{
				internal static List<MenuIllustration> Embrace(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 3");

					if (!flat)
					{
						scenes.Add(CreateIllus("4 - Solid", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - OA3Background", 6f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("2 - OA3Voided Back", 3.2f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("1 - OA3Family", 2.4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - OA3Voided Front", 1.8f, MenuDepthIllustration.MenuShader.Lighten));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 3 - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Fade(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 4");

					if (!flat)
					{
						scenes.Add(CreateIllus("4 - Solid", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - OA4Background", 6f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("2 - OA4Voided Back", 3.2f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("1 - OA4Family", 2.4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("0 - OA4Voided Front", 1.8f, MenuDepthIllustration.MenuShader.Lighten));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 4 - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> See(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 2");

					if (!flat)
					{
						scenes.Add(CreateIllus("3 - Solid", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - OA2Background", 6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - OA2Artificer", 2.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("0 - OA2ForeGroundSlugcats", 1.5f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 2 - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Shock(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 6");

					if (!flat)
					{
						scenes.Add(CreateIllus("3 - Solid", 8f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("2 - OA6Background", 4f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - OA6Artificer", 2f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("0 - OA6ForeGroundSlugcats", 1.5f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 6 - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Swim(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer left swim");

					if (!flat)
					{
						scenes.Add(CreateIllus("3 - OAMainSlugcat", 0.9f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - OABlueBloom", 5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("1 - OACatsBloom", 0.9f, MenuDepthIllustration.MenuShader.Lighten));
					}
					else
					{
						scenes.Add(CreateIllus("Outro 1 - OALeft Swim - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Zoom(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro artificer 1");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - Solid", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4 - OA1Background", 6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - OA1Swimmers", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - OA1Artificer", 2.5f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("1 - OA1Voided", 2.4f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("0 - OA1Foreground Lights", 1f, MenuDepthIllustration.MenuShader.Lighten));
					}
					else
					{
						scenes.Add(CreateIllus("Outro Artificer 1 - Flat"));
					}

					return scenes;
				}
			}

			internal class SelectMenu
			{
				internal static List<MenuIllustration> AltEndingScavKing(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat end_b - artificer");

					if (!flat)
					{
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - 5", 3.6f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - 4", 2.8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - 3", 2.25f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - 2", 2.2f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - 1", 2.15f, MenuDepthIllustration.MenuShader.Basic));
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - 0", 2.1f, MenuDepthIllustration.MenuShader.Basic));

						idleDepths = new()
							{
								3.2f,
								2.8f,
								2.7f,
								2.2f
							};
					}
					else
					{
						scenes.Add(CreateIllus("Slugcat End_B - Artificer - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Ghost(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat end - artificer");

					if (!flat)
					{
						scenes.Add(CreateIllus("Artificer Bkg", 4.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Artificer A", 2.85f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Artificer B", 2.7f, MenuDepthIllustration.MenuShader.Overlay));

						idleDepths = new()
							{
								3.1f,
								2.8f
							};
					}
					else
					{
						scenes.Add(CreateIllus("Artificer - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> SelectScreen(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat - artificer");

					if (!flat)
					{
						scenes.Add(CreateIllus("Artificer Background - 5", 3.6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Artificer Background - 5b", 3.6f, MenuDepthIllustration.MenuShader.Normal));
						scenes[scenes.Count - 1].alpha = MenuSceneHooks.artiKarmaCap > 0 ? (float)(MenuSceneHooks.artiKarmaCap + 1) / 10f : 0f;
						scenes.Add(CreateIllus("Artificer Vines - 4", 3.4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Artificer MidBg - 3", 3.1f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("Artificer Smoke - 2", 2.0f, MenuDepthIllustration.MenuShader.Normal));
						if (menuScene.owner is SlugcatSelectMenu.SlugcatPage page)
						{
							page.AddGlow();
						}
						if (menuScene.UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Artificer))
						{
							scenes.Add(CreateIllus("Artificer Slugcat - 1", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 2", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 2b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 3", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 3b", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 4", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 5", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Slugcat - 6", 2.7f, MenuDepthIllustration.MenuShader.Basic));
						}
						else
						{
							scenes.Add(CreateIllus("Artificer Slugcat - 1 - Dark", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
						}
						if (menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo)
						{
							scenes.Add(CreateIllus("artificer slugcat - 7", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes[scenes.Count - 1].alpha = MenuSceneHooks.artiKarmaCap > 0 ? (float)(MenuSceneHooks.artiKarmaCap + 1) / 10f : 0f;
							scenes.Add(CreateIllus("Artificer Robot - 1", 2.5f, MenuDepthIllustration.MenuShader.Basic));
						}
						else
						{
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("Artificer Robot - 0", 2.7f, MenuDepthIllustration.MenuShader.Basic));
						}
						scenes.Add(CreateIllus("Artificer Fg - 0", 2.7f, MenuDepthIllustration.MenuShader.Normal));

						idleDepths = new()
							{
								3.6f,
								3.6f,
								2.8f,
								2.7f,
								2.6f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1.5f,
								1f
							};
					}
					else
					{
						if (menuScene.UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Artificer))
						{
							if (menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo)
							{
								scenes.Add(CreateIllus("slugcat - artificer robot 1 - flat"));
							}
							else
							{
								scenes.Add(CreateIllus("slugcat - artificer - flat"));
							}
						}
						else
						{
							scenes.Add(CreateIllus("slugcat - artificer dark - flat"));
						}
					}

					return scenes;
				}
			}
		}
		public class HunterScenes
		{
			public static readonly string subDirectory = "slugcats";
			public static readonly string slugcatName = "red";

			internal static List<MenuIllustration> SelectScene(MenuScene.SceneID name)
			{
				List<MenuIllustration> scenes = [];

				scenes = name switch
				{
					var _ when name == MenuScene.SceneID.Slugcat_Red => SelectMenu.SelectScreen(ref scenes),
					var _ when name == MenuScene.SceneID.Slugcat_Dead_Red => SelectMenu.AltEndingOracle(ref scenes),

					var _ when name == MenuScene.SceneID.Outro_Hunter_1_Swim => GhostScenes.Swim(ref scenes),
					var _ when name == MenuScene.SceneID.Outro_Hunter_2_Sink => GhostScenes.Sink(ref scenes),
					var _ when name == MenuScene.SceneID.Outro_Hunter_3_Embrace => GhostScenes.Embrace(ref scenes),
				};

				return scenes;
			}

			internal class SelectMenu
			{
				internal static List<MenuIllustration> AltEndingOracle(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "dead red");

					string oracle = "";
					if (MenuSceneHooks.OracleValue != "")
					{
						if (MenuSceneHooks.OracleValue == Oracle.OracleID.SL.value)
						{
							oracle = " moon";
						}
						if (MenuSceneHooks.OracleValue == Oracle.OracleID.SS.value)
						{
							oracle = " pebbles";
						}
						Plugin.DebugLog(oracle);
					}

					if (!flat)
					{
						scenes.Add(CreateIllus("Dead Red" + oracle + " - Flat"));

						//scenes.Add(CreateIllus("Red Death - 5", 3.4f, MenuDepthIllustration.MenuShader.Normal));
						//scenes.Add(CreateIllus("Red Death - 4", 2.7f, MenuDepthIllustration.MenuShader.Normal));
						//scenes[scenes.Count - 1].setAlpha = new float?(0.53f);

						//scenes.Add(CreateIllus("Red Death - 3", 2.5f, MenuDepthIllustration.MenuShader.Normal));
						//scenes.Add(CreateIllus("Red Death - 2", 2.3f, MenuDepthIllustration.MenuShader.Normal));
						//scenes.Add(CreateIllus("Red Death - 1", 1.6f, MenuDepthIllustration.MenuShader.Normal));

						//idleDepths = new()
						//{
						//	3f,
						//	2.4f,
						//	2.3f,
						//	2.2f,
						//	1.5f
						//};
					}
					else
					{
						scenes.Add(CreateIllus("Dead Red - Flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> SelectScreen(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat - red");

					if (!flat)
					{
						scenes.Add(CreateIllus("red background - 4", 3.2f, MenuDepthIllustration.MenuShader.Normal));

						if (menuScene.UseSlugcatUnlocked(SlugcatStats.Name.Red))
						{
							float? scarProgression = 0f;
							if (menuScene.menu != null && menuScene.menu is SlugcatSelectMenu menu && menu.saveGameData != null && menu.saveGameData.ContainsKey(SlugcatStats.Name.Red) && menu.saveGameData[SlugcatStats.Name.Red] != null)
							{
								if (RedsIllness.RedsCycles(menu.saveGameData[SlugcatStats.Name.Red].redsExtraCycles) - menu.saveGameData[SlugcatStats.Name.Red].cycle < 0)
								{
									scarProgression = 1f;
								}
								else
								{
									scarProgression = new float?((float)(menu.saveGameData[SlugcatStats.Name.Red].cycle / (float)RedsIllness.RedsCycles(menu.saveGameData[SlugcatStats.Name.Red].redsExtraCycles))) - 0.1f;
								}
								Plugin.DebugLog(scarProgression.ToString());
							}

							scenes.Add(CreateIllus("red background - 4b", 3.2f, MenuDepthIllustration.MenuShader.Overlay));
							scenes[scenes.Count - 1].setAlpha = scarProgression;
							scenes.Add(CreateIllus("red slugcat - 1", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red slugcat - 1b", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red slugcat - 1c", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes[scenes.Count - 1].setAlpha = scarProgression;
							scenes.Add(CreateIllus("red slugcat - 2", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red slugcat - 3", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red slugcat - 3b", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red spears - 3", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red spears - 4", 2.3f, MenuDepthIllustration.MenuShader.Basic));
						}
						else
						{
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("red slugcat - 1 - dark", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
						}

						scenes.Add(CreateIllus("red fgplants - 0", 2.1f, MenuDepthIllustration.MenuShader.Normal));


						idleDepths =
							[
								3.3f,
								2.3f,
								2f
							];
					}
					else
					{
						if (menuScene.UseSlugcatUnlocked(SlugcatStats.Name.Red))
						{
							scenes.Add(CreateIllus("slugcat - red - flat"));
						}
						else
						{
							scenes.Add(CreateIllus("slugcat - red dark - flat"));
						}
					}

					return scenes;
				}
			}

			internal class GhostScenes
			{
				internal static List<MenuIllustration> Swim(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro hunter 1 - swim");

					if (!flat)
					{
						scenes.Add(CreateIllus("outro hunter 1 - swim - 6", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 1 - swim - 5", 10f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 1 - swim - 4", 6f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 1 - swim - 3", 5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 1 - swim - 2", 4.2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 1 - swim - 1", 6.5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 1 - swim - 0", 1f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outro hunter 1 - swim - flat"));
					}

					return scenes;
				}
				internal static List<MenuIllustration> Sink(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro hunter 2 - sink");

					if (!flat)
					{
						scenes.Add(CreateIllus("outro hunter 2 - sink - 5", 12f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 2 - sink - 4", 16f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 2 - sink - 3", 3.2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 2 - sink - 2", 3.4f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 2 - sink - 1", 3.1f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("outro hunter 2 - sink - 0", 1.3f, MenuDepthIllustration.MenuShader.SoftLight));
					}
					else
					{
						scenes.Add(CreateIllus("outro hunter 2 - sink - flat"));
					}

					return scenes;
				}
				internal static List<MenuIllustration> Embrace(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "outro hunter 3 - embrace");

					if (!flat)
					{
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 7", 15f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 6", 9f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 5", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 3", 4.9f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 2", 3.7f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 0", 3.8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 1", 7f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("outro hunter 3 - embrace - 4", 1.2f, MenuDepthIllustration.MenuShader.Normal));
					}
					else
					{
						scenes.Add(CreateIllus("outro hunter 3 - embrace - flat"));
					}

					return scenes;
				}
			}
		}

		public class SaintScenes
		{
			public static readonly string subDirectory = "slugcats";
			public static readonly string slugcatName = "saint";

			internal static List<MenuIllustration> SelectScene(MenuScene.SceneID name)
			{
				List<MenuIllustration> scenes = [];

				scenes = name switch
				{
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint || name == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max => SelectMenu.SelectScreen(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.End_Saint => SelectMenu.Ghost(ref scenes),

					var _ when name == MoreSlugcatsEnums.MenuSceneID.Intro_S1 || name == MoreSlugcatsEnums.MenuSceneID.Intro_S2 => IntroScenes.Void(ref scenes),
					var _ when name == MoreSlugcatsEnums.MenuSceneID.Intro_S4 => IntroScenes.Face(ref scenes),
				};

				return scenes;
			}

			public class SelectMenu
			{
				internal static List<MenuIllustration> Ghost(ref List<MenuIllustration> scenes)
				{
					menuScene.blurMin = 0.01f;
					menuScene.blurMax = 0.35f;

					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat end - saint");
					int karmaCap = 0;

					if (menuScene.menu != null && menuScene.menu is SlugcatSelectMenu menu && menu.saveGameData != null && menu.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Saint) && menu.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint] != null && !menu.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint].ascended)
					{
						karmaCap = menu.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint].karmaCap;
					}

					if (!flat)
					{
						scenes.Add(CreateIllus("saint end - 0", 5f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("saint end - 1 " + karmaCap.ToString(), 5f, MenuDepthIllustration.MenuShader.Lighten));
						scenes.Add(CreateIllus("saint end - 2", 4.4f, MenuDepthIllustration.MenuShader.SoftLight));
						scenes.Add(CreateIllus("saint end - 3", 4.4f, MenuDepthIllustration.MenuShader.Lighten));
						scenes[scenes.Count - 1].setAlpha = 0.5f;
						scenes.Add(CreateIllus("saint end - 4", 3f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("saint end - 5", 2.3f, MenuDepthIllustration.MenuShader.LightEdges));
						scenes.Add(CreateIllus("saint end - 6", 2f, MenuDepthIllustration.MenuShader.Lighten));

						idleDepths = [
							2.2f,
							3f
							];
					}
					else
					{
						scenes.Add(CreateIllus("saint end - flat " + karmaCap.ToString()));
					}

					return scenes;
				}

				internal static List<MenuIllustration> SelectScreen(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "slugcat - saint");

					if (!flat)
					{
						if (menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max)
						{
							scenes.Add(CreateIllus("empty", 4f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 10b", 4f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 3.8f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 9b", 3.8f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 8b", 3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 7b", 2.3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 1", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 1b", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 2b", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 3", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 4", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 5", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 5b", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 6", 2.5f, MenuDepthIllustration.MenuShader.Basic));
						}
						else if (menuScene.UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Saint))
						{
							float? scarAlpha = 0f;
							if (menuScene.menu != null && menuScene.menu is SlugcatSelectMenu menu && menu.saveGameData != null && menu.saveGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Saint) && menu.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint] != null)
							{
								scarAlpha = (float)(menu.saveGameData[MoreSlugcatsEnums.SlugcatStatsName.Saint].karmaCap - 1) / 10f;
							}

							scenes.Add(CreateIllus("slugcat - saint - 10", 4f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 10b", 4f, MenuDepthIllustration.MenuShader.Normal));
							scenes[scenes.Count - 1].setAlpha = scarAlpha;
							scenes.Add(CreateIllus("slugcat - saint - 9", 3.8f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 9b", 3.8f, MenuDepthIllustration.MenuShader.Normal));
							scenes[scenes.Count - 1].setAlpha = scarAlpha;
							scenes.Add(CreateIllus("slugcat - saint - 8", 3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 8b", 3f, MenuDepthIllustration.MenuShader.Normal));
							scenes[scenes.Count - 1].setAlpha = scarAlpha;
							scenes.Add(CreateIllus("slugcat - saint - 7", 2.3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 7b", 2.3f, MenuDepthIllustration.MenuShader.Normal));
							scenes[scenes.Count - 1].setAlpha = scarAlpha;
							scenes.Add(CreateIllus("slugcat - saint - 1", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 1b", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 2", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes[scenes.Count - 1].setAlpha = scarAlpha;
							scenes.Add(CreateIllus("slugcat - saint - 3", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 5", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 5b", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("slugcat - saint - 6", 2.5f, MenuDepthIllustration.MenuShader.Basic));
						}
						else
						{
							scenes.Add(CreateIllus("slugcat - saint - 10", 4f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 4f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 9", 3.8f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 3.8f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 8", 3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - 7", 2.3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("empty", 2.3f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("slugcat - saint - dark", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
							scenes.Add(CreateIllus("empty", 2.5f, MenuDepthIllustration.MenuShader.Basic));
						}

						idleDepths =
							[
								3.3f,
								2.3f,
								2f
							];
					}
					else
					{
						if (menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max)
						{
							scenes.Add(CreateIllus("slugcat - saint - flat max"));
						}
						else if (menuScene.UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Saint))
						{
							scenes.Add(CreateIllus("slugcat - saint - flat"));
						}
						else
						{
							scenes.Add(CreateIllus("slugcat - saint dark - flat"));
						}
					}

					return scenes;
				}
			}

			public class IntroScenes
			{
				internal static List<MenuIllustration> Face(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, "intro s2 - face");

					if (!flat)
					{
						scenes.Add(CreateIllus("5 - CloudsB", 8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("4 - CloudsA", 6.1f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("3 - BloomLights", 2.8f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("2 - FaceCloseUp", 2f, MenuDepthIllustration.MenuShader.Normal));
						scenes.Add(CreateIllus("1 - FaceBloom", 1.9f, MenuDepthIllustration.MenuShader.Overlay));
					}
					else
					{
						scenes.Add(CreateIllus("intro s2 - flat"));
					}

					return scenes;
				}

				internal static List<MenuIllustration> Void(ref List<MenuIllustration> scenes)
				{
					sceneFolder = SceneFolder(subDirectory, slugcatName, menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Intro_S1 ? "intro s1 - void" : "intro s3 - worm");

					if (!flat)
					{
						if (menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Intro_S1)
						{
							scenes.Add(CreateIllus("Void - 4", 9f, MenuDepthIllustration.MenuShader.Lighten));
							scenes.Add(CreateIllus("Void - 3", 6f, MenuDepthIllustration.MenuShader.Lighten));
							scenes.Add(CreateIllus("Void - 2", 4f, MenuDepthIllustration.MenuShader.Normal));
							scenes[scenes.Count - 1].setAlpha = 0.7f;
							scenes.Add(CreateIllus("Void - 1", 2.1f, MenuDepthIllustration.MenuShader.Normal));
							scenes.Add(CreateIllus("Upright Slugcat - 2", 1.4f, MenuDepthIllustration.MenuShader.LightEdges));
						}
						else
						{
							scenes.Add(CreateIllus("Void - 4", 9f, MenuDepthIllustration.MenuShader.Lighten));
							scenes.Add(CreateIllus("Void - 3", 6f, MenuDepthIllustration.MenuShader.Lighten));
							scenes.Add(CreateIllus("Ascension", 3.9f, MenuDepthIllustration.MenuShader.LightEdges));
							scenes.Add(CreateIllus("upright slugcat - 1", 6f, MenuDepthIllustration.MenuShader.LightEdges));
						}
					}
					else
					{
						scenes.Add(CreateIllus(menuScene.sceneID == MoreSlugcatsEnums.MenuSceneID.Intro_S1 ? "intro s1 - flat" : "intro s3 - flat"));
					}

					return scenes;
				}
			}
		}
	}

	public class SlideShowMaker
	{
		private static SlideShow slideShow;

		private static float ConvertTime(int minutes, int seconds, int pps)
		{
			if (slideShow != null)
			{
				return slideShow.ConvertTime(minutes, seconds, pps);
			}
			return 0f;
		}

		public static void BuildSlideShow(SlideShow self, ProcessManager manager, SlideShowID slideShowID)
		{
			slideShow = self;
			float fadeInTime = 0f;

			self.playList = GrabPlaylist(slideShowID, ref self.playList, ref self.waitForMusic, ref fadeInTime, ref self.processAfterSlideShow, ref manager.statsAfterCredits);

			if (manager.musicPlayer != null)
			{
				self.stall = true;
				manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 2f, fadeInTime);
			}

			for (int m = 0; m < self.playList.Count; m++)
			{
				Plugin.DebugLog("Loading " + self.playList[m].sceneID);
			}
			Plugin.DebugLog(self.slideShowID.ToString() + " initalized");
		}

		private static List<SlideShow.Scene> GrabPlaylist(SlideShowID slideShowID, ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
		{
			playList = slideShowID switch
			{
				var _ when slideShowID == MoreSlugcatsEnums.SlideShowID.SpearmasterOutro => SpearScenes.GhostOutro(ref playList, ref song, ref fadeIn, ref process, ref stats),
				var _ when slideShowID == SlidesShowIDs.SpearIntro => SpearScenes.Intro(ref playList, ref song, ref fadeIn, ref process, ref stats),
				var _ when slideShowID == SlidesShowIDs.SpearAltOutro => SpearScenes.CommsEnding(ref playList, ref song, ref fadeIn, ref process, ref stats),

				var _ when slideShowID == SlidesShowIDs.ArtificerDreamE => ArtiScenes.FInalDream(ref playList, ref song, ref fadeIn, ref process, ref stats),
				var _ when slideShowID == MoreSlugcatsEnums.SlideShowID.ArtificerAltEnd => ArtiScenes.ScavKingEnding(ref playList, ref song, ref fadeIn, ref process, ref stats),
				var _ when slideShowID == MoreSlugcatsEnums.SlideShowID.ArtificerOutro => ArtiScenes.GhostOutro(ref playList, ref song, ref fadeIn, ref process, ref stats),

				var _ when slideShowID == SlidesShowIDs.RedsDeath => RedScenes.OracleDeathEnding(ref playList, ref song, ref fadeIn, ref process, ref stats),

				var _ when slideShowID == MoreSlugcatsEnums.SlideShowID.SaintIntro => SaintScenes.Intro(ref playList, ref song, ref fadeIn, ref process, ref stats)
			};

			return playList;
		}

		internal class SpearScenes
		{

			internal static List<SlideShow.Scene> CommsEnding(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "NA_11 - Digital Sundown";
				fadeIn = 20f;
				process = ProcessManager.ProcessID.Credits;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, ConvertTime(0, 2, 20));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltComms, ConvertTime(0, 4, 0), ConvertTime(0, 5, 10), ConvertTime(0, 10, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltDescent, ConvertTime(0, 12, 0), ConvertTime(0, 13, 68), ConvertTime(0, 20, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltChimney, ConvertTime(0, 22, 0), ConvertTime(0, 23, 85), ConvertTime(0, 32, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltMice, ConvertTime(0, 34, 0), ConvertTime(0, 38, 0), ConvertTime(0, 43, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltWaterfront, ConvertTime(0, 46, 0), ConvertTime(0, 47, 87), ConvertTime(0, 55, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltLuna, ConvertTime(0, 57, 0), ConvertTime(1, 1, 83), ConvertTime(1, 10, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltChamber, ConvertTime(1, 12, 0), ConvertTime(1, 14, 36), ConvertTime(1, 20, 10));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroAltCollapse, ConvertTime(1, 23, 0), ConvertTime(1, 34, 91), ConvertTime(1, 40, 76));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(1, 44, 0), ConvertTime(1, 47, 0), ConvertTime(1, 48, 0));

				return playList;
			}

			internal static List<SlideShow.Scene> Intro(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "NA_02 - Dustcloud";
				fadeIn = 10f;
				process = ProcessManager.ProcessID.Game;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, ConvertTime(0, 2, 20));
				slideShow.AddNewScene(SceneIDs.Intro_Spearmasterentrance, ConvertTime(0, 2, 20), ConvertTime(0, 3, 40), ConvertTime(0, 7, 10));
				slideShow.AddNewScene(SceneIDs.Intro_Spearmasteroverseer, ConvertTime(0, 9, 80), ConvertTime(0, 11, 20), ConvertTime(0, 14, 80));
				slideShow.AddNewScene(SceneIDs.Intro_Spearmasterlook, ConvertTime(0, 19, 50), ConvertTime(0, 20, 20), ConvertTime(0, 22, 80));
				slideShow.AddNewScene(SceneIDs.Intro_Spearmasterdownlook, ConvertTime(0, 27, 70), ConvertTime(0, 28, 60), ConvertTime(0, 37, 10));
				slideShow.AddNewScene(SceneIDs.Intro_Spearmasterleap, ConvertTime(0, 42, 70), ConvertTime(0, 44, 90), ConvertTime(0, 54, 20));
				slideShow.AddNewScene(SceneIDs.Intro_SpearmasterSRS, ConvertTime(0, 58, 50), ConvertTime(1, 0, 30), ConvertTime(1, 9, 10));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(1, 10, 10), ConvertTime(1, 11, 40), ConvertTime(1, 11, 50));


				return playList;
			}

			internal static List<SlideShow.Scene> GhostOutro(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "RW_Outro_Theme";
				fadeIn = 10f;
				process = ProcessManager.ProcessID.Credits;
				stats = true;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, ConvertTime(0, 2, 0));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterSwimLeft, ConvertTime(0, 2, 20), ConvertTime(0, 4, 0), ConvertTime(0, 10, 0));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroSwim, ConvertTime(0, 12, 0), ConvertTime(0, 13, 0), ConvertTime(0, 17, 0));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroPause, ConvertTime(0, 21, 10), ConvertTime(0, 24, 20), ConvertTime(0, 27, 0));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroLook, ConvertTime(0, 30, 20), ConvertTime(0, 33, 0), ConvertTime(0, 38, 0));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroHop, ConvertTime(0, 41, 10), ConvertTime(0, 45, 20), ConvertTime(0, 46, 60));
				slideShow.AddNewScene(SceneIDs.Outro_SpearmasterOutroEmbrace, ConvertTime(0, 48, 20), ConvertTime(0, 51, 0), ConvertTime(0, 55, 60));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(1, 1, 0), ConvertTime(1, 1, 0), ConvertTime(1, 6, 0));

				return playList;
			}
		}

		internal class ArtiScenes
		{
			internal static List<SlideShow.Scene> GhostOutro(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "RW_Outro_Theme";
				fadeIn = 10f;
				process = ProcessManager.ProcessID.Credits;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, ConvertTime(0, 2, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Outro_Artificer1, ConvertTime(0, 2, 20), ConvertTime(0, 4, 0), ConvertTime(0, 10, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Outro_Artificer2, ConvertTime(0, 12, 0), ConvertTime(0, 13, 0), ConvertTime(0, 17, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Outro_Artificer3, ConvertTime(0, 21, 10), ConvertTime(0, 24, 20), ConvertTime(0, 28, 0));
				slideShow.AddNewScene(SceneIDs.Outro_Artificer6, ConvertTime(0, 31, 20), ConvertTime(0, 34, 0), ConvertTime(0, 38, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Outro_Artificer4, ConvertTime(0, 41, 10), ConvertTime(0, 45, 20), ConvertTime(0, 46, 60));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Outro_Artificer5, ConvertTime(0, 48, 20), ConvertTime(0, 51, 0), ConvertTime(0, 55, 60));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(1, 1, 0), ConvertTime(1, 1, 0), ConvertTime(1, 6, 0));

				return playList;
			}

			internal static List<SlideShow.Scene> ScavKingEnding(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "NA_10 - Qanda";
				fadeIn = 10f;
				process = ProcessManager.ProcessID.Credits;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, 0f);
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_1, ConvertTime(0, 1, 20), ConvertTime(0, 4, 0), ConvertTime(0, 8, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_2, ConvertTime(0, 10, 0), ConvertTime(0, 13, 0), ConvertTime(0, 17, 0));
				slideShow.AddNewScene(SceneIDs.AltEnd_Artificer_4, ConvertTime(0, 21, 10), ConvertTime(0, 25, 20), ConvertTime(0, 29, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.AltEnd_Artificer_3, ConvertTime(0, 32, 10), ConvertTime(0, 42, 20), ConvertTime(0, 45, 0));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(0, 46, 0), ConvertTime(0, 47, 0), ConvertTime(0, 48, 0));

				return playList;
			}

			internal static List<SlideShow.Scene> FInalDream(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				process = ProcessManager.ProcessID.SleepScreen;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, 0f);
				// NOTE: Add scene here

				return playList;
			}
		}

		internal class RedScenes
		{
			internal static List<SlideShow.Scene> OracleDeathEnding(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "RW_Outro_Theme";
				fadeIn = 10f;
				process = ProcessManager.ProcessID.Credits;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, ConvertTime(0, 2, 0));
				// NOTE: Add scene here

				return playList;
			}
		}

		internal class SaintScenes
		{
			internal static List<SlideShow.Scene> Intro(ref List<SlideShow.Scene> playList, ref string song, ref float fadeIn, ref ProcessManager.ProcessID process, ref bool stats)
			{
				song = "Totally seen lands";
				fadeIn = 10f;
				process = ProcessManager.ProcessID.Game;

				slideShow.AddNewScene(MenuScene.SceneID.Empty, 0f, 0f, ConvertTime(0, 3, 0));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Intro_S1, ConvertTime(0, 3, 20), ConvertTime(0, 8, 0), ConvertTime(0, 14, 80));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Intro_S2, ConvertTime(0, 19, 0), ConvertTime(0, 19, 60), ConvertTime(0, 28, 30));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(0, 29, 40), ConvertTime(0, 29, 60), ConvertTime(0, 30, 60));
				slideShow.AddNewScene(MoreSlugcatsEnums.MenuSceneID.Intro_S4, ConvertTime(0, 31, 60), ConvertTime(0, 33, 0), ConvertTime(0, 38, 40));
				slideShow.AddNewScene(MenuScene.SceneID.Empty, ConvertTime(0, 40, 0), ConvertTime(0, 40, 20), ConvertTime(0, 41, 0));

				return playList;
			}
		}
	}
}

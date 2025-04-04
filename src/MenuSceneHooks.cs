using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using static Menu.SlugcatSelectMenu;
using SlideShowID = Menu.SlideShow.SlideShowID;

namespace MagicasContentPack
{
	internal class MenuSceneHooks
	{
		private static bool playedCustomSound;

		public static int artiKarmaCap;
		public List<Color> slugcatColor;

		public static Dictionary<SlugcatStats.Name, ExtraGameData> magicaGameData;
		private static bool spearVisitedFP;
		private static int saintKarmaCap;

		public static string OracleValue { get; set; }


		internal static void PreInit()
		{
			try
			{
				On.Menu.MenuScene.BuildScene += BuildModifiedScenes;
				On.Menu.MenuScene.BuildMSCScene += BuildModifiedScenesMSC;
			}
			catch
			{
				Plugin.Log(Plugin.LogStates.HookFail, nameof(MenuSceneHooks) + " pre");
			}
		}

		internal static void Init()
		{
			try
			{
				// For building new scenes

				// Used for updating opacitys or depths on non-slideshow objects
				On.Menu.MenuScene.Update += MenuScene_Update;

				// For building new slideshows
				IL.Menu.SlideShow.ctor += AddIlSlideShow;
				On.Menu.SlideShow.RawUpdate += SlideShow_RawUpdate;
				On.Menu.SlideShowMenuScene.ApplySceneSpecificAlphas += ApplyNewAlphas;

				// Changes spearmaster's name
				On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
				On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += SlugcatPageNewGame_ctor;

				// For changing slugcatpage art and adding compatibility with custom colors
				On.Menu.SlugcatSelectMenu.SlugcatPage.AddAltEndingImage += AddCustomAltImages;
				On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += AddCustomImages;
				On.Menu.SlugcatSelectMenu.SlugcatPage.Update += CustomColorUpdate;

				// For adding custom intros
				IL.Menu.SlugcatSelectMenu.StartGame += AddIntroListeners;

				// Intro rolls
				IL.Menu.IntroRoll.ctor += IntroRoll_ctor;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				Plugin.Log(Plugin.LogStates.HookFail, nameof(MenuSceneHooks));
			}
		}

		private static void IntroRoll_ctor(ILContext il)
		{
			try
			{
				int stLoc = 4;
				ILCursor cursor = new(il);

				bool sooceed = cursor.TryGotoNext(
					x => x.MatchStloc(stLoc)
					);

				if (!sooceed)
				{
					Plugin.Log(Plugin.LogStates.FailILMatch, nameof(IntroRoll_ctor));
					return;
				}

				static string[] AddToArray(string[] names)
				{
					if (!string.IsNullOrEmpty(Plugin.modPath) && Directory.Exists(Plugin.modPath + Path.DirectorySeparatorChar + "illustrations"))
					{
						List<string> namesToAdd = [];
						string[] files = Directory.GetFiles(Plugin.modPath + Path.DirectorySeparatorChar + "illustrations").Where(x => x.Contains("intro_roll_c_")).ToArray();
						foreach (string slugRoll in files)
						{
							string subStringName = Path.GetFileNameWithoutExtension(slugRoll).Substring("intro_roll_c_".Length);
							if (!names.Contains(subStringName))
							{
								Plugin.DebugLog($"{subStringName} found in illustrations!");
								namesToAdd.Add(subStringName);
							}
						}
						if (namesToAdd.Count > 0)
						{
							return names.Concat(namesToAdd.ToArray()).ToArray();
						}
					}
					return names;
				}
				cursor.EmitDelegate(AddToArray);


				if (Plugin.debugState)
				{
					cursor.GotoNext(MoveType.After, x => x.MatchLdloc(stLoc));
					cursor.EmitDelegate((string[] array) =>
					{
						if (!array.Any(x => string.IsNullOrEmpty(x)))
						{
							Plugin.DebugLog("INTRO ROLL: " + string.Join(", ", array));
						}
						return array;
					});
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private static void MenuScene_Update(On.Menu.MenuScene.orig_Update orig, MenuScene self)
		{
			orig(self);

			if (self.sceneID == MenuScene.SceneID.SleepScreen && WinOrSaveHooks.HunterHasGreenNeuron && self.depthIllustrations.Count > 29 && SceneMaker.DreamScenes.dotFade < 200)
			{
				if (SceneMaker.DreamScenes.lastCycleCount > 0 && SceneMaker.DreamScenes.lastCycleCount <= 25)
				{
					SceneMaker.DreamScenes.dotFade++;
					if (SceneMaker.DreamScenes.dotFade > 100)
					{
						self.depthIllustrations[SceneMaker.DreamScenes.lastCycleCount + 11].alpha = 1f - (((float)SceneMaker.DreamScenes.dotFade - 100) / 100f);
					}
				}
			}
		}

		

		private static void SlideShow_RawUpdate(On.Menu.SlideShow.orig_RawUpdate orig, SlideShow self, float dt)
		{
			orig(self, dt);

			if (self.slideShowID == MagicaEnums.SlidesShowIDs.SpearAltOutro)
			{
				if (self.scene.sceneID == MagicaEnums.SceneIDs.Outro_SpearmasterOutroAltCollapse)
				{
					if (!playedCustomSound && self.manager.musicPlayer != null)
					{
						self.waitForMusic = "MOON_SIREN";
						self.manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 3f, 350f);
						playedCustomSound = true;
					}

				}
				if (playedCustomSound && self.scene.sceneID == MenuScene.SceneID.Empty)
				{
					playedCustomSound = false;
					self.waitForMusic = "RW_95 - Reflection of the Moon";
					self.manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 4f, 50f);
				}
			}
		}

		// Pushes new code into the SlideShow ctor after the[self.playList = new List<SlideShow.Scene>();] line
		private static void AddIlSlideShow(ILContext il)
		{
			ILCursor cursor = new(il);

			bool success = cursor.TryGotoNext(
				MoveType.Before,
			instruction => instruction.MatchLdfld(out _),
			instruction => instruction.MatchCallvirt(out _),
			instruction => instruction.MatchNewarr(out _)
			);

			if (!success)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(AddIlSlideShow));
			}

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldarg_1);
			cursor.Emit(OpCodes.Ldarg_2);
			static void PatchSlideshow(SlideShow self, ProcessManager manager, SlideShowID slideShowID)
			{
				if (ModOptions.CustomSlideShows.Value)
				{
					if (SceneMaker.IsCustomSlideShow(slideShowID))
					{
						self.playList.Clear();
						SlideShowMaker.BuildSlideShow(self, manager, slideShowID);
					}
				}
			}
			cursor.EmitDelegate(PatchSlideshow);

			Plugin.DebugLog("Slideshow code initiated");
		}

		private static void BuildModifiedScenes(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
		{
			if (self.sceneID != MenuScene.SceneID.Empty && SceneMaker.ReturnCustomScene(self.sceneID))
			{
				if (self is InteractiveMenuScene interactive)
				{
					interactive.idleDepths = [];
				}

				SlugcatStats.Name currentSlugcat;
				if (self.menu.manager.currentMainLoop is RainWorldGame game)
				{
					currentSlugcat = game.StoryCharacter;
				}
				else
				{
					currentSlugcat = self.menu.manager.rainWorld.progression.PlayingAsSlugcat;
				}

				if (self.sceneID == MenuScene.SceneID.SleepScreen && SceneMaker.SleepSlugcats().Contains(currentSlugcat))
				{
					SceneMaker.BuildScene(self);
					return;
				}
				else if (self.sceneID != MenuScene.SceneID.SleepScreen && SceneMaker.ReturnCustomScene(self.sceneID))
				{
					SceneMaker.BuildScene(self);
					return;
				}
			}

			orig(self);
		}

		private static void BuildModifiedScenesMSC(On.Menu.MenuScene.orig_BuildMSCScene orig, MenuScene self)
		{
			if (SceneMaker.ReturnCustomScene(self.sceneID))
			{
				return;
			}

			orig(self);
		}

		// Adds new scene-specific alphas
		private static void ApplyNewAlphas(On.Menu.SlideShowMenuScene.orig_ApplySceneSpecificAlphas orig, SlideShowMenuScene self)
		{
			if (ModOptions.CustomSlideShows.Value)
			{
				SceneMaker.ApplySceneSpecificAlphas(self);
			}

			orig(self);
		}

		private static void SlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
		{
			orig(self, menu, owner, pageIndex, slugcatNumber);

			if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.menu.manager.rainWorld.progression != null && self.menu.manager.rainWorld.progression.miscProgressionData != null && self.menu.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(WinOrSaveHooks.SpearMetSRS), out _))
			{
				self.regionLabel.text = menu.Translate("THE SPEARMASTER");
			}
		}

		private static void SlugcatPageNewGame_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
		{
			orig(self, menu, owner, pageIndex, slugcatNumber);

			if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
			{
				self.difficultyLabel.text = menu.Translate("THE MESSENGER");
			}
		}

		private static void AddCustomAltImages(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddAltEndingImage orig, SlugcatPage self)
		{
			if (ModOptions.CustomSlugcatPages.Value && self.slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
			{
				self.imagePos = new Vector2(683f, 484f);
				self.sceneOffset = default;
				self.slugcatDepth = 1f;
				MenuScene.SceneID sceneID = MoreSlugcatsEnums.MenuSceneID.AltEnd_Spearmaster;

				if (self.slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					if (self.menu.manager.rainWorld.progression != null && self.menu.manager.rainWorld.progression.miscProgressionData != null && self.menu.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(WinOrSaveHooks.SpearMetSRS), out _))
					{
						sceneID = MagicaEnums.SceneIDs.CustomSlugcat_SpearSRS;
						self.slugcatDepth = 3f;
						self.sceneOffset = new Vector2(10f, 75f);
						Plugin.DebugLog("Spear has visited SRS!");
					}
				}

				self.sceneOffset.x -= (1366f - self.menu.manager.rainWorld.options.ScreenSize.x) / 2f;
				self.slugcatImage = new InteractiveMenuScene(self.menu, self, sceneID);
				self.subObjects.Add(self.slugcatImage);
				return;
			}
			else
			{
				orig(self);
			}
		}

		private static void AddCustomImages(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, SlugcatPage self, bool ascended)
		{
			Vector2 sceneOffset = default;
			float slugcatDepth = 1f;
			Vector2 markOffset = default;
			Vector2 glowOffset = default;

			if (ModOptions.CustomSlugcatPages.Value)
			{
				magicaGameData = new Dictionary<SlugcatStats.Name, ExtraGameData>
				{
					[self.slugcatNumber] = MineForExtraData(self.menu.manager, self.slugcatNumber)
				};

				if (magicaGameData.ContainsKey(self.slugcatNumber) && magicaGameData[self.slugcatNumber] != null)
				{
					ExtraGameData data = magicaGameData[self.slugcatNumber];

					if (self.slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && data.ssAiConvosHad > 0)
					{
						spearVisitedFP = true;
						Plugin.DebugLog("Spear has visited FP!");
						Plugin.DebugLog("It's been " + data.cyclesSinceSsAi + " cycles since SSai!");
					}

					if (self.slugcatNumber == SlugcatStats.Name.Red && !string.IsNullOrEmpty(data.redDeath))
					{
						OracleValue = data.redDeath;
						Plugin.DebugLog("Hunter died with: " + OracleValue);
					}
				}

				self.imagePos = new Vector2(683f, 484f);
				self.sceneOffset = default;
				self.slugcatDepth = 1f;
				MenuScene.SceneID sceneID = MenuScene.SceneID.Slugcat_White;

				if (self.slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
				{
					if (spearVisitedFP)
					{
						sceneID = MagicaEnums.SceneIDs.CustomSlugcat_SpearPearl;
					}

					sceneOffset = new Vector2(-10f, 100f);
					slugcatDepth = 3.1000001f;
					markOffset = new Vector2(17f, -21f);
					glowOffset = new Vector2(-30f, -50f);
				}

				if (self.slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
				{
					if (self is SlugcatPageContinue slugPage)
					{
						artiKarmaCap = slugPage.saveGameData.karmaCap;
					}

					sceneOffset = new Vector2(10f, 75f);
					slugcatDepth = 3f;
					markOffset = new Vector2(-28f, -16f);
					glowOffset = new Vector2(-50f, -50f);
				}

				if (self.slugcatNumber == SlugcatStats.Name.Red)
				{
					if (self.menu is SlugcatSelectMenu menu && menu.redIsDead && !string.IsNullOrEmpty(OracleValue))
					{
						sceneID = MenuScene.SceneID.Slugcat_Dead_Red;
					}

					sceneOffset = new Vector2(10f, 45f);
					slugcatDepth = 2.7f;
					markOffset = new Vector2(-150f, -70f);
					glowOffset = new Vector2(-30f, -50f);
				}

				if (self.slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
				{
					if (self is SlugcatPageContinue slugPage)
					{
						saintKarmaCap = slugPage.saveGameData.karmaCap;
					}

					sceneOffset = new Vector2(10f, 75f);
					slugcatDepth = 3f;
					markOffset = new Vector2(-28f, -16f);
					glowOffset = new Vector2(-50f, -50f);
				}


				if (sceneID != MenuScene.SceneID.Slugcat_White)
				{
					Plugin.DebugLog(self.slugcatNumber.value + " needs custom art, implementing!");

					self.sceneOffset = sceneOffset;
					self.slugcatDepth = slugcatDepth;
					self.markOffset = markOffset;
					self.glowOffset = glowOffset;

					self.sceneOffset.x -= (1366f - self.menu.manager.rainWorld.options.ScreenSize.x) / 2f;
					self.slugcatImage = new InteractiveMenuScene(self.menu, self, sceneID);
					self.subObjects.Add(self.slugcatImage);
					if (self.HasMark)
					{
						self.markSquare = new FSprite("pixel", true)
						{
							scale = 14f,
							color = Color.Lerp(self.effectColor, Color.white, 0.7f)
						};
						self.Container.AddChild(self.markSquare);
						self.markGlow = new FSprite("Futile_White", true)
						{
							shader = self.menu.manager.rainWorld.Shaders["FlatLight"],
							color = self.effectColor
						};
						self.Container.AddChild(self.markGlow);
					}
					return;
				}
			}

			orig(self, ascended);

			if (ModOptions.CustomSlugcatPages.Value && markOffset != default && self.sceneOffset != markOffset)
			{
				self.sceneOffset = sceneOffset;
				self.slugcatDepth = slugcatDepth;
				self.markOffset = markOffset;
				self.glowOffset = glowOffset;
			}
		}

		private static void CustomColorUpdate(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_Update orig, SlugcatPage self)
		{
			bool slugcatHasCustomColor = self.menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(self.slugcatNumber.value);
			SlugcatColorsStorage slugcatColors = SlugcatColorsStorage.slugcatColorsCWT.GetOrCreateValue(self);

			if (slugcatColors != null && self.menu.manager.rainWorld.options.quality == Options.Quality.HIGH && slugcatHasCustomColor && ModOptions.CustomSlugcatPages.Value)
			{
				slugcatColors.colorsList = GetColorChoices(self, slugcatHasCustomColor);

				if (self.slugcatImage.depthIllustrations != null)
				{
					List<Color> colors = [];

					for (int j = 0; j < slugcatColors.colorsList.Count; j++)
					{
						Vector3 color = slugcatColors.colorsList[j];
						if (self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer || self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo)
						{
							color += new Vector3(0f, 0.2f, 0f);
						}
						if (self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max)
						{
							color = Vector3.Lerp(color, new Vector3(color[0], color[1], 0f), 0.8f);
						}

						colors.Add(Custom.HSL2RGB(color[0], color[1], color[2]));
					}

					CheckForCustomScenesThatHasColorableImages(self, colors, self.slugcatImage.depthIllustrations);

					if (SceneMaker.ReturnCustomScene(self.slugcatImage.sceneID))
					{
						if (self.markSquare != null)
						{
							self.markSquare.color = colors[0];
						}
						if (self.glowSpriteA != null)
						{
							self.glowSpriteA.color = colors[0];
							self.glowSpriteB.color = colors[0];
						}
					}
				}
			}

			if (Plugin.debugState)
			{
				if (self.markSquare != null && self.glowSpriteA != null && Input.GetKey("f"))
				{
					self.markOffset += new Vector2(-1f, 0f);
					Plugin.DebugLog("Mark offset: " + self.markOffset.ToString());

					self.glowOffset += new Vector2(-1f, 0f);
					Plugin.DebugLog("Glow offset: " + self.glowOffset.ToString());
				}
				if (self.markSquare != null && Input.GetKey("g"))
				{
					self.markOffset += new Vector2(0f, -1f);
					Plugin.DebugLog("Mark offset: " + self.markOffset.ToString());

					self.glowOffset += new Vector2(0f, -1f);
					Plugin.DebugLog("Glow offset: " + self.glowOffset.ToString());
				}
				if (self.markSquare != null && Input.GetKey("h"))
				{
					self.markOffset += new Vector2(1f, 0f);
					Plugin.DebugLog("Mark offset: " + self.markOffset.ToString());

					self.glowOffset += new Vector2(1f, 0f);
					Plugin.DebugLog("Glow offset: " + self.glowOffset.ToString());
				}
				if (self.markSquare != null && Input.GetKey("t"))
				{
					self.markOffset += new Vector2(0f, 1f);
					Plugin.DebugLog("Mark offset: " + self.markOffset.ToString());

					self.glowOffset += new Vector2(0f, 1f);
					Plugin.DebugLog("Glow offset: " + self.glowOffset.ToString());
				}
			}

			orig(self);
		}

		private static List<Vector3> GetColorChoices(SlugcatPage self, bool slugcatHasCustomColor)
		{
			if (slugcatHasCustomColor && self.menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled[self.slugcatNumber.value])
			{
				return (from x in self.menu.manager.rainWorld.progression.miscProgressionData.colorChoices[self.slugcatNumber.value] select ConvertStringToHSL(x)).ToList();
			}
			else
			{
				return PlayerGraphics.DefaultBodyPartColorHex(self.slugcatNumber).Select(x => Custom.RGB2HSL(Custom.hexToColor(x))).ToList();
			}
		}

		private static void CheckForCustomScenesThatHasColorableImages(SlugcatPage self, List<Color> colors, List<MenuDepthIllustration> imageList)
		{
			Dictionary<MenuDepthIllustration, Color> coloredImages = [];
			if (self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Spear || self.slugcatImage.sceneID == MagicaEnums.SceneIDs.CustomSlugcat_SpearPearl)
			{
				coloredImages.Add(imageList[4], colors[0]);
				coloredImages.Add(imageList[5], colors[0]);
				coloredImages.Add(imageList[6], GetHighlightColor(colors[0]));

				coloredImages.Add(imageList[7], colors[1]);

				float scarOpacity = 0.25f;
				if (self.menu is SlugcatSelectMenu && magicaGameData != null && magicaGameData.ContainsKey(MoreSlugcatsEnums.SlugcatStatsName.Spear) && magicaGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear] != null && magicaGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].ssAiConvosHad > 1)
				{
					scarOpacity = (float)magicaGameData[MoreSlugcatsEnums.SlugcatStatsName.Spear].cyclesSinceSsAi;
				}

				self.slugcatImage.depthIllustrations[8].setAlpha = (1f - (scarOpacity / 10f)) * 0.2f;

				coloredImages.Add(imageList[9], colors[2]);
				coloredImages.Add(imageList[10], colors[2]);
			}

			if (self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer || self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer_Robo)
			{
				coloredImages.Add(imageList[6], colors[0]);
				coloredImages.Add(imageList[7], colors[0]);

				coloredImages.Add(imageList[8], colors[2]);
				coloredImages.Add(imageList[9], colors[2]);

				coloredImages.Add(imageList[11], GetHighlightColor(colors[0]));

				coloredImages.Add(imageList[12], colors[1]);
			}

			if (self.slugcatImage.sceneID == MenuScene.SceneID.Slugcat_Red)
			{
				coloredImages.Add(imageList[2], colors[0]);
				coloredImages.Add(imageList[3], colors[0]);
				coloredImages.Add(imageList[4], colors[0]);

				coloredImages.Add(imageList[5], colors[1]);

				coloredImages.Add(imageList[6], colors[2]);
				coloredImages.Add(imageList[7], colors[2]);

				coloredImages.Add(imageList[9], GetHighlightColor(colors[0]));
			}

			if (self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint || self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max)
			{
				coloredImages.Add(imageList[8], colors[0]);
				coloredImages.Add(imageList[9], colors[0]);

				coloredImages.Add(imageList[11], colors[1]);

				if (self.slugcatImage.sceneID != MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max)
				{
					coloredImages.Add(imageList[10], colors[2]);
				}
				coloredImages.Add(imageList[13], colors[2]);
				coloredImages.Add(imageList[14], colors[2]);

				if (self.slugcatImage.sceneID == MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint_Max)
				{
					coloredImages.Add(imageList[15], new(1f, 0.906f, 0.525f));
				}
                else
                {
					coloredImages.Add(imageList[15], GetHighlightColor(colors[0]));
				}
			}

			foreach (var image in self.slugcatImage.depthIllustrations)
			{
				if (coloredImages.ContainsKey(image))
				{
					image.sprite.color = coloredImages[image];
				}
			}
		}

		private static Color GetHighlightColor(Color color)
		{
			return color;
		}

		private static Vector3 ConvertStringToHSL(string x)
		{
			Vector3 color = default;
			if (x.Contains(","))
			{
				string[] array = x.Split(new char[]
				{
					','
				});
				color = new Vector3(float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture));
			}
			return color;
		}

		private static void AddIntroListeners(ILContext il)
		{
			ILCursor cursor = new(il);

			bool soocood = cursor.TryGotoNext(
				x => x.MatchBrtrue(out _),
				x => x.MatchLdstr("s")
				);

			if (!soocood)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(AddIntroListeners) + " #1");
			}

			ILLabel nextJump = (ILLabel)cursor.Next.Operand;

			cursor.Emit(OpCodes.Brfalse_S, nextJump);
			cursor.Emit(OpCodes.Ldarg_1); 
			static bool HasCustomIntro(SlugcatStats.Name name)
			{
				return name != MoreSlugcatsEnums.SlugcatStatsName.Spear;
			}
			cursor.EmitDelegate(HasCustomIntro);


			bool success = cursor.TryGotoNext(
					MoveType.After,
				x => x.MatchLdsfld<SlideShowID>(nameof(SlideShowID.WhiteIntro)),
				x => x.MatchStfld(out _),
				x => x.MatchLdarg(0)
			);

			if (!success)
			{
				Plugin.Log(Plugin.LogStates.FailILMatch, nameof(AddIntroListeners) + " #2");
			}

			cursor.Emit(OpCodes.Ldarg_1);
			static void AddIntros(SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
			{
				if (ModOptions.CustomSlideShows.Value)
				{
					if (storyGameCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear)
					{
						self.manager.nextSlideshow = MagicaEnums.SlidesShowIDs.SpearIntro;
						self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
					}
				}

				Plugin.DebugLog("Intro changed to: " + self.manager.nextSlideshow.value);
			}
			cursor.EmitDelegate(AddIntros);
			cursor.Emit(OpCodes.Ldarg_0);
		}

		public static ExtraGameData MineForExtraData(ProcessManager manager, SlugcatStats.Name slugcat)
		{
			if (!manager.rainWorld.progression.IsThereASavedGame(slugcat))
			{
				return null;
			}
			if (manager.rainWorld.progression.currentSaveState != null && manager.rainWorld.progression.currentSaveState.saveStateNumber == slugcat)
			{
				if (manager.rainWorld.progression.miscProgressionData != null && manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet<bool>(nameof(WinOrSaveHooks.SpearMetSRS), out bool spearSRS))
				{
					WinOrSaveHooks.SpearMetSRS = spearSRS;
				}

				if (manager.rainWorld.progression.miscProgressionData != null && manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet<string>(nameof(WinOrSaveHooks.HunterOracleID), out string redsDeath))
				{
					WinOrSaveHooks.HunterOracleID = redsDeath;
				}

				ExtraGameData extraGameData = new()
				{
					ssAiConvosHad = manager.rainWorld.progression.currentSaveState.miscWorldSaveData.SSaiConversationsHad,
					cyclesSinceSsAi = manager.rainWorld.progression.currentSaveState.miscWorldSaveData.cyclesSinceSSai,
					spearMetSRS = WinOrSaveHooks.SpearMetSRS,
					redDeath = WinOrSaveHooks.HunterOracleID
				};

				return extraGameData;
			}

			if (!manager.rainWorld.progression.HasSaveData)
			{
				return null;
			}
			string[] progLinesFromMemory = manager.rainWorld.progression.GetProgLinesFromMemory();
			if (progLinesFromMemory.Length == 0)
			{
				return null;
			}

			for (int i = 0; i < progLinesFromMemory.Length; i++)
			{
				string[] array = Regex.Split(progLinesFromMemory[i], "<progDivB>");
				if (array.Length == 2 && array[0] == "SAVE STATE" && BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) == slugcat)
				{
					List<SaveStateMiner.Target> list =
					[
						new SaveStateMiner.Target(">SSaiConversationsHad", "<mwB>", "<mwA>", 20),
						new SaveStateMiner.Target(">CyclesSinceSSai", "<mwB>", "<mwA>", 20),
						new SaveStateMiner.Target(">OBJECTTRACKERS", "<svB>", "<svA>", 200),
					];

					List<SaveStateMiner.Result> list2 = SaveStateMiner.Mine(manager.rainWorld, array[1], list);
					ExtraGameData extraGameData2 = new();

					for (int j = 0; j < list2.Count; j++)
					{
						string name = list2[j].name;

						switch (name)
						{
							case ">SSaiConversationsHad":
								try
								{
									extraGameData2.ssAiConvosHad = int.Parse(list2[j].data, NumberStyles.Any, CultureInfo.InvariantCulture);
								}
								catch
								{
									Debug.LogWarning("failed to find ssaiconversationshas. Data: " + list2[j].data);
								}
								break;

							case ">CyclesSinceSSai":
								try
								{
									extraGameData2.cyclesSinceSsAi = int.Parse(list2[j].data, NumberStyles.Any, CultureInfo.InvariantCulture);
								}
								catch
								{
									Debug.LogWarning("failed to find cyclessincessai. Data:" + list2[j].data);
								}
								break;

							case ">OBJECTTRACKERS":
								try
								{
									extraGameData2.pearlBroadcastTagged = list2[j].data.Contains("<oA>Spearmasterpearl<oA>1");
								}
								catch
								{
									Debug.LogWarning("failed to find spearmasterpearl. Data:" + list2[j].data);
								}
								break;

							default:
								break;
						}
					}
					return extraGameData2;
				}
			}
			return null;
		}
	}
	public class ExtraGameData
	{
		public ExtraGameData() { }

		public int cyclesSinceSsAi;

		public int ssAiConvosHad;
		internal bool spearMetSRS;
		internal string redDeath;

		public bool pearlBroadcastTagged;
	}

	public class SlugcatColorsStorage
	{
		public static readonly ConditionalWeakTable<SlugcatPage, SlugcatColorsStorage> slugcatColorsCWT = new();

		public List<Vector3> colorsList;
	}
}
﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Music;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.RuntimeDetour;
using DressMySlugcat;
using System.Reflection;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace MagicasContentPack
{
	public class ObjectHooks
	{
		public static SaintBroadcastEffect ghostScreen;
		private static string saintBroadcastSong;
		private static int startSaintSongAtSeconds = 0;
		private static int saintBroadcastNum;
		private static bool setSaintSongTime;

		public static void Init()
		{
			try
			{
				// For Saint mechanic
				On.MoreSlugcats.SnowSource.Update += SnowSource_Update;
				On.MoreSlugcats.GhostPing.DrawSprites += GhostPing_DrawSprites;

				On.Room.PlaySound_SoundID_Vector2_float_float += Room_PlaySound_SoundID_Vector2_float_float;

				// Add custom pearls manually
				On.DataPearl.ApplyPalette += DataPearl_ApplyPalette;
				On.DataPearl.UniquePearlHighLightColor += DataPearl_UniquePearlHighLightColor;
				On.DataPearl.UniquePearlMainColor += DataPearl_UniquePearlMainColor;

				// Change whitetoken behavior for saint
				IL.MoreSlugcats.CollectiblesTracker.ctor += CollectiblesTracker_ctor;
				On.Music.MusicPlayer.Update += MusicPlayer_Update;
				On.MoreSlugcats.ChatLogDisplay.NewMessage_string_int += ChatLogDisplay_NewMessage_string_int;
				On.HUD.DialogBox.Update += DialogBox_Update;
				On.HUD.DialogBox.GetDelay += DialogBox_GetDelay;
				On.Music.PlayerThreatTracker.Update += PlayerThreatTracker_Update;
				IL.Room.Loaded += Room_Loaded;
				On.Player.ProcessChatLog += Player_ProcessChatLog;
				On.CollectToken.ctor += CollectToken_ctor;
				On.CollectToken.DrawSprites += CollectToken_DrawSprites;
				On.CollectToken.AddToContainer += CollectToken_AddToContainer;
				On.CollectToken.TokenStalk.Update += TokenStalk_Update;
				On.CollectToken.TokenStalk.AddToContainer += TokenStalk_AddToContainer;
				On.CollectToken.TokenStalk.DrawSprites += TokenStalk_DrawSprites;
				On.CollectToken.TokenStalk.ApplyPalette += TokenStalk_ApplyPalette;
				On.CollectToken.Pop += CollectToken_Pop;

				// Change the color of saint broadcasts
				On.MoreSlugcats.CollectionsMenu.ctor += CollectionsMenu_ctor;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static void SnowSource_Update(On.MoreSlugcats.SnowSource.orig_Update orig, SnowSource self, bool eu)
		{
			if (PlayerHooks.warmMode)
			{
				return;
			}
			orig(self, eu);	
		}

		private static void GhostPing_DrawSprites(On.MoreSlugcats.GhostPing.orig_DrawSprites orig, GhostPing self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (self.slatedForDeletetion)
			{
				if (rCam.room != null && rCam.room.world.worldGhost != null)
				{
					rCam.ghostMode = rCam.room.world.worldGhost.GhostMode(rCam.room, rCam.currentCameraPosition);
				}
				else
				{
					rCam.ghostMode = 0f;
				}

				sLeaser.CleanSpritesAndRemove();
				self.RemoveFromRoom();
				return;
			}
			if (self is SaintWarmthTransistion transistion && !transistion.go)
			{
				float num = ((float)PlayerHooks.warmthActivationTimer / (float)PlayerHooks.MaxWarmthActivationTimer) - 0.6f;
				if (num == 0f)
				{
					sLeaser.sprites[0].isVisible = false;
					return;
				}
				sLeaser.sprites[0].isVisible = true;
				sLeaser.sprites[0].alpha = 0.8f * num * self.alpha;
				rCam.ghostMode = num * self.alpha;
				return;
			}

			orig(self, sLeaser, rCam, timeStacker, camPos); 
		}

		private static void CollectiblesTracker_ctor(ILContext il)
		{
			try
			{
				ILCursor cursor = new(il);

				bool succeed = cursor.TryGotoNext(MoveType.After,
					x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)),
					x => x.MatchCall(out _));

				if (Plugin.ILMatchFail(succeed))
					return;

				ILCursor label = cursor;
				ILLabel jump = (ILLabel)cursor.Next.Operand;

				succeed = cursor.TryGotoNext(MoveType.After, 
					x => x.MatchBrfalse(out _));

				if (Plugin.ILMatchFail(succeed))
					return;

				ILLabel next = cursor.MarkLabel();
				label.Next.Operand = next;
				cursor.Emit(OpCodes.Ldarg, 5);
				static bool IsSaint(SlugcatStats.Name saveSlot)
				{
					return saveSlot == MoreSlugcatsEnums.SlugcatStatsName.Saint;
				}
				cursor.EmitDelegate(IsSaint);
				cursor.Emit(OpCodes.Brfalse, jump);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}

		private static void TokenStalk_Update(On.CollectToken.TokenStalk.orig_Update orig, CollectToken.TokenStalk self, bool eu)
		{
			if (self.room?.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && self.token == null && self.forceSatellite)
			{
				self.Destroy();
			}

			orig(self, eu);
		}

		private static void Room_PlaySound_SoundID_Vector2_float_float(On.Room.orig_PlaySound_SoundID_Vector2_float_float orig, Room self, SoundID soundId, Vector2 pos, float vol, float pitch)
		{
			if (soundId == SoundID.Coral_Circuit_Jump_Explosion) { vol /= 3f; }
			orig(self, soundId, pos, vol, pitch);
		}

		public static void PostInit()
		{
			try
			{
				On.MoreSlugcats.ChatlogData.getChatlog_ChatlogID += GetSaintSong;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static string[] GetSaintSong(On.MoreSlugcats.ChatlogData.orig_getChatlog_ChatlogID orig, ChatlogData.ChatlogID id)
		{
			if (id.value.Contains("Saintchatlog_"))
			{
				startSaintSongAtSeconds = 0;
				saintBroadcastNum = int.Parse(id.value.Substring("Saintchatlog_".Length));
				Plugin.DebugLog($"Log ID: {saintBroadcastNum}");
				switch (saintBroadcastNum)
				{
					case < 3:
						saintBroadcastSong = "Matvis1 - Stagnation";
						if (saintBroadcastNum == 2)
							startSaintSongAtSeconds = 45;
						break;

					case < 7:
						saintBroadcastSong = "Matvis2 - Remembrance";
						if (saintBroadcastNum > 4)
							startSaintSongAtSeconds = (60 * 1) + 42;
						break;

					case < 12:
						saintBroadcastSong = "Matvis3 - Sentimentality";
						if (saintBroadcastNum > 7)
							startSaintSongAtSeconds = (60 * 1);
						break;

					case < 18:
						saintBroadcastSong = "Matvis4 - Desperation";
						if (saintBroadcastNum > 13)
							startSaintSongAtSeconds = (60 * 1) + 2;
						break;

					case >= 18:
						saintBroadcastSong = "Matvis5 - Oneness";
						if (saintBroadcastNum > 19)
							startSaintSongAtSeconds = (60 * 1) + 20;
						else if (saintBroadcastNum > 21)
								startSaintSongAtSeconds = (60 * 2) + 31;
						break;
				}
			}

			return orig(id);
		}

		private static void DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			if (self.AbstractPearl.dataPearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl)
			{
				self.color = DataPearl.UniquePearlMainColor((self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType);
				self.highlightColor = DataPearl.UniquePearlHighLightColor((self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType);
			}
		}

		private static Color? DataPearl_UniquePearlHighLightColor(On.DataPearl.orig_UniquePearlHighLightColor orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
		{
			if (pearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl)
			{
				return new Color(0.984f, 1f, 0.71f);
			}
			return orig(pearlType);
		}

		private static Color DataPearl_UniquePearlMainColor(On.DataPearl.orig_UniquePearlMainColor orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
		{
			if (pearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl)
			{
				return new Color(0.71f, 1f, 0.91f);
			}
			return orig(pearlType);
		}

		private static void CollectionsMenu_ctor(On.MoreSlugcats.CollectionsMenu.orig_ctor orig, CollectionsMenu self, ProcessManager manager)
		{
			orig(self, manager);

			for (int i = 0; i < self.chatlogButtons.Length; i++)
			{
				HSLColor color = new(1f, 1f, 1f);

				if (i > self.prePebsBroadcastChatlogs.Count + self.postPebsBroadcastChatlogs.Count)
				{
					int index = i - (self.prePebsBroadcastChatlogs.Count + self.postPebsBroadcastChatlogs.Count);
					if (self.usedChatlogs[index].value.Contains("Saint"))
					{
						color = RainWorld.GoldHSL;
					}

					self.chatlogButtons[i].rectColor = color;
				}
			}
		}
		private static void MusicPlayer_Update(On.Music.MusicPlayer.orig_Update orig, MusicPlayer self)
		{
			orig(self);

			if (ghostScreen != null && self.song != null && self.song is SaintBroadcastSong && startSaintSongAtSeconds != 0 && !setSaintSongTime && self.song.subTracks.Where(x => x != null).FirstOrDefault() != null && self.song.subTracks.Where(x => x != null).FirstOrDefault().source != null)
			{
				setSaintSongTime = true;
				self.song.subTracks.Where(x => x != null).FirstOrDefault().source.time = startSaintSongAtSeconds;
				Plugin.DebugLog($"Saint broadcast song started at: {startSaintSongAtSeconds}");
			}
		}

		private static void PlayerThreatTracker_Update(On.Music.PlayerThreatTracker.orig_Update orig, PlayerThreatTracker self)
		{
			if (self.musicPlayer.manager.currentMainLoop == null || self.musicPlayer.manager.currentMainLoop.ID != ProcessManager.ProcessID.Game)
			{
				self.recommendedDroneVolume = 0f;
				self.currentThreat = 0f;
				self.currentMusicAgnosticThreat = 0f;
				self.region = null;
				return;
			}
			if (self.playerNumber >= (self.musicPlayer.manager.currentMainLoop as RainWorldGame).Players.Count)
			{
				return;
			}
			Player player = (self.musicPlayer.manager.currentMainLoop as RainWorldGame).Players[self.playerNumber].realizedCreature as Player;
			if (player == null || player.room == null)
			{
				return;
			}
			if (player.room.game.GameOverModeActive || player.redsIllness != null)
			{
				self.recommendedDroneVolume = 0f;
				self.currentThreat = 0f;
				self.currentMusicAgnosticThreat = 0f;
				return;
			}

			// Future note when custom song is made: use self.musicplayer.song.subtracks[0].source.time to set the start time
			if (ModOptions.CustomInGameCutscenes.Value && ghostScreen != null && self.ghostMode > 0f)
			{
				FadeOutAllNonBroadcastSongs(self.musicPlayer, 20);
				if ((self.musicPlayer.song == null || self.musicPlayer.song is not SaintBroadcastSong || self.musicPlayer.song.name != saintBroadcastSong) && !string.IsNullOrEmpty(saintBroadcastSong))
				{
					RequestBroadcastSong(self.musicPlayer, saintBroadcastSong);
				}
				return;
			}

			orig(self);
		}

		public static void RequestBroadcastSong(MusicPlayer self, string ghostSongName)
		{
			if (self.song != null && self.song is GhostSong)
			{
				return;
			}
			if (self.nextSong != null && self.nextSong is GhostSong)
			{
				return;
			}
			if (!self.manager.rainWorld.setup.playMusic)
			{
				return;
			}
			Song song = new SaintBroadcastSong(self, ghostSongName);
			if (self.song == null)
			{
				self.song = song;
				self.song.playWhenReady = true;
				self.song.baseVolume += 0.5f;

				return;
			}
			self.nextSong = song;
			self.nextSong.playWhenReady = false;
		}

		public static void FadeOutAllNonBroadcastSongs(MusicPlayer self, float fadeOutTime)
		{
			if (self.song != null && (!ModManager.MSC || self.song is not SaintBroadcastSong || self.song.name != saintBroadcastSong))
			{
				self.song.FadeOut(fadeOutTime);
			}
			if (self.nextSong != null && (!ModManager.MSC || self.song is not SaintBroadcastSong || self.song.name != saintBroadcastSong))
			{
				self.nextSong = null;
			}
			self.nextProcedural = "";
		}

		private static void CollectToken_ctor(On.CollectToken.orig_ctor orig, CollectToken self, Room room, PlacedObject placedObj)
		{
			orig(self, room, placedObj);

			if (ModOptions.CustomInGameCutscenes.Value && self.stalk != null)
			{
				SaintTokenStats.saintTokenCWT.Remove(self.stalk);
				SaintTokenStats.saintTokenCWT.Add(self.stalk, new(self.stalk));
			}
		}

		private static void CollectToken_Pop(On.CollectToken.orig_Pop orig, CollectToken self, Player player)
		{
			orig(self, player);

			if (ModOptions.CustomInGameCutscenes.Value && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && SaintTokenStats.saintTokenCWT.TryGetValue(self.stalk, out var saintToken) && saintToken.IsWhiteToken)
			{
				saintToken.startGlow = true;
			}
		}

		private static void Player_ProcessChatLog(On.Player.orig_ProcessChatLog orig, Player self)
		{
			if (ghostScreen != null && ghostScreen.destroy)
			{
				self.room.RemoveObject(ghostScreen);
				ghostScreen = null;
				setSaintSongTime = false;
			}
			if (ModOptions.CustomInGameCutscenes.Value && self.chatlog && self.room != null && self.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
			{
				self.mushroomCounter = 25;
				self.godWarmup = 1f;

				self.chatlogCounter++;

				if (ghostScreen == null && self.chatlogCounter < 60)
				{
					ghostScreen = new SaintBroadcastEffect(self.room, self.room.game.cameras[0]);
					self.room.AddObject(ghostScreen);
				}

				else if (self.room.game.cameras[0].hud.chatLog == null && self.chatlogCounter >= 60)
				{
					self.chatlog = false;
					if (Plugin.isCRSEnabled)
					{
						CRSHooks.SaveCustomBroadcast(self, self.chatlogID);
					}

					if (ghostScreen != null)
					{
						ghostScreen.broadcastEnded = true;
					}
				}
			}

			orig(self);
		}

		private static void ChatLogDisplay_NewMessage_string_int(On.MoreSlugcats.ChatLogDisplay.orig_NewMessage_string_int orig, ChatLogDisplay self, string text, int extraLinger)
		{
			if (ghostScreen != null && saintBroadcastNum < 1)
			{
				extraLinger += 70;
			}
			extraLinger = Mathf.RoundToInt((float)extraLinger * 0.6f);

			orig(self, text, extraLinger);
		}

		private static void DialogBox_Update(On.HUD.DialogBox.orig_Update orig, HUD.DialogBox self)
		{
			if (self.CurrentMessage != null && self.CurrentMessage.text.Contains("FP:") && self.hud?.rainWorld.inGameSlugCat == MoreSlugcatsEnums.SlugcatStatsName.Saint && saintBroadcastNum > 11)
			{
				self.showDelay += UnityEngine.Random.Range(0, 3);
			}
			orig(self);
		}

		private static int DialogBox_GetDelay(On.HUD.DialogBox.orig_GetDelay orig)
		{
			if (ghostScreen != null && saintBroadcastNum < 2)
			{
				if (Custom.rainWorld.options.language == InGameTranslator.LanguageID.Japanese)
				{
					return 5;
				}
				if (Custom.rainWorld.options.language == InGameTranslator.LanguageID.Korean)
				{
					return 5;
				}
				if (Custom.rainWorld.options.language == InGameTranslator.LanguageID.Chinese)
				{
					return 6;
				}
				return 3;
			}

			return orig();
		}

		private static void CollectToken_AddToContainer(On.CollectToken.orig_AddToContainer orig, CollectToken self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);

			if (ModOptions.CustomInGameCutscenes.Value && rCam.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && SaintTokenStats.saintTokenCWT.TryGetValue(self.stalk, out var saintToken) && saintToken.IsWhiteToken)
			{
				sLeaser.sprites[self.GoldSprite].RemoveFromContainer();
				sLeaser.sprites[self.GoldSprite].shader = rCam.game.rainWorld.Shaders["GhostDistortion"];

				rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[self.GoldSprite]);
			}
		}

		private static void TokenStalk_ApplyPalette(On.CollectToken.TokenStalk.orig_ApplyPalette orig, CollectToken.TokenStalk self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			if (ModOptions.CustomInGameCutscenes.Value && SaintTokenStats.saintTokenCWT.TryGetValue(self, out var saintToken))
			{
				saintToken.tokenBlackColor = palette.blackColor;
			}
		}

		private static void CollectToken_DrawSprites(On.CollectToken.orig_DrawSprites orig, CollectToken self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (ModOptions.CustomInGameCutscenes.Value && rCam.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && SaintTokenStats.saintTokenCWT.TryGetValue(self.stalk, out var saintToken) && saintToken.IsWhiteToken)
			{
				sLeaser.sprites[self.GoldSprite].scale = sLeaser.sprites[self.GoldSprite].alpha * 5f;

				sLeaser.sprites[self.TrailSprite].color = saintToken.antiGoldDark;
				sLeaser.sprites[self.MainSprite].color = saintToken.antiGoldDark;
				for (int i = 0; i < 4; i++)
				{
					sLeaser.sprites[self.LineSprite(i)].color = saintToken.antiGoldDark;
				}
			}
		}

		private static void TokenStalk_AddToContainer(On.CollectToken.TokenStalk.orig_AddToContainer orig, CollectToken.TokenStalk self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);

			if (ModOptions.CustomInGameCutscenes.Value && rCam.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && SaintTokenStats.saintTokenCWT.TryGetValue(self, out var saintToken) && saintToken.IsWhiteToken)
			{
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

				sLeaser.sprites[sLeaser.sprites.Length - 1] = new("Futile_White")
				{
					shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"]
				};

				rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[sLeaser.sprites.Length - 1]);
			}
		}

		private static void TokenStalk_DrawSprites(On.CollectToken.TokenStalk.orig_DrawSprites orig, CollectToken.TokenStalk self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (ModOptions.CustomInGameCutscenes.Value && rCam.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && SaintTokenStats.saintTokenCWT.TryGetValue(self, out var saintToken) && saintToken.IsWhiteToken)
			{
				if (self.room != null && self.room.game != null && self.room.game.cameras[0] != null && self.room.game.cameras[0].hud != null && self.room.game.cameras[0].hud.chatLog == null && saintToken.glowTimer > saintToken.maxGlowTimer * 5)
				{
					saintToken.startTokenFade = true;
				}

				for (int i = 0; i < sLeaser.sprites.Length - 1; i++)
				{
					if (saintToken.startTokenFade)
					{
						saintToken.fadeTime += 1f;
					}

					Color glow = Color.LerpUnclamped(saintToken.satGold, saintToken.satGold + new Color(0.2f, 0.2f, 0.2f), (saintToken.glowTimer / saintToken.maxGlowTimer));
					Color fade = Color.LerpUnclamped(saintToken.tokenBlackColor, saintToken.satGold, (float)(i + 1) / (float)(sLeaser.sprites.Length));

					if (saintToken.startGlow)
					{
						fade = Color.LerpUnclamped(fade, glow, (saintToken.glowTimer / saintToken.maxGlowTimer));
						saintToken.glowTimer++;
					}

					if (saintToken.fadeTime > 0)
					{
						fade = Color.LerpUnclamped(glow, saintToken.antiGold, (saintToken.fadeTime / saintToken.maxFadeTime));
						glow = Color.LerpUnclamped(saintToken.satGold + new Color(0.2f, 0.2f, 0.2f), saintToken.antiGold, (saintToken.fadeTime / saintToken.maxFadeTime));
						sLeaser.sprites[i].alpha = Mathf.Lerp(1f, 0f, saintToken.fadeTime / saintToken.maxFadeTime);
					}
					if (saintToken.fadeTime > saintToken.maxFadeTime)
					{
						sLeaser.sprites[i].isVisible = false;
					}

					sLeaser.sprites[self.HeadSprite].color = glow;
					sLeaser.sprites[i].color = fade;

				}

				sLeaser.sprites[sLeaser.sprites.Length - 1].x = sLeaser.sprites[self.HeadSprite].x;
				sLeaser.sprites[sLeaser.sprites.Length - 1].y = sLeaser.sprites[self.HeadSprite].y;
				sLeaser.sprites[sLeaser.sprites.Length - 1].scale = sLeaser.sprites[self.HeadSprite].scale * 6f;
				sLeaser.sprites[sLeaser.sprites.Length - 1].alpha = 0.5f;
				if (saintToken.fadeTime == 0)
				{
					sLeaser.sprites[sLeaser.sprites.Length - 1].color = Color.Lerp(saintToken.antiGoldDark, saintToken.gold, saintToken.glowTimer / saintToken.maxGlowTimer);
				}
				else
				{
					sLeaser.sprites[sLeaser.sprites.Length - 1].alpha = Mathf.Lerp(0.5f, 0f, saintToken.fadeTime / saintToken.maxFadeTime);
				}

				sLeaser.sprites[self.SataFlasher].color = Color.Lerp(saintToken.antiGold, Color.white, UnityEngine.Random.value * 0.1f);
				sLeaser.sprites[self.LampSprite].color = Color.Lerp(saintToken.antiGold, Color.white, Mathf.Lerp(self.lastLampPower, self.lampPower, timeStacker) * Mathf.Pow(UnityEngine.Random.value, 0.5f) * (0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lastSinCounter, self.sinCounter, timeStacker) / 6f)));

				if (saintToken.fadeTime > saintToken.maxFadeTime)
				{
					saintToken.fadeTime = 0f;
					self.Destroy();
					self.token?.Destroy();
				}
			}
		}

		private static void Room_Loaded(ILContext il)
		{
			try
			{
				// For saint broadcasts
				ILCursor cursor = new(il);
				bool success = cursor.TryGotoNext(
					MoveType.After,
					x => x.MatchCallvirt(out _),
					x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Spear)),
					x => x.MatchCall(out _)
					);

				if (Plugin.ILMatchFail(success))
					return;

				ILLabel nextJump = (ILLabel)cursor.Next.Operand;

				cursor.Emit(OpCodes.Brfalse_S, nextJump);
				cursor.Emit(OpCodes.Ldarg_0);
				static bool IsSaint(Room self)
				{
					return self.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Saint;
				}
				cursor.EmitDelegate(IsSaint);

				Plugin.ILSucceed();
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}

			try
			{
				// For removing karma flowers for Artificer and Spearmaster
				ILCursor cursor = new(il);
				for (int i = 0; i < 3; i++)
				{
					bool success = cursor.TryGotoNext(
						MoveType.After,
						x => x.MatchLdsfld<SlugcatStats.Name>(nameof(SlugcatStats.Name.Red)),
						x => x.MatchCall(out _)
						);

					if (Plugin.ILMatchFail(success))
						return;

					ILLabel jump = (ILLabel)cursor.Next.Operand;

					cursor.Emit(OpCodes.Brfalse, jump);
					cursor.Emit(OpCodes.Ldarg_0);
					static bool IsArtiOrSpear(Room self)
					{
						return self.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Spear && self.game.StoryCharacter != MoreSlugcatsEnums.SlugcatStatsName.Artificer;
					}
					cursor.EmitDelegate(IsArtiOrSpear);

					Plugin.ILSucceed();
				}
			}
			catch (Exception ex)
			{
				Plugin.ILFail(ex);
			}
		}
	}

	public class SaintWarmthTransistion : GhostPing
	{
		public float? ghostMode;

		public SaintWarmthTransistion(Room room) : base(room)
		{
			ghostMode = room?.world.worldGhost?.GhostMode(room, room.game.cameras[0].currentCameraPosition);
			goAt = PlayerHooks.MaxWarmthActivationTimer - (PlayerHooks.MaxWarmthActivationTimer / 5);
			alpha = 1;
		}

		public override void Update(bool eu)
		{
			counter++;
			if (!go && counter >= goAt)
			{
				go = true;
				room.PlaySound(PlayerHooks.warmMode ? SoundID.Slugcat_Ghost_Dissappear : SoundID.Slugcat_Ghost_Appear, 0f, 2f, 1f);
			}
			lastProg = prog;
			if (go)
			{
				prog = Mathf.Min(1f, prog + speed);
				if (prog >= 1f && lastProg >= 1f)
				{
					room?.game.cameras[0].ChangeRoom(room, room.game.cameras[0].currentCameraPosition);
					Destroy();
				}
			}
			if (!go && PlayerHooks.warmthActivationTimer <= 0f)
			{
				Destroy();
			}
		}
	}

	public class SaintBroadcastEffect : UpdatableAndDeletable
	{
		internal bool broadcastEnded;
		private RoomCamera roomCamera;
		private float originalGhostMode;
		private int timer;
		private CellDistortion cellDist;

		public bool destroy;

		public SaintBroadcastEffect(Room room, RoomCamera roomCamera)
		{
			this.room = room;
			this.roomCamera = roomCamera;

			timer = 0;
			originalGhostMode = roomCamera.ghostMode;

			cellDist = new(room.game.FirstRealizedPlayer.firstChunk.pos, 1500f, 0f, 0.3f, 0f, 0.3f);
			room.AddObject(cellDist);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			cellDist.intensity = roomCamera.ghostMode / 3f;

			if (roomCamera.hud.chatLog == null)
			{
				timer++;
				if (!broadcastEnded)
				{
					roomCamera.ghostMode = Mathf.Lerp(originalGhostMode, 1f, (float)timer / 60f);
				}
				else if (broadcastEnded && roomCamera.ghostMode > originalGhostMode)
				{
					roomCamera.ghostMode = Mathf.Lerp(1f, originalGhostMode, (float)timer / 120f);
				}
				else
				{
					destroy = true;
					cellDist.Destroy();
					Destroy();
				}
			}
		}
	}

	class SaintTokenStats
	{
		public static ConditionalWeakTable<CollectToken.TokenStalk, SaintTokenStats> saintTokenCWT = new();

		public float fadeTime;
		public float maxFadeTime = 2000;

		public bool startGlow;
		public float glowTimer;
		public float maxGlowTimer = 2000;

		public Color gold = RainWorld.GoldRGB;
		public Color satGold = RainWorld.SaturatedGold;
		public Color antiGold = Custom.HSL2RGB(RainWorld.AntiGold.hue, RainWorld.AntiGold.saturation, RainWorld.AntiGold.lightness);
		public Color antiGoldDark = Custom.HSL2RGB(RainWorld.AntiGold.hue, 0.6f, 0.2f);

		public Color tokenBlackColor;

		public CollectToken.TokenStalk tokenStalk;

		public SaintTokenStats(CollectToken.TokenStalk token)
		{
			tokenStalk = token;
			if (tokenStalk.token != null)
			{
				IsWhiteToken = tokenStalk.token.whiteToken;
			}
		}

		public bool IsWhiteToken;
		internal bool startTokenFade;
	}
	public class SaintBroadcastSong : Song
	{
		public SaintBroadcastSong(MusicPlayer musicPlayer, string songName) : base(musicPlayer, songName, MusicPlayer.MusicContext.StoryMode)
		{
			priority = 1.15f;
			stopAtGate = true;
			stopAtDeath = true;
			fadeInTime = 20f;
		}
	}
}
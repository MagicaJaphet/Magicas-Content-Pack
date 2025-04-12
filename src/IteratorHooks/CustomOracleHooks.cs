using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicasContentPack.IteratorHooks
{
	internal class CustomOracleHooks
	{
		private static int handPose;
		private static int handTimer;
		public static int handTimerMax = 30;

		public static void Init()
		{
			try
			{
				On.Oracle.OracleArm.ctor += CustomArmRestraints;
				On.Oracle.OracleArm.BaseDir += CustomArmBaseDir;
				On.Oracle.OracleArm.OnFramePos += CustomArmOnFramePos;

				On.OracleGraphics.AddToContainer += CustomAddToContainer;
				On.OracleGraphics.Update += CustomGraphicsUpdate;
				On.OracleGraphics.DrawSprites += CustomDrawSprites;
				On.OracleGraphics.ApplyPalette += CustomApplyPalette;
				On.OracleGraphics.Gown.Color += CustomGownColors;

				On.SSOracleBehavior.HandTowardsPlayer += CustomHandTowardPlayerCheck;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
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
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.head.pos, self.oracle.firstChunk.pos, handTimer / (float)handTimerMax) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos + new Vector2(-1f, 0f) - self.hands[1].pos, 10f) / 3f;
							break;

						case 1:
							self.eyesOpen = 0.6f;
							self.hands[0].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos + new Vector2(-1f, -1f) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos, self.hands[0].pos, handTimer / (float)handTimerMax / 1.2f) - self.hands[1].pos, 10f) / 3f;
							break;

						case 2:
							self.hands[0].vel += Vector2.ClampMagnitude(self.head.pos + new Vector2(-2f, 1f) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(self.oracle.firstChunk.pos - self.hands[1].pos, 10f) / 3f;
							break;

						case 3:
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos, self.hands[1].pos, handTimer / (float)handTimerMax / 1.5f) - self.hands[0].pos, 10f) / 3f;
							self.hands[1].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.head.pos, self.head.pos + new Vector2(3f, 2f), handTimer / (float)handTimerMax) - self.hands[1].pos, 10f) / 3f;
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
							self.hands[0].vel += Vector2.ClampMagnitude(Vector2.Lerp(self.oracle.firstChunk.pos + new Vector2(-1f, -1f), self.oracle.firstChunk.pos + new Vector2(-1f, -1f), handTimer / (float)handTimerMax) - self.hands[0].pos, 10f) / 3f;
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
					sLeaser.sprites[sLeaser.sprites.Length - (i + 1)].scaleX = i == 0 ? -1f : 1f;
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
					SlugcatStats.Timeline[] timeLine = SlugcatStats.SlugcatTimelineOrder().ToArray();

					rottedAmount = Mathf.Lerp(0.2f, 0.75f, timeLine.IndexOf(rCam.game.TimelinePoint) / (float)timeLine.Length);

					if (timeLine.IndexOf(SlugcatStats.Timeline.Spear) != 0 && timeLine.IndexOf(rCam.game.TimelinePoint) < timeLine.IndexOf(SlugcatStats.Timeline.Spear))
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
					if (self.gowns == null)
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
					sLeaser.sprites[self.neckSprite].color = OracleHooks.oracleColor[OracleHooks.OracleColor.SRSCloak];

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

		private static Color CustomGownColors(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
		{
			if (self.owner.IsSRS())
			{
				return Color.Lerp(OracleHooks.oracleColor[OracleHooks.OracleColor.SRSCloak], OracleHooks.oracleColor[OracleHooks.OracleColor.SRSCloakDark], Mathf.Lerp(0.4f, 0.75f, f));
			}
			return orig(self, f);
		}

		private static bool CustomHandTowardPlayerCheck(On.SSOracleBehavior.orig_HandTowardsPlayer orig, SSOracleBehavior self)
		{
			if (MagicaOracleBehavior.HandTowardPlayer)
			{
				return true;
			}
			return orig(self);
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

				if (oracle.room.game.cameras.Length > 0 && oracle.room.game.cameras[0].room == oracle.room && !oracle.room.game.cameras[0].AboutToSwitchRoom && oracle.room.game.cameras[0].paletteBlend != working || oracle.room.world.rainCycle.dayNightCounter > 0)
				{
					int mainPalette = 25;
					int fadePalette = 26;

					int duskPalette = 3;
					int nightPalette = 100;

					if (oracle.ID == MagicaEnums.Oracles.SRS && oracle.room.game.cameras[0].currentCameraPosition == 6)
					{
						if (oracle.room.world != null && oracle.room.world.rainCycle.dayNightCounter > 0)
						{

							if (oracle.room.world.rainCycle.dayNightCounter < cycleTimer * duskpoint)
							{
								if (working == 0f)
								{
									oracle.room.game.cameras[0].ChangeBothPalettes(mainPalette, duskPalette, Mathf.InverseLerp(cycleTimer, cycleTimer * duskpoint, oracle.room.world.rainCycle.dayNightCounter));
								}
								else
								{
									oracle.room.game.cameras[0].ChangeBothPalettes(fadePalette, duskPalette, Mathf.InverseLerp(cycleTimer, cycleTimer * duskpoint, oracle.room.world.rainCycle.dayNightCounter));
								}
							}
							else if (oracle.room.world.rainCycle.dayNightCounter < cycleTimer * nightpoint)
							{
								oracle.room.game.cameras[0].ChangeBothPalettes(duskPalette, nightPalette, Mathf.InverseLerp(cycleTimer * duskpoint, cycleTimer * nightpoint, oracle.room.world.rainCycle.dayNightCounter) * (oracle.room.game.cameras[0].effect_dayNight * 0.99f));
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
					if (investigateAngle < -90f || investigateAngle > 90f || oracle.room.aimap.getTerrainProximity(nextPos) < 2f)
					{
						investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
						invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
					}
					Vector2 vector = player.DangerPos + Custom.DegToVec(investigateAngle) * 150f;
					if (oracle.room.aimap.getTerrainProximity(vector) >= 2f)
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
			if (conversation == null && !MagicaSaveState.GetKey(MoreSlugcatsEnums.SlugcatStatsName.Spear.value, nameof(SaveValues.SpearMetSRS), out bool _) && action != MagicaEnums.OracleActions.SRSSeesPurple)
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
				Deactivate();
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
						CustomOracleHooks.handTimerMax = 80;
					}

					if (timer > 480)
					{
						if (player.graphicsModule is PlayerGraphics graphics)
						{
							graphics.markBaseAlpha = Mathf.Lerp(0f, 1f, (timer - 480) / 100f);
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
						(oracle.graphicsModule as OracleGraphics).head.vel += Vector2.ClampMagnitude(oracle.firstChunk.pos - new Vector2(3f, 0f) - (oracle.graphicsModule as OracleGraphics).head.pos, 10f) / 3f;
						(oracle.graphicsModule as OracleGraphics).hands[0].vel += Vector2.ClampMagnitude(player.firstChunk.pos + new Vector2(-20f, 10f) - (oracle.graphicsModule as OracleGraphics).hands[0].pos, 10f) / 3f;
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
						//WinOrSaveHooks.BeatGameMode(oracle.room.game);
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
						return new(true, Options.ControlSetup.Preset.None, player.firstChunk.pos.x > oracle.room.MiddleOfTile(33, 0).x ? -1 : 1, player.bodyMode == Player.BodyModeIndex.Crawl ? 1 : 0, false, false, false, false, false);
					}
					else if (player.lastFlipDirection != 1)
					{
						return new(true, Options.ControlSetup.Preset.None, 1, player.bodyMode == Player.BodyModeIndex.Crawl ? 1 : 0, false, false, false, false, false);
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
						return new(true, Options.ControlSetup.Preset.None, 1, player.bodyMode == Player.BodyModeIndex.Crawl ? 1 : 0, false, false, false, false, false);
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

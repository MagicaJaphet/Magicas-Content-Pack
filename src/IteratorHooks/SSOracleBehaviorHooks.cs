using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;
using RWCustom;

namespace MagicasContentPack.IteratorHooks
{
	internal class SSOracleBehaviorHooks
	{
		internal static HalcyonPearl halcyonPearl;
		private static int halcyonTimer;
		private static ProjectionCircle halcyonCircle;

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

		public static bool IsIntroAction(SSOracleBehavior owner)
		{
			if (owner.action == SSOracleBehavior.Action.General_GiveMark || owner.action == SSOracleBehavior.Action.General_MarkTalk || owner.action == SSOracleBehavior.Action.GetNeuron_GetOutOfStomach || owner.action == SSOracleBehavior.Action.GetNeuron_Init || owner.action == SSOracleBehavior.Action.GetNeuron_InspectNeuron || owner.action == SSOracleBehavior.Action.GetNeuron_TakeNeuron || owner.action == SSOracleBehavior.Action.MeetRed_Init || owner.action == SSOracleBehavior.Action.MeetWhite_Talking || owner.action == SSOracleBehavior.Action.MeetYellow_Init || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_StartDialog) { return true; }
			if (ModManager.MSC && (owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.ThrowOut_Singularity || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetArty_Init) || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetArty_Talking || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_SecondImages || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_StartDialog || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_ThirdCurious || owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Rubicon) { return true; }

			return false;
		}

		internal static bool CheckIfWhoTookThePearlIsBeforeCurrent(SlugcatStats.Timeline currentSlug)
		{
			return MagicaSaveState.GetKey(MagicaSaveState.anySave, SaveValues.WhoShowedFPThePearl, out string result) && new SlugcatStats.Timeline(result, false) is SlugcatStats.Timeline timeline && SlugcatStats.AtOrBeforeTimeline(timeline, currentSlug);
		}

		public static void Init()
		{
			try
			{
				_ = new Hook(typeof(SSOracleBehavior).GetProperty(nameof(SSOracleBehavior.EyesClosed), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<SSOracleBehavior, bool> orig, SSOracleBehavior oracle) =>
				{
					return orig(oracle) || IteratorHooks.moonHugRed;
				});

				On.SSOracleBehavior.Move += SSOracleBehavior_Move;
				On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
				On.SSOracleBehavior.Update += ItemRecognitions;
				On.SSOracleBehavior.SpecialEvent += CustomSpecialEvent;
				On.SSOracleBehavior.NewAction += CustomFPActions;
				On.SSOracleBehavior.StartItemConversation += AddPearlRecognition;
				On.SSOracleBehavior.PebblesConversation.AddEvents += CustomPebblesConversationEvents; // Also used for custom oracle readings, since they use the PebblesConversation class

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
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

		private static void ItemRecognitions(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
		{
			if (ModOptions.CustomInGameCutscenes.Value && IteratorHooks.allowFpToCheckHisPearl && self.oracle.ID == Oracle.OracleID.SS)
			{
				if (halcyonPearl == null)
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

				if (halcyonPearl != null && halcyonPearl.room == self.oracle.room && !string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl))
				{
					halcyonTimer++;
					halcyonPearl.firstChunk.vel = Vector2.zero;
					if (self.timeSinceSeenPlayer > 20 && self.player != null && (self.movementBehavior != SSOracleBehavior.MovementBehavior.Meditate || self.movementBehavior != SSOracleBehavior.MovementBehavior.Idle))
					{
						Vector2 moveTo = Custom.RotateAroundVector(self.oracle.firstChunk.pos - new Vector2(-20f, -20f), self.oracle.firstChunk.pos, Custom.AimFromOneVectorToAnother(self.oracle.firstChunk.pos, self.player.firstChunk.pos) + 145f);
						halcyonPearl.firstChunk.pos = Vector2.Lerp(halcyonPearl.firstChunk.pos, moveTo, 0.4f);
					}
					else
					{
						float spinAmount = 150f;
						Vector2 moveTo = Custom.RotateAroundVector(self.oracle.firstChunk.pos - new Vector2(-20f, -20f), self.oracle.firstChunk.pos, Mathf.Lerp(0f, 360f, halcyonTimer % spinAmount / spinAmount));
						halcyonPearl.firstChunk.pos = Vector2.Lerp(halcyonPearl.firstChunk.pos, moveTo, 0.4f);
					}
					halcyonPearl.firstChunk.lastPos = halcyonPearl.firstChunk.pos;

					if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate && halcyonPearl.hoverPos == null)
					{
						halcyonPearl.hoverPos = halcyonPearl.firstChunk.pos;
						self.TurnOffSSMusic(false);
					}
					else if (self.movementBehavior != SSOracleBehavior.MovementBehavior.Meditate)
					{
						halcyonPearl.hoverPos = null;
					}

					Vector2 offset = new(10f, -22f);
					if (halcyonCircle == null && self.getToWorking == 0f)
					{
						halcyonCircle = new ProjectionCircle(halcyonPearl.firstChunk.pos - offset, 0f, 3f);
						self.oracle.room.AddObject(halcyonCircle);
					}
					else if (self.getToWorking == 0f)
					{
						halcyonCircle.radius = 5f;
						halcyonCircle.pos = halcyonPearl.firstChunk.pos - offset;
					}
					else if (self.getToWorking != 0f && halcyonCircle != null)
					{
						halcyonCircle.Destroy();
						halcyonCircle = null;
					}
				}

				if (self.timeSinceSeenPlayer > 20 && !IsIntroAction(self) && self.conversation == null && self.oracle.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData.theMark && (self.action != MagicaEnums.OracleActions.FPMusicPearlInit || self.action != MagicaEnums.OracleActions.FPMusicPearlPlay || self.action != MagicaEnums.OracleActions.FPMusicPearlStop) && halcyonPearl != null && string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl))
				{
					self.NewAction(MagicaEnums.OracleActions.FPMusicPearlInit);
				}
			}

			// Add trigger that checks if the ending has already been played
			if (self.readDataPearlOrbits.Count >= 22)
			{
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

		private static void SSOracleBehavior_Move(On.SSOracleBehavior.orig_Move orig, SSOracleBehavior self)
		{
			if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Idle && self.oracle.ID == Oracle.OracleID.SS && UnityEngine.Random.value < 0.001f && !string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl))
			{
				self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
			}

			orig(self);
		}

		private static void AddPearlRecognition(On.SSOracleBehavior.orig_StartItemConversation orig, SSOracleBehavior self, DataPearl item)
		{
			if (ModManager.MSC && self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && item.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.RM)
			{
				Conversation.ID id = Conversation.DataPearlToConversation(item.AbstractPearl.dataPearlType);

				self.pearlConversation = new SLOracleBehaviorHasMark.MoonConversation(id, self, SLOracleBehaviorHasMark.MiscItemType.NA);
				if (!string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl) && SaveValues.WhoShowedFPThePearl == MoreSlugcatsEnums.SlugcatStatsName.Artificer.value)
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

		public class SSFPReadsMusicPearl(SSOracleBehavior owner) : SSOracleBehavior.ConversationBehavior(owner, MagicaEnums.OracleActions.MusicPearlInspect, Conversation.ID.None)
		{
			public OracleChatLabel chatLabel;
			public HalcyonPearl halcyon = null;
			private int numGrabbedHalcyon;
			private bool finishedFlag;
			private bool startedConverstation = false;
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
						owner.dialogBox.Interrupt(Translate("ss_reactiontorm0"), 50);
						owner.dialogBox.NewMessage(Translate("ss_reactiontorm1"), 40);
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
								player = oracle.room.game.session.Players[0].realizedCreature as Player;
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
								owner.dialogBox.Interrupt(Translate("ss_reactiontormbeinggrabbed0"), 50);
							}
							if (halcyon != null && !halcyon.Carried)
							{
								owner.dialogBox.Interrupt(Translate("ss_reactiontormresumeplaying"), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
								return;
							}

							if (inActionCounter == 500)
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience0"), 50);
							}

							if (inActionCounter == 1000)
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience1"), 50);
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
								owner.dialogBox.Interrupt(Translate("ss_reactiontormbeinggrabbed1"), 50);
							}
							if (halcyon != null && !halcyon.Carried)
							{
								owner.dialogBox.Interrupt(Translate("ss_reactiontormresumeplaying"), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
								return;
							}

							if (inActionCounter == 500)
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience0"), 50);
							}

							if (inActionCounter == 1000)
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience1"), 50);
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
								owner.dialogBox.Interrupt(Translate("ss_reactiontormbeinggrabbed2"), 50);
							}
							if (halcyon != null && !halcyon.Carried)
							{
								owner.dialogBox.Interrupt(Translate("..."), 50);
								owner.NewAction(MagicaEnums.OracleActions.ArtiFPPearlPlay);
								return;
							}

							if (inActionCounter == 500)
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience2"), 50);
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience3"), 50);
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
								owner.dialogBox.Interrupt(Translate("ss_reactiontormbeinggrabbed3"), 50);
								owner.dialogBox.NewMessage(Translate("ss_reactiontormimpatience3"), 50);
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
				SaveValues.WhoShowedFPThePearl = SlugcatStats.Timeline.Artificer.value;
				owner.UnlockShortcuts();
				oracle.marbleOrbiting = false;
				finishedFlag = true;
				if (halcyon != null)
				{
					halcyon.hoverPos = null;
				}
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
								halcyon = oracle.room.physicalObjects[i][j] as HalcyonPearl;
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
							owner.dialogBox.Interrupt(Translate("ss_reactiontormnonarti"), 10);
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
									OracleGraphics oracleGraphics = oracle.graphicsModule as OracleGraphics;
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
							OracleGraphics oracleGraphics = oracle.graphicsModule as OracleGraphics;
							oracleGraphics.hands[1].vel += Custom.DirVec(oracleGraphics.hands[1].pos, halcyon.firstChunk.pos) * 3f;
						}

						if (inActionCounter == 1000)
						{
							owner.dialogBox.NewMessage(Translate("ss_reactiontormmidplay"), 50);
						}
					}

					if (inActionCounter == 1600)
					{
						owner.dialogBox.NewMessage(Translate("..."), 50);
						owner.dialogBox.NewMessage(Translate("ss_reactiontormnonartipost0"), 50);
						owner.dialogBox.NewMessage(Translate("ss_reactiontormnonartipost1"), 50);
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
				SaveValues.WhoShowedFPThePearl = oracle.room.game.TimelinePoint.value;
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
			private DataPearl greenPearl;

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

			public Vector2 HoldPlayerPos
			{
				get
				{
					return new Vector2(488f, 350f + Mathf.Sin(inActionCounter / 70f * 3.1415927f * 2f) * 4f);
				}
			}

			public SSReactsToRedDying(SSOracleBehavior self) : base(self, MagicaEnums.OracleActions.RedDies, self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.moonRevived ? MagicaEnums.ConversationIDs.RedDiesPebbles1 : MagicaEnums.ConversationIDs.RedDiesPebbles2)
			{
				SaveValues.HunterOracleID = oracle.ID.value;
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
						foundPlayer = oracle.room.game.Players[0].realizedCreature as Player;
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
							dialogBox.Interrupt(Translate("ss_reactiontoreddyingintro"), 0);
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
							for (int i = 0; i < oracle.room.updateList.Count; i++)
							{
								UpdatableAndDeletable obj = oracle.room.updateList[i];
								if (obj is NSHSwarmer swarmer)
								{
									greenNeuron = swarmer;
								}
								if (obj is DataPearl pearl && pearl.AbstractPearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Red_stomach)
								{
									greenPearl = pearl;
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

				if (greenPearl != null)
				{
					greenPearl.firstChunk.vel = Vector2.zero;

					float heightVariance = Mathf.Lerp(10f, -10f, Mathf.Sin(inActionCounter / 30f));
					greenPearl.firstChunk.pos = Vector2.Lerp(greenPearl.firstChunk.pos, oracle.firstChunk.pos + new Vector2(20f, heightVariance), 0.05f);
				}

				if (phase == RedPhases.START)
				{
					owner.getToWorking = 0f;
					owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

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

					if (dialogueSet && owner.dialogBox.messages.Count == 0)
					{
						phase = RedPhases.TALK;
						return;
					}

					if (owner.dialogBox.messages.Count == 0 && !dialogueSet)
					{
						dialogueSet = true;

						if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap)
						{
							owner.dialogBox.NewMessage(Translate("ss_reactiontoreddying_extracycles"), 30);
						}
						else
						{
							owner.dialogBox.NewMessage(Translate("ss_reactiontoreddying_noextracycles"), 30);
						}

						if (greenNeuron != null)
						{
							if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron)
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontoreddying_seengreenneuron"), 20);
							}
							else
							{
								owner.dialogBox.NewMessage(Translate("ss_reactiontoreddying_withgreenneuron"), 20);
							}
						}

						if (greenPearl != null && !SaveValues.fpSeenHunterPearl)
						{
							owner.dialogBox.NewMessage(Translate("ss_reactiontoreddying_withpearl"), 20);
						}
					}

					if (owner.nextPos != oracle.room.MiddleOfTile(10, 22))
					{
						owner.SetNewDestination(oracle.room.MiddleOfTile(10, 22));
					}

					if (foundPlayer != null)
					{
						foundPlayer.mainBodyChunk.vel *= Custom.LerpMap(inActionCounter, 0f, 30f, 1f, 0.95f);
						foundPlayer.bodyChunks[1].vel *= Custom.LerpMap(inActionCounter, 0f, 30f, 1f, 0.95f);
						foundPlayer.mainBodyChunk.vel += Custom.DirVec(foundPlayer.mainBodyChunk.pos, HoldPlayerPos) * Mathf.Lerp(0.5f, Custom.LerpMap(Vector2.Distance(foundPlayer.mainBodyChunk.pos, HoldPlayerPos), 30f, 150f, 2.5f, 7f), oracle.room.gravity) * Mathf.InverseLerp(0f, 10f, inActionCounter) * Mathf.InverseLerp(0f, 30f, Vector2.Distance(player.mainBodyChunk.pos, HoldPlayerPos));

						if (projectionCircle == null)
						{
							projectionCircle = new ProjectionCircle(foundPlayer.bodyChunks[0].pos, 0f, 3f);
							oracle.room.AddObject(projectionCircle);
						}
						else
						{
							float radiusSize = Mathf.Lerp(0f, 1f, (inActionCounter - 300f) / 150f);
							projectionCircle.radius = 12f * Mathf.Clamp(radiusSize * 2f, 0f, 1f);
							projectionCircle.pos = foundPlayer.bodyChunks[0].pos;
						}

						if (UnityEngine.Random.value > 0.9f)
						{
							foundPlayer.Stun((int)(UnityEngine.Random.value * 5f));
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
						if (greenPearl != null)
						{
							owner.dialogBox.NewMessage(owner.Translate("..."), 60);
							owner.dialogBox.NewMessage(owner.Translate("ss_reactiontoreddying_hunterpearl0"), 60);
							owner.dialogBox.NewMessage(owner.Translate("ss_reactiontoreddying_hunterpearl1"), 60);
							SaveValues.fpSeenHunterPearl = true;
						}

						if (greenNeuron != null && !oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron)
						{
							if (greenPearl != null)
							{
								owner.dialogBox.NewMessage(owner.Translate("ss_reactiontoreddying_hunterpearlextra"), 60);
							}
							owner.InitateConversation(MagicaEnums.ConversationIDs.GreenNeuronTooLate, this);
							oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron = true;
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

						IteratorHooks.moonHugRed = true;
						phase = RedPhases.END;
						circleTimer = inActionCounter;
					}

					if (foundPlayer != null)
					{
						foundPlayer.mainBodyChunk.vel *= Custom.LerpMap(inActionCounter, 0f, 30f, 1f, 0.95f);
						foundPlayer.bodyChunks[1].vel *= Custom.LerpMap(inActionCounter, 0f, 30f, 1f, 0.95f);
						foundPlayer.mainBodyChunk.vel += Custom.DirVec(foundPlayer.mainBodyChunk.pos, HoldPlayerPos) * Mathf.Lerp(0.5f, Custom.LerpMap(Vector2.Distance(foundPlayer.mainBodyChunk.pos, HoldPlayerPos), 30f, 150f, 2.5f, 7f), oracle.room.gravity) * Mathf.InverseLerp(0f, 10f, inActionCounter) * Mathf.InverseLerp(0f, 30f, Vector2.Distance(player.mainBodyChunk.pos, HoldPlayerPos));

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
						if (Custom.DistLess(foundPlayer.firstChunk.pos, greenNeuron.firstChunk.pos, 20f) && !foundPlayer.grasps.Any(x => x != null && x.grabbed == greenNeuron))
						{
							greenNeuron.storyFly = false;
							foundPlayer.SlugcatGrab(greenNeuron, foundPlayer.flipDirection == 1f ? 1 : 0);
							foundPlayer.controller = new Player.NullController();
						}
						else if (!foundPlayer.grasps.Any(x => x != null && x.grabbed == greenNeuron))
						{
							greenNeuron.storyFly = true;
							greenNeuron.storyFlyTarget = foundPlayer.firstChunk.pos;
							foundPlayer.Stun(10);
						}
						else
						{
							foundPlayer.sleepCurlUp += 0.1f;
						}
					}
					else if (greenNeuron == null)
					{
						foundPlayer.Stun(10);
					}

					if (projectionCircle != null)
					{
						float radiusSize = Mathf.Lerp(1f, 0f, (inActionCounter - 300f) / circleTimer);
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
						IteratorHooks.moonHugRed = false;
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
	}
}

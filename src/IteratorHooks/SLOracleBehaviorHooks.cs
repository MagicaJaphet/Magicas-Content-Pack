using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagicasContentPack.IteratorHooks
{
	internal class SLOracleBehaviorHooks
	{
		public static bool moonRevivedThisCycle;
		private static bool inspectObjectIsMoonPearl;
		public static void Init()
		{
			try
			{
				// Moon / FP "hugging" hunter
				_ = new Hook(typeof(SLOracleBehaviorHasMark).GetProperty(nameof(SLOracleBehaviorHasMark.OracleGetToPos), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<SLOracleBehaviorHasMark, Vector2> orig, SLOracleBehaviorHasMark oracle) =>
				{
					if (IteratorHooks.moonHugRed && oracle.player != null)
					{
						return oracle.player.firstChunk.pos + new Vector2(15f, 0f);
					}
					return orig(oracle);
				});

				_ = new Hook(typeof(SLOracleBehavior).GetProperty(nameof(SLOracleBehavior.EyesClosed), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<SLOracleBehavior, bool> orig, SLOracleBehavior oracle) =>
				{
					return orig(oracle) || IteratorHooks.moonHugRed;
				});

				On.SLOracleBehaviorHasMark.ctor += SLOracleBehaviorHasMark_ctor;
				On.SLOracleBehavior.Update += SLOracleBehavior_Update;
				On.SLOracleBehavior.InitCutsceneObjects += SLOracleBehavior_InitCutsceneObjects;
				On.SLOracleBehaviorHasMark.GrabObject += SLOracleBehaviorHasMark_GrabObject;
				On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMark_Update;
				On.SLOracleWakeUpProcedure.ctor += SLOracleWakeUpProcedure_ctor;
				On.SLOracleBehaviorHasMark.SpecialEvent += SLOracleBehaviorHasMark_SpecialEvent;
				On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
				On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static void SLOracleBehaviorHasMark_ctor(On.SLOracleBehaviorHasMark.orig_ctor orig, SLOracleBehaviorHasMark self, Oracle oracle)
		{
			orig(self, oracle);

			MagicaSaveState.GetKey(MoreSlugcatsEnums.SlugcatStatsName.Saint.value, nameof(SaveValues.MoonOverWrotePearl), out SaveValues.MoonOverWrotePearl);
			MagicaSaveState.GetKey(MoreSlugcatsEnums.SlugcatStatsName.Saint.value, nameof(SaveValues.lttmSawAscensionCycle), out SaveValues.lttmSawAscensionCycle);
		}

		private static void SLOracleBehavior_Update(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
		{
			if (IteratorHooks.moonHugRed)
			{
				self.dontHoldKnees = 5;
			}

			orig(self, eu);
		}


		private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
		{
			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_PearlBleaching && !SaveValues.MoonOverWrotePearl && inspectObjectIsMoonPearl)
			{
				self.LoadEventsFromFile(413);
				return;
			}
			if (self.id == MoreSlugcatsEnums.ConversationID.Moon_PearlBleaching && !SaveValues.MoonOverWrotePearl)
			{
				SaveValues.MoonOverWrotePearl = true;
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
			if (self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint && SaveValues.lttmSawAscensionCycle == self.oracle.room.game.GetStorySession.saveState.cycleNumber)
			{
				self.currentConversation?.Destroy();
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

			if (self.oracle.room.world.game.GetStorySession != null && !WinOrSaveHooks.redEndingProcedure && self.player != null && self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == SlugcatStats.Name.Red && self.oracle.room.game.GetStorySession.RedIsOutOfCycles)
			{
				Plugin.DebugLog("Hunter ending with Moon started");
				self.oracle.room.AddObject(new RedDyingEnding(self.oracle));
				WinOrSaveHooks.redEndingProcedure = true;
			}
		}

		private static void SLOracleBehaviorHasMark_SpecialEvent(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
		{
			orig(self, eventName);

			if (eventName == "redhug")
			{
				IteratorHooks.moonHugRed = true;
			}
			if (eventName == "MoonRewritesSaintPearl")
			{
				if (self.holdingObject != null && self.holdingObject is DataPearl)
				{
					IteratorHooks.LerpPearlColors(self.holdingObject as DataPearl);
					(self.holdingObject as DataPearl).AbstractPearl.dataPearlType = MagicaEnums.DataPearlIDs.MoonRewrittenPearl;
					for (int i = 0; i < 20; i++)
					{
						self.oracle.room.AddObject(new Spark(self.holdingObject.firstChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
					}
					self.oracle.room.PlaySound(SoundID.Moon_Wake_Up_Swarmer_Ping, 0f, 1f, 1f);
				}
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
			if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.CheckForSaintAscension(self.player) && (Custom.Dist(self.oracle.bodyChunks[0].pos, new Vector2(self.player.mainBodyChunk.pos.x + self.player.burstX, self.player.mainBodyChunk.pos.y + self.player.burstY + 60f)) < 30f || ModOptions.CustomMechanics.Value && PlayerHooks.MagicaPlayer.magicaCWT.TryGetValue(self.player, out var saint) && saint.ascendTimer > 0f) && !IteratorHooks.hasBeenTouchedBySaintsHalo)
			{
				self.dialogBox.Interrupt(self.Translate("sl_reactiontokarma"), 80);
				IteratorHooks.hasBeenTouchedBySaintsHalo = true;
				if (self.currentConversation != null)
				{
					self.currentConversation.paused = true;
					self.resumeConversationAfterCurrentDialoge = true;
				}
			}
			if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.CheckForSaintAscension(self.player) && SaveValues.lttmSawAscensionCycle == -1)
			{
				SaveValues.lttmSawAscensionCycle = self.oracle.room.game.GetStorySession.saveState.cycleNumber;
				self.InitateConversation();
			}

			orig(self, eu);
		}

		private static void SLOracleWakeUpProcedure_ctor(On.SLOracleWakeUpProcedure.orig_ctor orig, SLOracleWakeUpProcedure self, Oracle SLOracle)
		{
			orig(self, SLOracle);
			moonRevivedThisCycle = true;
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

				SaveValues.HunterOracleID = oracle.ID.value;
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
						foundPlayer = room.game.Players[0].realizedCreature as Player;
					}
				}

				if (phase == RedPhases.WAIT && oracle.oracleBehavior.player.room == oracle.room)
				{
					if (oracle != null && oracle.ID == Oracle.OracleID.SL)
					{
						if (foundPlayer.firstChunk.pos.x > 800f)
						{
							foundPlayer.controller = new RedEndingController(this);
							timer = 0;
						}
						if (foundPlayer.firstChunk.pos.x > 1195 || (timer > 600 && oracle.Consious && oracle.oracleBehavior is SLOracleBehaviorHasMark moon && moon.currentConversation != null))
						{
							phase = RedPhases.START;
							timer = 0;
						}
					}
				}

				if (phase == RedPhases.START)
				{
					if (oracle != null && oracle.ID == Oracle.OracleID.SL)
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

						if ((oracle.Consious && timer > 1600) || (!oracle.Consious && timer > 500))
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
					if ((oracle.Consious && oracle.oracleBehavior is SLOracleBehaviorHasMark moon && moon.currentConversation == null) || !oracle.Consious)
					{
						foundPlayer.abstractCreature.world.game.GameOver(null);
						fadeOut.Destroy();
						IteratorHooks.moonHugRed = false;
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
	}
}

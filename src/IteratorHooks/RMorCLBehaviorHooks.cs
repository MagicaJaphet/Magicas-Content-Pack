using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagicasContentPack.IteratorHooks
{
	internal class RMorCLBehaviorHooks
	{
		public static void Init()
		{
			try
			{
				On.MoreSlugcats.CLOracleBehavior.ctor += CLOracleBehavior_ctor;
				On.MoreSlugcats.CLOracleBehavior.InitateConversation += CLOracleBehavior_InitateConversation;
				On.MoreSlugcats.CLOracleBehavior.Update += CLOracleBehavior_Update;

				Plugin.HookSucceed();
			}
			catch (Exception ex)
			{
				Plugin.HookFail(ex);
			}
		}

		private static void CLOracleBehavior_ctor(On.MoreSlugcats.CLOracleBehavior.orig_ctor orig, CLOracleBehavior self, Oracle oracle)
		{
			orig(self, oracle);
			MagicaSaveState.GetKey(MoreSlugcatsEnums.SlugcatStatsName.Saint.value, nameof(SaveValues.CLSeenMoonPearl), out SaveValues.CLSeenMoonPearl);
			MagicaSaveState.GetKey(MoreSlugcatsEnums.SlugcatStatsName.Saint.value, nameof(SaveValues.fpHasSeenMonkAscension), out SaveValues.fpHasSeenMonkAscension);
		}

		private static void CLOracleBehavior_InitateConversation(On.MoreSlugcats.CLOracleBehavior.orig_InitateConversation orig, CLOracleBehavior self)
		{
			if (SaveValues.fpHasSeenMonkAscension)
			{
				self.dialogBox.NewMessage(self.Translate("..."), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontokarma0"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontokarma1"), 80);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontokarma2"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontokarma3"), 80);
				return;
			}

			if (!SaveValues.CLSeenMoonPearl && IteratorHooks.moonPearlObj != null)
			{
				SaveValues.CLSeenMoonPearl = true;

				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl0"), 60);
				self.dialogBox.NewMessage(self.Translate("..."), 120);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl1"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl2"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl3"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl4"), 90);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl5"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl6"), 90);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl7"), 120);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl8"), 60);
				self.dialogBox.NewMessage(self.Translate("cl_reactiontopearl9"), 20);
				return;
			}

			orig(self);
		}

		private static void CLOracleBehavior_Update(On.MoreSlugcats.CLOracleBehavior.orig_Update orig, CLOracleBehavior self, bool eu)
		{
			if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && PlayerHooks.CheckForSaintAscension(self.player) && !SaveValues.fpHasSeenMonkAscension)
			{
				SaveValues.fpHasSeenMonkAscension = true;
				self.InitateConversation();
			}

			if (IteratorHooks.moonPearlObj == null && !SaveValues.CLSeenMoonPearl)
			{
				for (int j = 0; j < self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Count; j++)
				{
					if (self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item is DataPearl checkPearl && checkPearl.AbstractPearl.dataPearlType == MagicaEnums.DataPearlIDs.MoonRewrittenPearl && Custom.DistLess(checkPearl.firstChunk.pos, self.oracle.firstChunk.pos, 100f))
					{
						IteratorHooks.moonPearlObj = checkPearl;
						self.currentConversation?.Destroy();
						self.currentConversation = null;
						self.dialogBox.Interrupt(self.Translate("..."), 40);
						self.InitateConversation();
						break;
					}
				}
			}

			orig(self, eu);

			if (IteratorHooks.moonPearlObj != null && SaveValues.CLSeenMoonPearl && !self.FocusedOnHalcyon)
			{
				Vector2? pearlTargetPos = new Vector2?(self.oracle.bodyChunks[0].pos + new Vector2(10f, 20f));
				float num = Custom.Dist((Vector2)pearlTargetPos, IteratorHooks.moonPearlObj.firstChunk.pos);

				self.lookPoint = IteratorHooks.moonPearlObj.firstChunk.pos;

				IteratorHooks.moonPearlObj.firstChunk.vel *= Custom.LerpMap(IteratorHooks.moonPearlObj.firstChunk.vel.magnitude, 1f, 6f, 0.999f, 0.9f);
				IteratorHooks.moonPearlObj.firstChunk.vel += Vector2.ClampMagnitude(pearlTargetPos.Value - IteratorHooks.moonPearlObj.firstChunk.pos, 100f) / 100f * 0.4f;
				IteratorHooks.moonPearlObj.gravity = 0f;
			}
			else if (IteratorHooks.moonPearlObj != null)
			{
				IteratorHooks.moonPearlObj.gravity = 0.9f;
			}
		}
	}
}

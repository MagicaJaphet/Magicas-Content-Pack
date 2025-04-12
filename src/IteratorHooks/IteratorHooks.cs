using IL;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using On;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MagicasContentPack.IteratorHooks
{
	/// <summary>
	/// Enclosing hooks class for every iterator-related.
	/// </summary>
	public class IteratorHooks
    {

        internal static bool allowFpToCheckHisPearl;
        public static bool moonHugRed;
        internal static bool hasBeenTouchedBySaintsHalo;
        internal static bool checkedForMoonPearlThisCycle;
        internal static DataPearl moonPearlObj;

        public static void Init()
		{
			OracleHooks.Init();
			CustomOracleHooks.Init();
			SSOracleBehaviorHooks.Init();
			SLOracleBehaviorHooks.Init();
			RMorCLBehaviorHooks.Init();

			try
            {
                _ = new Hook(typeof(Inspector).GetProperty(nameof(Inspector.OwneriteratorColor), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), (Func<Inspector, Color> orig, Inspector inspector) =>
                {
                    var result = orig(inspector);
                    if (inspector.ownerIterator == 3)
                    {
                        return new(1f, 0f, 0f);
                    }
                    return result;
                });

                // For iterator related things
                On.MoreSlugcats.Inspector.InitiateGraphicsModule += Inspector_InitiateGraphicsModule;
                On.DataPearl.ApplyPalette += CustomPearlColors;

                Plugin.HookSucceed();
            }
            catch (Exception ex)
            {
                Plugin.HookFail(ex);
            }
        }

        private static void Inspector_InitiateGraphicsModule(On.MoreSlugcats.Inspector.orig_InitiateGraphicsModule orig, Inspector self)
        {
            if (self.room.game.IsStorySession && self.room.world.region != null)
            {
                if (self.room.world.region.name == "7S")
                {
                    self.ownerIterator = 3;
                }
            }

            orig(self);
        }

		private static void CustomPearlColors(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			// Fucked up and evil code if real
			if (rCam.game != null && rCam.game.IsStorySession && rCam.room.abstractRoom.name == "7S_AI")
			{
				if (self.color == new Color(1f, 0.47843137f, 0.007843138f))
				{
					self.color = OracleHooks.oracleColor[OracleHooks.OracleColor.SRSPearls];
				}
			}
		}

        internal static void LerpPearlColors(DataPearl dataPearl)
        {
            DataPearl.AbstractDataPearl.DataPearlType dataPearlType = dataPearl.AbstractPearl.dataPearlType;

            if (dataPearl != null)
            {
                dataPearl.color = DataPearl.UniquePearlMainColor(MagicaEnums.DataPearlIDs.MoonRewrittenPearl);
                dataPearl.highlightColor = DataPearl.UniquePearlHighLightColor(MagicaEnums.DataPearlIDs.MoonRewrittenPearl);
            }
        }
    }
}
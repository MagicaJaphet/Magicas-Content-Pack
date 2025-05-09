using SlideShowID = Menu.SlideShow.SlideShowID;
using SceneID = Menu.MenuScene.SceneID;
using Action = SSOracleBehavior.Action;
using SubBehavID = SSOracleBehavior.SubBehavior.SubBehavID;
using ConversationID = Conversation.ID;
using CustomOverseerConcern = OverseerCommunicationModule.PlayerConcern;
using CustomOverseerHolograms = OverseerHolograms.OverseerHologram.Message;
using CustomOverseerImage = OverseerHolograms.OverseerImage.ImageID;
using DataPearlID = DataPearl.AbstractDataPearl.DataPearlType;
using OracleID = Oracle.OracleID;
using OracleMovementID = SSOracleBehavior.MovementBehavior;
using TickerID = Menu.StoryGameStatisticsScreen.TickerID;
using BodyMode = Player.BodyModeIndex;

namespace MagicasContentPack
{
	public class MagicaEnums
	{
		public static void RegisterEnums()
		{
			BodyModes.Register();
			SaintRainPhases.Register();
			SceneIDs.Register();
			SlidesShowIDs.Register();
			OracleActions.Register();
			ConversationIDs.Register();
			Oracles.Register();
			OracleMovementIDs.Register();
			DataPearlIDs.Register();
			TickerIDs.Register();
		}

		public class SaintRainPhases(string value, bool register = false) : ExtEnum<SaintRainPhases>(value, register)
		{
			public static SaintRainPhases None;

			public static SaintRainPhases Linear;

			public static SaintRainPhases Wavering;

			public static SaintRainPhases Constant;

			public static void Register()
			{
				None = new(nameof(None), true);
				Linear = new(nameof(Linear), true);
				Wavering = new(nameof(Wavering), true);
				Constant = new(nameof(Constant), true);
			}
		}

		public class BodyModes
		{
			public static void Register()
			{
				SaintAscension = new(nameof(SaintAscension), true);
			}

			public static BodyMode SaintAscension;
		}

		//MenuScene MagicaEnums
		public class SceneIDs
		{
			public static SceneID CustomSlugcat_SpearPearl;
			public static SceneID CustomSlugcat_SpearSRS;

			public static SceneID Outro_Artificer6;

			public static SceneID AltEnd_Artificer_4;

			public static SceneID Outro_SpearmasterSwimLeft;
			public static SceneID Outro_SpearmasterOutroSwim;
			public static SceneID Outro_SpearmasterOutroPause;
			public static SceneID Outro_SpearmasterOutroLook;
			public static SceneID Outro_SpearmasterOutroHop;
			public static SceneID Outro_SpearmasterOutroEmbrace;

			public static SceneID Intro_Spearmasterentrance;
			public static SceneID Intro_Spearmasteroverseer;
			public static SceneID Intro_Spearmasterlook;
			public static SceneID Intro_Spearmasterdownlook;
			public static SceneID Intro_Spearmasterleap;
			public static SceneID Intro_SpearmasterSRS;

			public static SceneID Outro_SpearmasterOutroAltComms;
			public static SceneID Outro_SpearmasterOutroAltDescent;
			public static SceneID Outro_SpearmasterOutroAltChimney;
			public static SceneID Outro_SpearmasterOutroAltMice;
			public static SceneID Outro_SpearmasterOutroAltWaterfront;
			public static SceneID Outro_SpearmasterOutroAltLuna;
			public static SceneID Outro_SpearmasterOutroAltChamber;
			public static SceneID Outro_SpearmasterOutroAltCollapse;

			public static SceneID Placeholder;

			public static void Register()
			{
				CustomSlugcat_SpearPearl = new(nameof(CustomSlugcat_SpearPearl), true);
				CustomSlugcat_SpearSRS = new(nameof(CustomSlugcat_SpearSRS), true);

				Outro_Artificer6 = new(nameof(Outro_Artificer6), true);

				AltEnd_Artificer_4 = new(nameof(AltEnd_Artificer_4), true);

				Outro_SpearmasterSwimLeft = new(nameof(Outro_SpearmasterSwimLeft), true);
				Outro_SpearmasterOutroSwim = new(nameof(Outro_SpearmasterOutroSwim), true);
				Outro_SpearmasterOutroPause = new(nameof(Outro_SpearmasterOutroPause), true);
				Outro_SpearmasterOutroLook = new(nameof(Outro_SpearmasterOutroLook), true);
				Outro_SpearmasterOutroHop = new(nameof(Outro_SpearmasterOutroHop), true);
				Outro_SpearmasterOutroEmbrace = new(nameof(Outro_SpearmasterOutroEmbrace), true);

				Intro_Spearmasterentrance = new(nameof(Intro_Spearmasterentrance), true);
				Intro_Spearmasteroverseer = new(nameof(Intro_Spearmasteroverseer), true);
				Intro_Spearmasterlook = new(nameof(Intro_Spearmasterlook), true);
				Intro_Spearmasterdownlook = new(nameof(Intro_Spearmasterdownlook), true);
				Intro_Spearmasterleap = new(nameof(Intro_Spearmasterleap), true);
				Intro_SpearmasterSRS = new(nameof(Intro_SpearmasterSRS), true);

				Outro_SpearmasterOutroAltComms = new(nameof(Outro_SpearmasterOutroAltComms), true);
				Outro_SpearmasterOutroAltDescent = new(nameof(Outro_SpearmasterOutroAltDescent), true);
				Outro_SpearmasterOutroAltChimney = new(nameof(Outro_SpearmasterOutroAltChimney), true);
				Outro_SpearmasterOutroAltMice = new(nameof(Outro_SpearmasterOutroAltMice), true);
				Outro_SpearmasterOutroAltWaterfront = new(nameof(Outro_SpearmasterOutroAltWaterfront), true);
				Outro_SpearmasterOutroAltLuna = new(nameof(Outro_SpearmasterOutroAltLuna), true);
				Outro_SpearmasterOutroAltChamber = new(nameof(Outro_SpearmasterOutroAltChamber), true);
				Outro_SpearmasterOutroAltCollapse = new(nameof(Outro_SpearmasterOutroAltCollapse), true);

				Placeholder = new(nameof(Placeholder), true);
			}
		}

		// Slideshow MagicaEnums
		public class SlidesShowIDs
		{
			public static SlideShowID SpearIntro;
			public static SlideShowID SpearAltOutro;
			public static SlideShowID ArtificerDreamE;
			public static SlideShowID RedsDeath;

			public static void Register()
			{
				SpearIntro = new(nameof(SpearIntro), true);
				SpearAltOutro = new(nameof(SpearAltOutro), true);
				ArtificerDreamE = new(nameof(ArtificerDreamE), true);
				RedsDeath = new(nameof(RedsDeath), true);
			}
		}
		
		// Custom oracle actions
		public class OracleActions
		{
			public static Action ArtiFPPearlInit;
			public static Action ArtiFPPearlInspect;
			public static Action ArtiFPPearlPlay;
			public static Action ArtiFPPearlGrabbed;
			public static Action ArtiFPPearlStop;
			public static SubBehavID MusicPearlInspect;

			public static Action FPMusicPearlInit;
			public static Action FPMusicPearlPlay;
			public static Action FPMusicPearlStop;
			public static SubBehavID MusicPearlInspect2;

			public static Action ArtiFPAllPearls;
			public static SubBehavID ArtiFPEnding;

			public static Action SRSSeesPurple;
			public static SubBehavID SRSPurple;

			public static Action RedDiesInFP;
			public static SubBehavID RedDies;

			public static void Register()
			{
				ArtiFPPearlInit = new(nameof(ArtiFPPearlInit), true);
				ArtiFPPearlInspect = new(nameof(ArtiFPPearlInspect), true);
				ArtiFPPearlPlay = new(nameof(ArtiFPPearlPlay), true);
				ArtiFPPearlGrabbed = new(nameof(ArtiFPPearlGrabbed), true);
				ArtiFPPearlStop = new(nameof(ArtiFPPearlStop), true);
				MusicPearlInspect = new(nameof(MusicPearlInspect), true);

				FPMusicPearlInit = new(nameof(FPMusicPearlInit), true);
				FPMusicPearlPlay = new(nameof(FPMusicPearlPlay), true);
				FPMusicPearlStop = new(nameof(FPMusicPearlStop), true);
				MusicPearlInspect2 = new(nameof(MusicPearlInspect2), true);

				ArtiFPAllPearls = new(nameof(ArtiFPAllPearls), true);
				ArtiFPEnding = new(nameof(ArtiFPEnding), true);

				SRSSeesPurple = new(nameof(SRSSeesPurple), true);
				SRSPurple = new(nameof(SRSPurple), true);

				RedDiesInFP = new(nameof(RedDiesInFP), true);
				RedDies = new(nameof(RedDies), true);
			}
		}

		// Conversation IDs
		public class ConversationIDs
		{
			public static ConversationID MusicPearlDialogue;
			public static ConversationID MusicPearlDialogue2;
			public static ConversationID SRSMeetsSpear;

			public static ConversationID RedDiesMoon;

			public static ConversationID RedDiesPebbles1;
			public static ConversationID RedDiesPebbles2;

			public static ConversationID GreenNeuronTooLate;

			public static ConversationID RedRevivedMoonButDiesTragically;

			public static ConversationID SaintLTTMMonkAscension;

			public static void Register()
			{
				MusicPearlDialogue = new(nameof(MusicPearlDialogue), true);
				MusicPearlDialogue2 = new(nameof(MusicPearlDialogue2), true);
				SRSMeetsSpear = new(nameof(SRSMeetsSpear), true);

				RedDiesMoon = new(nameof(RedDiesMoon), true);

				RedDiesPebbles1 = new(nameof(RedDiesPebbles1), true);
				RedDiesPebbles2 = new(nameof(RedDiesPebbles2), true);

				GreenNeuronTooLate = new(nameof(GreenNeuronTooLate), true);

				RedRevivedMoonButDiesTragically = new(nameof(RedRevivedMoonButDiesTragically), true);

				SaintLTTMMonkAscension = new(nameof(SaintLTTMMonkAscension), true);
			}
		}
		
		// Custom iterators
		public class Oracles
		{
			public static OracleID SRS;

			public static void Register()
			{
				SRS = new(nameof(SRS), true);
			}
		}

		// Custom oracle movements
		public class OracleMovementIDs
		{
			public static OracleMovementID SitAndFocus = new(nameof(SitAndFocus), true);
			public static void Register()
			{
				SitAndFocus = new(nameof(SitAndFocus), true);
			}
		}

		// Custom pearls
		public class DataPearlIDs
		{
			public static DataPearlID MoonRewrittenPearl = new(nameof(MoonRewrittenPearl), true);
			public static void Register()
			{
				MoonRewrittenPearl = new(nameof(MoonRewrittenPearl), true);
			}
		}

		public class TickerIDs
		{
			public static TickerID HelpedBSM = new(nameof(HelpedBSM), true);
			public static void Register()
			{
				HelpedBSM = new(nameof(HelpedBSM), true);
			}
		}
	}
}

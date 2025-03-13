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

namespace MagicasContentPack
{
	public class MagicaEnums
	{
		//MenuScene MagicaEnums
		public class SceneIDs
		{
			public static SceneID CustomSlugcat_SpearPearl = new(nameof(CustomSlugcat_SpearPearl), true);
			public static SceneID CustomSlugcat_SpearSRS = new(nameof(CustomSlugcat_SpearSRS), true);

			public static SceneID Outro_Artificer6 = new(nameof(Outro_Artificer6), true);

			public static SceneID AltEnd_Artificer_4 = new(nameof(AltEnd_Artificer_4), true);

			public static SceneID Outro_SpearmasterSwimLeft = new(nameof(Outro_SpearmasterSwimLeft), true);
			public static SceneID Outro_SpearmasterOutroSwim = new(nameof(Outro_SpearmasterOutroSwim), true);
			public static SceneID Outro_SpearmasterOutroPause = new(nameof(Outro_SpearmasterOutroPause), true);
			public static SceneID Outro_SpearmasterOutroLook = new(nameof(Outro_SpearmasterOutroLook), true);
			public static SceneID Outro_SpearmasterOutroHop = new(nameof(Outro_SpearmasterOutroHop), true);
			public static SceneID Outro_SpearmasterOutroEmbrace = new(nameof(Outro_SpearmasterOutroEmbrace), true);

			public static SceneID Intro_Spearmasterentrance = new(nameof(Intro_Spearmasterentrance), true);
			public static SceneID Intro_Spearmasteroverseer = new(nameof(Intro_Spearmasteroverseer), true);
			public static SceneID Intro_Spearmasterlook = new(nameof(Intro_Spearmasterlook), true);
			public static SceneID Intro_Spearmasterdownlook = new(nameof(Intro_Spearmasterdownlook), true);
			public static SceneID Intro_Spearmasterleap = new(nameof(Intro_Spearmasterleap), true);
			public static SceneID Intro_SpearmasterSRS = new(nameof(Intro_SpearmasterSRS), true);

			public static SceneID Outro_SpearmasterOutroAltComms = new(nameof(Outro_SpearmasterOutroAltComms), true);
			public static SceneID Outro_SpearmasterOutroAltDescent = new(nameof(Outro_SpearmasterOutroAltDescent), true);
			public static SceneID Outro_SpearmasterOutroAltChimney = new(nameof(Outro_SpearmasterOutroAltChimney), true);
			public static SceneID Outro_SpearmasterOutroAltMice = new(nameof(Outro_SpearmasterOutroAltMice), true);
			public static SceneID Outro_SpearmasterOutroAltWaterfront = new(nameof(Outro_SpearmasterOutroAltWaterfront), true);
			public static SceneID Outro_SpearmasterOutroAltLuna = new(nameof(Outro_SpearmasterOutroAltLuna), true);
			public static SceneID Outro_SpearmasterOutroAltChamber = new(nameof(Outro_SpearmasterOutroAltChamber), true);
			public static SceneID Outro_SpearmasterOutroAltCollapse = new(nameof(Outro_SpearmasterOutroAltCollapse), true);
		}

		// Slideshow MagicaEnums
		public class SlidesShowIDs
		{
			public static SlideShowID SpearIntro = new(nameof(SpearIntro), true);
			public static SlideShowID SpearAltOutro = new(nameof(SpearAltOutro), true);
			public static SlideShowID ArtificerDreamE = new(nameof(ArtificerDreamE), true);
			public static SlideShowID RedsDeath = new(nameof(RedsDeath), true);
		}
		
		// Custom oracle actions
		public class OracleActions
		{
			public static Action ArtiFPPearlInit = new(nameof(ArtiFPPearlInit), true);
			public static Action ArtiFPPearlInspect = new(nameof(ArtiFPPearlInspect), true);
			public static Action ArtiFPPearlPlay = new(nameof(ArtiFPPearlPlay), true);
			public static Action ArtiFPPearlGrabbed = new(nameof(ArtiFPPearlGrabbed), true);
			public static Action ArtiFPPearlStop = new(nameof(ArtiFPPearlStop), true);
			public static SubBehavID MusicPearlInspect = new(nameof(MusicPearlInspect), true);

			public static Action FPMusicPearlInit = new(nameof(FPMusicPearlInit), true);
			public static Action FPMusicPearlPlay = new(nameof(FPMusicPearlPlay), true);
			public static Action FPMusicPearlStop = new(nameof(FPMusicPearlStop), true);
			public static SubBehavID MusicPearlInspect2 = new(nameof(MusicPearlInspect2), true);

			public static Action ArtiFPAllPearls = new(nameof(ArtiFPAllPearls), true);
			public static SubBehavID ArtiFPEnding = new(nameof(ArtiFPEnding), true);

			public static Action SRSSeesPurple = new(nameof(SRSSeesPurple), true);
			public static SubBehavID SRSPurple = new(nameof(SRSPurple), true);

			public static Action RedDiesInFP = new(nameof(RedDiesInFP), true);
			public static SubBehavID RedDies = new(nameof(RedDies), true);
		}

		// Conversation IDs
		public class ConversationIDs
		{
			public static ConversationID MusicPearlDialogue = new(nameof(MusicPearlDialogue), true);
			public static ConversationID MusicPearlDialogue2 = new(nameof(MusicPearlDialogue2), true);
			public static ConversationID SRSMeetsSpear = new(nameof(SRSMeetsSpear), true);

			public static ConversationID RedDiesMoon = new(nameof(RedDiesMoon), true);

			public static ConversationID RedDiesPebbles1 = new(nameof(RedDiesPebbles1), true);
			public static ConversationID RedDiesPebbles2 = new(nameof(RedDiesPebbles2), true);

			public static ConversationID GreenNeuronTooLate = new(nameof(GreenNeuronTooLate), true);

			public static ConversationID RedRevivedMoonButDiesTragically = new(nameof(RedRevivedMoonButDiesTragically), true);

			public static ConversationID SaintLTTMMonkAscension = new(nameof(SaintLTTMMonkAscension), true);
		}

		// Overseer concern
		public class OverseerConcerns
		{
			public static CustomOverseerConcern ShowSMHolograms = new(nameof(ShowSMHolograms), true);
			public static CustomOverseerHolograms SRSHolograms = new(nameof(SRSHolograms), true);
		}

		public class OverseerImages
		{
			public static CustomOverseerImage OETreeTopImage = new(nameof(OETreeTopImage), true);
		}
		
		// Custom iterators
		public class Oracles
		{
			public static OracleID SRS = new(nameof(OracleID), true);
		}

		// Custom oracle movements
		public class OracleMovementIDs
		{
			public static OracleMovementID SitAndFocus = new(nameof(SitAndFocus), true);
		}

		// Custom pearls
		public class DataPearlIDs
		{
			public static DataPearlID MoonRewrittenPearl = new(nameof(MoonRewrittenPearl), true);
		}

		public class TickerIDs
		{
			public static TickerID HelpedBSM = new(nameof(HelpedBSM), true);
		}
	}
}

using DressMySlugcat;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace MagicasContentPack
{
	internal class MagicaSaveState
	{
		internal static readonly string anySave = "Any";
		private static readonly string saveName = "sav";
		private static Dictionary<string, Dictionary<string, object>> save = [];
		private static bool beingWrittenTo;
		private static readonly string savePath = Path.Combine(Plugin.modPath, "saves");

		internal static Dictionary<string, Dictionary<string, object>> SaveInformation
		{
			get
			{
				if (save.Count == 0)
				{
					beingWrittenTo = true;
					LoadJSONSaveData();
				}
				return save;
			}
			set
			{
				save = value;
			}
		}

		private static string SaveFileJSON
		{
			get
			{
				if (Custom.rainWorld?.options?.saveSlot is int slot)
				{
					return Path.Combine(savePath, $"{saveName}{slot + 1}");
				}
				return Directory.GetFiles(savePath).FirstOrDefault() ?? Path.Combine(savePath, $"{saveName}Backup");
			}
		}

		internal static void LoadJSONSaveData()
		{
			if (!Directory.Exists(savePath))
			{
				Directory.CreateDirectory(savePath);
			}
			if (!File.Exists(SaveFileJSON))
			{
				CreateFile();
			}
			else
			{
				TryLoadSaveData();
			}
		}

		private static void TryLoadSaveData()
		{
			Dictionary<string, object> information = File.ReadAllText(SaveFileJSON).dictionaryFromJson();
			Dictionary<string, Dictionary<string, object>> save = [];
			foreach (var name in information.Keys)
			{
				if (information[name] != null) 
				{
					save.Add(name, information[name] as Dictionary<string, object>);
				}
			}

			if (save != null)
			{
				Plugin.DebugLog("Save data found!");
				SaveInformation = save;
				beingWrittenTo = false;
			}
			else
			{
				CreateFile();
			}
		}

		internal static void SaveFile(string slugcat)
		{
			try
			{
				if (SaveInformation.ContainsKey(slugcat))
				{
					Dictionary<string, object> properties = GetSlugcatSaveStatePropeties(slugcat);
					foreach (var key in properties.Keys)
					{
						if (SaveInformation[slugcat].ContainsKey(key) && SaveInformation[slugcat][key] != properties[key])
						{
							Plugin.DebugLog($"Changed {key} : {SaveInformation[slugcat][key]} to {properties[key]}");
							SaveInformation[slugcat][key] = properties[key];
						}
						else
						{
							SaveInformation[slugcat].Add(key, properties[key]);
							Plugin.DebugLog($"Added {key} : {properties[key]}");
						}
					}
					Dictionary<string, object> anyProperty = GetSlugcatSaveStatePropeties(anySave);
					if (anyProperty.Count > 0)
					{
						if (!SaveInformation.ContainsKey(anySave))
						{
							SaveInformation.Add(anySave, anyProperty);
						}
						else
						{
							foreach (var key in anyProperty.Keys)
							{
								if (SaveInformation[anySave].ContainsKey(key) && SaveInformation[anySave][key] != anyProperty[key])
								{
									Plugin.DebugLog($"Changed {key} : {SaveInformation[anySave][key]} to {anyProperty[key]}");
									SaveInformation[anySave][key] = anyProperty[key];
								}
								else
								{
									SaveInformation[anySave].Add(key, anyProperty[key]);
									Plugin.DebugLog($"Added {key} : {anyProperty[key]}");
								}
							}
						}
					}

					UpdateDiskSave();
					Plugin.DebugLog("Save file brought up to date!");
				}

			}
			catch (Exception ex)
			{
				Plugin.Logger.LogError($"FAILED TO SAVE TO SAVE FILE: {ex}");
			}
		}

		internal static bool GetKey(string slugName, string key, out bool result)
		{
			result = FindKey(slugName, key, out string text) && bool.TryParse(text, out result);
			return result;
		}

		internal static bool GetKey(string slugName, string key, out int result)
		{
			if (FindKey(slugName, key, out string text) && int.TryParse(text, out result))
			{
				return true;
			}
			result = -1;
			return false;
		}
		internal static bool GetKey(string slugName, string key, out string result)
		{
			FindKey(slugName, key, out result);
			return !string.IsNullOrEmpty(result);
		}

		private static bool FindKey(string slugName, string key, out string value)
		{
			value = null;
			try
			{
				if (string.IsNullOrEmpty(slugName))
					slugName = anySave;

				if (string.IsNullOrEmpty(key))
				{
					Plugin.Logger.LogError($"SEARCH KEY FOR SAVE DATA IS INVALID!");
					return false;
				}

				if (SaveInformation.ContainsKey(slugName) && SaveInformation[slugName].ContainsKey(key))
				{
					value = SaveInformation[slugName][key] switch
					{
						int x => x.ToString(),
						bool x => x.ToString(),
						string x => x,
						_ => null,
					};

					return value != null;
				}
			}
			catch (Exception ex)
			{
				Plugin.Logger.LogError($"FAILED TO FIND KEY: {ex}");
			}

			return false;
		}

		internal static void WipeSave(string name = null)
		{
			while (SaveInformation == null && beingWrittenTo) { }
			if (name != null)
			{
				if (SaveInformation.ContainsKey(name))
				{
					if (SaveInformation.ContainsKey(anySave) && GetKey(anySave, nameof(SaveValues.WhoShowedFPThePearl), out string pearlName) && pearlName == name)
					{
						SaveValues.WhoShowedFPThePearl = default;
						SaveInformation[anySave].Remove(nameof(SaveValues.WhoShowedFPThePearl));
					}
					SaveInformation[name] = GetSlugcatSaveStatePropeties(name, true);
				}
			}
			else
			{
				List<string> keys = SaveInformation.Keys.ToList();
				foreach (string key in keys)
				{
					if (SaveInformation.ContainsKey(key))
					{
						SaveInformation[key] = GetSlugcatSaveStatePropeties(key, true);
					}
				}
			}
			UpdateDiskSave();
		}

		private static void CreateFile()
		{
			Dictionary<string, Dictionary<string, object>> slugcatSlots = [];
			foreach (var name in SlugcatStats.Name.values.entries)
			{
				if (new SlugcatStats.Name(name, false) is SlugcatStats.Name slugname && Plugin.IsVanillaSlugcat(slugname))
				{
					slugcatSlots.Add(name, GetSlugcatSaveStatePropeties(name));
				}
			}
			if (GetSlugcatSaveStatePropeties(anySave).Count > 0)
			{
				slugcatSlots.Add(anySave, GetSlugcatSaveStatePropeties(anySave));
			}

			SaveInformation = slugcatSlots;
			beingWrittenTo = false;
			UpdateDiskSave();
		}

		private static Dictionary<string, object> GetSlugcatSaveStatePropeties(string slugname, bool reset = false)
		{
			Dictionary<string, object> values = [];

			if (!string.IsNullOrEmpty(slugname))
			{
				switch (slugname)
				{
					case var _ when slugname == anySave:
						if (reset)
						{
							SaveValues.WhoShowedFPThePearl = default;
						}

						if (!string.IsNullOrEmpty(SaveValues.WhoShowedFPThePearl))
							values.Add(nameof(SaveValues.WhoShowedFPThePearl), SaveValues.WhoShowedFPThePearl);
						break;

					case var _ when slugname == MoreSlugcatsEnums.SlugcatStatsName.Spear.value:
						if (reset)
						{
							SaveValues.OEGateOpenedAsSpear = default;
							SaveValues.SpearMetSRS = default;
						}

						if (SaveValues.OEGateOpenedAsSpear)
							values.Add(nameof(SaveValues.OEGateOpenedAsSpear), SaveValues.OEGateOpenedAsSpear);
						if (SaveValues.SpearMetSRS)
							values.Add(nameof(SaveValues.SpearMetSRS), SaveValues.SpearMetSRS);
						break;

					case var _ when slugname == MoreSlugcatsEnums.SlugcatStatsName.Artificer.value:
						if (reset)
						{
							SaveValues.scavsKilledThisCycle = default;
						}

						if (SaveValues.scavsKilledThisCycle > 0)
							values.Add(nameof(SaveValues.scavsKilledThisCycle), SaveValues.scavsKilledThisCycle);
						break;

					case var _ when slugname == SlugcatStats.Name.Red.value:
						if (reset)
						{
							SaveValues.HunterOracleID = default;
							SaveValues.fpSeenHunterPearl = default;
						}

						if (!string.IsNullOrEmpty(SaveValues.HunterOracleID))
							values.Add(nameof(SaveValues.HunterOracleID), SaveValues.HunterOracleID);
						if (SaveValues.fpSeenHunterPearl)
							values.Add(nameof(SaveValues.fpSeenHunterPearl), SaveValues.fpSeenHunterPearl);
						break;

					case var _ when slugname == MoreSlugcatsEnums.SlugcatStatsName.Gourmand.value:
						break;

					case var _ when slugname == SlugcatStats.Name.White.value:
						break;

					case var _ when slugname == SlugcatStats.Name.Yellow.value:
						break;

					case var _ when slugname == MoreSlugcatsEnums.SlugcatStatsName.Rivulet.value:
						break;

					case var _ when slugname == MoreSlugcatsEnums.SlugcatStatsName.Saint.value:
						if (reset)
						{
							SaveValues.fpSawAscensionCycle = -1;
							SaveValues.lttmSawAscensionCycle = -1;
							SaveValues.CLSeenMoonPearl = default;
							SaveValues.MoonOverWrotePearl = default;
							SaveValues.SaintWarmthMechanicTutorial = default;
						}

						if (SaveValues.fpSawAscensionCycle != -1)
							values.Add(nameof(SaveValues.fpSawAscensionCycle), SaveValues.fpSawAscensionCycle);
						if (SaveValues.lttmSawAscensionCycle != -1)
							values.Add(nameof(SaveValues.lttmSawAscensionCycle), SaveValues.lttmSawAscensionCycle);
						if (SaveValues.CLSeenMoonPearl)
							values.Add(nameof(SaveValues.CLSeenMoonPearl), SaveValues.CLSeenMoonPearl);
						if (SaveValues.MoonOverWrotePearl)
							values.Add(nameof(SaveValues.MoonOverWrotePearl), SaveValues.MoonOverWrotePearl);
						if (SaveValues.SaintWarmthMechanicTutorial)
							values.Add(nameof(SaveValues.SaintWarmthMechanicTutorial), SaveValues.SaintWarmthMechanicTutorial);
						break;
				}
			}

			return values;
		}

		private static void UpdateDiskSave()
		{
			File.WriteAllText(SaveFileJSON, Json.Serialize(SaveInformation));
		}
	}

	public class SaveValues
	{
		public static string WhoShowedFPThePearl;

		public static bool OEGateOpenedAsSpear;
		public static bool SpearMetSRS;

		internal static int scavsKilledThisCycle = 0;

		public static string HunterOracleID;
		public static bool fpSeenHunterPearl;

		public static int fpSawAscensionCycle = -1;
		public static int lttmSawAscensionCycle = -1;
		public static bool CLSeenMoonPearl;
		public static bool MoonOverWrotePearl;

		public static bool SaintWarmthMechanicTutorial;
	}
}
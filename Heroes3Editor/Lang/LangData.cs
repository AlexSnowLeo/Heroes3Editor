using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Heroes3Editor.Models;

namespace Heroes3Editor.Lang
{
    internal class LangData
    {
        public static LangData Instance { get; set; }
        
        public static string CurrentLang { get; set; }

        public static void SetInstance(string lang)
        {
            CurrentLang = lang.ToLower();
            if (CurrentLang == "en")
            {
                Instance = null;
            }
            else
            {
                var langFolder = Path.Combine(Environment.CurrentDirectory, "Lang");
                Instance = JsonSerializer.Deserialize<LangData>(
                    File.ReadAllText(Path.Combine(langFolder, $"lang-data.{CurrentLang}.json")));
            }
            
            Constants.Skills.SetLang(Instance?.Skills);
            Constants.SkillLevels.SetLang(Instance?.SkillLevels);
            Constants.Artifacts.SetLang(Instance?.CommonArtifacts);
            Constants.Weapons.SetLang(Instance?.Weapons);
            Constants.Shields.SetLang(Instance?.Shields);
            Constants.Helms.SetLang(Instance?.Helms);
            Constants.Armor.SetLang(Instance?.Armor);
            Constants.Cloak.SetLang(Instance?.Cloak);
            Constants.Boots.SetLang(Instance?.Boots);
            Constants.Neck.SetLang(Instance?.Neck);
            Constants.Rings.SetLang(Instance?.Rings);
            Constants.Items.SetLang(Instance?.Items);
            Constants.WarMachines.SetLang(Instance?.WarMachines);
            Constants.Spells.SetLang(Instance?.Spells, Instance?.SpellDescriptions);
            Constants.Creatures.SetLang(Instance?.Creatures);

            Constants.LoadAllArtifacts();
            Constants.ArtifactInfo.LangDescriptions = Instance?.ArtifactDescriptions;
            Constants.ArtifactInfo.HotaLangDescriptions = Instance?.HotaArtifactDescriptions;
            Constants.Towns.SetLang(Instance?.Towns, Instance?.HotaTowns, Instance?.Factions, Instance?.HotaFactions);

            //Constants.Artifacts.AppendLang(Instance.Weapons);
            //Constants.Artifacts.AppendLang(Instance.Shields);
            //Constants.Artifacts.AppendLang(Instance.Creatures);
        }

        public Dictionary<string, string> Heroes { get; set; }
        public Dictionary<string, string> Skills { get; set; }
        public Dictionary<string, string> SkillLevels { get; set; }
        public Dictionary<string, string> CommonArtifacts { get; set; }
        public Dictionary<string, string> Weapons { get; set; }
        public Dictionary<string, string> Shields { get; set; }
        public Dictionary<string, string> Helms { get; set; }
        public Dictionary<string, string> Armor { get; set; }
        public Dictionary<string, string> Cloak { get; set; }
        public Dictionary<string, string> Boots { get; set; }
        public Dictionary<string, string> Neck { get; set; }
        public Dictionary<string, string> Rings { get; set; }
        public Dictionary<string, string> Items { get; set; }
        public Dictionary<string, string> WarMachines { get; set; }
        public Dictionary<string, string> Spells { get; set; }
        public Dictionary<string, string> SpellDescriptions { get; set; }
        public Dictionary<string, string> ArtifactDescriptions { get; set; }
        public Dictionary<string, string> HotaArtifactDescriptions { get; set; }
        public Dictionary<string, string> Creatures { get; set; }
        public Dictionary<string, string> Factions { get; set; }
        public Dictionary<string, string> HotaFactions { get; set; }
        public Dictionary<string, string> Towns { get; set; }
        public Dictionary<string, string> HotaTowns { get; set; }
    }
}

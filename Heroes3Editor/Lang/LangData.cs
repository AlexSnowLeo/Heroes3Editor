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
            if (CurrentLang == lang.ToLower())
                return;

            CurrentLang = lang.ToLower();
            if (CurrentLang == "en")
            {
                Instance = null;
            }
            else
            {
                var langFolder = Path.Combine(Environment.CurrentDirectory, "Lang");
                Instance = JsonSerializer.Deserialize<LangData>(
                    File.ReadAllText(Path.Combine(langFolder, $"lang-data.{CurrentLang}.json")),
                    new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip });
            }
            
            Constants.Skills.SetLang(Instance?.Skills);
            Constants.SkillLevels.SetLang(Instance?.SkillLevels);
            Constants.Artifacts.SetLang(Instance?.CommonArtifacts);
            Constants.Weapons.SetLang(Instance?.Weapons, Instance?.HotaWeapons);
            Constants.Shields.SetLang(Instance?.Shields, Instance?.HotaShields);
            Constants.Helms.SetLang(Instance?.Helms, Instance?.HotaHelms);
            Constants.Armor.SetLang(Instance?.Armor, Instance?.HotaArmor);
            Constants.Cloak.SetLang(Instance?.Cloak, Instance?.HotaCloak);
            Constants.Boots.SetLang(Instance?.Boots, Instance?.HotaBoots);
            Constants.Neck.SetLang(Instance?.Neck, Instance?.HotaNeck);
            Constants.Rings.SetLang(Instance?.Rings, Instance?.HotaRings);
            Constants.Items.SetLang(Instance?.Items, Instance?.HotaItems);
            Constants.WarMachines.SetLang(Instance?.WarMachines);
            Constants.Spells.SetLang(Instance?.Spells, Instance?.SpellDescriptions);
            Constants.Creatures.SetLang(Instance?.Creatures);

            Constants.LoadAllArtifacts();
            Constants.ArtifactInfo.LangDescriptions = Instance?.ArtifactDescriptions;
            Constants.ArtifactInfo.HotaLangDescriptions = Instance?.HotaArtifactDescriptions;
            Constants.Towns.SetLang(Instance?.Towns, Instance?.HotaTowns, Instance?.Factions, Instance?.HotaFactions);

            Models.Heroes.SetLang(Instance?.Heroes, Instance?.HotaHeroes, Instance?.UniqueHeroes);

            //Constants.Artifacts.AppendLang(Instance.Weapons);
            //Constants.Artifacts.AppendLang(Instance.Shields);
            //Constants.Artifacts.AppendLang(Instance.Creatures);
        }

        public Dictionary<string, string> Heroes { get; set; }
        public Dictionary<string, string> HotaHeroes { get; set; } = [];
        public Dictionary<string, string> UniqueHeroes { get; set; } = [];
        public Dictionary<string, string> Skills { get; set; }
        public Dictionary<string, string> SkillLevels { get; set; }
        public Dictionary<string, string> CommonArtifacts { get; set; }
        public Dictionary<string, string> Weapons { get; set; }
        public Dictionary<string, string> HotaWeapons { get; set; }
        public Dictionary<string, string> Shields { get; set; }
        public Dictionary<string, string> HotaShields { get; set; }
        public Dictionary<string, string> Helms { get; set; }
        public Dictionary<string, string> HotaHelms { get; set; }
        public Dictionary<string, string> Armor { get; set; }
        public Dictionary<string, string> HotaArmor { get; set; }
        public Dictionary<string, string> Cloak { get; set; }
        public Dictionary<string, string> HotaCloak { get; set; }
        public Dictionary<string, string> Boots { get; set; }
        public Dictionary<string, string> HotaBoots { get; set; }
        public Dictionary<string, string> Neck { get; set; }
        public Dictionary<string, string> HotaNeck { get; set; }
        public Dictionary<string, string> Rings { get; set; }
        public Dictionary<string, string> HotaRings { get; set; }
        public Dictionary<string, string> Items { get; set; }
        public Dictionary<string, string> HotaItems { get; set; }
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

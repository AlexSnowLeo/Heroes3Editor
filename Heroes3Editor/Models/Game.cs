using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using Heroes3Editor.Lang;
using ICSharpCode.SharpZipLib.GZip;

namespace Heroes3Editor.Models
{
    public class Game
    {
        public bool IsHOTA { get; set; }
        public string Version { get; set; }
        public string Lang { get; set; }
        public byte[] Bytes { get; }
        public string FileName { get; set; }

        public IList<Hero> Heroes { get; } = [];

        public IList<Town> Towns { get; } = [];

        // CGM is supposed to be a GZip file, but GZipStream from .NET library throws a
        // "unsupported compression method" exception, which is why we use SharpZipLib.
        // Also CGM has incorrect CRC which every tool/library complains about.
        public Game(string file, bool bin = false)
        {
            var fileInfo = new FileInfo(file);
            using var fileStream = fileInfo.OpenRead();
            
            using var memoryStream = new MemoryStream();
            if (!bin)
            {
                using var gzipStream = new GZipInputStream(fileStream);
                gzipStream.CopyTo(memoryStream);
            } 
            else
            {
                fileStream.CopyTo(memoryStream);
            }

            Bytes = memoryStream.ToArray();
            var gameVersionMajor = Bytes[8];
            var gameVersionMinor = Bytes[12];
            
            FileName = fileInfo.Name;
            Lang = SearchHero(Models.Heroes.CatherinePL, Bytes.Length) > 0
                ? "pl"
                : SearchHero(Models.Heroes.AdelaRU, Bytes.Length) > 0 ? "ru" : "en";

            if (gameVersionMajor >= 44 && gameVersionMinor >= 5)
            {
                SetHOTA();
            }
            else
            {
                SetClassic();
            }

            Version = $"{gameVersionMajor}.{gameVersionMinor}{(IsHOTA ? " HotA" : "")}";

            Constants.LoadAllArtifacts();

            SearchTowns();
        }

        public void SetHOTA()
        {
            IsHOTA = true;
            Constants.LoadHOTAItems();
            Constants.HeroOffsets["SkillSlots"] = 923;
        }
        public void SetClassic()
        {
            IsHOTA = false;
            Constants.HeroOffsets["SkillSlots"] = 41;
            Constants.RemoveHOTAReferenceCodes();
        }
        public void Save(string file, bool binData = false)
        {
            var result = ValidateData();
            if (result.Length > 0)
            {
                MessageBox.Show("Validation data errors:\n" + string.Join("/n * ", result), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var fileStream = (new FileInfo(file)).OpenWrite();
            using var memoryStream = new MemoryStream(Bytes);
            if (binData)
            {
                memoryStream.CopyTo(fileStream);
                fileStream.Close();
            } else
            {
                using var gzipStream = new GZipOutputStream(fileStream);
                memoryStream.CopyTo(gzipStream);
            }
        }

        public void SaveBinData(string file)
        {
            File.WriteAllBytes(file, Bytes);
        }

        public string[] ValidateData()
        {
            var result = new List<string>();
            foreach (var hero in Heroes)
            {
                if (hero.EquippedArtifacts["Weapon"] == Constants.Artifacts[Constants.TITANS_THUNDER] && !hero.SpellBookExist)
                {
                    result.Add($"{hero.Name}: must have Spell Book with Titan's Thunder");
                }
            }

            return [.. result];
        }

        public bool SearchHero(string name)
        {
            int startPosition = Bytes.Length;
            foreach (var hero in Heroes)
            {
                if (hero.Name == name && startPosition > hero.BytePosition)
                    startPosition = hero.BytePosition - 1;
            }

            var bytePosition = SearchHero(name, startPosition);
            if (bytePosition > 0)
            {
                Heroes.Add(new Hero(name, this, bytePosition));
                return true;
            }
            else
                return false;
        }

        public int SearchHero(string name, int startPosition)
        {
            byte[] pattern = new byte[13];
            Encoding.ASCII.GetBytes(name).CopyTo(pattern, 0);
            if (Regex.IsMatch(name, @"\p{IsCyrillic}"))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding win1251 = Encoding.GetEncoding("windows-1251");
                pattern = win1251.GetBytes(name);
            }

            for (int i = startPosition - pattern.Length; i > 0; --i)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (Bytes[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SearchTowns()
        {
            Towns.Clear();

            var position = Bytes.Length;
            foreach (var faction in Models.Towns.Factions)
            {
                foreach (var town in Constants.Towns[faction])
                {
                    SearchTown(town, faction, position);
                }
            }
        }

        public (Town town, bool added) SearchTown(string town, string faction, int position)
        {
            var encoding = Encoding.ASCII;
            if (Regex.IsMatch(town, @"\p{IsCyrillic}"))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = Encoding.GetEncoding("windows-1251");
            }

            var factionLang = Constants.Towns.GetLangFactionValue(faction);
            byte[] pattern = encoding.GetBytes(town);

            for (int i = position - pattern.Length; i > 0; --i)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (Bytes[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    var existTown = Towns.FirstOrDefault(x => x.Position == i);
                    if (existTown != null)
                        return (existTown, false);

                    var prev = Convert.ToChar(Bytes[i - 1]);
                    var next = Convert.ToChar(Bytes[i + pattern.Length]);
                    if (Char.IsLetter(prev) || Char.IsLetter(next) || prev == ' ' || next == ' ')
                        continue;

                    var newTown = new Town
                    {
                        Faction = faction,
                        FactionLang = factionLang,
                        Name = town,
                        Position = i,
                    };

                    Towns.Add(newTown);

                    return (newTown, true);
                }
            }

            return (null, false);
        }
    }

    public class Hero
    {
        public string Name { get; }

        private Game _game;

        public bool IsHOTAGame => _game.IsHOTA;
        public int BytePosition { get; }

        public sbyte[] Attributes { get; } = new sbyte[4];
        public int NumOfSkills { get; private set; }
        public string[] Skills { get; } = new string[8];
        public byte[] SkillLevels { get; } = new byte[8];

        public ISet<string> Spells { get; } = new HashSet<string>();
        public bool SpellBookExist { get; private set; }

        public string[] Creatures { get; } = new string[7];
        public int[] CreatureAmounts { get; } = new int[7];

        public ISet<string> WarMachines { get; } = new HashSet<string>();
        public string[] ArtifactInfo { get; } = new string[1];

        public IDictionary<string, string> EquippedArtifacts = new Dictionary<string, string>()
        {
            {"Helm", "-"},
            {"Neck", "-"},
            {"Armor", "-"},
            {"Cloak", "-"},
            {"Boots", "-"},
            {"Weapon", "-"},
            {"Shield", "-"},
            {"LeftRing", "-"},
            {"RightRing", "-"},
            {"Item1", "-"},
            {"Item2", "-"},
            {"Item3", "-"},
            {"Item4", "-"},
            {"Item5", "-"}
        };
        
        public IDictionary<string, string> EquippedSpellScrolls = new Dictionary<string, string>()
        {
            {"Item1", ""},
            {"Item2", ""},
            {"Item3", ""},
            {"Item4", ""},
            {"Item5", ""}
        };

        public readonly IDictionary<byte, string> InventorySpellScrolls = new Dictionary<byte, string>();
        
        public List<string> Inventory { get; } = [];

        private const byte ON = 0x00;
        private const byte OFF = 0xFF;

        public Hero(string name, Game game, int bytePosition)
        {
            Name = name;
            _game = game;
            BytePosition = bytePosition;

            for (int i = 0; i < 4; ++i)
            {
                var attr = _game.Bytes[BytePosition + Constants.HeroOffsets["Attributes"] + i];
                Attributes[i] = (sbyte)attr;
            }

            NumOfSkills = _game.Bytes[BytePosition + Constants.HeroOffsets["NumOfSkills"]];
            for (byte i = 0; i < Constants.Skills.Names.Length; ++i)
            {
                var skillSlotIndex = _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + i];
                if (skillSlotIndex != 0)
                {
                    Skills[skillSlotIndex - 1] = Constants.Skills[i];
                    SkillLevels[skillSlotIndex - 1] = _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + i];
                }
            }

            SpellBookExist = _game.Bytes[BytePosition + Constants.HeroOffsets["SpellBookSlot"]] == ON;

            for (byte i = 0; i < 70; ++i)
            {
                if (_game.Bytes[BytePosition + Constants.HeroOffsets["Spells"] + i] == 1)
                    Spells.Add(Constants.Spells[i]);
            }

            for (int i = 0; i < 7; ++i)
            {
                var code = _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + i * 4];
                if (code != OFF)
                {
                    Creatures[i] = Constants.Creatures[code];
                    var amountBytes = _game.Bytes.AsSpan().Slice(BytePosition + Constants.HeroOffsets["CreatureAmounts"] + i * 4, 4);
                    CreatureAmounts[i] = BinaryPrimitives.ReadInt16LittleEndian(amountBytes);
                }
                else
                {
                    CreatureAmounts[i] = 0;
                }
            }

            foreach (var warMachine in Constants.WarMachines.OriginalNames)
            {
                if (_game.Bytes[BytePosition + Constants.HeroOffsets[warMachine]] == Constants.WarMachines[warMachine])
                    WarMachines.Add(warMachine);
            }

            var gears = new List<string>(EquippedArtifacts.Keys);
            foreach (var gear in gears)
            {
                var gearPos = BytePosition + Constants.HeroOffsets[gear];
                var code = _game.Bytes[gearPos];
                if (code == OFF) continue;

                EquippedArtifacts[gear] = Constants.Artifacts[code];
                if (gear.StartsWith("Item") && code == Constants.SPELL_SCROLL)
                {
                    var spellCode = _game.Bytes[gearPos + 4];
                    EquippedSpellScrolls[gear] = Constants.Spells[spellCode];
                }
            }

            var inventoryPos = BytePosition + Constants.HeroOffsets["Inventory"];
            for (byte i = 0; i < 64; i++)
            {
                var code = _game.Bytes[inventoryPos + i*8];
                if (code == OFF) continue;

                Inventory.Add(Constants.Artifacts[code]);

                if (code == Constants.SPELL_SCROLL)
                {
                    var spellCode = _game.Bytes[inventoryPos + i * 8 + 4];
                    InventorySpellScrolls.TryAdd(i, Constants.Spells[spellCode]);
                }
            }
        }

        public void UpdateAttribute(int i, sbyte value)
        {
            Attributes[i] = value;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Attributes"] + i] = (byte)value;
        }

        public void UpdateSkill(int slot, string skill)
        {
            if (skill == null) // Empty Slot
            {
                var oldSkill = Skills[slot];
                Skills[slot] = null;
                SkillLevels[slot] = 0;

                _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[oldSkill]] = 0;
                _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + Constants.Skills[oldSkill]] = 0;
                _game.Bytes[BytePosition + Constants.HeroOffsets["NumOfSkills"]] = (byte)--NumOfSkills;

                return;
            }

            if (slot < 0 || slot > NumOfSkills) return;
            for (int i = 0; i < NumOfSkills; ++i)
                if (Skills[i] == skill) return;

            byte skillLevel = 1;

            if (slot < NumOfSkills)
            {
                var oldSkill = Skills[slot];
                var oldSkillLevelPosition = BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[oldSkill];
                skillLevel = _game.Bytes[oldSkillLevelPosition];
                _game.Bytes[oldSkillLevelPosition] = 0;
                _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + Constants.Skills[oldSkill]] = 0;
            }

            Skills[slot] = skill;
            SkillLevels[slot] = skillLevel;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[skill]] = skillLevel;
            _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + Constants.Skills[skill]] = (byte)(slot + 1);

            if (slot == NumOfSkills)
            {
                ++NumOfSkills;
                _game.Bytes[BytePosition + Constants.HeroOffsets["NumOfSkills"]] = (byte)NumOfSkills;
            }
        }

        public void UpdateSkillLevel(int slot, byte level)
        {
            if (slot < 0 || slot > NumOfSkills || level < 1 || level > 3) return;

            SkillLevels[slot] = level;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[Skills[slot]]] = level;
        }

        public void AddSpell(string spell)
        {
            if (!Spells.Add(spell)) return;

            int spellPosition = BytePosition + Constants.HeroOffsets["Spells"] + Constants.Spells[spell];
            _game.Bytes[spellPosition] = 1;

            int spellBookPosition = BytePosition + Constants.HeroOffsets["SpellBook"] + Constants.Spells[spell];
            _game.Bytes[spellBookPosition] = 1;
        }

        public void RemoveSpell(string spell)
        {
            if (!Spells.Remove(spell)) return;

            int spellPosition = BytePosition + Constants.HeroOffsets["Spells"] + Constants.Spells[spell];
            _game.Bytes[spellPosition] = 0;

            int spellBookPosition = BytePosition + Constants.HeroOffsets["SpellBook"] + Constants.Spells[spell];
            _game.Bytes[spellBookPosition] = 0;
        }

        public void ToggleSpellBook(bool enable)
        {
            int spellBookSlotPosition = BytePosition + Constants.HeroOffsets["SpellBookSlot"];
            byte onOff = enable ? ON : OFF;
            _game.Bytes[spellBookSlotPosition] = onOff;
            _game.Bytes[spellBookSlotPosition + 1] = onOff;
            _game.Bytes[spellBookSlotPosition + 2] = onOff;
            _game.Bytes[spellBookSlotPosition + 3] = onOff;
            SpellBookExist = enable;
        }

        public void UpdateCreature(int i, string creature)
        {
            if (creature == null)
            {
                CreatureAmounts[i] = 0;
                UpdateCreatureAmount(i, 0);

                Creatures[i] = null;
                _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + i * 4] = OFF;
                _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 1] = OFF;
                _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 2] = OFF;
                _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 3] = OFF;

                return;
            }

            if (Creatures[i] == null)
            {
                CreatureAmounts[i] = 1;
                UpdateCreatureAmount(i, 1);
            }

            Creatures[i] = creature;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + i * 4] = Constants.Creatures[creature];
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 1] = ON;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 2] = ON;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 3] = ON;
        }

        public void UpdateCreatureAmount(int i, int amount)
        {
            var amountBytes = _game.Bytes.AsSpan().Slice(BytePosition + Constants.HeroOffsets["CreatureAmounts"] + i * 4, 4);
            BinaryPrimitives.WriteInt32LittleEndian(amountBytes, amount);
        }

        public void AddWarMachine(string warMachine)
        {
            if (!WarMachines.Add(warMachine)) return;

            int position = BytePosition + Constants.HeroOffsets[warMachine];
            _game.Bytes[position] = Constants.WarMachines[warMachine];
            _game.Bytes[position + 1] = ON;
            _game.Bytes[position + 2] = ON;
            _game.Bytes[position + 3] = ON;
        }

        public void RemoveWarMachine(string warMachine)
        {
            if (!WarMachines.Remove(warMachine)) return;

            int currentBytePos = BytePosition + Constants.HeroOffsets[warMachine];
            _game.Bytes[currentBytePos] = OFF;
            _game.Bytes[currentBytePos + 1] = OFF;
            _game.Bytes[currentBytePos + 2] = OFF;
            _game.Bytes[currentBytePos + 3] = OFF;
        }

        public void UpdateEquippedArtifact(string gear, string artifact, string spell)
        {
            var currentBytePos = BytePosition + Constants.HeroOffsets[gear];
            
            if (artifact is not "-")
            {
                EquippedArtifacts[gear] = artifact;
                
                var artKey = Constants.Artifacts[artifact];
                _game.Bytes[currentBytePos] = artKey;
                _game.Bytes[currentBytePos + 1] = ON;
                _game.Bytes[currentBytePos + 2] = ON;
                _game.Bytes[currentBytePos + 3] = ON;
                
                if (artKey == Constants.SPELL_SCROLL)
                {
                    if (string.IsNullOrEmpty(spell))
                        throw new ArgumentNullException(nameof(spell));
                        
                    _game.Bytes[currentBytePos + 4] = Constants.Spells[spell];
                    _game.Bytes[currentBytePos + 5] = ON;
                    _game.Bytes[currentBytePos + 6] = ON;
                    _game.Bytes[currentBytePos + 7] = ON;
                    
                    EquippedSpellScrolls[gear] = spell;
                }
            }
            else
            {
                EquippedArtifacts[gear] = "-";
                
                _game.Bytes[currentBytePos] = OFF;
                _game.Bytes[currentBytePos + 1] = OFF;
                _game.Bytes[currentBytePos + 2] = OFF;
                _game.Bytes[currentBytePos + 3] = OFF;

                if (gear.StartsWith("Item"))
                {
                    EquippedSpellScrolls[gear] = "";
                    _game.Bytes[currentBytePos + 4] = OFF;
                    _game.Bytes[currentBytePos + 5] = OFF;
                    _game.Bytes[currentBytePos + 6] = OFF;
                    _game.Bytes[currentBytePos + 7] = OFF;
                }
            }
        }
        
        public void UpdateInventory(string newArtifact = null, string spell = null, int? index = null)
        {
            if (newArtifact != null)
            {
                Inventory.Add(newArtifact);
                var artKey = Constants.Artifacts[newArtifact];
                if (artKey == Constants.SPELL_SCROLL)
                {
                    if (string.IsNullOrEmpty(spell))
                        throw new ArgumentNullException(nameof(spell));
                    
                    InventorySpellScrolls.TryAdd((byte)(Inventory.Count - 1), spell);
                }
            }
            
            for (byte i = 0; i < 64; i++)
            {
                var currentBytePos = BytePosition + Constants.HeroOffsets["Inventory"] + i * 8;
                
                if (Inventory.Count > i)
                {
                    var artifact = Inventory[i];
                    var artKey = Constants.Artifacts[artifact];
                    _game.Bytes[currentBytePos] = artKey;
                    _game.Bytes[currentBytePos + 1] = ON;
                    _game.Bytes[currentBytePos + 2] = ON;
                    _game.Bytes[currentBytePos + 3] = ON;
                    _game.Bytes[currentBytePos + 4] = ON;
                    _game.Bytes[currentBytePos + 5] = ON;
                    _game.Bytes[currentBytePos + 6] = ON;
                    _game.Bytes[currentBytePos + 7] = ON;
                
                    if (artKey == Constants.SPELL_SCROLL)
                    {
                        spell = InventorySpellScrolls[i];
                        _game.Bytes[currentBytePos + 4] = Constants.Spells[spell];
                    }
                    else
                    {
                        if (InventorySpellScrolls.ContainsKey(i))
                            InventorySpellScrolls.Remove(i);
                    }
                }
                else
                {
                    _game.Bytes[currentBytePos] = OFF;
                    _game.Bytes[currentBytePos + 1] = OFF;
                    _game.Bytes[currentBytePos + 2] = OFF;
                    _game.Bytes[currentBytePos + 3] = OFF;
                    _game.Bytes[currentBytePos + 4] = ON;
                    _game.Bytes[currentBytePos + 5] = ON;
                    _game.Bytes[currentBytePos + 6] = ON;
                    _game.Bytes[currentBytePos + 7] = ON;
                    
                    if (InventorySpellScrolls.ContainsKey(i))
                        InventorySpellScrolls.Remove(i);
                }
            }
        }

        public void RemoveFromInventory(string artifact, byte index)
        {
            Inventory.RemoveAt(index);
            
            if (InventorySpellScrolls.ContainsKey(index))
                InventorySpellScrolls.Remove(index);

            var spells = InventorySpellScrolls.ToArray();
            foreach (var (key, spell) in spells)
            {
                if (key > index)
                {
                    InventorySpellScrolls.Remove(key);
                    InventorySpellScrolls.Add((byte)(key - 1), spell);
                }
            }
        }

        //  NAME|ATTACK|DEFENSE|POWER|KNOWLEDGE|MORALE|LUCK|OTHER|SLOTS
        //   0  |   1  |   2   |  3  |    4    |   5  |  6 |  7  |  8
        // SLOTS - First letters of slots witch blocked: Helm, Neck, Armor, Cloak, Boots, Weapon, Shield, Left/Right Ring, for Items it's number of slots
        public string[] UpdateArtifactInfo(string artifact, string slotName, byte? inventory = null)
        {
            if (!string.IsNullOrEmpty(artifact) && !"-".Equals(artifact))
            {
                var infoKey = Constants.Artifacts[artifact];
                if (infoKey == Constants.SPELL_SCROLL)
                {
                    var spell = inventory.HasValue
                        ? Constants.Spells.GetLangValue(InventorySpellScrolls[inventory.Value])
                        : Constants.Spells.GetLangValue(EquippedSpellScrolls[slotName]);
                    
                    return [artifact, "", "", "", "", "", "", $"Add spell \'{spell}'" ];
                }

                var artInfo = Constants.ArtifactInfo[infoKey].Split("|");
                if (Constants.ArtifactInfo.LangDescriptions != null &&
                    Constants.ArtifactInfo.LangDescriptions.ContainsKey(artInfo[0]))
                    artInfo[7] = Constants.ArtifactInfo.LangDescriptions[artInfo[0]];

                return artInfo;
            }
            return null;
        }
    }

    public class Town
    {
        public string Name { get; set; }
        public string Faction { get; set; }
        public string FactionLang { get; set; }
        public int Position { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}

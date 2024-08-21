using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Heroes3Editor.Lang;
using Heroes3Editor.Models;

namespace Heroes3Editor
{
    /// <summary>
    /// Interaction logic for HeroPanel.xaml
    /// </summary>
    public partial class HeroPanel : UserControl
    {
        private Hero _hero;

        private bool _initializing;

        private Window MainWindow => Window.GetWindow(this);

        public Hero Hero
        {
            set
            {
                _initializing = true;
                _hero = value;
                if (_hero.IsHOTAGame)
                {
                    SetHOTASettings();
                }
                else
                {
                    SetClassicSettings();
                }
                for (int i = 0; i < 4; ++i)
                {
                    var txtBox = FindName("Attribute" + i) as TextBox;
                    var attr = _hero.Attributes[i] < 0 ? "0" : _hero.Attributes[i].ToString();
                    txtBox.Text = attr;
                }

                for (int i = 0; i < 8; ++i)
                {
                    var cboBoxSkill = FindName("Skill" + i) as ComboBox;
                    var cboBoxSkillLevel = FindName("SkillLevel" + i) as ComboBox;
                    if (i < _hero.NumOfSkills)
                    {
                        cboBoxSkill.SelectedItem = _hero.Skills[i];
                        cboBoxSkillLevel.SelectedIndex = _hero.SkillLevels[i] - 1;
                    }
                    else if (i > _hero.NumOfSkills)
                    {
                        cboBoxSkill.IsEnabled = false;
                        cboBoxSkillLevel.IsEnabled = false;
                    }
                    else
                    {
                        cboBoxSkillLevel.IsEnabled = false;
                    }
                }

                UpdateSkillNames();

                SpellBook.IsChecked = _hero.SpellBookExist;

                foreach (var spell in _hero.Spells)
                {
                    var chkBox = FindName(Constants.Spells.GetOriginalValue(spell).ToControlName()) as CheckBox;
                    chkBox.IsChecked = true;
                }

                for (int i = 0; i < 7; ++i)
                {
                    var cboBox = FindName("Creature" + i) as ComboBox;
                    var txtBox = FindName("CreatureAmount" + i) as TextBox;
                    if (_hero.Creatures[i] != null)
                    {
                        cboBox.SelectedItem = _hero.Creatures[i];
                        txtBox.Text = _hero.CreatureAmounts[i].ToString();
                    }
                    else
                    {
                        txtBox.IsEnabled = false;
                    }
                }

                foreach (var warMachine in _hero.WarMachines)
                {
                    var toggleComponent = FindName(warMachine.ToControlName()) as ToggleButton;
                    toggleComponent.IsChecked = true;
                }

                var gears = new List<string>(_hero.EquippedArtifacts.Keys);
                foreach (var gear in gears)
                {
                    // Attach an EA_ prefix to gear because there's already
                    // a CheckBox for the spell Shield
                    if (FindName("EA_" + gear) is not ComboBox cboBox)
                        continue;
                    
                    var artifact = _hero.EquippedArtifacts[gear];
                    cboBox.SelectedItem = artifact;
                }

                foreach (var gear in gears)
                {
                    var artifact = _hero.EquippedArtifacts[gear];
                    if (artifact is "-")
                        continue;
                    
                    var artInfo = _hero.UpdateArtifactInfo(artifact, gear);
                    if (artInfo?.Length == 9 && !string.IsNullOrEmpty(artInfo[8]))
                        UpdateSlotsEnable(gear, artInfo[8], false);
                }

                foreach (var art in _hero.Inventory)
                {
                    ListBoxInventory.Items.Add(art);
                }
                
                UpdateInventoryHeader();

                _initializing = false;
            }
        }

        public HeroPanel()
        {
            InitializeComponent();

            foreach (var spell in Constants.Spells.OriginalNames)
            {
                if (FindName(spell.ToControlName()) is not CheckBox spellCheckBox)
                    continue;

                spellCheckBox.Content = Constants.Spells.GetLangValue(spell);

                if (LangData.CurrentLang == "en")
                    continue;

                if (Constants.Spells.LangDescriptions != null &&
                    Constants.Spells.LangDescriptions.TryGetValue(spell, out var description))
                    spellCheckBox.ToolTip = description;
            }

            foreach (var warMachine in Constants.WarMachines.OriginalNames)
            {
                var toggleComponent = FindName(warMachine.ToControlName()) as ToggleButton;
                toggleComponent.Content = Constants.WarMachines.GetLangValue(warMachine);
            }

            BallistaRadio.Content = Constants.WarMachines[WarMachines.BALLISTA];

            for (int i = 0; i < 8; ++i)
            {
                var cboBoxSkill = FindName("Skill" + i) as ComboBox;
                foreach (var skill in Constants.Skills.Names)
                {
                    cboBoxSkill.Items.Add(skill);
                }
            }
        }

        private void SetHOTASettings()
        {
            Ballista.Visibility = Visibility.Collapsed;
            BallistaRadio.Visibility = Visibility.Visible;
            Canon.Visibility = Visibility.Visible;
        }
        private void SetClassicSettings()
        {
            Ballista.Visibility = Visibility.Visible;
            BallistaRadio.Visibility = Visibility.Collapsed;
            Canon.Visibility = Visibility.Collapsed;
        }
        private void SetComponentVisibility(string name, Visibility visibility)
        {
            if (FindName(name) is ButtonBase component)
            {
                component.Visibility = visibility;
            }
        }
        private void UpdateAttribute(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            
            if (e.Source is not TextBox txtBox)
                return;

            // TODO sbyte
            bool isNumber = byte.TryParse(txtBox.Text, out var value);
            if (!isNumber || value > 99)
            {
                if (value > 99)
                    txtBox.Text = "99";

                return;
            }

            var i = int.Parse(txtBox.Name["Attribute".Length..]);
            _hero.UpdateAttribute(i, Convert.ToSByte(value));
        }

        private void UpdateSkill(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            
            var cboBox = e.Source as ComboBox;
            var slot = int.Parse(cboBox.Name.Substring("Skill".Length));
            var skill = cboBox.SelectedItem as string;

            var oldNumOfSkills = _hero.NumOfSkills;
            _hero.UpdateSkill(slot, skill);

            if (_hero.NumOfSkills > oldNumOfSkills)
            {
                var comboBoxSkillLevel = FindName("SkillLevel" + slot) as ComboBox;
                comboBoxSkillLevel.SelectedIndex = _hero.SkillLevels[slot] - 1;
                comboBoxSkillLevel.IsEnabled = true;

                if (_hero.NumOfSkills < 8)
                {
                    var nextCboBox = FindName("Skill" + _hero.NumOfSkills) as ComboBox;
                    nextCboBox.IsEnabled = true;
                }
            }

            _initializing = true;
            UpdateSkillNames();
            _initializing = false;
        }

        private void UpdateSkillNames()
        {
            var skillNames = Constants.Skills.Names;
            for (var i = 0; i < 8; i++)
            {
                var clearSkill = FindName("ClearSkill" + i) as Button;
                clearSkill.IsEnabled = i == _hero.NumOfSkills - 1;

                var cboBoxSkill = FindName("Skill" + i) as ComboBox;
                cboBoxSkill.Items.Clear();
                foreach (var s in skillNames.Except(_hero.Skills.Except([_hero.Skills[i]])))
                {
                    cboBoxSkill.Items.Add(s);
                }

                cboBoxSkill.SelectedItem = _hero.Skills[i];
            }
        }

        private void ClearSkill(object sender, RoutedEventArgs e)
        {
            var button = e.Source as Button;
            var slot = byte.Parse(button.Name["ClearSkill".Length..]);

            _hero.UpdateSkill(slot, null);

            _initializing = true;
            var comboBoxSkillLevel = FindName("SkillLevel" + slot) as ComboBox;
            comboBoxSkillLevel.SelectedItem = null;
            comboBoxSkillLevel.IsEnabled = false;

            var comboBoxSkill = FindName("Skill" + slot) as ComboBox;
            comboBoxSkill.SelectedItem = null;
            _initializing = false;

            if (_hero.NumOfSkills < 7)
            {
                var nextCboBox = FindName("Skill" + (_hero.NumOfSkills + 1)) as ComboBox;
                nextCboBox.IsEnabled = false;
            }

            _initializing = true;
            UpdateSkillNames();
            _initializing = false;
        }

        private void UpdateSkillLevel(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            var comboBox = e.Source as ComboBox;
            var slot = int.Parse(comboBox.Name["SkillLevel".Length..]);

            _hero.UpdateSkillLevel(slot, (byte) (comboBox.SelectedIndex + 1));
        }

        private void AddSpell(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            
            var chkBox = e.Source as CheckBox;
            var spell = chkBox.Name.FromControlName();
            _hero.AddSpell(Constants.Spells.GetLangValue(spell));

            if (SpellBook.IsChecked != true)
                SpellBook.IsChecked = true;
        }

        private void RemoveSpell(object sender, RoutedEventArgs e)
        {
            var chkBox = e.Source as CheckBox;
            var spell = chkBox.Name.FromControlName();
            _hero.RemoveSpell(Constants.Spells.GetLangValue(spell));
        }

        private void UpdateCreature(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;

            var cboBox = e.Source as ComboBox;
            var i = int.Parse(cboBox.Name["Creature".Length..]);
            var creature = cboBox.SelectedItem as string;

            _hero.UpdateCreature(i, creature);
            var txtBox = FindName("CreatureAmount" + i) as TextBox;
            if (!txtBox.IsEnabled)
            {
                txtBox.Text = _hero.CreatureAmounts[i].ToString();
                txtBox.IsEnabled = true;
            }
        }

        private void UpdateCreatureAmount(object sender, RoutedEventArgs e)
        {
            var txtBox = e.Source as TextBox;
            var i = int.Parse(txtBox.Name["CreatureAmount".Length..]);

            int amount;
            bool isNumber = int.TryParse(txtBox.Text, out amount);
            if (!isNumber || amount < 0 || amount > 9999)
            {
                if (amount < 0)
                    txtBox.Text = "0";

                if (amount > 9999)
                    txtBox.Text = "9999";

                return;
            }

            _hero.UpdateCreatureAmount(i, amount);
        }

        private void ClearCreature(object sender, RoutedEventArgs e)
        {
            var button = e.Source as Button;
            var slot = int.Parse(button.Name["ClearCreature".Length..]);

            _initializing = true;
            var txtBox = FindName("CreatureAmount" + slot) as TextBox;
            txtBox.Text = null;
            txtBox.IsEnabled = false;
            var comboBox = FindName("Creature" + slot) as ComboBox;
            comboBox.SelectedItem = null;
            button.IsEnabled = true;
            _initializing = false;

            _hero.UpdateCreature(slot, null);
        }

        private void AddWarMachine(object sender, RoutedEventArgs e)
        {
            var component = e.Source as ButtonBase;
            if (component == null)
            {
                return;
            }
            _hero.AddWarMachine(component.Tag.ToString());
        }

        private void RemoveWarMachine(object sender, RoutedEventArgs e)
        {
            var component = e.Source as ButtonBase;
            _hero.RemoveWarMachine(component.Tag.ToString());
        }

        private void UpdateEquippedArtifact(object sender, RoutedEventArgs e)
        {
            if (_initializing)
                return;
            
            if (e.Source is not ComboBox cboBox) 
                return;
            
            var gear = cboBox.Name["EA_".Length..];
            var artifact = cboBox.SelectedItem as string;
            
            var prevArt = _hero.EquippedArtifacts[gear];
            if (prevArt is not ("-" or ""))
            {
                var prevArtInfo = _hero.UpdateArtifactInfo(prevArt, gear);
                UpdatePrimarySkills(prevArtInfo, false);
                if (prevArtInfo?.Length == 9 && !string.IsNullOrEmpty(prevArtInfo[8]))
                    UpdateSlotsEnable(gear, prevArtInfo[8], true);
            }

            string slotNotAvailable = null;
            var artInfo = _hero.UpdateArtifactInfo(artifact, gear);
            if (artInfo?.Length == 9 && !string.IsNullOrEmpty(artInfo[8]))
                slotNotAvailable = UpdateSlotsEnable(gear, artInfo[8], false);

            if (slotNotAvailable != null)
            {
                MessageBox.Show(
                    $"{slotNotAvailable}'",
                    "Slot(s) not available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);

                cboBox.SelectedItem = prevArt;
                if (prevArt != "-")
                {
                    var prevArtInfo = _hero.UpdateArtifactInfo(prevArt, gear);
                    UpdatePrimarySkills(prevArtInfo, true);
                    if (prevArtInfo?.Length == 9 && !string.IsNullOrEmpty(prevArtInfo[8]))
                        UpdateSlotsEnable(gear, prevArtInfo[8], false);
                } else
                {
                    UpdatePrimarySkills(artInfo, false);
                }
                
                return;
            }
            
            UpdatePrimarySkills(artInfo, true);

            var artKey = Constants.Artifacts[artifact];
            string spell = "";
            if (artKey == Constants.SPELL_SCROLL)
            {
                spell = _hero.EquippedSpellScrolls[gear];

                if (spell == "")
                {
                    var selectSpell = new SelectSpell { Owner = MainWindow };
                    selectSpell.ShowDialog();
                    if (string.IsNullOrEmpty(selectSpell.SelectedSpell))
                    {
                        cboBox.SelectedItem = null;
                        return;
                    }

                    spell = selectSpell.SelectedSpell;
                }
            }

            // 0x87, "Titan's Thunder" automatically add Spell Book
            if (artKey == 0x87)
            {
                if (SpellBook.IsChecked != true)
                    SpellBook.IsChecked = true;
            }
            
            _hero.UpdateEquippedArtifact(gear, artifact, spell);
        }

        private void UpdatePrimarySkills(string[] artInfo, bool increase)
        {
            if (artInfo == null || artInfo.Length == 0)
                return;

            for (int i = 0; i < 4; i++)
            {
                var updateValue = artInfo[i + 1] != ""
                    ? sbyte.Parse(artInfo[i + 1])
                    : (sbyte)0;
                
                if (updateValue == 0)
                    continue;

                if (!increase)
                    updateValue *= -1;

                if (FindName("Attribute" + i) is not TextBox attrControl)
                    continue;

                var value = (sbyte) (_hero.Attributes[i] + updateValue);
                
                _initializing = true;
                attrControl.Text = (value < 0 ? 0 : value).ToString();
                _initializing = false;

                _hero.UpdateAttribute(i, value);
            }
        }

        /// <summary>
        /// slots - First letters of slots which blocked:
        /// H: Helm, N: Neck, A: Armor, C: Cloak, B: Boots, W: Weapon, S: Shield, L,R,r: Left/Right Ring, r - any Ring, 1-4: for Items it's number of slots
        /// </summary>
        private string UpdateSlotsEnable(string gear, string slots, bool enable)
        {
            var affectedSlots = new List<string>();
            var affectedControls = new List<Control>();
            for (byte i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var slotControlName = slot switch 
                {
                    'H' => "EA_Helm",
                    'N' => "EA_Neck",
                    'A' => "EA_Armor",
                    'C' => "EA_Cloak",
                    'B' => "EA_Boots",
                    'W' => "EA_Weapon",
                    'S' => "EA_Shield",
                    'L' => "EA_LeftRing",
                    'R' => "EA_RightRing",
                    'r' => "EA_Ring", // any of Rings
                    '1' => "EA_Item", // count of Items
                    '2' => "EA_Item",
                    '3' => "EA_Item",
                    '4' => "EA_Item",
                    _ => throw new ArgumentOutOfRangeException(nameof(slots))
                };

                if (slotControlName == "EA_Item")
                {
                    var items = byte.Parse(slot.ToString());
                    if (!_initializing && !enable)
                    {
                        var availableItems = 0;
                        for (int j = 1; j <= 5; j++)
                        {
                            var itemControl = (ComboBox)FindName(slotControlName + j)!;
                            if (itemControl!.IsEnabled == false || (string)itemControl.SelectedItem != "-")
                                continue;

                            availableItems++;
                        }

                        if (items > availableItems)
                            return $"{slot} Item slots are not available";
                    }

                    for (int j = 5; j >= 1; j--)
                    {
                        var itemControl = (ComboBox)FindName(slotControlName + j)!;
                        if ("Item"+j == gear)
                            continue;
                        if ((string)itemControl.SelectedItem != "-")
                            continue;
                        if (!_initializing && !enable && itemControl!.IsEnabled == false)
                            continue;
                        
                        if (!_initializing && !enable)
                            itemControl.SelectedItem = "-";

                        //itemControl.IsEnabled = enable;
                        affectedControls.Add(itemControl);
                        items--;
                        
                        if (items == 0)
                            break;
                    }
                    
                    continue;
                }

                ComboBox slotControl;
                if (slotControlName == "EA_Ring")
                {
                    var rightRing = slotControl = EA_RightRing;
                    if (!_initializing && enable && rightRing.IsEnabled)
                        slotControl = EA_LeftRing;
                    
                    if (!_initializing && !enable && (string)rightRing.SelectedItem != "-")
                    {
                        var leftRing = slotControl = EA_LeftRing;
                        if (!_initializing && (string)leftRing.SelectedItem != "-")
                            return "Ring slots not available";
                    }
                }
                else
                {
                    slotControl = (ComboBox)FindName(slotControlName);
                }
                
                if (slotControl == null)
                    continue;
                
                if (!_initializing && !enable && (slotControl.IsEnabled == false ||  (string)slotControl.SelectedItem != "-"))
                    return $"Slot '{slotControlName["EA_".Length..]}' not available";
                
                /*if (!_initializing && !enable)
                    slotControl.SelectedItem = "-";*/
                        
                //slotControl.IsEnabled = enable;
                affectedControls.Add(slotControl);
            }

            foreach (var control in affectedControls)
            {
                control.IsEnabled = enable;
            }

            return null;
        }

        private void UpdateArtifactInfo(object sender, RoutedEventArgs e)
        {
            var cboBox = e.Source as ComboBox;
            var artifact = cboBox.SelectedItem as string;

            var artInfo = _hero.UpdateArtifactInfo(artifact, cboBox.Name.Replace("EA_",""));

            if (null != artInfo)
            {
                Attack.Text = artInfo[1];
                Defense.Text = artInfo[2];
                Power.Text = artInfo[3];
                Knowledge.Text = artInfo[4];
                Morale.Text = artInfo[5];
                Luck.Text = artInfo[6];
                Effects.Text = artInfo[7];
            }
        }

        private void ClearArtifactInfo(object sender, RoutedEventArgs e)
        {
            Attack.Text = "";
            Defense.Text = "";
            Power.Text = "";
            Knowledge.Text = "";
            Morale.Text = "";
            Luck.Text = "";
            Effects.Text = "";
        }

        private void AddInventoryItem(object sender, RoutedEventArgs e)
        {
            var selectArtifact = new SelectArtifact { Owner = MainWindow };
            selectArtifact.ShowDialog();

            if (selectArtifact.SelectedArtifact is not (null or "-"))
            {
                ListBoxInventory.Items.Add(selectArtifact.SelectedArtifact);
                _hero.UpdateInventory(selectArtifact.SelectedArtifact, selectArtifact.SelectedSpell);

                UpdateInventoryHeader();

                if (_hero.Inventory.Count == 64)
                    ((Button)e.Source).IsEnabled = false;
            }
        }

        private void RemoveInventoryItem(object sender, RoutedEventArgs e)
        {
            if (ListBoxInventory.Items.Count == 0) return;

            var art = (string)ListBoxInventory.SelectedValue;
            var selectedIndex = (byte)ListBoxInventory.SelectedIndex;

            if (art == null) return;
            
            ListBoxInventory.Items.RemoveAt(selectedIndex);
            _hero.RemoveFromInventory(art, selectedIndex);
            _hero.UpdateInventory();

            UpdateInventoryHeader();

            if (_hero.Inventory.Count < 64)
                ((Button)e.Source).IsEnabled = true;
        }

        private void RemoveAllInventoryItems(object sender, RoutedEventArgs e)
        {
            if (ListBoxInventory.Items.Count == 0)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to remove all items from Inventory?", 
                "Please confirm", 
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes) 
            {
                for (var i = ListBoxInventory.Items.Count - 1; i >= 0; i--)
                {
                    var item = (string)ListBoxInventory.Items[i];
                    _hero.RemoveFromInventory(item, (byte) i);
                }

                _hero.UpdateInventory();
                ListBoxInventory.Items.Clear();

                UpdateInventoryHeader();
            }
        }

        private void UpdateInventoryHeader()
        {
            GroupBoxInventory.Header = $"Inventory ({_hero.Inventory.Count})";
        }

        private void ListBoxInventory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not ListBox listBox)
                return;

            var artifact = listBox.SelectedItem as string;
            var artInfo = _hero.UpdateArtifactInfo(artifact, null, (byte)listBox.SelectedIndex);

            if (null != artInfo)
            {
                Attack.Text = artInfo[1];
                Defense.Text = artInfo[2];
                Power.Text = artInfo[3];
                Knowledge.Text = artInfo[4];
                Morale.Text = artInfo[5];
                Luck.Text = artInfo[6];
                Effects.Text = artInfo[7];
            }
        }

        private void ClearItem(object sender, RoutedEventArgs e)
        {
            if (e.Source is not Button button)
                return;

            var skillNo = (string)button.Tag;
            if (FindName(skillNo) is not ComboBox skillComboBox)
                return;

            skillComboBox.SelectedItem = null;
        }

        private void ToggleSpellBook(object sender, RoutedEventArgs e)
        {
            if (_initializing) 
                return;

            if (e.Source is not CheckBox chkBox)
                return;

            _hero.ToggleSpellBook(chkBox.IsChecked ?? false);
        }

        private void ListBoxInventory_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.Source is not ListBox listBox)
                return;

            var artifact = listBox.SelectedItem as string;
            var artInfo = _hero.UpdateArtifactInfo(artifact, null, (byte)listBox.SelectedIndex);

            if (null != artInfo)
            {
                Attack.Text = artInfo[1];
                Defense.Text = artInfo[2];
                Power.Text = artInfo[3];
                Knowledge.Text = artInfo[4];
                Morale.Text = artInfo[5];
                Luck.Text = artInfo[6];
                Effects.Text = artInfo[7];
            }
        }

        private void CreatureAmounMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;

            if (e.Source is not TextBox textBox)
                return;

            var value = int.Parse(textBox.Text);
            textBox.Text = (e.Delta > 0 ? value + 1 : value - 1).ToString(); 
        }

        private void NumberPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }

    public static class StringExtensions
    {
        public static string ToControlName(this string val)
        {
            return val.Replace(" ", "_").Replace("'", "__");
        }

        public static string FromControlName(this string val)
        {
            return val.Replace("__", "'").Replace("_", " ");
        }
    }
}

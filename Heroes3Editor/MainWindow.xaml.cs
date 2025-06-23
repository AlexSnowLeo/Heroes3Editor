using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Heroes3Editor.Lang;
using Heroes3Editor.Models;
using Microsoft.Win32;

namespace Heroes3Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Game Game { get; set; }

        private const string SaveGamesFilter = "HoMM3 Save games |*.*GM;*.GM*";

        public MainWindow()
        {
            InitializeComponent();

            MenuUpdateGameLang.IsChecked = Properties.Settings.Default.UpdateGameLangWithAppLang;
            MenuUseUniqueHeroes.IsChecked = Properties.Settings.Default.UseUniqueHeroes;

            App.LanguageChanged += LanguageChanged;

            CultureInfo currLang = App.Language;

            Title = GetTitle();

            //Заполняем меню смены языка:
            MenuLanguage.Items.Clear();
            foreach (var lang in App.Languages)
            {
                MenuItem menuLang = new MenuItem();
                menuLang.Header = lang.NativeName;
                menuLang.Tag = lang;
                menuLang.IsChecked = lang.Equals(currLang);
                menuLang.Click += ChangeLanguageClick;
                MenuLanguage.Items.Add(menuLang);
            }

            if (Properties.Settings.Default.RecentFiles != null)
            {
                foreach (var recentFile in Properties.Settings.Default.RecentFiles)
                {
                    var mItem = new MenuItem { Header = recentFile, Tag = recentFile };
                    mItem.Click += RecentFileLoadClick;

                    menuRecentFiles.Items.Add(mItem);
                }
            }

            heroTabs.Visibility = Visibility.Hidden;
            LangCboBox.SelectedValue = Constants.Lang[0];
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

#if !DEBUG
            MenuDebug.Visibility = Visibility.Hidden;
#endif
        }

        private void LanguageChanged(Object sender, EventArgs e)
        {
            CultureInfo currLang = App.Language;

            foreach (MenuItem item in MenuLanguage.Items)
            {
                if (item.Tag is not CultureInfo ci)
                    continue;

                item.IsChecked = ci.Equals(currLang);
            }

            if (Properties.Settings.Default.UpdateGameLangWithAppLang)
            {
                var lang = currLang.TwoLetterISOLanguageName.ToUpper();
                if (LangCboBox.Items.Contains(lang))
                    LangCboBox.SelectedItem = lang;
            }

            UpdateGameVersionStatus();
            Title = GetTitle();
        }

        private string GetTitle()
        {
            var title = LangHepler.Get("main_Title");
            return $"{title} {GetType().Assembly.GetName().Version}";
        }

        private void ChangeLanguageClick(Object sender, EventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is CultureInfo lang)
                {
                    App.Language = lang;
                }
            }

        }

        private void UpdateLangData(object sender, EventArgs eventArgs)
        {
            LangData.SetInstance((string)LangCboBox.SelectedValue);

            if (Properties.Settings.Default.UseUniqueHeroes)
            {
                Heroes.LoadUniqueValues();
                Heroes.LoadUniqueLang();
            }
            else
            {
                Heroes.RemoveUniqueValues();
                Heroes.RemoveUniqueLang();
            }

            UpdateHotaLangData();

            if (heroTabs.Items.Count == 0)
                return;

            var selected = heroTabs.SelectedIndex;
            var heroes = new List<string>();
            foreach (TabItem item in heroTabs.Items)
            {
                heroes.Add((string)item.Header);
                item.Visibility = Visibility.Hidden;
            }

            if (Game == null)
                return;

            Game.Heroes.Clear();
            heroTabs.Items.Clear();
            foreach (var hero in heroes)
            {
                AddHeroTab(hero);
            }

            heroTabs.SelectedIndex = selected;
        }

        private void UpdateHotaLangData()
        {
            if (Game?.IsHOTA ?? false)
            {
                Constants.Towns.LoadHotaLang();
                Heroes.LoadHotaLang();
            }
            else
            {
                Constants.Towns.RemoveHotaLang();
                Heroes.RemoveHotaLang();
            }

            HeroCboBox.ItemsSource = Heroes.Names;

            Game?.SearchTowns();
            if (Game?.Towns.Count > 0)
            {
                TownCboBox.Items.Clear();
                if (Game.Towns.Count > 0)
                {
                    foreach (var town in Game.Towns)
                    {
                        TownCboBox.Items.Add(town);
                    }
                }
            }

            if (TownCboBox.Items.Count > 0)
            {
                foreach (Town town in TownCboBox.Items)
                {
                    town.FactionLang = Constants.Towns.GetLangFactionValue(town.Faction);
                }

                TownCboBox.Items.Refresh();
            }
        }

        private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = SaveGamesFilter };
            if (openDlg.ShowDialog() == true)
            {
                if (Properties.Settings.Default.RecentFiles == null)
                    Properties.Settings.Default.RecentFiles = [];

                if (!Properties.Settings.Default.RecentFiles.Contains(openDlg.FileName))
                {
                    Properties.Settings.Default.RecentFiles.Insert(0, openDlg.FileName);

                    var mItem = new MenuItem { Header = openDlg.FileName, Tag = openDlg.FileName };
                    mItem.Click += RecentFileLoadClick;

                    menuRecentFiles.Items.Insert(0, mItem);
                }
                    
                if (Properties.Settings.Default.RecentFiles.Count > 5)
                    Properties.Settings.Default.RecentFiles.RemoveAt(5);
                Properties.Settings.Default.Save();

                LoadFile(openDlg.FileName);
            }
        }

        private void LoadFile(string fileName)
        {
            Game = new Game(fileName);

            heroTabs.Items.Clear();
            heroTabs.Visibility = Visibility.Hidden;

            var lang = Game.Lang.ToUpper();

            if (LangCboBox.SelectedItem.ToString() == lang)
            {
                UpdateHotaLangData();
            }

            LangCboBox.SelectedItem = lang;

            HeroCboBox.IsEnabled = true;
            heroSearchBtn.IsEnabled = true;

            status.Text = fileName;
            UpdateGameVersionStatus();

            TownCboBox.IsEnabled = true;
        }

        private void UpdateGameVersionStatus()
        {
            if (Game == null)
                return;

            var gameVerTpl = LangHepler.Get("main_gameVersion");
            GameVersion.Text = string.Format(gameVerTpl, Game.Version, Game.Lang.ToUpper());
        }

        private void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Game != null;
        }

        private void SaveCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog { Filter = SaveGamesFilter };
            if (saveDlg.ShowDialog() == true)
            {
                Game.Save(saveDlg.FileName);
                status.Text = saveDlg.FileName;
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchHero(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HeroCboBox.Text)) return;

            AddHeroTab(HeroCboBox.Text);
        }

        private void AddHeroTab(string heroName)
        {
            if (Game.Heroes.Any(x => x.Name == heroName))
            {
                foreach(TabItem tab in heroTabs.Items)
                {
                    if (tab.Header.ToString() == heroName)
                    {
                        tab.IsSelected = true;
                        return;
                    }
                }
            }

            var added = Game.SearchHero(heroName);
            if (added)
            {
                var hero = Game.Heroes.Last();
                var heroTab = new TabItem()
                {
                    Header = hero.Name
                };
                heroTab.Content = new HeroPanel()
                {
                    Hero = hero
                };
                heroTabs.Items.Add(heroTab);
                heroTabs.Visibility = Visibility.Visible;
                heroTab.IsSelected = true;
            }
        }

        private void LoadBinData(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "Bin game data |*.bin" };
            if (openDlg.ShowDialog() == true)
            {
                Game = new Game(openDlg.FileName, bin: true);

                heroTabs.Items.Clear();
                heroTabs.Visibility = Visibility.Hidden;
                HeroCboBox.IsEnabled = true;
                heroSearchBtn.IsEnabled = true;

                status.Text = openDlg.FileName;
            }
        }

        private void SaveBinData(object sender, RoutedEventArgs e)
        {
            if (Game == null)
                return;

            var fileName = Path.GetFileNameWithoutExtension(Game.FileName);
            var saveDlg = new SaveFileDialog { FileName = fileName, Filter = "Bin game data |*.bin" };
            if (saveDlg.ShowDialog() == true)
            {
                Game.Save(saveDlg.FileName, true);
                status.Text = saveDlg.FileName;
            }
        }

        private void TestSearchHeroes(object sender, RoutedEventArgs e)
        {
            if (Game == null)
                return;

            var notFoundHeroes = new List<string>();
            foreach (string hero in HeroCboBox.Items)
            {
                if (Game.SearchHero(hero, Game.Bytes.Length) == -1)
                {
                    var originalName = Heroes.GetOriginalValue(hero);
                    var uniqueHero = Heroes.UuniqueHeroes.FirstOrDefault(x => x.Name == Heroes.GetOriginalValue(hero));
                    var heroInfo = uniqueHero != null
                        ? $"{hero}: {uniqueHero.Description} ({uniqueHero.Expansion})"
                        : hero;

                    notFoundHeroes.Add(heroInfo);
                }
            }

            string mess;
            if (notFoundHeroes.Count > 50)
                mess = $"The number of undiscovered heroes on the loaded map ({notFoundHeroes.Count}) " +
                    $"is very large. Perhaps you have chosen the wrong game language?";
            else
            {
                mess = notFoundHeroes.Count == 0
                    ? "All characters of the selected language have been successfully found on the loaded map."
                    : $"The number of heroes has been found on this map is {HeroCboBox.Items.Count - notFoundHeroes.Count}\n\n" +
                      $"Heroes that were not found on the loaded map ({notFoundHeroes.Count}):" + 
                    "\n\n - " + string.Join("\n - ", notFoundHeroes);

                File.WriteAllText("not-found-heroes.txt", mess);
            }

            MessageBox.Show(mess, "Test Search Heroes");
        }

        private void TownSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            var (town, added) = Game.SearchTown(TownCboBox.Text, null, Game.Bytes.Length);
            if (added)
                TownCboBox.Items.Add(town);

            if (town != null)
                TownCboBox.SelectedItem = town;
        }

        private void OptSetUpdateGameLang(object sender, RoutedEventArgs e)
        {
            if (e.Source is not MenuItem mi)
                return;

            mi.IsChecked = !mi.IsChecked;
            Properties.Settings.Default.UpdateGameLangWithAppLang = mi.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void OptSetUseUniqueHeroes(object sender, RoutedEventArgs e)
        {
            if (e.Source is not MenuItem mi)
                return;

            mi.IsChecked = !mi.IsChecked;
            Properties.Settings.Default.UseUniqueHeroes = mi.IsChecked;
            Properties.Settings.Default.Save();

            if (mi.IsChecked)
            {
                Heroes.LoadUniqueValues();
                Heroes.LoadUniqueLang();
            }
            else
            {
                Heroes.RemoveUniqueValues();
                Heroes.RemoveUniqueLang();
            }

            HeroCboBox.ItemsSource = Heroes.Names;
        }

        private void RecentFileLoadClick(Object sender, EventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is string fileName)
                {
                    LoadFile(fileName);
                }
            }
        }

        private void SaveInfo_Click(object sender, RoutedEventArgs e)
        {
            if (Game == null)
                return;

            var saveInfo = new SaveInfo { Owner = this };

            saveInfo.TextInfo.Text = $"""
                File Name: {Game.FileName}
                Version:   {Game.Version}
                """;

            saveInfo.ShowDialog();
        }
    }
}

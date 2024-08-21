using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            heroTabs.Visibility = Visibility.Hidden;
            langCboBox.SelectedValue = Constants.Lang[0];
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

#if !DEBUG
            MenuDebug.Visibility = Visibility.Hidden;
#endif
        }

        private void UpdateLangData(object sender, EventArgs eventArgs)
        {
            LangData.SetInstance((string)langCboBox.SelectedValue);
            heroCboBox.ItemsSource = Heroes.Names;

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

        private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = SaveGamesFilter };
            if (openDlg.ShowDialog() == true)
            {
                Game = new Game(openDlg.FileName);

                heroTabs.Items.Clear();
                heroTabs.Visibility = Visibility.Hidden;
                heroCboBox.ItemsSource = Heroes.Names;
                heroCboBox.IsEnabled = true;
                heroSearchBtn.IsEnabled = true;

                status.Text = openDlg.FileName;
                GameVersion.Text = $" | Save Game version: {Game.Version} | Game Lang: {Game.Lang.ToUpper()}";
            }
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
            if (string.IsNullOrWhiteSpace(heroCboBox.Text)) return;

            AddHeroTab(heroCboBox.Text);
        }

        private void AddHeroTab(string heroName)
        {
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
                heroCboBox.IsEnabled = true;
                heroSearchBtn.IsEnabled = true;

                status.Text = openDlg.FileName;
            }
        }

        private void SaveBinData(object sender, RoutedEventArgs e)
        {
            var fileName = Path.GetFileNameWithoutExtension(Game.FileName);
            var saveDlg = new SaveFileDialog { FileName = fileName, Filter = "Bin game data |*.bin" };
            if (saveDlg.ShowDialog() == true)
            {
                Game.Save(saveDlg.FileName);
                status.Text = saveDlg.FileName;
            }
        }

        private void TestSearchHeroes(object sender, RoutedEventArgs e)
        {
            if (Game == null)
                return;

            var notFoundHeroes = new List<string>();
            foreach (string hero in heroCboBox.Items)
            {
                if (Game.SearchHero(hero, Game.Bytes.Length) == -1)
                {
                    var heroInfo = Heroes.TestDescriptions.TryGetValue(Heroes.GetLangValue(hero), out var desc) 
                        ? hero + ": " + desc 
                        : hero;

                    notFoundHeroes.Add(heroInfo);
                }
            }

            var mess = "";
            if (notFoundHeroes.Count == 0)
                mess = "All characters of the selected language have been successfully found on the loaded map.";

            if (notFoundHeroes.Count > 30)
                mess = $"The number of undiscovered heroes on the loaded map ({notFoundHeroes.Count}) " +
                    $"is very large. Perhaps you have chosen the wrong game language?";
            else
            {
                mess = "Heroes that were not found on the loaded map:" +
                    "\n\n - " + string.Join("\n - ", notFoundHeroes);
            }

            MessageBox.Show(mess, "Test Search Heroes");
        }
    }
}

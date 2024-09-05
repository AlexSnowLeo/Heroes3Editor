using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Heroes3Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly List<CultureInfo> _languages = [];

        public static List<CultureInfo> Languages
        {
            get
            {
                return _languages;
            }
        }

        private static readonly string[] _cultures = ["en-US", "ru-RU", "pl-PL", "fr-FR"];

        public App()
        {
            InitializeComponent();
            App.LanguageChanged += App_LanguageChanged;

            _languages.Clear();
            foreach (var culture in _cultures)
            {
                _languages.Add(new CultureInfo(culture));
            }

            Language = Heroes3Editor.Properties.Settings.Default.DefaultLanguage;
        }

        public static event EventHandler LanguageChanged;

        public static CultureInfo Language
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) return;

                System.Threading.Thread.CurrentThread.CurrentUICulture = value;

                var dict = new ResourceDictionary
                {
                    Source = new Uri($"Resources/lang.{value.Name}.xaml", UriKind.Relative)
                };

                var oldDict = (from d in Current.Resources.MergedDictionaries
                               where d.Source != null && d.Source.OriginalString.StartsWith("Resources/lang.")
                               select d).First();

                if (oldDict != null)
                {
                    var ind = Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Current.Resources.MergedDictionaries.Remove(oldDict);
                    Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Current.Resources.MergedDictionaries.Add(dict);
                }

                LanguageChanged(Current, new EventArgs());
            }
        }

        private void App_LanguageChanged(object sender, EventArgs e)
        {
            Heroes3Editor.Properties.Settings.Default.DefaultLanguage = Language;
            Heroes3Editor.Properties.Settings.Default.Save();
        }
    }
}

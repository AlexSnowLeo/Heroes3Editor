using System.Windows;
using Heroes3Editor.Models;

namespace Heroes3Editor;

public partial class SelectSpell : Window
{
    public string SelectedSpell { get; private set; }
    public SelectSpell()
    {
        InitializeComponent();

        foreach (var spell in Constants.Spells.Names)
        {
            ComboBoxSpells.Items.Add(Constants.Spells.ByLang(spell));
        }

        ComboBoxSpells.SelectedIndex = 0;
        SelectedSpell = (string)ComboBoxSpells.SelectedValue;
    }

    private void BtnSelect_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedSpell = (string)ComboBoxSpells.SelectedValue;
        this.Close();
    }
}


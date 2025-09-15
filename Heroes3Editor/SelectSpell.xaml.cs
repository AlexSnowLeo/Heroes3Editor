using System.Linq;
using System.Windows;
using Heroes3Editor.Models;

namespace Heroes3Editor;

public partial class SelectSpell : Window
{
    public string SelectedSpell { get; private set; }
    public SelectSpell()
    {
        InitializeComponent();

        var titansLightningBolt = Constants.Spells[Spells.TitansLightningBolt];
        ComboBoxSpells.ItemsSource = Constants.Spells.Names.Except([titansLightningBolt]);

        ComboBoxSpells.SelectedIndex = 0;
        SelectedSpell = (string)ComboBoxSpells.SelectedValue;
    }

    private void BtnSelect_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedSpell = (string)ComboBoxSpells.SelectedValue;
        this.Close();
    }
}


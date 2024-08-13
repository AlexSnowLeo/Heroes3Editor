using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Heroes3Editor.Models;

namespace Heroes3Editor;

public partial class SelectArtifact : Window
{
    public string SelectedArtifact { get; private set; }
    public string SelectedSpell { get; private set; }
    public SelectArtifact()
    {
        InitializeComponent();

        foreach (var spell in Constants.Spells.Names)
        {
            ComboBoxSpells.Items.Add(Constants.Spells.ByLang(spell));
        }

        ComboBoxArtifacts.ItemsSource = Constants.Artifacts.ArtifactsToEquip;
        
        ComboBoxArtifacts.SelectedIndex = 0;
        SelectedArtifact = (string)ComboBoxSpells.SelectedValue;
    }

    private void BtnSelect_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedArtifact = (string)ComboBoxArtifacts.SelectedValue;
        SelectedSpell = (string)ComboBoxSpells.SelectedValue;
        this.Close();
    }

    private void ComboBoxArtifacts_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (e.Source is not ComboBox cBox)
            return;

        var artCode = Constants.Artifacts[(string)cBox.SelectedValue];
        ComboBoxSpells.IsEnabled = artCode == Constants.SPELL_SCROLL;
        if (artCode == Constants.SPELL_SCROLL)
        {
            ComboBoxSpells.SelectedIndex = 0;
            SelectedSpell = (string)ComboBoxSpells.SelectedValue;
            
            return;
        }

        ComboBoxSpells.SelectedValue = null;
        SelectedSpell = null;
    }
}


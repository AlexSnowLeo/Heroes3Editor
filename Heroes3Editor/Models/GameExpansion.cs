using System.ComponentModel;

namespace Heroes3Editor.Models
{
    public enum GameExpansion
    {
        [Description("The Restoration Of Erathia")]
        RoE,
        [Description("The Shadow of Death")]
        SoD,
        [Description("Armageddon's Blade")]
        AB,
        [Description("Heroes Chronicles")]
        Chronicles,
        [Description("In the Wake of Gods")]
        WoG,
        [Description("Horn of the Abyss")]
        HotA
    }
}

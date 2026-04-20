using System.Collections.Generic;
using System.ComponentModel;

namespace Heroes3Editor.Models
{
    public enum GameExpansion
    {
        [Description("The Restoration of Erathia (HD Edition)")]
        RoEHD,
        [Description("The Restoration of Erathia")]
        RoE,
        [Description("Heroes Chronicles")]
        Chronicles,
        [Description("Armageddon's Blade")]
        AB,
        [Description("The Shadow of Death")]
        SoD,
        [Description("In the Wake of Gods")]
        WoG,
        [Description("Horn of the Abyss")]
        HotA
    }
}

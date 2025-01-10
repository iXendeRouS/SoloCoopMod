using BTD_Mod_Helper.Api.Data;
using BTD_Mod_Helper.Api.ModOptions;

namespace SoloCoopMod;

public class Settings : ModSettings
{
    public static readonly ModSettingBool EnableMod = new(true)
    {
        displayName = "Enable Mod"
    };

    public static readonly ModSettingInt NCoopMembers = new(3)
    {
        displayName = "Additional Coop Members"
    };

    public static readonly ModSettingString Heroes = new("")
    {
        displayName = "Heroes (separate with commas)"
    };

    public static readonly ModSettingBool MasterDoubleCross = new(true)
    {
        displayName = "Master Double Cross MK"
    };
}
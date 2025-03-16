using BTD_Mod_Helper.Api.Data;
using BTD_Mod_Helper.Api.ModOptions;

namespace SoloCoopMod;

public class Settings : ModSettings
{
    public static readonly ModSettingCategory heroes = new("Heroes")
    {
        order = -1
    };

    public static readonly ModSettingEnum<Heroes> Hero1 = new(Heroes.None) { category = heroes };
    public static readonly ModSettingEnum<Heroes> Hero2 = new(Heroes.None) { category = heroes };
    public static readonly ModSettingEnum<Heroes> Hero3 = new(Heroes.None) { category = heroes };
    public static readonly ModSettingEnum<Heroes> Hero4 = new(Heroes.None) { category = heroes };

    public static readonly ModSettingBool EnableMod = new(true)
    {
        displayName = "Enable Mod",
    };

    public static readonly ModSettingBool UseCoopStartingCash = new(true)
    {
        displayName = "Use Coop Starting Cash"
    };

    public static readonly ModSettingBool MasterDoubleCross = new(true)
    {
        displayName = "Master Double Cross MK"
    };
}
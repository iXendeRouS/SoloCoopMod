using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Models.SimulationBehaviors;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;
using Il2CppAssets.Scripts.Models.Towers;
using Newtonsoft.Json.Linq;
using SoloCoopMod;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(SoloCoopMod.SoloCoopMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SoloCoopMod;

public class SoloCoopMod : BloonsTD6Mod
{
    ModSettingEnum<Heroes>[] heroSettings = [Settings.Hero1, Settings.Hero2, Settings.Hero3, Settings.Hero4];
    private static List<string> heroes = new List<string>();

    public override void OnLoadSettings(JObject settings)
    {
        base.OnLoadSettings(settings);
        UpdateHeroes();
    }

    public override void OnSaveSettings(JObject settings)
    {
        base.OnSaveSettings(settings);
        UpdateHeroes();
    }

    private void UpdateHeroes()
    {
        heroes.Clear();

        foreach (var heroSetting in heroSettings)
            if ((Heroes)heroSetting.GetValue() != Heroes.None)
                heroes.Add(heroSetting.GetValue().ToString());
    }

    public override void OnNewGameModel(GameModel result)
    {
        base.OnNewGameModel(result);

        if (Settings.EnableMod && heroes.Count > 0)
            ApplyTierRestrictions(result);
    }

    [HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.MatchReady))]
    internal static class UnityToSimulation_MatchReady
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            if (Settings.EnableMod && heroes.Count > 0)
            {
                ApplyCoopHeroes();
                ApplyCoopStartingCash();
            }
        }
    }

    private static void ApplyTierRestrictions(GameModel model)
    {
        var restrictionList = new List<TowerTierRestrictionModel>(model.towerTierRestrictions);

        foreach (var item in GameModelExt.GetAllShopTowerDetails(model))
            for (var i = 0; i < 3; i++)
                restrictionList.Add(new TowerTierRestrictionModel(item.towerId + "Restriction", item.towerId, i, 5, heroes.Count - 1));

        if (Settings.MasterDoubleCross)
            restrictionList.Add(new TowerTierRestrictionModel("DartMonkey" + "Restriction", "DartMonkey", 2, 5, heroes.Count - 1));

        model.towerTierRestrictions = restrictionList.ToArray();

        foreach (TowerModel towerModel in model.towers)
            if (towerModel.tier >= 5)
                towerModel.GetDescendants<LimitProjectileModel>().ForEach(lpmodel => lpmodel.globalForPlayer = false);
    }

    private static void ApplyCoopHeroes()
    {
        var towerMaxes = InGame.instance.GetTowerInventory().towerMaxes;

        foreach (var hero in Game.instance.GetHeroDetailModels())
            towerMaxes[hero.towerId] = 0;

        foreach (var heroId in heroes)
            if (Game.instance.GetHeroDetailModels().Any(h => h.towerId == heroId))
                towerMaxes[heroId]++;

        RefreshShop();
    }

    private static void RefreshShop()
    {
        ShopMenu.instance.RebuildTowerSet();

        foreach (var button in ShopMenu.instance.ActiveTowerButtons)
            button.Cast<TowerPurchaseButton>().Update();
    }

    private static void ApplyCoopStartingCash()
    {
        if (!Settings.UseCoopStartingCash || InGame.instance.GetSimulation().roundTime.elapsed != 0 || InGameData.CurrentGame.IsSandbox || heroes.Count < 2) return;
        InGame.instance.AddCash((heroes.Count - 1) * 650 - heroes.Count * heroes.Count * 50);
    }
}
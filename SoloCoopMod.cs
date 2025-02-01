using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using SoloCoopMod;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Models.SimulationBehaviors;
using System.Collections.Generic;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;
using Il2CppAssets.Scripts.Models.Towers;

[assembly: MelonInfo(typeof(SoloCoopMod.SoloCoopMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SoloCoopMod;

public class SoloCoopMod : BloonsTD6Mod
{
    public override void OnNewGameModel(GameModel result)
    {
        MelonLogger.Msg("OnNewGameModel");
        base.OnNewGameModel(result);

        if (Settings.EnableMod && Settings.NCoopMembers > 0)
        {
            ApplyTierRestrictions(result);
        }
    }

    [HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.MatchReady))]
    internal static class UnityToSimulation_MatchReady
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            MelonLogger.Msg("MatchReady - Reapplying Coop Rules");
            if (Settings.EnableMod && Settings.NCoopMembers > 0)
            {
                ApplyCoopHeroes();
                ApplyCoopStartingCash();
            }
        }
    }

    // Allow multiple T5s with special handling for Master Double Master Cross MK setting
    private static void ApplyTierRestrictions(GameModel model)
    {
        MelonLogger.Msg("ApplyTierRestrictions");

        var restrictionList = new List<TowerTierRestrictionModel>(model.towerTierRestrictions);

        foreach (var item in GameModelExt.GetAllShopTowerDetails(model))
        {
            for (var i = 0; i < 3; i++)
            {
                restrictionList.Add(new TowerTierRestrictionModel(item.towerId + "Restriction", item.towerId, i, 5, Settings.NCoopMembers));
            }
        }

        if (Settings.MasterDoubleCross)
        {
            restrictionList.Add(new TowerTierRestrictionModel("DartMonkey" + "Restriction", "DartMonkey", 2, 5, Settings.NCoopMembers));
        }

        model.towerTierRestrictions = restrictionList.ToArray();

        foreach (TowerModel towerModel in model.towers)
        {
            if (towerModel.tier >= 5)
            {
                towerModel.GetDescendants<LimitProjectileModel>().ForEach(lpmodel => lpmodel.globalForPlayer = false);
            }
        }
    }

    private static void ApplyCoopHeroes()
    {
        MelonLogger.Msg("ApplyCoopHeroes");

        var inventory = InGame.instance.GetTowerInventory();
        
        foreach (var hero in Game.instance.GetHeroDetailModels())
        {
            inventory.towerMaxes[hero.towerId] = 0;
        }

        // Parse the heroes defined in settings or fallback to the default selected hero
        var definedHeroes = Settings.Heroes.GetValue()?.ToString().Split(',') ?? new string[0];
        var nCoopMembers = Settings.NCoopMembers;
        var totalHeroesAllowed = nCoopMembers + 1;
        var heroQueue = new Queue<string>(definedHeroes);
        var selectedHero = InGame.instance.SelectedHero;

        while (heroQueue.Count > 0 && totalHeroesAllowed > 0)
        {
            var heroId = heroQueue.Dequeue();
            if (Game.instance.GetHeroDetailModels().Any(h => h.towerId == heroId))
            {
                inventory.towerMaxes[heroId]++;
                totalHeroesAllowed--;
            }
        }

        if (totalHeroesAllowed > 0)
        {
            inventory.towerMaxes[selectedHero] += totalHeroesAllowed;
        }

        RefreshShop();
    }

    private static void RefreshShop()
    {
        MelonLogger.Msg("Refreshing shop for updated hero inventory");

        ShopMenu.instance.RebuildTowerSet();
        foreach (var button in ShopMenu.instance.ActiveTowerButtons)
        {
            button.Cast<TowerPurchaseButton>().Update();
        }
    }

    private static void ApplyCoopStartingCash()
    {
        MelonLogger.Msg("ApplyCoopStartingCash");

        if (!Settings.UseCoopStartingCash || InGameData.CurrentGame.IsSandbox) return;
        if (InGame.instance.GetCash() != 850 && InGame.instance.GetCash() != 650) return;

        int n = Settings.NCoopMembers + 1;

        double extraCash = ((n - 1) * (650 - 50 * n) - 50 * n) * (InGameData.CurrentGame.selectedMode == "HalfCash" ? 0.5 : 1);

        InGame.instance.AddCash(extraCash);
    }
}

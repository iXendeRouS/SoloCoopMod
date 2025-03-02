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
using Newtonsoft.Json.Linq;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppSystem.Runtime.InteropServices;

[assembly: MelonInfo(typeof(SoloCoopMod.SoloCoopMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SoloCoopMod;

public class SoloCoopMod : BloonsTD6Mod
{
    private static int _activeCoopMembersCount = 0;
    private static List<string> _selectedHeroIds = new List<string>();

    ModSettingEnum<Heroes>[] heroSettings = [
        Settings.Hero1,
        Settings.Hero2,
        Settings.Hero3,
        Settings.Hero4
    ];

    public override void OnLoadSettings(JObject settings)
    {
        base.OnLoadSettings(settings);
        UpdateCachedValues();
    }

    public override void OnSaveSettings(JObject settings)
    {
        base.OnSaveSettings(settings);
        UpdateCachedValues();
    }

    private void UpdateCachedValues()
    {
        _selectedHeroIds = new List<string>();

        foreach (var heroSetting in heroSettings)
        {
            if ((Heroes)heroSetting.GetValue() != Heroes.None)
            {
                _selectedHeroIds.Add(heroSetting.GetValue().ToString());
            }
        }

        _activeCoopMembersCount = _selectedHeroIds.Count;
    }

    public override void OnNewGameModel(GameModel result)
    {
        base.OnNewGameModel(result);

        if (Settings.EnableMod && _activeCoopMembersCount > 0)
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
            if (Settings.EnableMod && _activeCoopMembersCount > 0)
            {
                ApplyCoopHeroes();
                ApplyCoopStartingCash();
            }
        }
    }

    // Allow multiple T5s with special handling for Master Double Master Cross MK setting
    private static void ApplyTierRestrictions(GameModel model)
    {
        var restrictionList = new List<TowerTierRestrictionModel>(model.towerTierRestrictions);

        foreach (var item in GameModelExt.GetAllShopTowerDetails(model))
        {
            for (var i = 0; i < 3; i++)
            {
                restrictionList.Add(new TowerTierRestrictionModel(item.towerId + "Restriction", item.towerId, i, 5, _activeCoopMembersCount - 1));
            }
        }

        if (Settings.MasterDoubleCross)
        {
            restrictionList.Add(new TowerTierRestrictionModel("DartMonkey" + "Restriction", "DartMonkey", 2, 5, _activeCoopMembersCount - 1));
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
        var towerMaxes = InGame.instance.GetTowerInventory().towerMaxes;

        foreach (var hero in Game.instance.GetHeroDetailModels())
        {
            towerMaxes[hero.towerId] = 0;
        }

        foreach (var heroId in _selectedHeroIds)
        {
            if (Game.instance.GetHeroDetailModels().Any(h => h.towerId == heroId)) {
                towerMaxes[heroId]++;
            }
        }

        RefreshShop();
    }

    private static void RefreshShop()
    {
        ShopMenu.instance.RebuildTowerSet();
        foreach (var button in ShopMenu.instance.ActiveTowerButtons)
        {
            button.Cast<TowerPurchaseButton>().Update();
        }
    }

    private static void ApplyCoopStartingCash()
    {
        if (!Settings.UseCoopStartingCash || InGameData.CurrentGame.IsSandbox || _activeCoopMembersCount < 2 || InGame.instance.GetSimulation().roundTime.elapsed != 0) return;
        InGame.instance.AddCash((_activeCoopMembersCount - 1) * 650 - _activeCoopMembersCount * _activeCoopMembersCount * 50);
    }
}
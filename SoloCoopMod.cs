using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using SoloCoopMod;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity;

[assembly: MelonInfo(typeof(SoloCoopMod.SoloCoopMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SoloCoopMod;

public class SoloCoopMod : BloonsTD6Mod
{
    public override void OnRestart()
    {
        MelonLogger.Msg("OnRestart");

        base.OnRestart();

        if (Settings.EnableMod && Settings.NCoopMembers >= 1)
        {
            ApplyCoopHeroes();
            ApplyTierRestrictions();
            SetCoopStartingCash();
        }
    }

    public override void OnMatchStart()
    {
        MelonLogger.Msg("OnMatchStart");

        base.OnMatchStart();

        if (Settings.EnableMod && Settings.NCoopMembers >= 1)
        {
            ApplyCoopHeroes();
            ApplyTierRestrictions();
            SetCoopStartingCash();
        }
    }

    // Set hero counts for coop members given a comma-separated list of Heroes Settings.Heroes
    private static void ApplyCoopHeroes()
    {
        var inventory = InGame.instance.GetTowerInventory();

        foreach (var item in Game.instance.GetHeroDetailModels())
        {
            string s = item.towerId;
            inventory.towerMaxes[s] = 0;
        }

        var heroes = Settings.Heroes.GetValue().ToString();
        int nHeroes = 1;
        int nHeroesMax = 1 + Settings.NCoopMembers;

        if (string.IsNullOrEmpty(heroes))
        {
            inventory.towerMaxes[InGame.instance.SelectedHero] = nHeroesMax;
        }
        else
        {
            inventory.towerMaxes[InGame.instance.SelectedHero] = 1;

            foreach (var item in heroes.Split(','))
            {
                if (inventory.towerMaxes.ContainsKey(item))
                {
                    inventory.towerMaxes[item] += 1;
                }

                if (nHeroes >= nHeroesMax)
                {
                    break;
                }
                nHeroes++;
            }

            inventory.towerMaxes[InGame.instance.SelectedHero] += nHeroesMax - nHeroes;
        }
    }

    // Allow multiple T5s with special handling for Master Double Cross MK setting
    private static void ApplyTierRestrictions()
    {
        var inventory = InGame.instance.GetTowerInventory();
        var towers = GameModelExt.GetAllTowerDetails(Game.instance.model);

        foreach (var tower in towers)
        {
            if (tower.Is<ShopTowerDetailsModel>())
                for (var path = 0; path < 3; path++)
                    inventory.AddTierRestriction(tower.towerId, path, 5, Settings.NCoopMembers);
        }

        if (Settings.MasterDoubleCross)
        {
            inventory.AddTierRestriction("DartMonkey", 2, 5, Settings.NCoopMembers);
        }
    }

    // Sets starting cash to the total of what it would be in Coop
    private static void SetCoopStartingCash()
    {
        if (!Settings.UseCoopStartingCash || InGame.instance.GetCash() >= 9999999) return;

        int n = Settings.NCoopMembers;
        n = n > 5 ? 5 : n;
        int[] coopCash = { 650, 1100, 1500, 1800, 2000, 2100 };

        InGame.instance.SetCash(InGame.instance.GetCash() - 650 + coopCash[n]);
    }
}

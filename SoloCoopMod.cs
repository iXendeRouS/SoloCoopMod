using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using SoloCoopMod;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using System.Linq;

[assembly: MelonInfo(typeof(SoloCoopMod.SoloCoopMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SoloCoopMod;

public class SoloCoopMod : BloonsTD6Mod
{
    // Sets starting cash to the total of what it would be in Coop
    public override void OnInGameLoaded(InGame inGame)
    {
        base.OnInGameLoaded(inGame);

        if (!Settings.EnableMod || !Settings.UseCoopStartingCash || Settings.NCoopMembers < 1 || InGame.instance.GetCash() >= 9999999) return;

        int numPlayers = Settings.NCoopMembers + 1;

        int[] coopCash = { 0, 650, 1100, 1500, 1800, 2000, 2100 };

        if (numPlayers > 6) numPlayers = 6;

        double defaultCash = 650;
        double currentCash = InGame.instance.GetCash();
        InGame.instance.SetCash((currentCash - defaultCash) + coopCash[numPlayers]);
    }

    [HarmonyPatch(typeof(TowerInventory), nameof(TowerInventory.SetTowerTierRestrictions))]
    internal static class TowerInventory_SetTowerTierRestrictions
    {
        [HarmonyPostfix]
        private static void Postfix(TowerInventory __instance, IEnumerable<TowerDetailsModel> towers)
        {
            if (!Settings.EnableMod || Settings.NCoopMembers < 1)
                return;

            var heroes = Settings.Heroes.GetLastSavedValue()?.ToString();

            if (!string.IsNullOrEmpty(heroes))
            {
                ApplyCustomHeroes(__instance, heroes);
            }
            else
            {
                ApplyDefaultHeroes(__instance);
            }

            ApplyTierRestrictions(__instance, towers);
        }

        // Set hero counts for coop members given a comma-separated list of Heroes Settings.Heroess
        private static void ApplyCustomHeroes(TowerInventory inventory, string heroes)
        {
            var heroCounts = new Dictionary<string, int>();
            int totalHeroesAllowed = Settings.NCoopMembers;
            int allocatedHeroes = 0;

            // Retrieve valid hero names from the inventory for validation
            var validHeroes = inventory.GetTowerInventoryMaxes().Keys().ToHashSet();
            if (validHeroes == null) return;

            foreach (var hero in heroes.Split(','))
            {
                if (allocatedHeroes >= totalHeroesAllowed)
                    break;

                var trimmedHero = hero.Trim();

                // Skip invalid hero names
                if (!validHeroes.Contains(trimmedHero))
                    continue;

                if (!heroCounts.ContainsKey(trimmedHero))
                {
                    heroCounts[trimmedHero] = 0;
                }

                heroCounts[trimmedHero]++;
                allocatedHeroes++;
            }

            // Prepare changes to be applied to the inventory
            var changes = new Dictionary<string, int>();
            foreach (var entry in inventory.GetTowerInventoryMaxes())
            {
                if (heroCounts.TryGetValue(entry.Key, out var count))
                {
                    changes[entry.Key] = count;
                }
            }

            // Apply changes to the inventory
            foreach (var change in changes)
            {
                inventory.GetTowerInventoryMaxes()[change.Key] += change.Value;
            }
        }


        // Just use multiple of the players selected hero is Settings.Heroes is invalid
        private static void ApplyDefaultHeroes(TowerInventory inventory)
        {
            foreach (var entry in inventory.GetTowerInventoryMaxes())
            {
                if (entry.Value == 1)
                {
                    inventory.GetTowerInventoryMaxes()[entry.Key] += Settings.NCoopMembers;
                    break;
                }
            }
        }

        // Allow multiple T5s with special handling for Master Double Cross MK setting
        private static void ApplyTierRestrictions(TowerInventory inventory, IEnumerable<TowerDetailsModel> towers)
        {
            towers.ForEach(tower =>
            {
                if (tower.Is<ShopTowerDetailsModel>())
                    for (var path = 0; path < 3; path++)
                        inventory.AddTierRestriction(tower.towerId, path, 5, Settings.NCoopMembers);
            });

            if (Settings.MasterDoubleCross)
            {
                inventory.AddTierRestriction("DartMonkey", 2, 5, Settings.NCoopMembers);
            }
        }
    }
}

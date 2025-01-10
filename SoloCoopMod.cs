using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using SoloCoopMod;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppSystem.Collections.Generic;
using HarmonyLib;

[assembly: MelonInfo(typeof(SoloCoopMod.SoloCoopMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace SoloCoopMod;

public class SoloCoopMod : BloonsTD6Mod
{
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

        // Set hero counts for coop members given a comma separated list of Heroes Settings.Heroess
        private static void ApplyCustomHeroes(TowerInventory inventory, string heroes)
        {
            var heroCounts = new Dictionary<string, int>();
            int totalHeroesAllowed = Settings.NCoopMembers;
            int allocatedHeroes = 0;

            foreach (var hero in heroes.Split(','))
            {
                if (allocatedHeroes >= totalHeroesAllowed)
                    break;

                var trimmedHero = hero.Trim();
                if (!heroCounts.ContainsKey(trimmedHero))
                {
                    heroCounts[trimmedHero] = 0;
                }

                heroCounts[trimmedHero]++;
                allocatedHeroes++;
            }

            var changes = new Dictionary<string, int>();
            foreach (var entry in inventory.GetTowerInventoryMaxes())
            {
                if (heroCounts.TryGetValue(entry.Key, out var count))
                {
                    changes[entry.Key] = count;
                }
            }

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

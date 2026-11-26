using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using Blukulele.SaveSystem;
using Gambonanza.ModSdk;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Gambonanza.Completionist
{
    /// <summary>
    /// </summary>
    public sealed class CompletionistMod : IMod
    {
        private IModContext _context;
        // A dictionary that maps rarities to their plurar string name
        private Dictionary<Rarity, string> rarities = new Dictionary<Rarity, string>
        {
            {Rarity.COMMON, "COMMONS"},
            {Rarity.RARE, "RARES"},
            {Rarity.EPIC, "EPICS"},
            {Rarity.LEGENDARY, "LEGENDARIES"}
        };

        // Initializes and registers the command
        public void OnLoad(IModContext context)
        {
            _context = context;
            var console = _context.Console;
            if (console == null)
            {
                context.LogLine("Completionist could not create new console instance.");
                return;
            }

            // Register the new command with its name, help message,
            // action when run, and tab completers
            console.RegisterCommand("completionist", "get all gambits without king medals",
            args =>
            {
                // Object of game translations. Used to get gambit names of player's language
                JSONNode traduction = SingletonMonoBehaviour<LocalizationManager>.Instance.GetTraduction();
                // Counts of total gambits and gambits with king medals
                int totalCount = 0;
                int missingCount = 0;
                // A dictionary that maps rarities to a list of gambits. New missing gambits are inserted into the list of their rarity
                Dictionary<Rarity, List<SO_Gambit>> missingGambits = new Dictionary<Rarity, List<SO_Gambit>>();
                foreach (Rarity rarity in rarities.Keys)
                {
                    missingGambits[rarity] = new List<SO_Gambit>();
                }

                // Fills gambits dictionary with gambits that have are missing a king medal
                foreach (SO_Gambit gambit in SingletonMonoBehaviour<GambitLibrary>.Instance.GambitsInfo)
                {
                    if (!SingletonMonoBehaviour<GambitMedalManager>.Instance.HasGambitMedal(gambit.ID, DIFFICULTY.KING))
                    {
                        missingGambits[gambit.Rarity].Add(gambit);
                        missingCount++;
                    }
                    totalCount++;
                }
                double missingPercentage = Math.Round(missingCount * 100.0 / totalCount, 2);
                console.PrintInfo(missingPercentage + "% of gambits have king medals (" + missingCount + "/" + totalCount + ")");
                console.PrintInfo("The gambits without king medals are as follows:");
                foreach (List<SO_Gambit> gambitsByRarity in missingGambits.Values)
                {
                    if (gambitsByRarity.Any()) {
                        console.PrintInfo("");
                        console.PrintInfo(rarities[gambitsByRarity.First().Rarity]);
                    } else {
                        continue;
                    }
                    foreach (SO_Gambit gambit in gambitsByRarity)
                    {
                        string name = traduction["gambit"][gambit.GambitName];
                        console.PrintInfo("- " + name);
                    }
                }
                console.PrintInfo("");
                console.PrintInfo(missingPercentage + "% of gambits have king medals (" + missingCount + "/" + totalCount + ")");
            },

            // No tab completer
            (args, argIndex) => null
            );
        }

        public void OnDisable() => _context?.Console.UnregisterCommand("completionist");
    }
}

using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.PrincesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitPrince.cs.
    /// </summary>
    public sealed class PrincesGambitMod : IMod
    {
        public void OnLoad(IModContext context)
        {
            context.LogLine("[PrincesGambit] registering Prince's Gambit.");

            // Optional custom art: put `Prince.png` next to mod.json.
            // In source form that means:
            //   PrincesGambit/Prince.png
            // After building/installing, sample_mods/build.sh copies it beside the DLL:
            //   Mods/PrincesGambit/Prince.png
            // If the file is missing, we generate a tiny placeholder so the mod still works.
            var spritePath = Path.Combine(context.ModDirectory, "Prince.png");
            var sprite = File.Exists(spritePath)
                ? ModGambitApi.LoadSprite(spritePath)
                : GenerateFallbackSprite();

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit Prince`, so keep it short and readable.
            var def = GambitBuilder.Create("Prince")
                .WithName("Prince's Gambit")
                .WithDescription("After each game, <shake>shuffle</shake> the tiles on the board. Stepping on 46 modified tiles gives <color=∞>$100</color> and replaces this gambit with <color=Ø>INHERITANCE'S GAMBIT</color>")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.MONEY)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(0.85f)
                // This tells GambitApi to attach GambitPrince to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitPrince>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[PrincesGambit] registered '{def.Id}'.");
        }

        // Keeping the same fallback sprite as Spike's Gambit
        private static Sprite GenerateFallbackSprite()
        {
            const int w = 17;
            const int h = 26;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var c = Color.clear;

                    // Three little spike clusters, readable at gambit-card size.
                    var spikeA = y >= 5 && y <= 14 && x >= 2 && x <= 7 && (x + y) % 3 != 0;
                    var spikeB = y >= 9 && y <= 19 && x >= 6 && x <= 12 && (x + 2 * y) % 4 != 0;
                    var spikeC = y >= 3 && y <= 12 && x >= 10 && x <= 15 && (2 * x + y) % 3 != 0;
                    var stem = (y == 15 && x >= 2 && x <= 13) || (y == 20 && x >= 5 && x <= 13);

                    if (spikeA || spikeB || spikeC || stem)
                    {
                        var edge = x <= 2 || x >= 15 || y <= 4 || y >= 20;
                        c = edge ? new Color(0.18f, 0.12f, 0.08f, 1f) : new Color(0.70f, 0.55f, 0.30f, 1f);
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

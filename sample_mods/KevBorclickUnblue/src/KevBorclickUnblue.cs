using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.ModSdk;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace Gambonanza.KevBorclickUnblue
{
    /// <summary>
    /// Removes the blue glow shown under Kev Borclick's eyes.
    /// 
    /// This is done by:
    /// - Listening for any state change that could possibly load Kev Borclick
    /// - Use FindObjectOfType to try and grab the loaded ClockBossBehaviour object
    /// - Recursively search through the object's transform nodes to find and disable
    ///     any instance of SPR_CircleFade_Larger from within their renderers.
    /// 
    /// There's also a console command that toggles debugging messages sent to the mod console
    /// </summary>
    public sealed class KevBorclickUnblueMod : IMod
    {
        private const string EyeFadeObjectName = "FadeEffect";
        private const string EyeFadeSpriteName = "SPR_CircleFade_Larger";
        private const string USAGE_MESSAGE = "kevunbluelogging [on|off]";

        private IModContext _context;

        private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;
        private static ClockBossBehaviour _cbb;
        public bool logging = false;

        public void OnLoad(IModContext context)
        {
            _context = context;
            GameManager.Instance.onStateChanged += Behave;
            _context.Console.RegisterCommand("kevunbluelogging", "toggle debugging for KevBorclickUnblue",
            args =>
            {
                if (args.Length != 1)
                {
                    _context.Console.PrintWarn("Input invalid. Usage: "+USAGE_MESSAGE);
                    return;
                }
                switch (args[0])
                {
                    case "on":
                        logging = true;
                        break;
                    case "off":
                        logging = false;
                        break;
                    default:
                        _context.Console.PrintWarn("Input invalid. Usage: "+USAGE_MESSAGE);
                        break;
                }
            },
            (args, argIndex) => 
                argIndex == 0 ? (new[] { "on", "off" }): null
            );
        }

        // Is subscribed to GameManager.onStateChanged. Checks if the shop was exited, or
        // if an existing run was loaded from the main menu. If so, call PossibleKevLoadOccured()
        private void Behave(State state)
        {
            State previousState = SingletonMonoBehaviour<GameManager>.Instance.PreviousState;
            if (previousState == State.LOAD_RUN) {
                if (state == State.BOARD_PLACEMENT || state == State.INGAME) {
                    PossibleKevLoadOccured(state, previousState);
                }
            } else if (previousState == State.SHOP && state == State.BOARD_PLACEMENT) {
                PossibleKevLoadOccured(state, previousState);
            }
        }

        // Simply does a log and begins the coroutine of disabling the blue eye glows
        private void PossibleKevLoadOccured(State state, State previousState)
        {
            if (logging) 
                _context.Console.PrintInfo($"State changed to {state.ToString()} from {previousState.ToString()}.");
            GameManager.Instance.StartCoroutine(CO_DisableEyeGlows(0.3f));
        }

        // Waits a given amount of time, then searches the game for a currently loaded instance
        // of ClockBossBehaviour. Logs whether or not it was found. If so, begins the recursive
        // function finding and disabling the blue eye glows. Since it starts a recursive function,
        // the log after has to be in this function to ensure it only runs once.
        private IEnumerator CO_DisableEyeGlows(float delay)
        {
            yield return new WaitForSeconds(delay);
            _cbb = UnityEngine.Object.FindObjectOfType<ClockBossBehaviour>();
            bool found = _cbb != null;
            if (logging)
                _context.Console.PrintInfo("Kev was "+(found?"found, attempting eye glow disable":"not found."));
            if (found)
            {
                int disabledRenderers = DisableEyeGlows(_cbb.transform);
                if (logging)
                    _context.Console.PrintInfo($"Waited {delay} seconds and disabled {disabledRenderers} eye fade renderer(s).");
            }
        }

        // Function that recursively searches a transform node and and all its children.
        // For each node found, if checks if the name is FadeEffect. If so, it checks if
        // any of its SpriteRenderer components have a sprite name of SPR_CircleFade_Larger.
        // If so, they are disabled.
        private int DisableEyeGlows(Transform node)
        {
            int disabledRenderers = 0;
            if (node.name == EyeFadeObjectName)
            {
                foreach (var spriteRenderer in node.GetComponents<SpriteRenderer>())
                {
                    if (spriteRenderer.sprite != null && spriteRenderer.sprite.name == EyeFadeSpriteName)
                    {
                        spriteRenderer.enabled = false;
                        disabledRenderers++;
                    }
                }
            }

            for (int i = 0; i < node.childCount; i++)
                disabledRenderers += DisableEyeGlows(node.GetChild(i));

            return disabledRenderers;
        }

        public void OnDisable() {
            GameManager.Instance.onStateChanged -= Behave;
            _context.Console.UnregisterCommand("kevunbluelogging");
        }
    }
}

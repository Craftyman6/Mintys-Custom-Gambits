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

namespace Gambonanza.MechagnusUntext
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MechagnusUntextMod : IMod
    {
        private readonly string[] REMOVED_NODES = {"ContainerMask", "Broken_Pixel", "NoiseEffect", "FaceHalo"};
        private const string USAGE_MESSAGE = "mechuntextlogging [on|off]";

        private IModContext _context;

        private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;
        private static ComputerBossBehaviour _cbb;
        public bool logging = false;

        public void OnLoad(IModContext context)
        {
            _context = context;
            GameManager.Instance.onStateChanged += Behave;
            _context.Console.RegisterCommand("mechuntextlogging", "toggle debugging for MechagnusUntext",
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
        // if an existing run was loaded from the main menu. If so, call PossibleMechLoadOccured()
        private void Behave(State state)
        {
            State previousState = SingletonMonoBehaviour<GameManager>.Instance.PreviousState;
            if (previousState == State.LOAD_RUN) {
                if (state == State.BOARD_PLACEMENT || state == State.INGAME) {
                    PossibleMechLoadOccured(state, previousState);
                }
            } else if (previousState == State.SHOP && state == State.BOARD_PLACEMENT) {
                PossibleMechLoadOccured(state, previousState);
            }
        }

        // Simply does a log and begins the coroutine of disabling text
        private void PossibleMechLoadOccured(State state, State previousState)
        {
            if (logging) 
                _context.Console.PrintInfo($"State changed to {state.ToString()} from {previousState.ToString()}.");
            GameManager.Instance.StartCoroutine(CO_DisableText(0.3f));
        }

        // Waits a given amount of time, then searches the game for a currently loaded instance
        // of ComputerBossBehaviour. Logs whether or not it was found. If so, begins the recursive
        // function finding and disabling certain transform nodes. Since it starts a recursive function,
        // the log after has to be in this function to ensure it only runs once.
        private IEnumerator CO_DisableText(float delay)
        {
            yield return new WaitForSeconds(delay);
            _cbb = UnityEngine.Object.FindObjectOfType<ComputerBossBehaviour>();
            bool found = _cbb != null;
            if (logging)
                _context.Console.PrintInfo($"After {delay} seconds, Mechagnus was "+(found?"found, attempting text disable":"not found."));
            if (found)
            {
                int removedNodes = DisableText(_cbb.transform);
                if (logging)
                    _context.Console.PrintInfo($"Disabled {removedNodes} nodes.");
            }
        }

        // Function that recursively searches a transform node and all its children.
        // For each node found, if checks if the name is in the list of nodes to remove.
        // If so, they are set as inactive.
        private int DisableText(Transform node) {
            int removedNodes = 0;
            if (Array.IndexOf(REMOVED_NODES, node.name) >= 0) {
                node.gameObject.SetActive(false);
                removedNodes++;
            }
            for (int i = 0; i < node.childCount; i++)
                removedNodes += DisableText(node.GetChild(i));
            return removedNodes;
        }
        
        public void OnDisable() {
            GameManager.Instance.onStateChanged -= Behave;
            _context.Console.UnregisterCommand("mechuntextlogging");
        }
    }
}

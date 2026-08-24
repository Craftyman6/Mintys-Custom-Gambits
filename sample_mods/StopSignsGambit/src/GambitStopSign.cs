using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.StopSignsGambit
{
    /// <summary>
    /// Stop Sign's Gambit behaviour.
    ///
    /// Any time the player waits, the call to skip the enemy turn is run.
    /// </summary>
    public sealed class GambitStopSign : BaseGambit
    {
        private bool _subscribed;

        private void Start()
        {
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;

            // Create an instance of the wait manager to subscribe to
            WaitManager instance = SingletonMonoBehaviour<WaitManager>.Instance;
            if (instance == null) return;

            // Vanilla code often uses Delegate.Combine/Remove instead of +=/-=, so
            // we mirror that style. It is equivalent to subscribing to an event, but
            // works because OnWait is a public Action field, not a C# event.
            instance.OnWait = (Action)Delegate.Combine(instance.OnWait, new Action(Behave));
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (!(bool)SingletonMonoBehaviour<WaitManager>.Instance) return;

            WaitManager instance = SingletonMonoBehaviour<WaitManager>.Instance;
            if (instance == null) return;

            // Always unsubscribe in OnDestroy. Otherwise old handlers can survive on
            // the manager and keep calling into destroyed gambit objects after the run
            // changes or the mod is disabled.
            instance.OnWait = (Action)Delegate.Remove(instance.OnWait, new Action(Behave));
            _subscribed = false;
        }

        private void Behave()
        {
            try {SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn(); }
            catch {return; }

            Trigger();
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

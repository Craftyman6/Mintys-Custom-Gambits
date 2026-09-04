using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.CrumbleApi;
using UnityEngine;

namespace Gambonanza.TimeLoopsGambit
{
    /// <summary>
    /// Time Loop's Gambit behaviour.
    /// </summary>
    public sealed class GambitTimeLoop : BaseGambit
    {
        private CrumbleHandle _block;
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
            
            _block = Crumble.Block(this, "Time Loop's Gambit");

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            _block?.Dispose();

            _subscribed = false;
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

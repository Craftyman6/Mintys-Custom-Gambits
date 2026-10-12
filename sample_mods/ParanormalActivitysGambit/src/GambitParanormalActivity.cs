using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.ParanormalActivitysGambit
{
    /// <summary>
    /// Paranormal Activity's Gambit behaviour.
    /// 
    /// Triggers whenever a gambit (that's not Ghost's Gambit) is sold. On trigger, 10/11 chance to
    /// give Ghost's Gambit, and 1/11 chance to give Demon's Gambit.
    /// </summary>
    public sealed class GambitParanormalActivity : BaseGambit
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

            // Assign the class' Behave() method to the game's actions
		    SellManager instance = SingletonMonoBehaviour<SellManager>.Instance;
		    instance.OnSellGambit = (Action<GambitBehaviour>)Delegate.Combine(instance.OnSellGambit, new Action<GambitBehaviour>(CO_Behave));

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' Behave() method to the game's actions
			SellManager instance = SingletonMonoBehaviour<SellManager>.Instance;
		    instance.OnSellGambit = (Action<GambitBehaviour>)Delegate.Remove(instance.OnSellGambit, new Action<GambitBehaviour>(CO_Behave));

            _subscribed = false;
        }

        private void CO_Behave(GambitBehaviour gambit)
        {
            base.StartCoroutine(Behave(gambit));
        }

        private IEnumerator Behave(GambitBehaviour gambit)
        {
            // Don't give a ghost if a ghost is sold
            if (gambit.Info.ToString().Split(' ')[0] != "Gambit_Ghost")
            {
                // Wait for gambit to be sold
                yield return new WaitForSeconds(0.1f);
                // Theoritically this would never not be the case, but juuuuuust in case.
                if (!SingletonMonoBehaviour<GambitManager>.Instance.IsFull())
                {
                    Effect();
                }
            }
        }

        public void Effect()
        {
            // Randomly select gambit to give
            string gambitToGive = ChanceManager.Instance.ComputeChance((float)1, (float)11, "PARANORMAL_ACTIVITY_OCCURRANCE")?"demon":"ghost";
            // Give gambit
            SingletonMonoBehaviour<GambitLibrary>.Instance.SpawnGambit(gambitToGive, base.transform);
            // BOING!!
            Trigger();
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.GoldenRoadsGambit
{
    /// <summary>
    /// Golden Road's Gambit behaviour.
    /// 
    /// Whenever a promotion happens, the gambit checks if
    /// it was promoted into a king. If so, it turns every
    /// present tile golden, rewards kings until the stock
    /// is full, spawns Dainsleif's Gambit, and removes itself
    /// </summary>
    public sealed class GambitGoldenRoad : BaseGambit
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
		    PromotionManager instance = SingletonMonoBehaviour<PromotionManager>.Instance;
		    instance.OnPromotePlayer = (Action<PieceType>)Delegate.Combine(instance.OnPromotePlayer, new Action<PieceType>(Behave));

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' Behave() method to the game's actions
			PromotionManager instance = SingletonMonoBehaviour<PromotionManager>.Instance;
			instance.OnPromotePlayer = (Action<PieceType>)Delegate.Remove(instance.OnPromotePlayer, new Action<PieceType>(Behave));

            _subscribed = false;
        }

        private void Behave(PieceType pieceType)
        {
            if (pieceType == PieceType.KING)
            {
                Effect();
            }
        }

        public void Effect()
        {
            // Change every tile to Golden
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    TileBehaviour tileBehaviour = SingletonMonoBehaviour<BoardManager>.Instance.Board[i, j];
                    tileBehaviour.TurnToGold(true);
                }
            }
            // Fill stock with kings
            while (SingletonMonoBehaviour<StockManager>.Instance.RoomAvailable())
            {
                SingletonMonoBehaviour<StockManager>.Instance.AddPiece(PieceType.KING, base.transform.position);
            }
            // Give Dainsleif's Gambit
            GetComponentInParent<GambitPlaceBehaviour>().CurrentGambit = null;
            SingletonMonoBehaviour<GambitLibrary>.Instance.SpawnGambit("dainsleif", base.transform);
            // Remove self
            UnityEngine.Object.Destroy(base.gameObject);
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

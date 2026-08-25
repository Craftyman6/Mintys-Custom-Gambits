using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.SpeedLimitsGambit
{
    /// <summary>
    /// Speed Limit's Gambit behaviour.
    ///
    /// Any time a piece is moved, check Point A Manager to see if the
    /// piece was moved only one tile to reward $1. If it was instead
    /// moved 3 or more tiles, charge $3.
    /// </summary>
    public sealed class GambitSpeedLimit : BaseGambit
    {
        [SerializeField]
        private int VALUE_TO_EARN = 1;

        [SerializeField]
        private int VALUE_TO_LOSE = 3;

        private bool _subscribed;

        // Behave() uses this to tell Trigger() whether to reward or remove money
        private bool RewardWithMoney;

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

            // Assign class' Behave() method to the game's piece move action
            SelectionManager.Instance.OnMove += CO_Behave;
            // In case you got this gambit mid-game, Populate PointAManager's pieceTracker
            PointAManager.Instance.InstantFill();

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            // Unassign class' Behave() method to the game's piece move action
            SelectionManager.Instance.OnMove -= CO_Behave;

            _subscribed = false;
        }

        private void CO_Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            base.StartCoroutine(Behave(piece, tile, 0.1f));
        }

        private IEnumerator Behave(BasePieceBehaviour piece, TileBehaviour tile, float delay)
        {
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            // Find the displacement of the piece's move
            (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);
            // Check if piece moved only one tile
            if (Math.Abs(delta.x) < 2 && Math.Abs(delta.y) < 2)
            {
                RewardWithMoney = true;
                Trigger();
            }
            // Check if piece moved 3 or more tiles
            else if (Math.Abs(delta.x) > 2 || Math.Abs(delta.y) > 2)
            {
                RewardWithMoney = false;
                Trigger();
            }
        }

        public override void Trigger()
        {
            if (RewardWithMoney)
            {
                SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(VALUE_TO_EARN);
                SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, VALUE_TO_EARN);
            }
            else if (SingletonMonoBehaviour<ChessDataManager>.Instance.Coins <= 0)
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
            else
            {
                SingletonMonoBehaviour<ChessDataManager>.Instance.DecreaseCoin(VALUE_TO_LOSE);
            }
            // BOING!!
            try { VisualEffect(); } catch { }
        }
    }
}

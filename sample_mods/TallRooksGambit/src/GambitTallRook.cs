using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.TallRooksGambit
{
    /// <summary>
    /// Tall Rook's Gambit behaviour.
    /// 
    /// Subscribes to player OnMove action. Any time a player moves
    /// a piece, it checks if it's a rook. If it is, it check if it
    /// moved 4 or more tiles. If so, skips the enemy turn.
    /// </summary>
    public sealed class GambitTallRook : BaseGambit
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
            // Immediately return if not a rook
            if (piece.GetPieceType() != PieceType.ROOK) {return; }
            
            base.StartCoroutine(Behave(piece, tile, 0.1f));
        }

        private IEnumerator Behave(BasePieceBehaviour piece, TileBehaviour tile, float delay)
        {
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            // Find the displacement of the piece's move
            (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);
            // Check if piece moved 4 or more tiles
            if (Math.Abs(delta.x) > 3 || Math.Abs(delta.y) > 3)
            {
                Trigger();
            }
        }

        public override void Trigger()
        {
            // Skip enemy turn
            SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn(); 
            // BOING!!
            try { VisualEffect(); } catch { }
        }
    }
}

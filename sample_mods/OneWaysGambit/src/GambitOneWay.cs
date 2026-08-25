using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.OneWaysGambit
{
    /// <summary>
    /// One Way's Gambit behaviour.
    ///
    /// Any time a piece is moved, check Point A Manager to see if the
    /// piece was moved east. If so, apply the protective modifier to it
    /// </summary>
    public sealed class GambitOneWay : BaseGambit
    {
        private bool _subscribed;

        private BasePieceBehaviour m_Piece;

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
            // Assign class' piece variable to the moved piece
            m_Piece = piece;
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            // Find the displacement of the piece's move
            (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);
            // Check if piece was move to the right/East to trigger
            if (delta.x > 0)
            {
                Trigger();
            }
        }

        public override void Trigger()
        {
            // Set the piece's modifier to be protective
            try { m_Piece.Modifier.Protect(); } catch { }
            // BOING!!
            try { VisualEffect(); } catch { }
        }
    }
}

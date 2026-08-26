using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.RoadWorkAheadsGambit
{
    /// <summary>
    /// Road Work Ahead's Gambit behaviour.
    /// 
    /// Any time an enemy piece is captured, set the tile
    /// above it to be crumbled.
    /// </summary>
    public sealed class GambitRoadWorkAhead : BaseGambit
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
		    SelectionManager.Instance.OnCapture += CO_Behave;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' Behave() method to the game's actions
            SelectionManager.Instance.OnCapture -= CO_Behave;
            // Note that we don't set CrumbleManager.CrumblerInGame to false since
            // if there were a crumbler in the game it would stop functioning correctly.
            // The variable will get set back to false on its own.

            _subscribed = false;
        }

        private void CO_Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour tile)
        {
            Behave(tile);
        }

        private void Behave(TileBehaviour tile)
        {
            // This is to get the tile above/North of the captured tile
            // Same process that TileBehaviour.GetNeighbourTiles() uses
            int layerMask = 64;
            RaycastHit2D raycastHit2D = Physics2D.Raycast((Vector2)tile.transform.position + Vector2.up, Vector3.forward, 1f, layerMask);
            if (raycastHit2D.collider == null) {return; }
            TileBehaviour aheadTile = raycastHit2D.transform.GetComponent<TileBehaviour>();
            if (!aheadTile.IsStock)
            {
                // Now that the tile is found, we must crumble it.
                // In order for the crumble to happen, CrumbleManager.CO_MakeShakingTileFall()
                // must be called from CrumbleManager.PlayerTurnEffects(), and it will only
                // call it under certain circumstances. The CrumbleManager.CrumblerInGame
                // variable is one of those circumstances, so we enable that before triggering
                // to ensure the crumble happens, and leave it on. As far as I know, this doesn't
                // have any adverse effects.
                SingletonMonoBehaviour<CrumbleManager>.Instance.CrumblerInGame = true;
                SingletonMonoBehaviour<CrumbleManager>.Instance.CrumblerEffect(aheadTile); 

                Trigger();

            }


        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

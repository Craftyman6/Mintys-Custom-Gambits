using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.GumbosGambit
{
    /// <summary>
    /// Gumbo's Gambit behaviour.
    /// 
    /// Subscribes to the OnFall action, and sets the tile below/under the fallen tile
    /// as crumbling when called.
    /// </summary>
    public sealed class GambitGumbo : BaseGambit
    {
        private bool _subscribed;
        private bool triggeredThisTurn = false;

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

            // Assign the class' methods to the game's actions
		    CrumbleManager.Instance.OnFall += CO_Behave;
            GameManager.Instance.onStateChanged += CO_Uncrumble;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' methods to the game's actions
            CrumbleManager.Instance.OnFall -= CO_Behave;
            GameManager.Instance.onStateChanged -= CO_Uncrumble;

            _subscribed = false;
        }

        private void CO_Behave(TileBehaviour tile)
        {
            base.StartCoroutine(Behave(tile));
        }
        private IEnumerator Behave(TileBehaviour tile)
        {
            // This is to get the tile below/South of the fallen tile
            // Same process that TileBehaviour.GetNeighbourTiles() uses
            int layerMask = 64;
            RaycastHit2D raycastHit2D = Physics2D.Raycast((Vector2)tile.transform.position + Vector2.down, Vector3.forward, 1f, layerMask);
            if (raycastHit2D.collider != null) 
            {
                TileBehaviour belowTile = raycastHit2D.transform.GetComponent<TileBehaviour>();
                if (!belowTile.IsStock) 
                {
                    // Right after the OnFall action is run, the CrumbleMananager.m_CrumbleTiles
                    // list is cleared. We need this crumbling tile to be in that list, so we
                    // wait until after it is cleared.
                    yield return new WaitForSeconds(0.1f);
                
                    // Now that the tile is found, we must set it to be crumbling.
                    // Because this method runs with the OnFall action, the newly
                    // crumbling tile will not fall until next turn, which is what
                    // we want. However, only under certain circumstances will crumbling tiles fall.
                    // The CrumbleManager.CrumblerInGame variable is one of those circumstances, 
                    // so we enable that to ensure the crumble happens next turn. In some testing,
                    // capturing a crumbler piece can turn this variable false and stop these
                    // crumbling tiles from falling until crumble mode it reached, but in
                    // recent testing, that has not been the case.
                    SingletonMonoBehaviour<CrumbleManager>.Instance.CrumblerInGame = true;
                    SingletonMonoBehaviour<CrumbleManager>.Instance.CrumblerEffect(belowTile); 

                    // Only run the trigger function if it hasn't been run this
                    // turn. This way the sound doesn't play multiple times over.
                    if (!triggeredThisTurn)
                    {
                        Trigger();
                        triggeredThisTurn = true;
                        base.StartCoroutine(RefreshTrigger());
                    }
                }
            }
        }

        // Since the gambit trigger waits a moment to set the tiles as
        // crumbling, if it triggers as a game is finished, the tile is
        // set as crumbling after CrumbleManager.ResetTiles() is called,
        // meaning it'd still be crumbling after the game is
        // finished. This prevents that by reseting the tiles a second
        // after the gambit activation.
        private void CO_Uncrumble(State state)
        {
            base.StartCoroutine(Uncrumble(state));
        }
        private IEnumerator Uncrumble(State state)
        {
            if (state == State.RESULT)
            {
                yield return new WaitForSeconds(1f);
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        SingletonMonoBehaviour<BoardManager>.Instance.Board[i, j].StopCrumble();
                    }
                }
            }
        }

        // Waits one second, then allows the trigger function to run again.
        private IEnumerator RefreshTrigger()
        {
            yield return new WaitForSeconds(1.0f);
            triggeredThisTurn = false;
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

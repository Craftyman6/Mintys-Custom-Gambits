using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.TempWorksGambit
{
    /// <summary>
    /// Temp Work's Gambit behaviour.
    /// 
    /// This gambit triggers when any game is finished. When it does, it searches the
    /// board for any player queen (or bishop is Pope's gambit is owned). When it finds
    /// one, it then searches for any adjacent player piece and check if it's a pawn.
    /// If it finds a pawn adjacent to a queen, it'll reward the player with $10 (it
    /// does not reward multiple times for multiple queens adjacent to pawns)
    /// </summary>
    public sealed class GambitTempWork : BaseGambit
    {
        private bool _subscribed;
        private int VALUE_TO_EARN = 10;

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
            GameManager.Instance.onStateChanged += CO_Behave;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' methods to the game's actions
            GameManager.Instance.onStateChanged -= CO_Behave;

            _subscribed = false;
        }
        
        private void CO_Behave(State state)
        {
            // Stolen from Shadow's Gambit
            if (SingletonMonoBehaviour<GameManager>.Instance.PreviousState != State.PAUSE && SingletonMonoBehaviour<GameManager>.Instance.PreviousState != State.RUN_INFO && state == State.WIN)
            {
                base.StartCoroutine(Behave(state));
            }
        }
        private IEnumerator Behave(State state)
        {
            yield return new WaitForSeconds(0.3f);
            bool toTrigger = false;
            bool pope = SingletonMonoBehaviour<GambitManager>.Instance.PopeEnable;
            foreach (BasePieceBehaviour piece in SingletonMonoBehaviour<PieceManager>.Instance.GetWhitePieces())
            {
                if (piece.GetPieceType() != PieceType.QUEEN && !(piece.GetPieceType() == PieceType.BISHOP && pope)) {continue; }
                foreach (TileBehaviour tile in piece.GetNeighbourTiles())
                {
                    if (tile.Piece.GetPieceType() != PieceType.PAWN || tile.Piece.PieceColor != PieceColor.WHITE) {continue; }

                    toTrigger = true;
                }
            }
            if (toTrigger) {Trigger(); }
        }

        public override void Trigger()
        {
            // Give money and produce money effect
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(VALUE_TO_EARN);
            SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, VALUE_TO_EARN);
            // BOING!!
            try { VisualEffect(); } catch { }
        }
    }
}

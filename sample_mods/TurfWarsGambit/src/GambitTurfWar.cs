using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.TurfWarsGambit
{
    /// <summary>
    /// Turf War's Gambit behaviour.
    /// 
    /// When the game begins, all tiles with pieces on them are set to the
    /// same color as the piece. When a piece moves, the new tile is set
    /// to the piece's color. When a piece captures, all adjacent tiles are
    /// set to the capturing piece's color. At the end of the round, rewards
    /// the player with $1 for every 5 white tiles on the board (including
    /// crumbled tiles).
    /// </summary>
    public sealed class GambitTurfWar : BaseGambit
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

            // Assign the class' methods to their respective game action
            SelectionManager.Instance.OnMove += WhiteMove;
            SelectionManager.Instance.OnCapture += WhiteCapture;
            EnemyManager.Instance.OnMove += BlackMove;
            EnemyManager.Instance.OnCapture += BlackCapture;
            GameManager.Instance.onStateChanged += GameStateChange;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' methods from their respective game action
            SelectionManager.Instance.OnMove -= WhiteMove;
            SelectionManager.Instance.OnCapture -= WhiteCapture;
            EnemyManager.Instance.OnMove -= BlackMove;
            EnemyManager.Instance.OnCapture -= BlackCapture;
            GameManager.Instance.onStateChanged -= GameStateChange;

            _subscribed = false;
        }

        // Changes a tile's color to white
        private void WhiteMove(BasePieceBehaviour piece, TileBehaviour tile)
        {
            if ((int)piece.PieceColor == (int)tile.TileColor) {return; }
            tile.ChangeColor(TileColor.WHITE);
            Trigger();
        }

        // Changes a tile's neighboring tiles to white
        private void WhiteCapture(BasePieceBehaviour capturingPiece, BasePieceBehaviour capturedPiece, TileBehaviour tile)
        {
            foreach (TileBehaviour neighbourTile in GetNeighbourTiles(tile))
            {
                WhiteMove(capturingPiece, neighbourTile);
            }
        }

        // Changes a tile's color to black
        private void BlackMove(BasePieceBehaviour piece, TileBehaviour tile)
        {
            if ((int)piece.PieceColor == (int)tile.TileColor) {return; }
            tile.ChangeColor(TileColor.BLACK);
            Trigger();
        }

        // Changes a tile's neighboring tiles to black
        private void BlackCapture(BasePieceBehaviour capturingPiece, BasePieceBehaviour capturedPiece, TileBehaviour tile)
        {
            foreach (TileBehaviour neighbourTile in GetNeighbourTiles(tile))
            {
                BlackMove(capturingPiece, neighbourTile);
            }
        }

        // Used for both when the game begins (to change tile colors)
        // and for when the game ends (to reward money)
        private void GameStateChange(State state)
        {
            if (SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.PAUSE) {return; }
            if (SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.RUN_INFO) {return; }
            // Check if board formation phase was exited in order to apply colors
            if (state == State.INGAME && SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.BOARD_PLACEMENT)
            {
                for (int x = 0; x < 5 + SingletonMonoBehaviour<BoardManager>.Instance.ColumnAdded; x++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        TileBehaviour tile = SingletonMonoBehaviour<BoardManager>.Instance.Board[y, x];
                        if (tile.Piece == null) {continue; }
                        if (tile.Piece.PieceColor == PieceColor.WHITE)
                        {
                            WhiteMove(tile.Piece, tile);
                        }
                        else if (tile.Piece.PieceColor == PieceColor.BLACK)
                        {
                            BlackMove(tile.Piece, tile);
                        }
                    }
                }
            }
            // Check if game was ended in order to reward money
            else if (state == State.WIN)
            {
                int whiteTotal = 0;
                for (int x = 0; x < 5 + SingletonMonoBehaviour<BoardManager>.Instance.ColumnAdded; x++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        TileBehaviour tile = SingletonMonoBehaviour<BoardManager>.Instance.Board[y, x];
                        whiteTotal += tile.TileColor == TileColor.WHITE ? 1 : 0;
                    }
                }
                int moneyToReward = whiteTotal / 5;
                SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(moneyToReward);
                SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, moneyToReward);
            }
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }

        // Literally copy and paste function from TileBehaviour.GetNeighborTiles(), but
        // without the check for if the tile has a piece on it.
        private List<TileBehaviour> GetNeighbourTiles(TileBehaviour tile)
        {
            List<TileBehaviour> list = new List<TileBehaviour>();
            int layerMask = 64;
            foreach (Vector2 item in new List<Vector2>
            {
                Vector2.down,
                Vector2.up,
                Vector2.right,
                Vector2.left
            })
            {
                RaycastHit2D raycastHit2D = Physics2D.Raycast((Vector2)tile.transform.position + item, Vector3.forward, 1f, layerMask);
                if (raycastHit2D.collider != null)
                {
                    TileBehaviour component = raycastHit2D.transform.GetComponent<TileBehaviour>();
                    if (!component.IsStock)
                    {
                        list.Add(component);
                    }
                }
            }
            return list;
        }
    }
}

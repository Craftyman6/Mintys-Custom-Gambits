using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using Blukulele.SaveSystem;
using Gambonanza.ModSdk;
using UnityEngine;
using DG.Tweening;
using System.Text.RegularExpressions;

namespace Gambonanza.StockSwap
{
    internal sealed class StockSwapRunner : MonoBehaviour
    {
        public IModContext _context;

        // Called every frame
        private void Update()
        {
            if (_context != null && _context.WasKeybindPressed("swapStock"))
                PressButton();
        }

        // Called when the keybind is pressed
        private void PressButton()
        {
            // Exit if not in shop nor board formation state
            if (!(SingletonMonoBehaviour<GameManager>.Instance.CurrentState == State.SHOP) &&
            !(SingletonMonoBehaviour<GameManager>.Instance.CurrentState == State.BOARD_PLACEMENT))
            {
                return;
            }

            // Find piece currently being hovered over
            // Code found from SelectionManager.HandleMovementInPiecePlacement
            BasePieceBehaviour foundPiece = null;
            Vector3 pointerPosition = PointerPosition;
            RaycastHit2D hit_Piece = Physics2D.Raycast(pointerPosition, Vector3.forward, 20f, 256);
            if (hit_Piece.collider != null)
                foundPiece = hit_Piece.collider.GetComponent<BasePieceBehaviour>();
            if (!(bool)foundPiece)
            {
                return;
            }

            PressPiece(foundPiece);
        }

        // Called when keybind is pressed on a piece
        private void PressPiece(BasePieceBehaviour piece)
        {
            // Exit if an enemy piece was clicked
            if (piece.PieceColor == PieceColor.BLACK)
                return;
            if (piece.InStock)
            {
                // Move piece from stock to board
                // Code from StockManager.AddPiece

                // Find an open board tile
                TileBehaviour[,] board = SingletonMonoBehaviour<BoardManager>.Instance.Board;
                int upperBound = board.GetUpperBound(0);
                int upperBound2 = board.GetUpperBound(1);
                for (int i = upperBound; i >= board.GetLowerBound(0); i--)
                {
                    int num = board.GetLowerBound(1);
                    TileBehaviour tileBehaviour;
                    while (num <= upperBound2)
                    {
                        tileBehaviour = board[i, num];
                        if (!tileBehaviour.CanBeLandedOn || tileBehaviour.IsOccupied || !tileBehaviour.gameObject.activeInHierarchy)
                        {
                            num++;
                            continue;
                        }
                        goto IL_00a6;
                    }
                    continue;
                    IL_00a6:

                    // Clear stock tile of piece reference
                    TileBehaviour[] Places = SingletonMonoBehaviour<StockManager>.Instance.Places;
                    BasePieceBehaviour[] Pieces = SingletonMonoBehaviour<StockManager>.Instance.Pieces;
                    for (int j = 0; j < Pieces.Length; j++) {
                        if (Pieces[j] == piece) {
                            Pieces[j] = null;
                            Places[j].Piece = null;
                            break;
                        }
                    }

                    // Move the piece to the new tile
                    piece.InStock = false;
                    piece.CurrentTile.Piece = null;
                    piece.CurrentTile = tileBehaviour;
                    tileBehaviour.Piece = piece;
                    piece.transform.SetParent(tileBehaviour.transform, true);
                    piece.transform.DOFollow(tileBehaviour.PlaceToPutPieces, 0.1f);
                    SingletonMonoBehaviour<SelectionManager>.Instance?.OnPutPieceOnBoard?.Invoke(piece);
                    break;
                }
            }
            else
            {
                // Move piece from board to stock

                TileBehaviour[] Places = SingletonMonoBehaviour<StockManager>.Instance.Places;
                BasePieceBehaviour[] Pieces = SingletonMonoBehaviour<StockManager>.Instance.Pieces;
                for (int j = 0; j < Places.Length; j++)
                {
                    if (Places[j].GetComponentInChildren<BasePieceBehaviour>() == null && Pieces[j] == null)
                    {
                        StockBlockBehaviour component = Places[j].GetComponent<StockBlockBehaviour>();
                        if (component != null && component.IsActive)
                        {
                            return;
                        }
                        piece.CurrentTile.Piece = null;
                        piece.InStock = true;
                        Pieces[j] = piece;
                        Pieces[j].CurrentTile = Places[j];
                        piece.CurrentTile = Places[j];
                        Places[j].Piece = piece;
                        piece.transform.SetParent(Places[j].transform, true);
                        piece.transform.DOFollow(Places[j].PlaceToPutPieces, 0.1f);
                        SingletonMonoBehaviour<SelectionManager>.Instance?.OnPutPieceInStock?.Invoke(piece);
                        break;
                    }
                }
            }
        }

        // Method found from SelectionManager
        public Vector3 PointerPosition
        {
            get
            {
                if (ControlSchemeManager.CurrentControlScheme == EControlScheme.Gamepad)
                {
                    return SingletonMonoBehaviour<GamePadSelectionManager>.Instance.Position;
                }
                return SingletonMonoBehaviour<CameraManager>.Instance.MainCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
    }
}
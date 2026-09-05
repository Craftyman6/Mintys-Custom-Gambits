using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.PrincesGambit
{
    /// <summary>
    /// Prince's Gambit behaviour.
    /// 
    /// When a game is finished, the positions of all the tile modifications
    /// on the board are shuffled. The gambit keeps track of how many times
    /// the player steps on a modified tile. When that count reaches 46,
    /// the player earns $100, Inheritence's Gambit, and then Prince's Gambit
    /// self destructs.
    /// </summary>
    public sealed class GambitPrince : BaseGambit
    {
        private bool _subscribed;
        private int _count = 0;
        private int VALUE_TO_EARN = 100;
        private List<string> modificationPool = new List<string>();

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
            GameManager.Instance.onStateChanged += Shuffle;
            SelectionManager.Instance.OnMoveOnModifiedTile += Count;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            // Unassign the class' Behave() method to the game's actions
            GameManager.Instance.onStateChanged -= Shuffle;
            SelectionManager.Instance.OnMoveOnModifiedTile -= Count;
            // Reset count
            _count = 0;

            _subscribed = false;
        }

        // Shuffles the positions of every modified tile on the board
        private void Shuffle(State state)
        {
            // Ensure state change is end of game
            if (state != State.SHOP || !(SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.WIN || SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.GRAVEYARD)) {return; }

            // Fill a string list of every tile modification on the board then turn every tile default
            modificationPool.Clear();
            for (int x = 0; x < 5 + SingletonMonoBehaviour<BoardManager>.Instance.ColumnAdded; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    TileBehaviour tile = SingletonMonoBehaviour<BoardManager>.Instance.Board[y, x];
                    if (!tile.IsModified()) {modificationPool.Add("default"); }
                    else if (tile.IsGolden) {modificationPool.Add("golden"); }
                    else if (tile.IsProtection) {modificationPool.Add("protection"); }
                    else if (tile.IsBenediction) {modificationPool.Add("benediction"); }
                    else if (tile.IsHunter) {modificationPool.Add("hunter"); }
                    else if (tile.IsPhantom) {modificationPool.Add("phantom"); }
                    else {modificationPool.Add("default"); }

                    tile.TurnToDefault();
                }
            }

            // Iterate through every tile and give it a random modification from the list
            for (int x = 0; x < 5 + SingletonMonoBehaviour<BoardManager>.Instance.ColumnAdded; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    if (modificationPool.Count <= 0) {continue; }
                    TileBehaviour tile = SingletonMonoBehaviour<BoardManager>.Instance.Board[y, x];
                    var random = new System.Random();
                    int index = random.Next(modificationPool.Count);
                    switch (modificationPool[index])
                    {
                        case "default":
                            break;
                        case "golden":
                            tile.TurnToGold(false);
                            break;
                        case "protection":
                            tile.TurnToShield(false);
                            break;
                        case "benediction":
                            tile.TurnToBenediction(false);
                            break;
                        case "hunter":
                            tile.TurnToHunter(false);
                            break;
                        case "phantom":
                            tile.TurnToPhantom(false);
                            break;
                    }
                    modificationPool.RemoveAt(index);
                }
            }

            // BOING!!
            Trigger();
        }

        public void Count(TileBehaviour _)
        {
            if (++_count >= 46)
            {
                // Give money and produce money effect
                SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(VALUE_TO_EARN);
                SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, VALUE_TO_EARN);
                // Give Inheritance's Gambit
                GetComponentInParent<GambitPlaceBehaviour>().CurrentGambit = null;
                SingletonMonoBehaviour<GambitLibrary>.Instance.SpawnGambit("greed", base.transform);
                // Remove self
                UnityEngine.Object.Destroy(base.gameObject);
            }
            else
            {
                m_FeedbackIncrementor.Spawn("Room "+_count);
			    m_FeedbackIncrementor.IncrementSound();
            }
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

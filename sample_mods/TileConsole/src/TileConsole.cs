using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.ModSdk;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Gambonanza.TileConsole
{
    /// <summary>
    /// Registers a command named "tile" to the console. This command
    /// is to be used to modify/color/crumble a tile. This can also be
    /// applied to a whole row/column of tiles, or even every tile on
    /// the board.
    /// 
    /// The first argument of the command specifies which tiles are to
    /// be effected. In order to use this effectively, enable the toggle
    /// in the game's settings to display tile coordinates. Set the
    /// argument to "all" to modify every tile on the board. Set the
    /// argument to a number between 1 and 5 to modify an entire row
    /// of tiles. Set the the argument to a letter between a and h to
    /// modify an entire column of tiles. Set the argument to a cordinate
    /// (e.g "a1", "h5", "b4", "d3") to modify a specific tile on the board.
    /// 
    /// The second argument of the command specifies what will be done
    /// to the tile(s). Set the argument to a tile modifier ("golden",
    /// "protective", "blessing", "trap", "phantom", "cursed") to modify
    /// the tile(s) to the respective modification. Set the argument to
    /// a color ("black", "white") to modify the tile color of the tile(s).
    /// Set the argument to "crumble" to crumble the tile(s). Set the
    /// argument to "default" to remove tile modification and reappear a
    /// crumbled tile.
    /// </summary>
    public sealed class TileConsoleMod : IMod
    {
        private IModContext _context;
        [SerializeField]
        private string USAGE_MESSAGE = "tile [all|a1-h5|a-h|1-5] [golden|protective|blessing|trap|phantom|cursed|white|black|crumble|default]";
        [SerializeField]
        private string[] MODIFICATIONS = {"golden", "protective", "blessing", "trap", "phantom", "cursed", "white", "black", "crumble", "default"} ;
        // List of tiles to modify. When a command is run, the list is
        // cleared, filled, then iterated through for modification. List
        // type is a custom object type created for this mod.
        private List<TileCoordinate> tilesToModify = new List<TileCoordinate>();
        // Data collected during the argument reading process that's used to
        // give better dictation during the success console message
        //                          all    row    column
        private bool[] printData = {false, false, false};

        public void OnLoad(IModContext context)
        {
            _context = context;
            var console = _context.Console;
            if (console == null)
            {
                context.LogLine("TileConsole could not create new console instance.");
                return;
            }

            // Register the new command with its name, help message,
            // action when run, and tab completers
            //                                        vvv usage message starts with "tile" anyways
            console.RegisterCommand("tile", "modify a "+USAGE_MESSAGE,
            args =>
            {
                // Exit if there are more or less than 2 arguments
                if (args.Length != 2)
                {
                    console.PrintWarn("Input invalid. Usage: "+USAGE_MESSAGE);
                    return;
                }

                // Clear the tile list so that it may be filled
                tilesToModify.Clear();

                // Clear print data for success console printing accuracy
                for (int i = 0; i < 3; i++) {printData[i] = false;}

                // FIRST ARGUMENT
                // If the first argument is "all"
                if (printData[0] = args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    for (int x = 0; x < GetColumnCount(); x++)
                    {
                        for (int y = 0; y < 5; y++)
                        {
                            tilesToModify.Add(new TileCoordinate(x, y));
                        }
                    }
                }
                // If the first argument is a row/column
                else if (args[0].Length == 1)
                {
                    char lineHeader = char.ToLower(char.Parse(args[0]));
                    // If the first argument is a row
                    if (printData[1] = (lineHeader >= '1' && lineHeader <= '5'))
                    {
                        int y = lineHeader - '1';
                        int maxX = GetColumnCount();
                        // Add all tiles of the row to the tile list
                        for (int x = 0; x < maxX; x++)
                        {
                            tilesToModify.Add(new TileCoordinate(x, y));
                        }
                    }
                    // If the first argument is a column
                    else if (printData[2] = (lineHeader >= 'a' && lineHeader <= 'h'))
                    {
                        int x = lineHeader - 'a';
                        // Exit if column has yet to be formed
                        if (!IsColumnFormed(x))
                        {
                            console.PrintWarn("Column "+lineHeader+" has yet to be formed.");
                            return;
                        }

                        // Add all tiles of the column to the tile list
                        for (int y = 0; y < 5; y++) 
                        {
                            tilesToModify.Add(new TileCoordinate(x, y));
                        }
                    }
                    // Exit if first argument was a single character but
                    // not 1-5 or a-h
                    else
                    {
                        console.PrintWarn("Column/Row "+lineHeader+" not recognized.");
                        return;
                    }
                }
                // If the first argument is a tile coordinate
                else if (args[0].Length == 2)
                {
                    char columnChar = args[0][0];
                    char rowChar = args[0][1];
                    // Exit if the column value is invalid
                    if (columnChar < 'a' || columnChar > 'h')
                    {
                        console.PrintWarn("Column "+columnChar+" is not valid.");
                        return;
                    }
                    // Exit if the row value is invald
                    else if (rowChar < '1' || rowChar > '5')
                    {
                        console.PrintWarn("Row "+rowChar+" is not valid.");
                        return;
                    }
                    int x = columnChar - 'a';
                    int y = rowChar - '1';
                    // Exit if the column has yet to be formed
                    if (!IsColumnFormed(x))
                    {
                        console.PrintWarn("Column "+columnChar+" has yet to be formed.");
                        return;
                    }
                    // Add tile to the tile list
                    tilesToModify.Add(new TileCoordinate(x, y));
                }
                // Exit if first argument didn't match any of the three
                // criteria (all, row/column, nor coordinate).
                else
                {
                    console.PrintWarn("Tile location "+args[0]+" invalid. Usage: "+USAGE_MESSAGE);
                    return;
                }

                // SECOND ARGUMENT
                string secondArg = args[1].ToLower();
                // Exit if second argument is not a valid modification
                if (!MODIFICATIONS.Contains(secondArg))
                {
                    console.PrintWarn("Modification "+secondArg+" is not valid. Usage: "+USAGE_MESSAGE);
                    return;
                }
                
                // MODIFICATION
                // Iterate through all the tiles added to the tile list
                // and modify them accordingly (from the ModifyTile method).
                foreach (TileCoordinate tile in tilesToModify)
                {
                    try {ModifyTile(tile.x, tile.y, secondArg); }
                    catch (Exception ex)
                    {
                        console.PrintWarn("An error occured attempting to modify tile "+tile.toString()+".\n"+ex.Message+"\nExiting...");
                        return;
                    }
                }
                // Print success message, which depends on which tiles
                // were actually just modified.
                string whichTilesWereModified = printData[0] ? "All tiles"
                : printData[1] ? "Row "+args[0].ToLower()
                : printData[2] ? "Column "+args[0].ToLower()
                : "Tile "+args[0].ToLower();
                console.PrintInfo(whichTilesWereModified+" modified successfully!");
                console.Close();
            },

            // Register the tab completer. At the moment, the second
            // argument's tab completion is being registered has the
            // first argument.
            (args, argIndex) => 
                argIndex == 0 ? 
                (
                    new[] 
                    {
                        "all", "a1", "a2", "a3", "a4", "a5",
                        "b1", "b2", "b3", "b4", "b5",
                        "c1", "c2", "c3", "c4", "c5",
                        "d1", "d2", "d3", "d4", "d5",
                        "e1", "e2", "e3", "e4", "e5",
                        "f1", "f2", "f3", "f4", "f5",
                        "g1", "g2", "g3", "g4", "g5",
                        "h1", "h2", "h3", "h4", "h5",
                        "a", "b", "c", "d", "e", "f", "g", "h",
                        "1", "2", "3", "4", "5"
                    }
                )
                : 
                (
                    argIndex == 1 ? MODIFICATIONS : null
                )
            );
        }

        public void OnDisable() => _context?.Console.UnregisterCommand("tile");

        public bool IsColumnFormed(int column)
        {
            return column >= 0 && column < GetColumnCount();
        }

        public int GetColumnCount()
        {
            return 5 + SingletonMonoBehaviour<BoardManager>.Instance.ColumnAdded;
        }

        private void ModifyTile(int x, int y, string modification)
        {
            // Get TileBehavior object of the tile on the board
            // NOTE: the y coordinate actually goes top down instead of
            // bottom up. Coordinate needs reversed with 4 - y
            // NOTE: it's gotten with (y, x) and not (x, y). Whacky.
            TileBehaviour tile = SingletonMonoBehaviour<BoardManager>.Instance.Board[4 - y, x];

            // Modify tiles accordingly
            switch(modification)
            {
                case "golden":
                    tile.TurnToGold(true);
                    break;
                case "protective":
                    tile.TurnToShield(true);
                    break;
                case "blessing":
                    tile.TurnToBenediction(true);
                    break;
                case "trap":
                    tile.TurnToHunter(true);
                    break;
                case "phantom":
                    tile.TurnToPhantom(true);
                    break;
                case "cursed":
                    tile.TurnToCursed(true);
                    break;
                case "white":
                    tile.ChangeColorWhite();
                    break;
                case "black":
                    tile.ChangeColorBlack();
                    break;
                case "crumble":
                    tile.Fall(); 
                    break;
                case "default":
                    tile.TurnToDefault();
                    if (tile.HasFell) {tile.ReAppear();}
                    break;
                default:
                    throw new Exception("Tile modification type unrecognized.");
            }
        }
    }

    // Simple data object that holds a tile coordinate in the following format:
    // a1 -> (x: 0, y: 0) | h5 -> (x: 7, y: 4)
    // Contains a method for translating coordinate back into regualr name.
    // A list of these objects are collected as the first argument is being
    // read, which is then iterated through when those tiles are modified.
    public class TileCoordinate
    {
        public int x;
        public int y;
        private string[] COLUMN_NAMES = {"a", "b", "c", "d", "e", "f", "g" };
        public TileCoordinate(int x, int y)
        {
            this.x = (x >= 0 && x < 8) ? x : 0;
            this.y = (y >= 0 && y < 5) ? y : 0;
        }
        // Convert tile cordinate object to display cordinate string.
        // Converts y to column letter and adds one to x to get the
        // row (since internally, rows are 0-4 when externally they're 1-5).
        public string toString()
        {
            return COLUMN_NAMES[x]+(y+1);
        }
    }
}

// Done: Implement the "open lines" heuristic.
// An open line for a given player is a row, column, or diagonal in which no square is occupied by the opposing player.
// Value(Board, Player) = 100 if it is a win for Player;
// else Value(Board, Player) = -100 if it is a win for Opponent(Player);
// else Value(Board, Player) = NumOpenLines(Board, Player) - NumOpenLines(Board, Opponent(Player)).
// See http://www.csupomona.edu/~jrfisher/www/prolog_tutorial/5_3.html for an example in Prolog.
#define USE_OPEN_LINES_HEURISTIC

using System;
using System.Collections.Generic;
using System.ComponentModel;        // For BackgroundWorker
using System.Linq;
using System.Text;

namespace WPFTicTacToe.GameEngine
{
    public enum SquareContentType
    {
        EmptySquare = -1,
        X = 0,
        O = 1
    }

    public interface IGameWindow
    {
        void PlacePiece(SquareContentType piece, int row, int column);
        void DisplayMessage(string message);
    }

    public class BestMoveData
    {
        public readonly int score;
        public readonly int row;
        public readonly int column;

        public BestMoveData(int score, int row, int column)
        {
            this.score = score;
            this.row = row;
            this.column = column;
        }
    }

    public class GameEngine
    {
        public const int defaultBoardDimension = 3;
        public const int minimumBoardDimension = 3;     // I.e. the board has at least 3 rows and 3 columns.
        public const int maximumBoardDimension = 4;     // I.e. the board has at most 4 rows and 4 columns.
        public readonly int boardDimension;
        public readonly int boardArea;      // board.Length may make this unnecessary.
        public readonly SquareContentType[] board;
        public SquareContentType currentPlayer;
        public readonly Dictionary<SquareContentType, string> playerNameDictionary = new Dictionary<SquareContentType, string>();
        public readonly Dictionary<SquareContentType, bool> isAutomated = new Dictionary<SquareContentType, bool>();
        public const int defaultPly = 6;    // It is possible for the computer to lose if ply == 5.
        public const int minimumPly = 1;
        public const int maximumPly = 16;
        public readonly Dictionary<SquareContentType, int> playerPlyDictionary = new Dictionary<SquareContentType, int>();
        public IGameWindow gameWindow;
        public int boardPopulation;
        public bool isGameOver;
#if USE_OPEN_LINES_HEURISTIC
        public const int victoryValue = 100;
#else
        public const int victoryValue = 1;
#endif
        public const int defeatValue = -victoryValue;
        public readonly BackgroundWorker backgroundWorker;

        public GameEngine(IGameWindow gameWindow, int boardDimension, string boardAsString = null, BackgroundWorker backgroundWorker = null)
        {

            if (gameWindow == null)
            {
                throw new ArgumentNullException("gameWindow", "GameEngine constructor: The gameWindow parameter is null.");
            }

            this.gameWindow = gameWindow;

            if (boardDimension < minimumBoardDimension || boardDimension > maximumBoardDimension)
            {
                throw new ArgumentOutOfRangeException("boardDimension",
                    string.Format("GameEngine constructor: The given board dimension {0} is not in the range [{1}..{2}] inclusive.",
                        boardDimension, minimumBoardDimension, maximumBoardDimension));
            }

            this.boardDimension = boardDimension;
            this.boardArea = this.boardDimension * this.boardDimension;
            this.board = new SquareContentType[this.boardArea];         // The board is always square.
            this.playerNameDictionary[SquareContentType.X] = "X";
            this.playerNameDictionary[SquareContentType.O] = "O";
            this.isAutomated[SquareContentType.X] = true;
            this.isAutomated[SquareContentType.O] = false;
            this.playerPlyDictionary[SquareContentType.X] = defaultPly;
            this.playerPlyDictionary[SquareContentType.O] = defaultPly;
            this.backgroundWorker = backgroundWorker;
            BeginNewGame(boardAsString);
        }

        public void BeginNewGame(string boardAsString = null)
        {

            for (int i = 0; i < this.board.Length; ++i)
            {
                this.board[i] = SquareContentType.EmptySquare;
            }

            this.boardPopulation = 0;

            if (boardAsString != null)
            {

                if (boardAsString.Length != this.boardArea)
                {
                    throw new ArgumentException(
                        string.Format("boardAsString.Length is {0} instead of the expected {1}.", boardAsString.Length, this.boardArea),
                        "boardAsString");
                }

                // Deserialize boardAsString
                int stringIndex = 0;

                for (int row = 0; row < boardDimension; ++row)
                {

                    for (int column = 0; column < boardDimension; ++column)
                    {
                        SquareContentType squareContent = SquareContentType.EmptySquare;

                        switch (boardAsString[stringIndex])
                        {
                            case 'X':
                                squareContent = SquareContentType.X;
                                break;

                            case 'O':
                                squareContent = SquareContentType.O;
                                break;

                            default:
                                break;
                        }

                        if (squareContent != SquareContentType.EmptySquare)
                        {
                            PlacePiece(squareContent, row, column, false);
                        }

                        ++stringIndex;
                    }
                }
            }

            this.currentPlayer = SquareContentType.X;
            this.isGameOver = false;
        }

        public string GetBoardAsString()
        {
            var sb = new StringBuilder();
            int boardIndex = 0;

            for (int row = 0; row < this.boardDimension; ++row)
            {

                for (int column = 0; column < this.boardDimension; ++column)
                {
                    char c;

                    switch (this.board[boardIndex])
                    {
                        case SquareContentType.X:
                            c = 'X';
                            break;

                        case SquareContentType.O:
                            c = 'O';
                            break;

                        default:
                            c = ' ';
                            break;
                    }

                    sb.Append(c);
                    ++boardIndex;
                }
            }

            return sb.ToString();
        }

        public bool IsVictory(SquareContentType player, int row, int column)
        {
            // 1) Check the specified row.
            bool victory = true;

            for (int column2 = 0; column2 < this.boardDimension; ++column2)
            {

                if (this.board[row * this.boardDimension + column2] != player)
                {
                    victory = false;
                    break;
                }
            }

            if (victory)
            {
                return true;
            }

            // 2) Check the specified column.
            victory = true;

            for (int row2 = 0; row2 < this.boardDimension; ++row2)
            {

                if (this.board[row2 * this.boardDimension + column] != player)
                {
                    victory = false;
                    break;
                }
            }

            if (victory)
            {
                return true;
            }

            if (row == column)
            {
                // 3) Check the primary diagonal.
                victory = true;

                for (int i = 0; i < this.boardDimension; ++i)
                {

                    if (this.board[i * this.boardDimension + i] != player)
                    {
                        victory = false;
                        break;
                    }
                }

                if (victory)
                {
                    return true;
                }
            }

            if (row + column == this.boardDimension - 1)
            {
                // 4) Check the secondary diagonal.
                victory = true;

                for (int i = 0; i < this.boardDimension; ++i)
                {

                    if (this.board[i * this.boardDimension + this.boardDimension - 1 - i] != player)
                    {
                        victory = false;
                        break;
                    }
                }

                if (victory)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PlacePiece(SquareContentType player, int row, int column, bool displayMove)
        {
            // If player is X or O, the square being written to must be empty just before the move is made.
            // If player is Empty, the square being written to must be non-empty just before the move is made, and displayMove must be false.

            if (row < 0 || row >= this.boardDimension)
            {
                throw new ArgumentOutOfRangeException("row", string.Format("PlacePiece() : row {0} is out of range.", row));
            }

            if (column < 0 || column >= this.boardDimension)
            {
                throw new ArgumentOutOfRangeException("column", string.Format("PlacePiece() : column {0} is out of range.", column));
            }

            var oldSquareContent = this.board[row * this.boardDimension + column];

            if (player != SquareContentType.EmptySquare)
            {

                if (oldSquareContent != SquareContentType.EmptySquare)
                {
                    throw new ArgumentException("PlacePiece() : Attempted to write an X or an O into a non-empty square.", "player");
                }
            }
            else
            {

                if (oldSquareContent == SquareContentType.EmptySquare)
                {
                    throw new ArgumentException("PlacePiece() : Attempted to erase an already-empty square.", "player");
                }

                if (displayMove)
                {
                    throw new ArgumentException("PlacePiece() : Attempted to display an erasing move to the game window.");
                }
            }

            this.board[row * this.boardDimension + column] = player;

            if (player == SquareContentType.EmptySquare)
            {
                --this.boardPopulation;
            }
            else
            {
                ++this.boardPopulation;
            }

            var victory = player != SquareContentType.EmptySquare && IsVictory(player, row, column);

            if (displayMove)
            {
                this.gameWindow.PlacePiece(player, row, column);
                this.isGameOver = victory;   // This can only be set to true during real moves.
            }

            return victory; // This can return true for real or speculative moves.
        }

#if USE_OPEN_LINES_HEURISTIC
        public int GetNumOpenLines(SquareContentType opponent)
        {
            int numOpenLines = 2 * this.boardDimension + 2;
            int row;
            int column;

            // 1) Check all rows.

            for (row = 0; row < this.boardDimension; ++row)
            {

                for (column = 0; column < this.boardDimension; ++column)
                {

                    if (this.board[row * this.boardDimension + column] == opponent)
                    {
                        --numOpenLines;
                        break;
                    }
                }
            }

            // 2) Check all columns.

            for (column = 0; column < this.boardDimension; ++column)
            {

                for (row = 0; row < this.boardDimension; ++row)
                {

                    if (this.board[row * this.boardDimension + column] == opponent)
                    {
                        --numOpenLines;
                        break;
                    }
                }
            }

            // 3) Check the primary diagonal.

            for (row = 0; row < this.boardDimension; ++row)
            {

                if (this.board[row * this.boardDimension + row] == opponent)
                {
                    --numOpenLines;
                    break;
                }
            }

            // 4) Check the secondary diagonal.

            for (row = 0; row < this.boardDimension; ++row)
            {

                if (this.board[row * this.boardDimension + this.boardDimension - 1 - row] == opponent)
                {
                    --numOpenLines;
                    break;
                }
            }

            return numOpenLines;
        }

        public int GetBoardValue(SquareContentType player, SquareContentType opponent /* , int row, int column */ )
        {
#if DEAD_CODE
            // row and column are passed to IsVictory().

            if (IsVictory(player, row, column))
            {
                return victoryValue;
            }

            if (IsVictory(opponent, row, column))
            {
                return defeatValue;
            }
#endif

            // 2014/06/26 : This is backwards:
            //return GetNumOpenLines(player) - GetNumOpenLines(opponent);
            // This is correct:
            return GetNumOpenLines(opponent) - GetNumOpenLines(player);
        }
#endif

        private int FindBestMove(SquareContentType player, int ply,
            int bestUncleRecursiveScore,	// For alpha-beta pruning.
            bool returnBestCoordinates, List<int> bestMoveListCopy,
            out int bestRow, out int bestColumn)
        {
            var opponent = (player == SquareContentType.X) ? SquareContentType.O : SquareContentType.X;
            var bestMoveValue = defeatValue - 1;     // Worse than the worst possible move value.
            var bestMoveList = returnBestCoordinates ? new List<int>() : null;
            var doneSearching = false;
            var userCancelled = false;

            // Return values (when USE_OPEN_LINES_HEURISTIC is not defined):
            // 1 == Victory for the player specified by the parameter "player"
            // -1 == Victory for the opponent
            // 0 == No victory for either side

            for (int row = 0; row < this.boardDimension && !doneSearching; ++row)
            {

                for (int column = 0; column < this.boardDimension; ++column)
                {
                    var moveValue = 0;
                    var currentSquareIndex = row * this.boardDimension + column;

                    if (this.board[currentSquareIndex] != SquareContentType.EmptySquare)
                    {
                        continue;
                    }

                    if (PlacePiece(player, row, column, false)) // I.e. if this move results in immediate victory.
                    {
                        moveValue = victoryValue;
                    }
                    else if (this.boardPopulation < this.boardArea && ply > 1)
                    {
                        int dummyBestRow;
                        int dummyBestColumn;

                        moveValue = -FindBestMove(opponent, ply - 1, bestMoveValue, false, null, out dummyBestRow, out dummyBestColumn);
                    }
#if USE_OPEN_LINES_HEURISTIC
                    else
                    {
                        moveValue = GetBoardValue(player, opponent /*, row, column */ );
                    }
#endif

                    PlacePiece(SquareContentType.EmptySquare, row, column, false);

                    if (this.backgroundWorker != null && this.backgroundWorker.CancellationPending)
                    {
                        userCancelled = true;
                        doneSearching = true;
                        break;
                    }

                    if (moveValue == bestMoveValue && bestMoveList != null)
                    {
                        bestMoveList.Add(currentSquareIndex);
                    }
                    else if (moveValue > bestMoveValue)
                    {
                        bestMoveValue = moveValue;

                        if (bestMoveValue > -bestUncleRecursiveScore) 
                        {
                            // Alpha-beta pruning.  Because of the initial parameters for the top-level move, this break is never executed for the top-level move.
                            doneSearching = true;
                            break; // ie. return.
                        }
                        else if (bestMoveList != null)
                        {
                            bestMoveList.Clear();
                            bestMoveList.Add(currentSquareIndex);
                        }
                        else if (bestMoveValue == victoryValue)
                        {
                            // Prune the search tree, since we are not constructing a list of all of the best moves.
                            doneSearching = true;
                            break;
                        }
                    }
                }
            }

            if (!userCancelled && (bestMoveValue < defeatValue || bestMoveValue > victoryValue))
            {
                throw new Exception("FindBestMove() : bestMoveValue is out of range.");
            }
            else if (bestMoveList == null || userCancelled)
            {
                bestRow = -1;
                bestColumn = -1;
            }
            else if (bestMoveList.Count == 0)
            {
                throw new Exception("FindBestMove() : The bestMoveList is empty at the end of the method.");
            }
            else
            {
                var randomNumberGenerator = new Random();
                var index = randomNumberGenerator.Next(bestMoveList.Count);
                var packedCoordinates = bestMoveList[index];

                bestRow = packedCoordinates / this.boardDimension;
                bestColumn = packedCoordinates % this.boardDimension;

                if (bestMoveListCopy != null)
                {
                    bestMoveListCopy.Clear();
                    bestMoveListCopy.AddRange(bestMoveList);
                }
            }

            return bestMoveValue;
        }

        public int FindBestMove(SquareContentType player, int ply,
            // no bestUncleRecursiveScore
            bool returnBestCoordinates, List<int> bestMoveListCopy,
            out int bestRow, out int bestColumn)
        {
            return FindBestMove(player, ply, defeatValue - 1, returnBestCoordinates, bestMoveListCopy, out bestRow, out bestColumn);
        }

        public BestMoveData FindBestMoveWrapper(SquareContentType player, int ply)
        {
            int bestRow;
            int bestColumn;
            int bestScore = FindBestMove(player, ply, true, null, out bestRow, out bestColumn);

            return new BestMoveData(bestScore, bestRow, bestColumn);
        }
    }
}

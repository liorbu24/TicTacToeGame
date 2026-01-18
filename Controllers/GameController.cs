using Microsoft.AspNetCore.Mvc;
using TicTacToeGame.Models;
using System.Text.Json;

namespace TicTacToeGame.Controllers
{
    public class GameController : Controller
    {
        private const string GameStateSessionKey = "GameState";

        public IActionResult Index()
        {
            // Initialize new game
            var gameState = new GameState(3);
            SaveGameState(gameState);
            return View(gameState);
        }

        [HttpPost]
        public IActionResult MakeMove([FromBody] MoveRequest request)
        {
            var gameState = GetGameState();
            
            if (gameState == null)
            {
                gameState = new GameState(3);
            }

            // Reset expansion flag
            gameState.BoardExpanded = false;

            // Validate move
            if (gameState.GameOver)
            {
                return Json(GameStateDto.FromGameState(gameState));
            }

            if (request.Row < 0 || request.Row >= gameState.BoardSize ||
                request.Col < 0 || request.Col >= gameState.BoardSize)
            {
                gameState.Message = "מהלך לא חוקי - מחוץ ללוח";
                return Json(GameStateDto.FromGameState(gameState));
            }

            if (!string.IsNullOrEmpty(gameState.Board[request.Row, request.Col]))
            {
                gameState.Message = "משבצת תפוסה! בחר משבצת אחרת";
                return Json(GameStateDto.FromGameState(gameState));
            }

            // Make the move
            gameState.Board[request.Row, request.Col] = gameState.CurrentPlayer.ToString();

            // Check for win
            if (CheckWin(gameState, request.Row, request.Col))
            {
                gameState.GameOver = true;
                gameState.Winner = gameState.CurrentPlayer;
                gameState.Message = $"🎉 שחקן {gameState.CurrentPlayer} ניצח!";
                SaveGameState(gameState);
                return Json(GameStateDto.FromGameState(gameState));
            }

            // Check if board is full
            if (IsBoardFull(gameState))
            {
                // Expand the board
                ExpandBoard(gameState);
                gameState.BoardExpanded = true;
                gameState.Message = $"הלוח מלא! הלוח הורחב ל-{gameState.BoardSize}x{gameState.BoardSize}. תור השחקן: {gameState.CurrentPlayer}";
            }
            else
            {
                // Switch player
                gameState.CurrentPlayer = gameState.CurrentPlayer == 'X' ? 'O' : 'X';
                gameState.Message = $"תור השחקן: {gameState.CurrentPlayer}";
            }

            SaveGameState(gameState);
            return Json(GameStateDto.FromGameState(gameState));
        }

        [HttpPost]
        public IActionResult NewGame()
        {
            var gameState = new GameState(3);
            SaveGameState(gameState);
            return Json(GameStateDto.FromGameState(gameState));
        }

        [HttpGet]
        public IActionResult GetState()
        {
            var gameState = GetGameState();
            if (gameState == null)
            {
                gameState = new GameState(3);
                SaveGameState(gameState);
            }
            return Json(GameStateDto.FromGameState(gameState));
        }

        /// <summary>
        /// Check if the current player has won (3 in a row anywhere on the board)
        /// </summary>
        private bool CheckWin(GameState state, int lastRow, int lastCol)
        {
            string player = state.CurrentPlayer.ToString();
            int size = state.BoardSize;

            // Check all possible winning lines of 3 that include the last move

            // Check horizontal (row) - look for 3 in a row
            for (int startCol = Math.Max(0, lastCol - 2); startCol <= Math.Min(size - 3, lastCol); startCol++)
            {
                if (state.Board[lastRow, startCol] == player &&
                    state.Board[lastRow, startCol + 1] == player &&
                    state.Board[lastRow, startCol + 2] == player)
                {
                    return true;
                }
            }

            // Check vertical (column) - look for 3 in a row
            for (int startRow = Math.Max(0, lastRow - 2); startRow <= Math.Min(size - 3, lastRow); startRow++)
            {
                if (state.Board[startRow, lastCol] == player &&
                    state.Board[startRow + 1, lastCol] == player &&
                    state.Board[startRow + 2, lastCol] == player)
                {
                    return true;
                }
            }

            // Check diagonal (top-left to bottom-right)
            for (int offset = -2; offset <= 0; offset++)
            {
                int startRow = lastRow + offset;
                int startCol = lastCol + offset;
                
                if (startRow >= 0 && startCol >= 0 &&
                    startRow + 2 < size && startCol + 2 < size)
                {
                    if (state.Board[startRow, startCol] == player &&
                        state.Board[startRow + 1, startCol + 1] == player &&
                        state.Board[startRow + 2, startCol + 2] == player)
                    {
                        return true;
                    }
                }
            }

            // Check anti-diagonal (top-right to bottom-left)
            for (int offset = -2; offset <= 0; offset++)
            {
                int startRow = lastRow + offset;
                int startCol = lastCol - offset;
                
                if (startRow >= 0 && startCol < size &&
                    startRow + 2 < size && startCol - 2 >= 0)
                {
                    if (state.Board[startRow, startCol] == player &&
                        state.Board[startRow + 1, startCol - 1] == player &&
                        state.Board[startRow + 2, startCol - 2] == player)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the board is completely full
        /// </summary>
        private bool IsBoardFull(GameState state)
        {
            for (int i = 0; i < state.BoardSize; i++)
            {
                for (int j = 0; j < state.BoardSize; j++)
                {
                    if (string.IsNullOrEmpty(state.Board[i, j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Expand the board by doubling its size and centering existing moves
        /// </summary>
        private void ExpandBoard(GameState state)
        {
            int oldSize = state.BoardSize;
            int newSize = oldSize * 2;
            
            // Create new larger board
            var newBoard = new string[newSize, newSize];
            
            // Initialize with empty strings
            for (int i = 0; i < newSize; i++)
            {
                for (int j = 0; j < newSize; j++)
                {
                    newBoard[i, j] = "";
                }
            }

            // Calculate offset to center old board in new board
            int offset = oldSize / 2;

            // Copy old board to center of new board
            for (int i = 0; i < oldSize; i++)
            {
                for (int j = 0; j < oldSize; j++)
                {
                    newBoard[i + offset, j + offset] = state.Board[i, j];
                }
            }

            // Update state
            state.Board = newBoard;
            state.BoardSize = newSize;
        }

        private GameState? GetGameState()
        {
            var json = HttpContext.Session.GetString(GameStateSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var dto = JsonSerializer.Deserialize<GameStateDto>(json);
                if (dto == null) return null;

                var state = new GameState
                {
                    CurrentPlayer = dto.CurrentPlayer,
                    BoardSize = dto.BoardSize,
                    GameOver = dto.GameOver,
                    Winner = dto.Winner,
                    Message = dto.Message,
                    BoardExpanded = dto.BoardExpanded
                };
                state.SetBoardFromList(dto.Board);
                return state;
            }
            catch
            {
                return null;
            }
        }

        private void SaveGameState(GameState state)
        {
            var dto = GameStateDto.FromGameState(state);
            var json = JsonSerializer.Serialize(dto);
            HttpContext.Session.SetString(GameStateSessionKey, json);
        }
    }
}

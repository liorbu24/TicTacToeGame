using Microsoft.AspNetCore.SignalR;
using TicTacToeGame.Models;
using TicTacToeGame.Services;

namespace TicTacToeGame.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomManager _roomManager;

        public GameHub(RoomManager roomManager)
        {
            _roomManager = roomManager;
        }

        public async Task CreateRoom(string playerName)
        {
            var room = _roomManager.CreateRoom(Context.ConnectionId, playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
            
            await Clients.Caller.SendAsync("RoomCreated", new RoomDto
            {
                RoomCode = room.RoomCode,
                PlayerSymbol = 'X',
                IsGameStarted = false,
                IsYourTurn = true,
                GameState = GameStateDto.FromGameState(room.GameState)
            });
        }

        public async Task JoinRoom(string roomCode, string playerName)
        {
            var (room, error) = _roomManager.JoinRoom(roomCode, Context.ConnectionId, playerName);
            
            if (error != null)
            {
                await Clients.Caller.SendAsync("Error", error);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, room!.RoomCode);

            // Notify the joining player
            await Clients.Caller.SendAsync("RoomJoined", new RoomDto
            {
                RoomCode = room.RoomCode,
                PlayerSymbol = 'O',
                OpponentName = room.PlayerXName,
                IsGameStarted = true,
                IsYourTurn = false,
                GameState = GameStateDto.FromGameState(room.GameState)
            });

            // Notify player X that someone joined
            if (room.PlayerXConnectionId != null)
            {
                await Clients.Client(room.PlayerXConnectionId).SendAsync("OpponentJoined", new RoomDto
                {
                    RoomCode = room.RoomCode,
                    PlayerSymbol = 'X',
                    OpponentName = room.PlayerOName,
                    IsGameStarted = true,
                    IsYourTurn = true,
                    GameState = GameStateDto.FromGameState(room.GameState)
                });
            }
        }

        public async Task MakeMove(int row, int col)
        {
            var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "לא נמצא חדר משחק");
                return;
            }

            if (!room.IsGameStarted)
            {
                await Clients.Caller.SendAsync("Error", "המשחק עדיין לא התחיל - מחכים לשחקן נוסף");
                return;
            }

            if (!room.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "זה לא התור שלך!");
                return;
            }

            var gameState = room.GameState;
            
            // Reset expansion flag
            gameState.BoardExpanded = false;

            // Validate move
            if (gameState.GameOver)
            {
                await Clients.Caller.SendAsync("Error", "המשחק כבר הסתיים");
                return;
            }

            if (row < 0 || row >= gameState.BoardSize || col < 0 || col >= gameState.BoardSize)
            {
                await Clients.Caller.SendAsync("Error", "מהלך לא חוקי");
                return;
            }

            if (!string.IsNullOrEmpty(gameState.Board[row, col]))
            {
                await Clients.Caller.SendAsync("Error", "משבצת תפוסה");
                return;
            }

            // Make the move
            gameState.Board[row, col] = gameState.CurrentPlayer.ToString();

            // Check for win
            if (CheckWin(gameState, row, col))
            {
                gameState.GameOver = true;
                gameState.Winner = gameState.CurrentPlayer;
                var winnerName = gameState.CurrentPlayer == 'X' ? room.PlayerXName : room.PlayerOName;
                gameState.Message = $"🎉 {winnerName} ({gameState.CurrentPlayer}) ניצח!";
            }
            // Check if board is full
            else if (IsBoardFull(gameState))
            {
                ExpandBoard(gameState);
                gameState.BoardExpanded = true;
                gameState.Message = $"הלוח מלא! הלוח הורחב ל-{gameState.BoardSize}x{gameState.BoardSize}";
            }
            else
            {
                // Switch player
                gameState.CurrentPlayer = gameState.CurrentPlayer == 'X' ? 'O' : 'X';
                var currentPlayerName = gameState.CurrentPlayer == 'X' ? room.PlayerXName : room.PlayerOName;
                gameState.Message = $"תור של {currentPlayerName} ({gameState.CurrentPlayer})";
            }

            // Send update to both players
            await SendGameUpdate(room);
        }

        public async Task RequestRematch()
        {
            var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
            if (room == null) return;

            var opponentId = room.GetOpponentConnectionId(Context.ConnectionId);
            if (opponentId != null)
            {
                var playerName = room.GetPlayerSymbol(Context.ConnectionId) == 'X' 
                    ? room.PlayerXName 
                    : room.PlayerOName;
                await Clients.Client(opponentId).SendAsync("RematchRequested", playerName);
            }
        }

        public async Task AcceptRematch()
        {
            var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
            if (room == null) return;

            // Reset the game
            room.GameState = new GameState();
            room.GameState.Message = $"משחק חדש! תור של {room.PlayerXName} (X)";

            await SendGameUpdate(room);
            await Clients.Group(room.RoomCode).SendAsync("RematchAccepted");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
            if (room != null)
            {
                var playerSymbol = room.GetPlayerSymbol(Context.ConnectionId);
                var playerName = playerSymbol == 'X' ? room.PlayerXName : room.PlayerOName;
                var opponentId = room.GetOpponentConnectionId(Context.ConnectionId);
                
                if (opponentId != null)
                {
                    await Clients.Client(opponentId).SendAsync("OpponentDisconnected", playerName);
                }
            }

            _roomManager.RemoveConnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendGameUpdate(GameRoom room)
        {
            // Send to Player X
            if (room.PlayerXConnectionId != null)
            {
                await Clients.Client(room.PlayerXConnectionId).SendAsync("GameUpdated", new RoomDto
                {
                    RoomCode = room.RoomCode,
                    PlayerSymbol = 'X',
                    OpponentName = room.PlayerOName,
                    IsGameStarted = room.IsGameStarted,
                    IsYourTurn = room.GameState.CurrentPlayer == 'X' && !room.GameState.GameOver,
                    GameState = GameStateDto.FromGameState(room.GameState)
                });
            }

            // Send to Player O
            if (room.PlayerOConnectionId != null)
            {
                await Clients.Client(room.PlayerOConnectionId).SendAsync("GameUpdated", new RoomDto
                {
                    RoomCode = room.RoomCode,
                    PlayerSymbol = 'O',
                    OpponentName = room.PlayerXName,
                    IsGameStarted = room.IsGameStarted,
                    IsYourTurn = room.GameState.CurrentPlayer == 'O' && !room.GameState.GameOver,
                    GameState = GameStateDto.FromGameState(room.GameState)
                });
            }
        }

        #region Game Logic (same as GameController)
        
        private bool CheckWin(GameState state, int lastRow, int lastCol)
        {
            string player = state.CurrentPlayer.ToString();
            int size = state.BoardSize;

            // Check horizontal
            for (int startCol = Math.Max(0, lastCol - 2); startCol <= Math.Min(size - 3, lastCol); startCol++)
            {
                if (state.Board[lastRow, startCol] == player &&
                    state.Board[lastRow, startCol + 1] == player &&
                    state.Board[lastRow, startCol + 2] == player)
                    return true;
            }

            // Check vertical
            for (int startRow = Math.Max(0, lastRow - 2); startRow <= Math.Min(size - 3, lastRow); startRow++)
            {
                if (state.Board[startRow, lastCol] == player &&
                    state.Board[startRow + 1, lastCol] == player &&
                    state.Board[startRow + 2, lastCol] == player)
                    return true;
            }

            // Check diagonal
            for (int offset = -2; offset <= 0; offset++)
            {
                int startRow = lastRow + offset;
                int startCol = lastCol + offset;
                
                if (startRow >= 0 && startCol >= 0 && startRow + 2 < size && startCol + 2 < size)
                {
                    if (state.Board[startRow, startCol] == player &&
                        state.Board[startRow + 1, startCol + 1] == player &&
                        state.Board[startRow + 2, startCol + 2] == player)
                        return true;
                }
            }

            // Check anti-diagonal
            for (int offset = -2; offset <= 0; offset++)
            {
                int startRow = lastRow + offset;
                int startCol = lastCol - offset;
                
                if (startRow >= 0 && startCol < size && startRow + 2 < size && startCol - 2 >= 0)
                {
                    if (state.Board[startRow, startCol] == player &&
                        state.Board[startRow + 1, startCol - 1] == player &&
                        state.Board[startRow + 2, startCol - 2] == player)
                        return true;
                }
            }

            return false;
        }

        private bool IsBoardFull(GameState state)
        {
            for (int i = 0; i < state.BoardSize; i++)
                for (int j = 0; j < state.BoardSize; j++)
                    if (string.IsNullOrEmpty(state.Board[i, j]))
                        return false;
            return true;
        }

        private void ExpandBoard(GameState state)
        {
            int oldSize = state.BoardSize;
            int newSize = oldSize * 2;
            var newBoard = new string[newSize, newSize];
            
            for (int i = 0; i < newSize; i++)
                for (int j = 0; j < newSize; j++)
                    newBoard[i, j] = "";

            int offset = oldSize / 2;
            for (int i = 0; i < oldSize; i++)
                for (int j = 0; j < oldSize; j++)
                    newBoard[i + offset, j + offset] = state.Board[i, j];

            state.Board = newBoard;
            state.BoardSize = newSize;
        }

        #endregion
    }
}

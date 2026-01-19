namespace TicTacToeGame.Models
{
    public class GameRoom
    {
        public string RoomCode { get; set; } = "";
        public string? PlayerXConnectionId { get; set; }
        public string? PlayerOConnectionId { get; set; }
        public string? PlayerXName { get; set; }
        public string? PlayerOName { get; set; }
        public GameState GameState { get; set; } = new GameState();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsGameStarted => PlayerXConnectionId != null && PlayerOConnectionId != null;
        
        public bool IsFull => PlayerXConnectionId != null && PlayerOConnectionId != null;
        
        public char? GetPlayerSymbol(string connectionId)
        {
            if (connectionId == PlayerXConnectionId) return 'X';
            if (connectionId == PlayerOConnectionId) return 'O';
            return null;
        }

        public string? GetOpponentConnectionId(string connectionId)
        {
            if (connectionId == PlayerXConnectionId) return PlayerOConnectionId;
            if (connectionId == PlayerOConnectionId) return PlayerXConnectionId;
            return null;
        }

        public bool IsPlayerTurn(string connectionId)
        {
            var symbol = GetPlayerSymbol(connectionId);
            return symbol.HasValue && symbol.Value == GameState.CurrentPlayer;
        }
    }

    public class RoomDto
    {
        public string RoomCode { get; set; } = "";
        public char? PlayerSymbol { get; set; }
        public string? OpponentName { get; set; }
        public bool IsGameStarted { get; set; }
        public bool IsYourTurn { get; set; }
        public GameStateDto? GameState { get; set; }
    }

    public class JoinRoomRequest
    {
        public string RoomCode { get; set; } = "";
        public string PlayerName { get; set; } = "";
    }
}

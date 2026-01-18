namespace TicTacToeGame.Models
{
    public class GameState
    {
        public string[,] Board { get; set; } = new string[3, 3];
        public char CurrentPlayer { get; set; }
        public int BoardSize { get; set; }
        public bool GameOver { get; set; }
        public char? Winner { get; set; }
        public string Message { get; set; } = "";
        public bool BoardExpanded { get; set; }

        public GameState()
        {
            Initialize(3);
        }

        public GameState(int size)
        {
            Initialize(size);
        }

        public void Initialize(int size)
        {
            BoardSize = size;
            Board = new string[size, size];
            CurrentPlayer = 'X';
            GameOver = false;
            Winner = null;
            Message = $"תור השחקן: X";
            BoardExpanded = false;

            // Initialize empty board
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Board[i, j] = "";
                }
            }
        }

        public string[,] CopyBoard()
        {
            var copy = new string[BoardSize, BoardSize];
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    copy[i, j] = Board[i, j];
                }
            }
            return copy;
        }

        // Convert 2D array to list for JSON serialization
        public List<List<string>> GetBoardAsList()
        {
            var list = new List<List<string>>();
            for (int i = 0; i < BoardSize; i++)
            {
                var row = new List<string>();
                for (int j = 0; j < BoardSize; j++)
                {
                    row.Add(Board[i, j] ?? "");
                }
                list.Add(row);
            }
            return list;
        }

        // Set board from list (for deserialization)
        public void SetBoardFromList(List<List<string>> list)
        {
            if (list == null || list.Count == 0) return;
            
            BoardSize = list.Count;
            Board = new string[BoardSize, BoardSize];
            
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    Board[i, j] = list[i][j] ?? "";
                }
            }
        }
    }

    // DTO for JSON serialization
    public class GameStateDto
    {
        public List<List<string>> Board { get; set; } = new List<List<string>>();
        public char CurrentPlayer { get; set; }
        public int BoardSize { get; set; }
        public bool GameOver { get; set; }
        public char? Winner { get; set; }
        public string Message { get; set; } = "";
        public bool BoardExpanded { get; set; }

        public static GameStateDto FromGameState(GameState state)
        {
            return new GameStateDto
            {
                Board = state.GetBoardAsList(),
                CurrentPlayer = state.CurrentPlayer,
                BoardSize = state.BoardSize,
                GameOver = state.GameOver,
                Winner = state.Winner,
                Message = state.Message,
                BoardExpanded = state.BoardExpanded
            };
        }
    }

    // Request model for making moves
    public class MoveRequest
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }
}

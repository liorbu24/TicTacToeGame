using System.Collections.Concurrent;
using TicTacToeGame.Models;

namespace TicTacToeGame.Services
{
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
        private readonly ConcurrentDictionary<string, string> _connectionToRoom = new();
        private static readonly Random _random = new();

        public GameRoom CreateRoom(string connectionId, string playerName)
        {
            var roomCode = GenerateRoomCode();
            var room = new GameRoom
            {
                RoomCode = roomCode,
                PlayerXConnectionId = connectionId,
                PlayerXName = playerName
            };
            
            _rooms[roomCode] = room;
            _connectionToRoom[connectionId] = roomCode;
            
            return room;
        }

        public (GameRoom? room, string? error) JoinRoom(string roomCode, string connectionId, string playerName)
        {
            roomCode = roomCode.ToUpper();
            
            if (!_rooms.TryGetValue(roomCode, out var room))
            {
                return (null, "החדר לא נמצא");
            }

            if (room.IsFull)
            {
                return (null, "החדר מלא");
            }

            room.PlayerOConnectionId = connectionId;
            room.PlayerOName = playerName;
            _connectionToRoom[connectionId] = roomCode;

            return (room, null);
        }

        public GameRoom? GetRoom(string roomCode)
        {
            _rooms.TryGetValue(roomCode.ToUpper(), out var room);
            return room;
        }

        public GameRoom? GetRoomByConnection(string connectionId)
        {
            if (_connectionToRoom.TryGetValue(connectionId, out var roomCode))
            {
                return GetRoom(roomCode);
            }
            return null;
        }

        public void RemoveConnection(string connectionId)
        {
            if (_connectionToRoom.TryRemove(connectionId, out var roomCode))
            {
                if (_rooms.TryGetValue(roomCode, out var room))
                {
                    // If both players left, remove the room
                    if (room.PlayerXConnectionId == connectionId)
                    {
                        room.PlayerXConnectionId = null;
                        room.PlayerXName = null;
                    }
                    else if (room.PlayerOConnectionId == connectionId)
                    {
                        room.PlayerOConnectionId = null;
                        room.PlayerOName = null;
                    }

                    // Remove room if empty
                    if (room.PlayerXConnectionId == null && room.PlayerOConnectionId == null)
                    {
                        _rooms.TryRemove(roomCode, out _);
                    }
                }
            }
        }

        public string? GetRoomCodeByConnection(string connectionId)
        {
            _connectionToRoom.TryGetValue(connectionId, out var roomCode);
            return roomCode;
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string code;
            do
            {
                code = new string(Enumerable.Range(0, 6).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
            } while (_rooms.ContainsKey(code));
            
            return code;
        }

        // Cleanup old rooms (older than 2 hours)
        public void CleanupOldRooms()
        {
            var cutoff = DateTime.UtcNow.AddHours(-2);
            var oldRooms = _rooms.Where(r => r.Value.CreatedAt < cutoff).Select(r => r.Key).ToList();
            
            foreach (var roomCode in oldRooms)
            {
                if (_rooms.TryRemove(roomCode, out var room))
                {
                    if (room.PlayerXConnectionId != null)
                        _connectionToRoom.TryRemove(room.PlayerXConnectionId, out _);
                    if (room.PlayerOConnectionId != null)
                        _connectionToRoom.TryRemove(room.PlayerOConnectionId, out _);
                }
            }
        }
    }
}

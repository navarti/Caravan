using CaravanOnline.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Services
{
    public class OnlineGameStateService
    {
        private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();

        public string CreateRoom(string player1ConnectionId, string player1Name)
        {
            var roomId = System.Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            var room = new GameRoom(roomId, player1ConnectionId, player1Name);
            _gameRooms.TryAdd(roomId, room);
            return roomId;
        }

        public bool JoinRoom(string roomId, string player2ConnectionId, string player2Name)
        {
            if (_gameRooms.TryGetValue(roomId, out var room))
            {
                if (!room.IsFull())
                {
                    room.Player2ConnectionId = player2ConnectionId;
                    room.Player2Name = player2Name;
                    room.UpdateLastActivity();
                    return true;
                }
            }
            return false;
        }

        public GameRoom? GetRoom(string roomId)
        {
            _gameRooms.TryGetValue(roomId, out var room);
            return room;
        }

        public List<object> GetAvailableRooms()
        {
            return _gameRooms.Values
                .Where(r => !r.IsFull() && !r.IsGameStarted)
                .Select(r => new { r.RoomId, r.Player1Name })
                .Cast<object>()
                .ToList();
        }

        public void RemovePlayerFromRoom(string connectionId)
        {
            var roomToRemove = _gameRooms.Values.FirstOrDefault(r =>
                r.Player1ConnectionId == connectionId || r.Player2ConnectionId == connectionId);

            if (roomToRemove != null)
            {
                _gameRooms.TryRemove(roomToRemove.RoomId, out _);
            }
        }

        public string? GetRoomIdByConnection(string connectionId)
        {
            var room = _gameRooms.Values.FirstOrDefault(r =>
                r.Player1ConnectionId == connectionId || r.Player2ConnectionId == connectionId);
            return room?.RoomId;
        }

        public void RemoveRoom(string roomId)
        {
            _gameRooms.TryRemove(roomId, out _);
        }

        public List<GameRoom> GetAbandonedRooms(TimeSpan abandonedThreshold)
        {
            var cutoffTime = DateTime.UtcNow - abandonedThreshold;
            return _gameRooms.Values
                .Where(r => r.LastActivityAt < cutoffTime)
                .ToList();
        }

        public int GetActiveRoomCount()
        {
            return _gameRooms.Count;
        }
    }
}

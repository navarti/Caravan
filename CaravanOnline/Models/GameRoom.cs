using System;
using System.Collections.Generic;

namespace CaravanOnline.Models
{
    public class GameRoom
    {
        public string RoomId { get; set; } = string.Empty;
        public string Player1ConnectionId { get; set; } = string.Empty;
        public string? Player2ConnectionId { get; set; }
        public string Player1Name { get; set; } = "Player 1";
        public string? Player2Name { get; set; }
        public List<Card> Player1Cards { get; set; } = new();
        public List<Card> Player2Cards { get; set; } = new();
        public List<List<Card>> Lanes { get; set; } = new();
        public string CurrentPlayerConnectionId { get; set; } = string.Empty;
        public int CurrentLane { get; set; } = 1;
        public int Phase { get; set; } = 1;
        public string Message { get; set; } = "Welcome to the game!";
        public bool IsGameStarted { get; set; } = false;
        public Card? SelectedCardPhase2 { get; set; }

        // Timestamp tracking for cleanup
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public GameRoom(string roomId, string player1ConnectionId, string player1Name)
        {
            RoomId = roomId;
            Player1ConnectionId = player1ConnectionId;
            Player1Name = player1Name;
            CurrentPlayerConnectionId = player1ConnectionId;
            CreatedAt = DateTime.UtcNow;
            LastActivityAt = DateTime.UtcNow;
        }

        public bool IsFull() => !string.IsNullOrEmpty(Player2ConnectionId);

        public string? GetPlayerName(string connectionId)
        {
            if (connectionId == Player1ConnectionId) return Player1Name;
            if (connectionId == Player2ConnectionId) return Player2Name;
            return null;
        }

        public int GetPlayerNumber(string connectionId)
        {
            if (connectionId == Player1ConnectionId) return 1;
            if (connectionId == Player2ConnectionId) return 2;
            return 0;
        }

        public string GetCurrentPlayerName()
        {
            return GetPlayerName(CurrentPlayerConnectionId) ?? "Unknown";
        }

        public void UpdateLastActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }

        public void SwitchPlayer()
        {
            CurrentPlayerConnectionId = CurrentPlayerConnectionId == Player1ConnectionId 
                ? Player2ConnectionId ?? Player1ConnectionId 
                : Player1ConnectionId;
            UpdateLastActivity();
        }
    }
}

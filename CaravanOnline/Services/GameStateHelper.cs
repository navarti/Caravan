using CaravanOnline.Models;
using CaravanOnline.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace CaravanOnline.Services
{
    public class GameStateHelper
    {
        private readonly ISession _session;

        public GameStateHelper(IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session;
        }

        public bool IsInitialized()
        {
            return _session.GetString("Initialized") == "true";
        }

        public void InitializeGameState(List<Card> player1Cards, List<Card> player2Cards, List<List<Card>> lanes)
        {
            _session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(player1Cards));
            _session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(player2Cards));
            _session.SetString("CurrentPlayer", "Player 1");
            _session.SetInt32("CurrentLane", 1);
            _session.SetInt32("Phase", 1);
            _session.SetString("Lanes", SerializationHelper.SerializeLanes(lanes));
            _session.SetString("Initialized", "true");
        }

        public void LoadGameState(out List<Card> player1Cards, out List<Card> player2Cards, out int currentLane, out int phase, out string message, out List<List<Card>> lanes)
        {
            player1Cards = SerializationHelper.DeserializePlayerCards(_session.GetString("Player1Cards") ?? "");
            player2Cards = SerializationHelper.DeserializePlayerCards(_session.GetString("Player2Cards") ?? "");
            currentLane = _session.GetInt32("CurrentLane") ?? 1;
            phase = _session.GetInt32("Phase") ?? 1;
            message = _session.GetString("Message") ?? "Welcome to the game!";
            var serializedLanes = _session.GetString("Lanes") ?? "";
            lanes = !string.IsNullOrEmpty(serializedLanes) ? SerializationHelper.DeserializeLanes(serializedLanes) : new List<List<Card>>();
        }

        public void SaveGameState(List<Card> player1Cards, List<Card> player2Cards, int currentLane, int phase, string message, List<List<Card>> lanes)
        {
            _session.SetString("Player1Cards", SerializationHelper.SerializePlayerCards(player1Cards));
            _session.SetString("Player2Cards", SerializationHelper.SerializePlayerCards(player2Cards));
            _session.SetInt32("CurrentLane", currentLane);
            _session.SetInt32("Phase", phase);
            _session.SetString("Message", message);
            _session.SetString("Lanes", SerializationHelper.SerializeLanes(lanes));
        }
    }
}

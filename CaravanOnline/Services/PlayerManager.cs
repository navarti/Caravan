using CaravanOnline.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Services
{
    public class PlayerManager
    {
        private readonly CardManager _cardManager;

        public PlayerManager(CardManager cardManager)
        {
            _cardManager = cardManager;
        }

        public void SwitchPlayer(ISession session)
        {
            string current = session.GetString("CurrentPlayer") ?? "Player 1";
            string next = (current == "Player 1") ? "Player 2" : "Player 1";
            session.SetString("CurrentPlayer", next);
            Console.WriteLine($"Switched from {current} to {next}.");
        }

        public void AddRandomCardIfNecessary(string currentPlayer, List<Card> hand)
        {
            if (hand.Count < 5)
            {
                try
                {
                    var newCard = _cardManager.GetRandomCard();
                    hand.Add(newCard);
                    Console.WriteLine($"Drew new card for {currentPlayer}: {newCard.Face} {newCard.Suit}. Hand size now {hand.Count}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing new card for {currentPlayer}: {ex.Message}");
                }
            }
        }

        public string GetCurrentPlayer(ISession session)
        {
            return session.GetString("CurrentPlayer") ?? "Player 1";
        }
    }
}

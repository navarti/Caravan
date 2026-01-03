using Microsoft.AspNetCore.Mvc;
using CaravanOnline.Models;
using System.Linq;
using System;
using Microsoft.AspNetCore.Mvc;
using CaravanOnline.Pages;    

namespace CaravanOnline.Services
{
    public class PhaseManager
    {
        private readonly LaneManager _laneManager;
        private readonly PlayerManager _playerManager;
        private readonly GameStateHelper _gameStateHelper;
        private readonly CardManager _cardManager;

        public PhaseManager(
            LaneManager laneManager,
            PlayerManager playerManager,
            GameStateHelper gameStateHelper,
            CardManager cardManager)
        {
            _laneManager = laneManager;
            _playerManager = playerManager;
            _gameStateHelper = gameStateHelper;
            _cardManager = cardManager;
        }

        public IActionResult HandlePhase1(
            IndexModel indexModel, 
            string currentPlayer, 
            string? selectedCard, 
            string? selectedLane)
        {
            if (!string.IsNullOrEmpty(selectedCard))
            {
                var parts = selectedCard.Split(' ');
                if (parts.Length < 2)
                {
                    indexModel.Message = "Invalid card selected.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                var face = parts[0];
                var suit = parts[1];
                var cardToPlay = (currentPlayer == "Player 1")
                    ? indexModel.Player1Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit)
                    : indexModel.Player2Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);

                if (cardToPlay == null)
                {
                    indexModel.Message = $"Card not found in {currentPlayer}'s hand.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                bool success = _laneManager.AddCardToLane(indexModel.CurrentLane, cardToPlay);
                if (!success)
                {
                    indexModel.Message = _laneManager.LastFailReason;
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                if (currentPlayer == "Player 1")
                {
                    indexModel.Player1Cards.Remove(cardToPlay);
                    _playerManager.AddRandomCardIfNecessary("Player 1", indexModel.Player1Cards);
                }
                else
                {
                    indexModel.Player2Cards.Remove(cardToPlay);
                    _playerManager.AddRandomCardIfNecessary("Player 2", indexModel.Player2Cards);
                }

                if (_laneManager.Lanes.All(l => l.Count >= 1))
                {
                    indexModel.Phase = 2;
                    indexModel.HttpContext.Session.SetInt32("Phase", indexModel.Phase);
                    indexModel.HttpContext.Session.SetString("CurrentPlayer", "Player 1");
                }
                else
                {
                    indexModel.CurrentLane = indexModel.SwitchLane(currentPlayer, indexModel.CurrentLane);
                    indexModel.HttpContext.Session.SetInt32("CurrentLane", indexModel.CurrentLane);
                    _playerManager.SwitchPlayer(indexModel.HttpContext.Session);
                }

                indexModel.SaveState();
                return indexModel.RedirectToPage();
            }

            indexModel.SaveState();
            return indexModel.Page();
        }

        public IActionResult HandlePhase2(
            IndexModel indexModel, 
            string currentPlayer, 
            string? selectedCard, 
            string? selectedLane)
        {
            if (!string.IsNullOrEmpty(selectedCard))
            {
                var parts = selectedCard.Split(' ');
                if (parts.Length < 2)
                {
                    indexModel.Message = "Invalid card selected.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                var face = parts[0];
                var suit = parts[1];
                indexModel.SelectedCardPhase2 = (currentPlayer == "Player 1")
                    ? indexModel.Player1Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit)
                    : indexModel.Player2Cards.FirstOrDefault(c => c.Face == face && c.Suit == suit);

                if (indexModel.SelectedCardPhase2 == null)
                {
                    indexModel.Message = "Card not found.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                indexModel.HttpContext.Session.SetString(
                    "SelectedCardPhase2",
                    SerializationHelper.SerializePlayerCards(
                        new System.Collections.Generic.List<Card> { indexModel.SelectedCardPhase2 }
                    )
                );
                indexModel.Message = "Please select a lane";
                indexModel.SaveState();
                return indexModel.Page();
            }
            else if (!string.IsNullOrEmpty(selectedLane))
            {
                var serializedSelected = indexModel.HttpContext.Session.GetString("SelectedCardPhase2") ?? "";
                if (string.IsNullOrEmpty(serializedSelected))
                {
                    indexModel.Message = "Please select a card first.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                indexModel.SelectedCardPhase2 = SerializationHelper
                    .DeserializePlayerCards(serializedSelected)
                    .FirstOrDefault();

                if (indexModel.SelectedCardPhase2 == null)
                {
                    indexModel.Message = "Invalid card.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                if (!int.TryParse(selectedLane, out int laneNumber) || laneNumber < 1 || laneNumber > 6)
                {
                    indexModel.Message = "Invalid lane selected.";
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                bool success = _laneManager.AddCardToLane(laneNumber, indexModel.SelectedCardPhase2);
                if (!success)
                {
                    indexModel.Message = _laneManager.LastFailReason;
                    indexModel.SaveState();
                    return indexModel.Page();
                }

                if (currentPlayer == "Player 1")
                {
                    var cardToRemove = indexModel.Player1Cards
                        .FirstOrDefault(c => 
                            c.Face == indexModel.SelectedCardPhase2.Face && 
                            c.Suit == indexModel.SelectedCardPhase2.Suit
                        );
                    if (cardToRemove != null)
                    {
                        indexModel.Player1Cards.Remove(cardToRemove);
                        _playerManager.AddRandomCardIfNecessary("Player 1", indexModel.Player1Cards);
                    }
                    else
                    {
                        indexModel.Message = "Card not found in Player 1's hand.";
                        indexModel.SaveState();
                        return indexModel.Page();
                    }
                }
                else
                {
                    var cardToRemove = indexModel.Player2Cards
                        .FirstOrDefault(c => 
                            c.Face == indexModel.SelectedCardPhase2.Face && 
                            c.Suit == indexModel.SelectedCardPhase2.Suit
                        );
                    if (cardToRemove != null)
                    {
                        indexModel.Player2Cards.Remove(cardToRemove);
                        _playerManager.AddRandomCardIfNecessary("Player 2", indexModel.Player2Cards);
                    }
                    else
                    {
                        indexModel.Message = "Card not found in Player 2's hand.";
                        indexModel.SaveState();
                        return indexModel.Page();
                    }
                }

                indexModel.HttpContext.Session.Remove("SelectedCardPhase2");
                indexModel.SaveState();
                _playerManager.SwitchPlayer(indexModel.HttpContext.Session);

                var gameResult = _laneManager.EvaluateGame();
                if (gameResult != "The game is still ongoing.")
                {
                    indexModel.Message = gameResult;
                    indexModel.HttpContext.Session.Clear();
                    return indexModel.Page();
                }
                return indexModel.RedirectToPage();
            }
            else
            {
                indexModel.Message = "No card or lane selected.";
            }

            indexModel.SaveState();
            return indexModel.Page();
        }
    }
}

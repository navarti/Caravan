using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CaravanOnline.Services;
using CaravanOnline.Models;
using CaravanOnline.Models.DTO;
using System.Collections.Generic;
using System.Linq;

namespace CaravanOnline.Pages
{
    public class IndexModel : PageModel
    {
        private readonly LaneManager _laneManager;
        private readonly CardManager _cardManager;
        private readonly GameStateHelper _gameStateHelper;
        private readonly PlayerManager _playerManager;
        private readonly PhaseManager _phaseManager;

        public List<Card> Player1Cards { get; set; } = new();
        public List<Card> Player2Cards { get; set; } = new();
        public string Message { get; set; } = "Welcome to the game!";
        public int CurrentLane { get; set; } = 1;
        public int Phase { get; set; } = 1;
        public Card? SelectedCardPhase2 { get; set; }
        public List<List<Card>> Lanes => _laneManager.Lanes;

        public IndexModel(
            LaneManager laneManager,
            CardManager cardManager,
            GameStateHelper gameStateHelper,
            PlayerManager playerManager,
            PhaseManager phaseManager)
        {
            _laneManager = laneManager;
            _cardManager = cardManager;
            _gameStateHelper = gameStateHelper;
            _playerManager = playerManager;
            _phaseManager = phaseManager;
        }

        public void OnGet()
        {
            if (!_gameStateHelper.IsInitialized())
            {
                Player1Cards = _cardManager.GetRandomCards(8);
                Player2Cards = _cardManager.GetRandomCards(8);
                _gameStateHelper.InitializeGameState(Player1Cards, Player2Cards, _laneManager.Lanes);
            }
            else
            {
                _gameStateHelper.LoadGameState(
                    out var tempPlayer1Cards,
                    out var tempPlayer2Cards,
                    out var tempCurrentLane,
                    out var tempPhase,
                    out var tempMessage,
                    out var tempLanes
                );
                Player1Cards = tempPlayer1Cards;
                Player2Cards = tempPlayer2Cards;
                CurrentLane = tempCurrentLane;
                Phase = tempPhase;
                Message = tempMessage;
                if (tempLanes.Any()) _laneManager.Lanes = tempLanes;
            }
        }

        public IActionResult OnPost(string? selectedCard = null, string? selectedLane = null)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
            Phase = HttpContext.Session.GetInt32("Phase") ?? 1;
            var currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);

            Player1Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player1Cards") ?? "");
            Player2Cards = SerializationHelper.DeserializePlayerCards(HttpContext.Session.GetString("Player2Cards") ?? "");

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (!string.IsNullOrEmpty(lanesSerialized))
            {
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);
            }

            if (Phase == 1)
                return _phaseManager.HandlePhase1(this, currentPlayer, selectedCard, selectedLane);
            else if (Phase == 2)
                return _phaseManager.HandlePhase2(this, currentPlayer, selectedCard, selectedLane);

            SaveState();
            return Page();
        }

        [HttpPost]
        public IActionResult OnPostDiscardLaneClick([FromBody] LaneDiscardData data)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
            Phase = HttpContext.Session.GetInt32("Phase") ?? 1;
            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (string.IsNullOrEmpty(lanesSerialized))
                return new JsonResult(new { success = false, message = "Lanes data not found." });

            _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);

            if (string.IsNullOrEmpty(data.Lane))
                return new JsonResult(new { success = false, message = "Lane data is missing." });
            if (!int.TryParse(data.Lane, out int laneNum) || laneNum < 1 || laneNum > 6)
                return new JsonResult(new { success = false, message = "Invalid lane number." });

            _laneManager.DiscardLane(laneNum);
            _playerManager.SwitchPlayer(HttpContext.Session);
            _gameStateHelper.SaveGameState(player1Hand, player2Hand, CurrentLane, Phase, Message, _laneManager.Lanes);
            return new JsonResult(new { success = true, message = $"Discarded lane {laneNum}." });
        }

        [HttpPost]
        public IActionResult OnPostDiscardCardClick([FromBody] CardDiscardData data)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
            Phase = HttpContext.Session.GetInt32("Phase") ?? 1;

            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (!string.IsNullOrEmpty(lanesSerialized))
                _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);

            if (string.IsNullOrEmpty(data.Face) || string.IsNullOrEmpty(data.Suit))
                return new JsonResult(new { success = false, message = "Invalid card data." });

            var currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);

            if (currentPlayer == "Player 1")
            {
                var found = player1Hand.FirstOrDefault(c => c.Face == data.Face && c.Suit == data.Suit);
                if (found == null)
                    return new JsonResult(new { success = false, message = "Card not found in Player 1's hand." });
                player1Hand.Remove(found);
                _playerManager.AddRandomCardIfNecessary("Player 1", player1Hand);
            }
            else
            {
                var found = player2Hand.FirstOrDefault(c => c.Face == data.Face && c.Suit == data.Suit);
                if (found == null)
                    return new JsonResult(new { success = false, message = "Card not found in Player 2's hand." });
                player2Hand.Remove(found);
                _playerManager.AddRandomCardIfNecessary("Player 2", player2Hand);
            }

            _playerManager.SwitchPlayer(HttpContext.Session);
            _gameStateHelper.SaveGameState(player1Hand, player2Hand, CurrentLane, Phase, Message, _laneManager.Lanes);
            return new JsonResult(new { success = true, message = $"Discarded card {data.Face} {data.Suit}." });
        }

        [HttpPost]
        public IActionResult OnPostPlaceCardNextTo([FromBody] CardPlacementData data)
        {
            CurrentLane = HttpContext.Session.GetInt32("CurrentLane") ?? 1;
            Phase = HttpContext.Session.GetInt32("Phase") ?? 1;

            var p1Serialized = HttpContext.Session.GetString("Player1Cards") ?? "";
            var p2Serialized = HttpContext.Session.GetString("Player2Cards") ?? "";
            var player1Hand = SerializationHelper.DeserializePlayerCards(p1Serialized);
            var player2Hand = SerializationHelper.DeserializePlayerCards(p2Serialized);

            var lanesSerialized = HttpContext.Session.GetString("Lanes") ?? "";
            if (string.IsNullOrEmpty(lanesSerialized))
                return new JsonResult(new { success = false, message = "Lanes data not found." });

            _laneManager.Lanes = SerializationHelper.DeserializeLanes(lanesSerialized);

            if (string.IsNullOrEmpty(data.Card) || string.IsNullOrEmpty(data.AttachedCard))
                return new JsonResult(new { success = false, message = "Card or AttachedCard data is missing." });

            var currentPlayer = _playerManager.GetCurrentPlayer(HttpContext.Session);

            var cardParts = data.Card.Split(' ');
            if (cardParts.Length < 2)
                return new JsonResult(new { success = false, message = "Invalid card format." });
            var baseCardFace = cardParts[0];
            var baseCardSuit = cardParts[1];

            if (data.Lane < 1 || data.Lane > _laneManager.Lanes.Count)
                return new JsonResult(new { success = false, message = "Invalid lane number." });

            var lane = _laneManager.Lanes[data.Lane - 1];
            if (data.CardIndex < 0 || data.CardIndex >= lane.Count)
                return new JsonResult(new { success = false, message = "Invalid card index." });

            var baseCard = lane[data.CardIndex];
            if (baseCard.Face != baseCardFace || baseCard.Suit != baseCardSuit)
                return new JsonResult(new { success = false, message = "Card not found in specified lane and index." });

            var attachedParts = data.AttachedCard.Split(' ');
            if (attachedParts.Length < 2)
                return new JsonResult(new { success = false, message = "Invalid attached card format." });

            var attachedFace = attachedParts[0];
            var attachedSuit = attachedParts[1];

            Card? cardToAttach;
            if (currentPlayer == "Player 1")
            {
                cardToAttach = player1Hand.FirstOrDefault(c => c.Face == attachedFace && c.Suit == attachedSuit);
                if (cardToAttach == null)
                    return new JsonResult(new { success = false, message = "Attached card not found in Player 1's hand." });
                player1Hand.Remove(cardToAttach);
                _playerManager.AddRandomCardIfNecessary("Player 1", player1Hand);
            }
            else
            {
                cardToAttach = player2Hand.FirstOrDefault(c => c.Face == attachedFace && c.Suit == attachedSuit);
                if (cardToAttach == null)
                    return new JsonResult(new { success = false, message = "Attached card not found in Player 2's hand." });
                player2Hand.Remove(cardToAttach);
                _playerManager.AddRandomCardIfNecessary("Player 2", player2Hand);
            }

            if (attachedFace == "J")
            {
                lane.Remove(baseCard);
            }
            else if (attachedFace == "Q")
            {
                if (data.CardIndex != lane.Count - 1)
                    return new JsonResult(new { success = false, message = "Queens can only attach to the last card in a lane." });
                baseCard.AttachedCards.Add(cardToAttach);
                if (baseCard.Direction == "up") baseCard.Direction = "down";
                else if (baseCard.Direction == "down") baseCard.Direction = "up";
            }
            else
            {
                baseCard.AttachedCards.Add(cardToAttach);
            }

            _playerManager.SwitchPlayer(HttpContext.Session);
            Player1Cards = player1Hand;
            Player2Cards = player2Hand;
            _gameStateHelper.SaveGameState(Player1Cards, Player2Cards, CurrentLane, Phase, Message, _laneManager.Lanes);
            return new JsonResult(new { success = true });
        }

        public void SaveState()
        {
            _gameStateHelper.SaveGameState(Player1Cards, Player2Cards, CurrentLane, Phase, Message, _laneManager.Lanes);
        }

        public int SwitchLane(string currentPlayer, int currentLane)
        {
            if (currentPlayer == "Player 1")
            {
                return currentLane switch
                {
                    1 => 4,
                    2 => 5,
                    3 => 6,
                    _ => 1
                };
            }
            else
            {
                return currentLane switch
                {
                    4 => 2,
                    5 => 3,
                    6 => 1,
                    _ => 4
                };
            }
        }

        public int CalculateLaneScore(int lane)
        {
            return _laneManager.CalculateLaneScore(lane);
        }
    }
}

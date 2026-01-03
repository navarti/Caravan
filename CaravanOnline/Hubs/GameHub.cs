using Microsoft.AspNetCore.SignalR;
using CaravanOnline.Models;
using CaravanOnline.Models.DTO;
using CaravanOnline.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace CaravanOnline.Hubs
{
    public class GameHub : Hub
    {
        private readonly OnlineGameStateService _gameStateService;
        private readonly CardManager _cardManager;
        private readonly LaneManager _laneManager;
        private readonly PlayerManager _playerManager;

        public GameHub(
            OnlineGameStateService gameStateService,
            CardManager cardManager,
            LaneManager laneManager,
            PlayerManager playerManager)
        {
            _gameStateService = gameStateService;
            _cardManager = cardManager;
            _laneManager = laneManager;
            _playerManager = playerManager;
        }

        public async Task CreateRoom(string playerName)
        {
            var roomId = _gameStateService.CreateRoom(Context.ConnectionId, playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", roomId, playerName);
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            var success = _gameStateService.JoinRoom(roomId, Context.ConnectionId, playerName);
            if (success)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                var room = _gameStateService.GetRoom(roomId);
                if (room != null)
                {
                    await Clients.Group(roomId).SendAsync("PlayerJoined", playerName, room.Player1Name, room.Player2Name);
                    await StartGame(roomId);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("JoinFailed", "Room is full or does not exist.");
            }
        }

        public async Task GetAvailableRooms()
        {
            var rooms = _gameStateService.GetAvailableRooms();
            await Clients.Caller.SendAsync("AvailableRooms", rooms);
        }

        public async Task RejoinRoom(string roomId, string playerName)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Room not found.");
                return;
            }

            // Update connection ID based on player name
            if (room.Player1Name == playerName)
            {
                room.Player1ConnectionId = Context.ConnectionId;
                if (room.CurrentPlayerConnectionId != room.Player1ConnectionId && 
                    room.CurrentPlayerConnectionId != room.Player2ConnectionId)
                {
                    room.CurrentPlayerConnectionId = room.Player1ConnectionId;
                }
            }
            else if (room.Player2Name == playerName)
            {
                room.Player2ConnectionId = Context.ConnectionId;
                if (room.CurrentPlayerConnectionId != room.Player1ConnectionId && 
                    room.CurrentPlayerConnectionId != room.Player2ConnectionId)
                {
                    room.CurrentPlayerConnectionId = room.Player2ConnectionId ?? room.Player1ConnectionId;
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Player not in this room.");
                return;
            }

            // Add connection to the room group
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Send current game state to the caller immediately
            await BroadcastGameState(roomId);
        }

        private async Task StartGame(string roomId)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null || !room.IsFull()) return;

            room.Player1Cards = _cardManager.GetRandomCards(8);
            room.Player2Cards = _cardManager.GetRandomCards(8);
            room.Lanes = new List<List<Card>>
            {
                new List<Card>(), new List<Card>(), new List<Card>(),
                new List<Card>(), new List<Card>(), new List<Card>()
            };
            room.IsGameStarted = true;
            room.CurrentPlayerConnectionId = room.Player1ConnectionId;
            room.CurrentLane = 1;
            room.Phase = 1;

            await BroadcastGameState(roomId);
        }

        public async Task PlaceCard(string roomId, string selectedCard, int selectedLane)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null || Context.ConnectionId != room.CurrentPlayerConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Not your turn or invalid room.");
                return;
            }

            var cardParts = selectedCard.Split(' ');
            if (cardParts.Length < 2)
            {
                await Clients.Caller.SendAsync("Error", "Invalid card format.");
                return;
            }

            var face = cardParts[0];
            var suit = cardParts[1];
            var playerNumber = room.GetPlayerNumber(Context.ConnectionId);
            var playerHand = playerNumber == 1 ? room.Player1Cards : room.Player2Cards;

            var card = playerHand.FirstOrDefault(c => c.Face == face && c.Suit == suit);
            if (card == null)
            {
                await Clients.Caller.SendAsync("Error", "Card not found in hand.");
                return;
            }

            if (selectedLane < 1 || selectedLane > 6)
            {
                await Clients.Caller.SendAsync("Error", "Invalid lane number.");
                return;
            }

            // Phase 1: Must play in current lane
            if (room.Phase == 1 && selectedLane != room.CurrentLane)
            {
                await Clients.Caller.SendAsync("Error", $"Phase 1: You must play in lane {room.CurrentLane}.");
                return;
            }

            // Use LaneManager to validate and add card
            _laneManager.Lanes = room.Lanes;
            bool success = _laneManager.AddCardToLane(selectedLane, card);

            if (!success)
            {
                await Clients.Caller.SendAsync("Error", _laneManager.LastFailReason);
                return;
            }

            // Update room lanes with the validated result
            room.Lanes = _laneManager.Lanes;
            playerHand.Remove(card);
            _playerManager.AddRandomCardIfNecessary($"Player {playerNumber}", playerHand);

            // Handle Phase 1 logic
            if (room.Phase == 1)
            {
                // Check if all lanes have at least 1 card
                if (room.Lanes.All(l => l.Count >= 1))
                {
                    room.Phase = 2;
                    room.CurrentPlayerConnectionId = room.Player1ConnectionId;
                    room.Message = "Phase 2: Build your caravans!";
                }
                else
                {
                    // Switch lane for current player
                    room.CurrentLane = SwitchLane(playerNumber, room.CurrentLane);
                    room.SwitchPlayer();
                }
            }
            else
            {
                // Phase 2: Just switch players
                room.SwitchPlayer();
            }

            await BroadcastGameState(roomId);
        }

        private int SwitchLane(int playerNumber, int currentLane)
        {
            if (playerNumber == 1)
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

        public async Task DiscardCard(string roomId, string face, string suit)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null || Context.ConnectionId != room.CurrentPlayerConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Not your turn or invalid room.");
                return;
            }

            var playerNumber = room.GetPlayerNumber(Context.ConnectionId);
            var playerHand = playerNumber == 1 ? room.Player1Cards : room.Player2Cards;

            var card = playerHand.FirstOrDefault(c => c.Face == face && c.Suit == suit);
            if (card == null)
            {
                await Clients.Caller.SendAsync("Error", "Card not found in hand.");
                return;
            }

            playerHand.Remove(card);
            _playerManager.AddRandomCardIfNecessary($"Player {playerNumber}", playerHand);

            room.SwitchPlayer();
            await BroadcastGameState(roomId);
        }

        public async Task DiscardLane(string roomId, int laneNumber)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null || Context.ConnectionId != room.CurrentPlayerConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Not your turn or invalid room.");
                return;
            }

            if (laneNumber < 1 || laneNumber > 6) return;

            room.Lanes[laneNumber - 1].Clear();

            room.SwitchPlayer();
            await BroadcastGameState(roomId);
        }

        public async Task PlaceCardNextTo(string roomId, string card, string attachedCard, int lane, int cardIndex)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null || Context.ConnectionId != room.CurrentPlayerConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Not your turn or invalid room.");
                return;
            }

            var cardParts = card.Split(' ');
            if (cardParts.Length < 2)
            {
                await Clients.Caller.SendAsync("Error", "Invalid card format.");
                return;
            }

            var attachedParts = attachedCard.Split(' ');
            if (attachedParts.Length < 2)
            {
                await Clients.Caller.SendAsync("Error", "Invalid attached card format.");
                return;
            }

            var playerNumber = room.GetPlayerNumber(Context.ConnectionId);
            var playerHand = playerNumber == 1 ? room.Player1Cards : room.Player2Cards;

            var attachedFace = attachedParts[0];
            var attachedSuit = attachedParts[1];

            // Validate face card (only J, Q, K can be attached)
            if (attachedFace != "J" && attachedFace != "Q" && attachedFace != "K")
            {
                await Clients.Caller.SendAsync("Error", "Only Jacks, Queens, and Kings can be attached to cards.");
                return;
            }

            var cardToAttach = playerHand.FirstOrDefault(c => c.Face == attachedFace && c.Suit == attachedSuit);
            if (cardToAttach == null)
            {
                await Clients.Caller.SendAsync("Error", "Card not found in hand.");
                return;
            }

            if (lane < 1 || lane > room.Lanes.Count)
            {
                await Clients.Caller.SendAsync("Error", "Invalid lane number.");
                return;
            }

            var laneCards = room.Lanes[lane - 1];
            if (cardIndex < 0 || cardIndex >= laneCards.Count)
            {
                await Clients.Caller.SendAsync("Error", "Invalid card index.");
                return;
            }

            var baseCard = laneCards[cardIndex];
            var baseFace = cardParts[0];
            var baseSuit = cardParts[1];

            if (baseCard.Face != baseFace || baseCard.Suit != baseSuit)
            {
                await Clients.Caller.SendAsync("Error", "Target card mismatch.");
                return;
            }

            // Handle Jack - removes the target card
            if (attachedFace == "J")
            {
                laneCards.Remove(baseCard);
                playerHand.Remove(cardToAttach);
                _playerManager.AddRandomCardIfNecessary($"Player {playerNumber}", playerHand);
            }
            // Handle Queen - reverses direction, can only attach to last card
            else if (attachedFace == "Q")
            {
                if (cardIndex != laneCards.Count - 1)
                {
                    await Clients.Caller.SendAsync("Error", "Queens can only attach to the last card in a lane.");
                    return;
                }
                baseCard.AttachedCards.Add(cardToAttach);
                baseCard.Direction = baseCard.Direction == "up" ? "down" : "up";
                playerHand.Remove(cardToAttach);
                _playerManager.AddRandomCardIfNecessary($"Player {playerNumber}", playerHand);
            }
            // Handle King - doubles the card value
            else if (attachedFace == "K")
            {
                baseCard.AttachedCards.Add(cardToAttach);
                playerHand.Remove(cardToAttach);
                _playerManager.AddRandomCardIfNecessary($"Player {playerNumber}", playerHand);
            }

            room.SwitchPlayer();
            await BroadcastGameState(roomId);
        }

        private async Task BroadcastGameState(string roomId)
        {
            var room = _gameStateService.GetRoom(roomId);
            if (room == null) return;

            var laneScores = new List<int>();
            for (int i = 0; i < room.Lanes.Count; i++)
            {
                _laneManager.Lanes = room.Lanes;
                laneScores.Add(_laneManager.CalculateLaneScore(i + 1));
            }

            // Check if game should be evaluated
            _laneManager.Lanes = room.Lanes;
            string gameResult = _laneManager.EvaluateGame();
            bool isGameComplete = !gameResult.Contains("ongoing");

            if (isGameComplete)
            {
                room.Message = gameResult;
            }

            await Clients.Group(roomId).SendAsync("GameStateUpdated", new
            {
                player1Cards = room.Player1Cards,
                player2Cards = room.Player2Cards,
                lanes = room.Lanes,
                currentPlayerConnectionId = room.CurrentPlayerConnectionId,
                currentPlayerName = room.GetCurrentPlayerName(),
                currentLane = room.CurrentLane,
                phase = room.Phase,
                message = room.Message,
                laneScores = laneScores,
                player1Name = room.Player1Name,
                player2Name = room.Player2Name,
                isGameComplete = isGameComplete,
                gameResult = isGameComplete ? gameResult : null
            });
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Don't remove room if game is in progress (player might be reconnecting)
            var roomId = _gameStateService.GetRoomIdByConnection(Context.ConnectionId);
            if (roomId != null)
            {
                var room = _gameStateService.GetRoom(roomId);
                if (room != null && !room.IsGameStarted)
                {
                    // Only remove if game hasn't started (lobby disconnect)
                    _gameStateService.RemovePlayerFromRoom(Context.ConnectionId);
                }
                // If game has started, keep the room for reconnection
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}

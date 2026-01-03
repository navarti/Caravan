# Caravan Online - Multiplayer Implementation

## Overview
This implementation adds **online multiplayer** functionality to the Caravan card game, allowing two players to play against each other from different machines over the internet using real-time communication.

## What's New

### Architecture Changes
- **SignalR Integration**: Real-time bidirectional communication between clients and server
- **Game Room System**: Players can create and join game rooms with unique room codes
- **Separate Game Modes**: 
  - **Local Mode** (`/Index`): Original same-screen multiplayer (preserved)
  - **Online Mode** (`/Lobby` ? `/IndexOnline`): New online multiplayer

### New Files Created

#### Backend (C#)
1. **Models/GameRoom.cs**
   - Manages game room state for each online match
   - Tracks both players, their cards, lanes, and turn information
   - Handles player switching and game state

2. **Hubs/GameHub.cs**
   - SignalR hub for real-time communication
   - Handles all game actions: creating/joining rooms, placing cards, discarding, etc.
   - Broadcasts game state updates to all players in a room

3. **Services/OnlineGameStateService.cs**
   - Manages all active game rooms in memory
   - Provides room creation, joining, and lookup functionality
   - Cleans up rooms when players disconnect

4. **Pages/Lobby.cshtml & Lobby.cshtml.cs**
   - Matchmaking lobby where players can:
     - Create new game rooms
     - Join existing rooms by code
     - Browse available rooms

5. **Pages/IndexOnline.cshtml & IndexOnline.cshtml.cs**
   - Online game interface (rendered dynamically via JavaScript)
   - Real-time game board that updates automatically

#### Frontend (JavaScript)
1. **wwwroot/js/lobby.js**
   - SignalR client for lobby functionality
   - Handles room creation, joining, and room listing
   - Redirects to game when both players are ready

2. **wwwroot/js/game.js**
   - SignalR client for in-game functionality
   - Renders game state dynamically (player hands, lanes, scores)
   - Sends player actions to server
   - Receives and applies real-time game state updates

#### Modified Files
1. **Program.cs**
   - Added SignalR services and hub endpoint configuration
   - Registered `OnlineGameStateService` as singleton

2. **CaravanOnline.csproj**
   - Added `Microsoft.AspNetCore.SignalR.Client` package reference

3. **Pages/Shared/_Layout.cshtml**
   - Updated navigation to include "Local Game" and "Online Game" links

4. **wwwroot/css/site.css**
   - Added styles for card selection, hover effects, and disabled states

## How to Play Online

### For Player 1 (Host):
1. Navigate to **Online Game** in the navigation menu (`/Lobby`)
2. Enter your name
3. Click **Create Room**
4. Share the generated **Room Code** with your opponent
5. Wait for Player 2 to join
6. Game starts automatically when both players are connected

### For Player 2 (Guest):
1. Navigate to **Online Game** (`/Lobby`)
2. Enter your name
3. Either:
   - Enter the **Room Code** provided by Player 1 and click **Join Room**, OR
   - Click **Refresh** to see available rooms and join one
4. Game starts automatically

### During the Game:
- Only the current player can take actions (buttons are disabled for the waiting player)
- The game board updates in real-time for both players
- Each player sees only their own cards
- Turn automatically switches after each action
- Actions include:
  - Placing cards in lanes
  - Attaching cards to existing lane cards
  - Discarding cards
  - Discarding entire lanes

## Technical Details

### SignalR Hub Methods (Server ? Client)
- `RoomCreated(roomId, playerName)` - Confirms room creation
- `PlayerJoined(playerName, player1Name, player2Name)` - Notifies when opponent joins
- `GameStateUpdated(gameState)` - Broadcasts updated game state
- `JoinFailed(message)` - Notifies of join errors
- `AvailableRooms(rooms)` - Lists available rooms
- `Error(message)` - Sends error messages

### SignalR Client ? Server Methods
- `CreateRoom(playerName)` - Creates a new game room
- `JoinRoom(roomId, playerName)` - Joins an existing room
- `GetAvailableRooms()` - Requests list of available rooms
- `PlaceCard(roomId, selectedCard, selectedLane)` - Places a card in a lane
- `DiscardCard(roomId, face, suit)` - Discards a card from hand
- `DiscardLane(roomId, laneNumber)` - Discards all cards in a lane
- `PlaceCardNextTo(roomId, card, attachedCard, lane, cardIndex)` - Attaches a card to another

### Game State Management
- **In-Memory Storage**: Game rooms are stored in a thread-safe `ConcurrentDictionary`
- **Auto-Cleanup**: Rooms are automatically removed when a player disconnects
- **Connection Tracking**: Each player identified by their SignalR connection ID
- **Turn Management**: Server validates that only the current player can make moves

## Deployment Considerations

### For Production:
1. **Persistent Storage**: Consider replacing in-memory room storage with Redis or a database for scalability
2. **Authentication**: Add user authentication for better player identification
3. **Game History**: Store completed games for statistics and replay
4. **Reconnection**: Implement reconnection logic for network interruptions
5. **Room Expiration**: Add timeout logic to clean up abandoned rooms
6. **Spectator Mode**: Allow others to watch ongoing games

### Configuration:
- SignalR hub endpoint: `/gameHub`
- Default session timeout: 30 minutes (inherited from existing configuration)
- No authentication required (trust-based for now)

## Backward Compatibility
The original **local multiplayer mode** remains fully functional and unchanged:
- Navigate to "Local Game" (`/Index`) to play the same-screen version
- All existing game logic, services, and session-based state management preserved
- Both modes can coexist and run simultaneously

## Testing Locally
1. Build and run the application: `dotnet run`
2. Open two different browsers (or incognito windows)
3. In Browser 1: Go to `/Lobby`, create a room
4. In Browser 2: Go to `/Lobby`, join the room using the code
5. Play the game with real-time updates across both browsers

## Future Enhancements
- Chat system for players
- Game lobby with multiple simultaneous rooms
- Matchmaking queue system
- Player rankings and statistics
- Game replay functionality
- Mobile-responsive design improvements
- Sound effects and animations

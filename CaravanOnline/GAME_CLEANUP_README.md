# Game Resource Management - Memory Leak Prevention

## Overview
This implementation prevents memory leaks by automatically cleaning up abandoned game rooms that have no recent activity.

## Components

### 1. GameRoom Timestamp Tracking
- **CreatedAt**: Records when the game room was created (UTC)
- **LastActivityAt**: Updated whenever any game action occurs (UTC)
- **UpdateLastActivity()**: Method to update the LastActivityAt timestamp

### 2. GameCleanupService (Background Service)
A hosted background service that runs periodically to clean up abandoned games.

**Configuration:**
- **Check Interval**: 5 minutes (how often cleanup runs)
- **Abandoned Threshold**: 30 minutes (games with no activity for 30+ minutes are removed)

**Logging:**
- Logs when service starts/stops
- Logs how many abandoned games are found
- Logs details of each removed game (RoomId, creation time, last activity, players)
- Logs errors if cleanup fails (service continues running)

### 3. Activity Tracking
The following actions update `LastActivityAt`:
- Creating a room
- Joining a room
- Rejoining a room
- Starting a game
- Placing a card
- Placing a card next to another (face cards)
- Discarding a card
- Discarding a lane
- Switching players

### 4. OnlineGameStateService Methods
**New Methods:**
- `RemoveRoom(string roomId)`: Explicitly removes a room by ID
- `GetAbandonedRooms(TimeSpan threshold)`: Returns rooms with LastActivityAt older than threshold
- `GetActiveRoomCount()`: Returns total number of active rooms

**Updated Methods:**
- `JoinRoom()`: Now calls `UpdateLastActivity()`

## Memory Management

### Cleanup Process
1. Background service runs every 5 minutes
2. Finds all rooms with LastActivityAt > 30 minutes ago
3. Logs details of abandoned games
4. Removes room from dictionary
5. Explicitly clears large collections (Player1Cards, Player2Cards, Lanes) to help GC

### What Gets Cleaned Up
- Game rooms where both players have been inactive for 30+ minutes
- All associated game state (cards, lanes, attached cards)
- Connection mappings

### What Doesn't Get Cleaned Up
- Active games (any game with activity in the last 30 minutes)
- Games where players are still connected and playing

## Configuration

You can adjust the cleanup behavior by modifying values in `GameCleanupService.cs`:

```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);      // How often to check
private readonly TimeSpan _abandonedThreshold = TimeSpan.FromMinutes(30); // When to consider abandoned
```

## Monitoring

Check application logs for cleanup activity:
```
[Information] Game Cleanup Service started.
[Information] Found 2 abandoned game(s) to clean up
[Information] Cleaning up abandoned game: RoomId=ABC123, Created=2024-01-15 10:30:00 UTC, LastActivity=2024-01-15 10:45:00 UTC, Players=Alice/Bob
[Information] Successfully cleaned up 2 abandoned game(s)
```

## Benefits
1. **Prevents Memory Leaks**: Automatically removes abandoned game rooms
2. **No Manual Intervention**: Runs automatically in the background
3. **Graceful Handling**: Allows reconnections within 30-minute window
4. **Observable**: Detailed logging for monitoring and debugging
5. **Resilient**: Continues running even if individual cleanup operations fail

## Technical Details

### Thread Safety
- Uses `ConcurrentDictionary` for thread-safe room storage
- Background service runs on separate thread
- No locks needed due to concurrent collection usage

### GC Optimization
- Explicitly clears large collections before removing rooms
- Helps garbage collector reclaim memory faster
- Prevents retention of large object graphs

### Service Lifecycle
- Starts automatically when application starts
- Stops gracefully when application shuts down
- Registered as `IHostedService` in DI container

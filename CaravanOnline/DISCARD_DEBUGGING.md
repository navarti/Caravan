# Debugging Discard Buttons Not Working

## Console Logging Added

When you click the discard buttons, you should see detailed console output. Open browser Developer Tools (F12) and check the Console tab.

### Expected Console Output

#### When Page Loads:
```
[Init] DOM Content Loaded, initializing game connection
[Init] Discard Lane button found and event listener attached
[Init] Discard Card button found and event listener attached
Connected to game
Rejoined room: ABC123
```

#### When Game State Updates:
```
[UpdateGameUI] Turn check - myConnectionId: xyz123 currentPlayerConnectionId: xyz123 isMyTurn: true
[UpdateGameUI] Buttons state - discardLaneBtn.disabled: false discardCardBtn.disabled: false
[UpdateGameUI] Button visibility - discardLaneBtn: found discardCardBtn: found
[UpdateGameUI] Are buttons clickable? discardLaneBtn.onclick: function window.activateDiscardLane: function
```

#### When Clicking "Discard Lane" Button:
```
[Button] Discard Lane button clicked (event listener) disabled: false
=== BUTTON CLICKED: Discard Lane ===
[DiscardLane] Activating discard lane mode
[DiscardLane] Current state - gameConnection: [object Object] currentRoomId: ABC123
Select a lane to discard (info message appears)
```

#### When Clicking a Lane Header (in discard mode):
```
[SelectLane] Lane clicked: 2 discardLaneMode: true selectedCard: null
[SelectLane] Discard lane mode active, calling discardLaneAction
[DiscardLane] Starting discard lane action for lane: 2
[DiscardLane] Invoking DiscardLane with roomId: ABC123 laneNum: 2
[DiscardLane] Successfully discarded lane 2
```

#### When Clicking "Discard Card" Button:
```
[Button] Discard Card button clicked (event listener) disabled: false
=== BUTTON CLICKED: Discard Card ===
[DiscardCard] Activating discard card mode
[DiscardCard] Current state - gameConnection: [object Object] currentRoomId: ABC123
Select a card from your hand to discard (info message appears)
```

#### When Clicking a Card (in discard mode):
```
[HighlightCards] Card clicked: 5 Hearts discardCardMode: true
[HighlightCards] Discard card mode active, calling discardCardAction
[DiscardCard] Starting discard card action with selectedCard: 5 Hearts
[DiscardCard] Invoking DiscardCard with roomId: ABC123 face: 5 suit: Hearts
[DiscardCard] Successfully discarded card: 5 Hearts
```

## Troubleshooting

### Issue 1: No Console Output When Clicking Buttons

**Symptoms:** Clicking buttons shows no console output at all

**Possible Causes:**
1. JavaScript file not loading
2. IIFE preventing function exposure
3. Browser caching old version

**Solutions:**
1. Hard refresh: Ctrl+Shift+R (or Cmd+Shift+R on Mac)
2. Clear browser cache
3. Check Network tab to verify game.js is loading (200 status)
4. Check Console for JavaScript errors on page load

### Issue 2: Buttons Are Disabled (Grayed Out)

**Symptoms:** 
- Buttons appear grayed out and non-clickable
- Console shows: `discardLaneBtn.disabled: true`

**Possible Causes:**
1. It's not your turn (`isMyTurn: false`)
2. Wrong player connection ID

**Check Console:**
Look for this line:
```
[UpdateGameUI] Turn check - myConnectionId: xyz123 currentPlayerConnectionId: abc456 isMyTurn: false
```

If `isMyTurn: false`, the buttons are correctly disabled because it's not your turn.

**Solutions:**
1. Wait for your turn
2. If playing alone for testing, use two browser windows/tabs
3. Check if connection IDs match

### Issue 3: Click Shows Error Message

**Symptoms:** Console shows error logs

**Common Errors:**

#### "No game connection available"
```
[DiscardCard] No game connection available
```
**Solution:** Wait for SignalR connection to establish. Look for "Connected to game" in console.

#### "Room not found"
```
[DiscardLane] No room ID available
```
**Solution:** Rejoin the room or refresh the page

#### "Not your turn or invalid room"
```
[DiscardCard] Error discarding card: Error: Not your turn or invalid room
```
**Solution:** Wait for your turn

#### "Card not found in hand"
```
[DiscardCard] Error: Card not found in hand
```
**Solution:** Make sure you're clicking a card from YOUR hand, not from the lanes

### Issue 4: Action Completes But Nothing Happens

**Symptoms:** 
- Console shows "Successfully discarded"
- But UI doesn't update

**Check:**
1. Look for `[BroadcastGameState]` in server logs
2. Check if `GameStateUpdated` event is received:
   - Should trigger `[UpdateGameUI]` console log

**Possible Cause:** Game state not broadcasting properly

**Solution:** Check server-side logs for errors in BroadcastGameState method

### Issue 5: Buttons Work But Server Rejects Action

**Server-Side Errors** (check Output window in Visual Studio):
```
[DiscardCard] Access denied - RoomId: ABC123, ConnectionId: xyz123
[DiscardLane] Invalid lane number: 7
```

**Solutions:**
1. Access denied: Not your turn or wrong room
2. Invalid lane: Check lane number is 1-6

## Testing Procedure

1. **Open Browser Developer Tools** (F12)
2. **Navigate to Console tab**
3. **Load the game page**
4. **Join a room and start a game**
5. **Wait for your turn** (check console for `isMyTurn: true`)
6. **Click "Discard Lane"**
   - Should see "=== BUTTON CLICKED: Discard Lane ===" immediately
   - Should see status message: "Select a lane to discard"
7. **Click a lane header**
   - Should see discard action execute
   - Lane should clear
8. **On next turn, click "Discard Card"**
   - Should see "=== BUTTON CLICKED: Discard Card ===" immediately
   - Should see status message: "Select a card from your hand to discard"
9. **Click a card in your hand**
   - Should see discard action execute
   - Card should be removed and replaced

## Quick Test

Copy and paste this in the browser console when on the game page:

```javascript
// Test if functions are available
console.log('activateDiscardLane:', typeof window.activateDiscardLane);
console.log('activateDiscardCard:', typeof window.activateDiscardCard);

// Test calling them directly
window.activateDiscardLane();
// You should see console logs and status message appear

// Reset
window.resetClientState();
```

If you see "function" for both, the functions are properly exposed.

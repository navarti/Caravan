# Fix for Both Local and Online Game Discard Functions

## Problem
The discard functions were removed from `site.js` to fix the online game, which broke the local game.

## Root Cause
- **Local game** (`Index.cshtml`) relies on `site.js` for discard functionality
- **Online game** (`IndexOnline.cshtml`) loads both `site.js` (from layout) and `game.js`
- When both scripts were loaded, `site.js` versions were being called instead of `game.js` versions
- Removing functions from `site.js` fixed online but broke local

## Solution

### 1. Restored Functions to site.js
Re-added the discard functions to `site.js` for the local game:
```javascript
let discardLaneMode = false;
let discardCardMode = false;

function activateDiscardLane() {
    discardLaneMode = true;
    discardCardMode = false;
    setUserMessage("Click on the lane button you want to discard.");
}

function activateDiscardCard() {
    discardLaneMode = false;
    discardCardMode = true;
    setUserMessage("Click on a card in your HAND to discard it.");
}
```

### 2. Ensured game.js Overwrites for Online Game
Added flag in `game.js` to mark when online version is active:
```javascript
window.USING_ONLINE_GAME = true;
window.activateDiscardLane = activateDiscardLane;
window.activateDiscardCard = activateDiscardCard;
// ... other functions
```

### 3. How It Works

#### Local Game (Index.cshtml):
1. **Loads:** `site.js` (from `_Layout.cshtml`)
2. **Uses:** Functions from `site.js`
3. **Does NOT load:** `game.js`
4. **Discard functions:** POST to Razor Page handlers (`/?handler=DiscardCardClick`)

#### Online Game (IndexOnline.cshtml):
1. **Loads:** `site.js` (from `_Layout.cshtml`)
2. **Then loads:** `game.js` (from page-specific script tag)
3. **game.js OVERWRITES:** All discard functions with online versions
4. **Discard functions:** Use SignalR to call `GameHub.DiscardCard()` and `GameHub.DiscardLane()`

### 4. Verification

**Local Game Console Output:**
```
site.js loaded successfully.
```
- No `[Exposure]` logs (game.js not loaded)
- Clicking "Discard Card" calls `site.js` version
- Uses traditional form POST

**Online Game Console Output:**
```
site.js loaded successfully.
[Exposure] BEFORE exposure - window.activateDiscardCard: function
[Exposure] AFTER exposure - Functions exposed to window (ONLINE GAME):
  - window.USING_ONLINE_GAME: true
  - window.activateDiscardLane: function
  - window.activateDiscardCard: function
=== BUTTON CLICKED: Discard Card ===  ? This confirms game.js version is being called
[DiscardCard] Activating discard card mode
```

## Files Modified

### CaravanOnline\wwwroot\js\site.js
- ? Restored `discardLaneMode` and `discardCardMode` variables
- ? Restored `activateDiscardLane()` function
- ? Restored `activateDiscardCard()` function
- ? Kept all local game functionality intact

### CaravanOnline\wwwroot\js\game.js
- ? Added `window.USING_ONLINE_GAME = true` flag
- ? Overwrites `site.js` functions for online game
- ? Comprehensive logging to verify correct version is called

## Testing

### Test Local Game:
1. Navigate to `/Index` (Local Game)
2. Open browser console (F12)
3. Should see: `site.js loaded successfully.`
4. Should NOT see: `[Exposure]` logs
5. Click "Discard Card"
6. Click a card from hand
7. Page should reload with card discarded

### Test Online Game:
1. Navigate to `/Lobby` ? Create/Join room
2. Open browser console (F12)
3. Should see: `site.js loaded successfully.`
4. Should see: `[Exposure] AFTER exposure - Functions exposed to window (ONLINE GAME):`
5. Should see: `window.USING_ONLINE_GAME: true`
6. Click "Discard Card"
7. Should see: `=== BUTTON CLICKED: Discard Card ===`
8. Click a card from hand
9. Card should be discarded via SignalR

## Why This Works

1. **Script Loading Order:**
   - `site.js` loads first (from layout)
   - `game.js` loads second (from IndexOnline.cshtml only)

2. **Function Overwriting:**
   - JavaScript allows later definitions to overwrite earlier ones
   - `game.js` explicitly sets `window.functionName = localFunction`
   - This overwrites the `site.js` versions

3. **Page-Specific Behavior:**
   - Local game page never loads `game.js`
   - Online game page loads both, with `game.js` taking precedence

## Key Takeaway

Both games now work correctly:
- **Local game:** Uses `site.js` functions with traditional POST
- **Online game:** Uses `game.js` functions with SignalR
- **No interference:** Each page uses the correct version

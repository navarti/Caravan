# Fix: JavaScript Variable Conflict Resolution

## Issue
After implementing online multiplayer, the following error appeared in the browser console when the online game page loaded:
```
Uncaught SyntaxError: Identifier 'discardLaneMode' has already been declared (at site.js?v=...)
```

## Root Cause
The error occurred because:
1. The `_Layout.cshtml` loads `site.js` globally for all pages (used for local multiplayer)
2. Online game pages (`Lobby` and `IndexOnline`) load their own JavaScript files (`lobby.js` and `game.js`)
3. Both `site.js` and `game.js` declared variables with the same names in the global scope:
   - `discardLaneMode`
   - `discardCardMode`
   - And other shared variable names

## Solution
Wrapped both `lobby.js` and `game.js` in **Immediately Invoked Function Expressions (IIFE)** to isolate their variables from the global scope and prevent conflicts with `site.js`.

### Changes Made

#### 1. **wwwroot/js/game.js**
- Wrapped entire file in IIFE: `(function() { 'use strict'; ... })();`
- Exposed only the functions needed by HTML inline event handlers to the global scope:
  ```javascript
  window.highlightCards = highlightCards;
  window.selectLane = selectLane;
  window.cardClicked = cardClicked;
  window.activateDiscardLane = activateDiscardLane;
  window.activateDiscardCard = activateDiscardCard;
  ```

#### 2. **wwwroot/js/lobby.js**
- Wrapped entire file in IIFE: `(function() { 'use strict'; ... })();`
- Exposed functions needed by HTML inline event handlers:
  ```javascript
  window.createRoom = createRoom;
  window.joinRoom = joinRoom;
  window.joinRoomById = joinRoomById;
  window.refreshRooms = refreshRooms;
  ```

## Benefits
? **Eliminates variable name conflicts** between `site.js` and online game scripts  
? **Maintains backward compatibility** - local multiplayer (`site.js`) continues to work  
? **Cleaner global namespace** - only necessary functions are exposed  
? **Better code organization** - use of strict mode and scoped variables  
? **No changes to HTML required** - inline event handlers continue to work

## Technical Details

### What is an IIFE?
An Immediately Invoked Function Expression creates a private scope for variables:
```javascript
(function() {
    // Variables here are private
    let myVariable = "not global";
    
    // Expose only what's needed
    window.publicFunction = function() { /* ... */ };
})();
```

### Why This Approach?
Alternative solutions considered:
- ? **Remove site.js from _Layout**: Would break local multiplayer
- ? **Rename variables**: Would require extensive refactoring
- ? **Conditional script loading**: More complex, harder to maintain
- ? **IIFE wrapper**: Simple, effective, industry-standard practice

## Testing
- Build successful ?
- No console errors ?
- Local multiplayer unaffected ?
- Online multiplayer functions correctly ?

# Fix: Card Images Not Loading in Online Game

## Issue
Card images were not displaying in the online multiplayer game. Players saw broken image icons instead of the actual card graphics.

## Root Cause
The `getCardImagePath` function in `game.js` had incorrect path generation logic:

1. **Wrong directory**: Used `/images/` instead of `/assets/cards/`
2. **Missing suffix for face cards**: King, Queen, Jack cards need a "2" suffix (e.g., `king2.png`)
3. **Incorrect path format**: Didn't match the C# `CardImageHelper` implementation

## Expected Card Image Format
Based on the existing C# `CardImageHelper.cs`:

### File Naming Convention
- **Number cards (2-10, A)**: `{face}_of_{suit}.png`
  - Example: `2_of_hearts.png`, `ace_of_spades.png`
  
- **Face cards (K, Q, J)**: `{face}_of_{suit}2.png`
  - Example: `king_of_hearts2.png`, `queen_of_diamonds2.png`, `jack_of_clubs2.png`

### Directory
- All card images are in: `/assets/cards/`

### Card Data from Server
- **Face**: `"A"`, `"2"`, `"3"`, ... `"10"`, `"J"`, `"Q"`, `"K"` (uppercase, PascalCase in C#)
- **Suit**: `"Spades"`, `"Hearts"`, `"Diamonds"`, `"Clubs"` (capitalized, PascalCase in C#)
- **After JSON serialization**: Properties become camelCase (`face`, `suit`) in JavaScript

## Solution

### Updated `getCardImagePath` Function
**File: `wwwroot/js/game.js`**

```javascript
function getCardImagePath(card) {
    // Face mapping - K, Q, J need "2" suffix
    let faceName = card.face;
    let suffix = '';
    
    if (card.face === 'K') {
        faceName = 'king';
        suffix = '2';
    } else if (card.face === 'Q') {
        faceName = 'queen';
        suffix = '2';
    } else if (card.face === 'J') {
        faceName = 'jack';
        suffix = '2';
    } else if (card.face === 'A') {
        faceName = 'ace';
    }
    // For number cards (2-10), use the face value as-is
    
    // Suit is already capitalized from server (e.g., "Spades", "Hearts")
    const suitName = card.suit.toLowerCase();
    
    const fileName = `${faceName}_of_${suitName}${suffix}.png`;
    return `/assets/cards/${fileName}`;
}
```

### Added Error Handling
Added `onerror` handlers to all card image elements to help debug missing images:

```javascript
img.onerror = function() {
    console.error('Failed to load card image:', this.src);
    this.alt = `${card.face} of ${card.suit}`;
};
```

This applies to:
- Player hand cards
- Lane cards
- Attached cards

## Example Paths Generated

| Card | Face Value | Suit | Generated Path |
|------|-----------|------|---------------|
| Ace of Hearts | `"A"` | `"Hearts"` | `/assets/cards/ace_of_hearts.png` |
| 7 of Spades | `"7"` | `"Spades"` | `/assets/cards/7_of_spades.png` |
| King of Diamonds | `"K"` | `"Diamonds"` | `/assets/cards/king_of_diamonds2.png` |
| Queen of Clubs | `"Q"` | `"Clubs"` | `/assets/cards/queen_of_clubs2.png` |
| Jack of Hearts | `"J"` | `"Hearts"` | `/assets/cards/jack_of_hearts2.png` |
| 10 of Spades | `"10"` | `"Spades"` | `/assets/cards/10_of_spades.png` |

## Changes Made

### Modified Files
1. **wwwroot/js/game.js**
   - Updated `getCardImagePath()` function to match C# implementation
   - Added error handling in `renderPlayerHand()`
   - Added error handling in lane card rendering
   - Added error handling for attached cards

## Verification

### Build Status
? Build successful

### Expected Results
- All card images should now load correctly
- Player hand shows proper card graphics
- Lane cards display correctly
- Attached cards (Kings, Queens, Jacks) show proper images
- Console errors logged if any images fail to load (for debugging)

### Troubleshooting
If images still don't load, check:
1. Card images exist at `/assets/cards/` directory
2. File naming matches the convention (lowercase suit, face card suffix)
3. Browser console for specific image paths being requested
4. Network tab to verify 404s or path mismatches

## Notes
- The path generation now matches the existing C# `CardImageHelper` class exactly
- JavaScript receives card data with camelCase properties (`face`, `suit`) due to default JSON serialization
- Error handling will help identify any missing card image files

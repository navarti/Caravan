// Wrap in IIFE to avoid global scope conflicts with site.js
(function() {
    'use strict';

let gameConnection = null;
let currentRoomId = null;
let myConnectionId = null;
let gameState = null;
let selectedCard = null;
let discardLaneMode = false;
let discardCardMode = false;

function resetClientState() {
    // Reset all client-side state variables
    selectedCard = null;
    discardLaneMode = false;
    discardCardMode = false;

    // Clear all visual selections
    const cards = document.querySelectorAll('.card');
    cards.forEach(c => c.classList.remove('selected'));

    // Clear status message if it's an info message (not error/success)
    const statusDiv = document.getElementById("statusMessage");
    if (statusDiv && statusDiv.style.display === 'block') {
        // Only clear if it's showing an info-type message about discarding
        if (statusDiv.style.backgroundColor === 'rgb(209, 236, 241)') {
            statusDiv.style.display = 'none';
        }
    }
}

function initializeGameConnection() {
    const urlParams = new URLSearchParams(window.location.search);
    currentRoomId = urlParams.get('roomId');

    if (!currentRoomId) {
        window.location.href = '/Lobby';
        return;
    }

    // Get player name from session storage
    const playerName = sessionStorage.getItem('playerName');
    if (!playerName) {
        showStatus("Player name not found. Redirecting to lobby...", "error");
        setTimeout(() => window.location.href = '/Lobby', 2000);
        return;
    }

    // Reset client state on page load/reload
    resetClientState();

    // Show loading state
    document.getElementById('loadingRoomId').textContent = currentRoomId;
    document.getElementById('roomIdDisplay').textContent = currentRoomId;

    gameConnection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub")
        .withAutomaticReconnect()
        .build();

    gameConnection.on("GameStateUpdated", (state) => {
        gameState = state;
        updateGameUI(state);
    });

    gameConnection.on("Error", (message) => {
        showStatus(message, "error");
    });

    // Handle reconnection events
    gameConnection.onreconnecting(() => {
        showStatus("Connection lost. Reconnecting...", "info");
    });

    gameConnection.onreconnected(() => {
        showStatus("Reconnected! Refreshing game state...", "success");
        resetClientState();
        const playerName = sessionStorage.getItem('playerName');
        gameConnection.invoke("RejoinRoom", currentRoomId, playerName)
            .catch(err => console.error("Error rejoining after reconnect:", err));
    });

    gameConnection.onclose(() => {
        showStatus("Connection closed. Please refresh the page.", "error");
    });

    gameConnection.start()
        .then(() => {
            console.log("Connected to game");
            myConnectionId = gameConnection.connectionId;

            // Get player name from session storage
            const playerName = sessionStorage.getItem('playerName');

            // Rejoin the room and get current game state
            return gameConnection.invoke("RejoinRoom", currentRoomId, playerName);
        })
        .then(() => {
            console.log("Rejoined room:", currentRoomId);
        })
        .catch(err => {
            console.error("Connection error:", err);
            showStatus("Failed to connect to game", "error");
        });
}

function updateGameUI(state) {
    // Hide loading, show game
    document.getElementById('loadingState').style.display = 'none';
    document.getElementById('gameContainer').style.display = 'block';

    const isMyTurn = state.currentPlayerConnectionId === myConnectionId;
    const myPlayerNumber = getMyPlayerNumber(state);

    console.log('[UpdateGameUI] Turn check - myConnectionId:', myConnectionId, 'currentPlayerConnectionId:', state.currentPlayerConnectionId, 'isMyTurn:', isMyTurn);

    // Update game info
    document.getElementById('currentPlayerName').textContent = state.currentPlayerName;
    document.getElementById('playerRole').textContent = myPlayerNumber === 1 ? state.player1Name : state.player2Name;

    // Update message with phase info
    const phaseText = state.phase === 1 ? `Phase 1 - Lane ${state.currentLane}` : 'Phase 2';
    const messageText = state.message || 'Welcome to Caravan Online!';
    document.getElementById('userMessage').textContent = `${phaseText}: ${messageText}`;

    // Reset discard modes if it's not our turn anymore
    if (!isMyTurn) {
        discardLaneMode = false;
        discardCardMode = false;
    }

    // Check if game is complete
    if (state.isGameComplete) {
        showStatus(state.gameResult, "success");
        document.getElementById('discardLaneBtn').disabled = true;
        document.getElementById('discardCardBtn').disabled = true;
    } else {
        renderPlayerHand(myPlayerNumber === 1 ? state.player1Cards : state.player2Cards, isMyTurn);
        renderLanes(state.lanes, state.laneScores, isMyTurn, myPlayerNumber);

        const discardLaneBtn = document.getElementById('discardLaneBtn');
        const discardCardBtn = document.getElementById('discardCardBtn');

        discardLaneBtn.disabled = !isMyTurn;
        discardCardBtn.disabled = !isMyTurn;

        console.log('[UpdateGameUI] Buttons state - discardLaneBtn.disabled:', discardLaneBtn.disabled, 'discardCardBtn.disabled:', discardCardBtn.disabled);
        console.log('[UpdateGameUI] Button visibility - discardLaneBtn:', discardLaneBtn ? 'found' : 'NOT FOUND', 'discardCardBtn:', discardCardBtn ? 'found' : 'NOT FOUND');
        console.log('[UpdateGameUI] Are buttons clickable? discardLaneBtn.onclick:', typeof discardLaneBtn.onclick, 'window.activateDiscardLane:', typeof window.activateDiscardLane);
    }
}

function getMyPlayerNumber(state) {
    const myPlayerName = sessionStorage.getItem('playerName');
    if (myPlayerName === state.player1Name) return 1;
    if (myPlayerName === state.player2Name) return 2;
    return 1; // Default to player 1
}

function renderPlayerHand(cards, isMyTurn) {
    const container = document.getElementById('playerHandCards');
    container.innerHTML = '';

    cards.forEach(card => {
        const cardBtn = document.createElement('button');
        cardBtn.type = 'button';
        cardBtn.className = 'card';
        cardBtn.disabled = !isMyTurn;
        cardBtn.dataset.face = card.face;
        cardBtn.dataset.suit = card.suit;
        cardBtn.dataset.full = `${card.face} ${card.suit}`;
        cardBtn.onclick = () => highlightCards(card.face, `${card.face} ${card.suit}`);

        const img = document.createElement('img');
        img.src = getCardImagePath(card);
        img.alt = `${card.face} ${card.suit}`;
        img.style.width = '80px';
        img.style.height = 'auto';
        img.onerror = function() {
            console.error('Failed to load card image:', this.src);
            this.alt = `${card.face} of ${card.suit}`;
        };

        cardBtn.appendChild(img);
        container.appendChild(cardBtn);
    });
}

function renderLanes(lanes, laneScores, isMyTurn, myPlayerNumber) {
    const lanes13 = document.getElementById('lanes-1-3');
    const lanes46 = document.getElementById('lanes-4-6');
    
    lanes13.innerHTML = '';
    lanes46.innerHTML = '';
    
    for (let i = 0; i < 3; i++) {
        lanes13.appendChild(createLaneElement(i + 1, lanes[i], laneScores[i], isMyTurn, myPlayerNumber));
    }
    
    for (let i = 3; i < 6; i++) {
        lanes46.appendChild(createLaneElement(i + 1, lanes[i], laneScores[i], isMyTurn, myPlayerNumber));
    }
}

function createLaneElement(laneNum, laneCards, score, isMyTurn, myPlayerNumber) {
    const laneDiv = document.createElement('div');
    laneDiv.style.border = '1px solid #ccc';
    laneDiv.style.padding = '10px';
    laneDiv.style.margin = '5px';
    laneDiv.style.minWidth = '150px';

    // Lanes can be clicked for:
    // 1. Normal card placement: player's own lanes (P1: 1-3, P2: 4-6)
    // 2. Discard lane mode: any lane (controlled separately in selectLane)
    const canClickForPlacement = isMyTurn && (
        (myPlayerNumber === 1 && laneNum <= 3) || 
        (myPlayerNumber === 2 && laneNum >= 4)
    );

    const header = document.createElement('h4');
    header.textContent = `Lane ${laneNum} (${score})`;
    // Make lane clickable if it's player's turn (for discard or placement)
    header.style.cursor = isMyTurn ? 'pointer' : 'default';
    if (isMyTurn) {
        header.onclick = () => selectLane(laneNum);
    }
    laneDiv.appendChild(header);
    
    if (laneCards && laneCards.length > 0) {
        laneCards.forEach((card, index) => {
            const cardContainer = document.createElement('div');
            cardContainer.className = 'card-container';
            cardContainer.style.marginBottom = '10px';
            
            const cardP = document.createElement('p');
            cardP.className = 'lane-card card';
            cardP.dataset.lane = laneNum;
            cardP.dataset.card = `${card.face} ${card.suit}`;
            cardP.dataset.index = index;
            if (isMyTurn) {
                cardP.onclick = (e) => cardClicked(card.face, card.suit, index, laneNum, e);
            }
            
            const cardImg = document.createElement('img');
            cardImg.src = getCardImagePath(card);
            cardImg.alt = `${card.face} ${card.suit}`;
            cardImg.style.width = '80px';
            cardImg.style.height = 'auto';
            cardImg.onerror = function() {
                console.error('Failed to load card image:', this.src);
                this.alt = `${card.face} of ${card.suit}`;
            };
            cardP.appendChild(cardImg);
            cardContainer.appendChild(cardP);

            if (card.attachedCards && card.attachedCards.length > 0) {
                const attachedDiv = document.createElement('div');
                attachedDiv.className = 'attached-cards';
                card.attachedCards.forEach(attached => {
                    const attachedP = document.createElement('p');
                    attachedP.className = 'card';
                    const attachedImg = document.createElement('img');
                    attachedImg.src = getCardImagePath(attached);
                    attachedImg.alt = `${attached.face} ${attached.suit}`;
                    attachedImg.style.width = '60px';
                    attachedImg.style.height = 'auto';
                    attachedImg.onerror = function() {
                        console.error('Failed to load attached card image:', this.src);
                        this.alt = `${attached.face} of ${attached.suit}`;
                    };
                    attachedP.appendChild(attachedImg);
                    attachedDiv.appendChild(attachedP);
                });
                cardContainer.appendChild(attachedDiv);
            }
            
            laneDiv.appendChild(cardContainer);
        });
    } else {
        const empty = document.createElement('p');
        empty.textContent = 'Empty';
        laneDiv.appendChild(empty);
    }
    
    return laneDiv;
}

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

function highlightCards(face, fullCard) {
    console.log('[HighlightCards] Card clicked:', fullCard, 'discardCardMode:', discardCardMode);
    selectedCard = fullCard;

    const cards = document.querySelectorAll('.card');
    cards.forEach(c => c.classList.remove('selected'));

    const matchingCards = document.querySelectorAll(`[data-full="${fullCard}"]`);
    matchingCards.forEach(c => c.classList.add('selected'));

    if (discardCardMode) {
        console.log('[HighlightCards] Discard card mode active, calling discardCardAction');
        discardCardAction();
    }
}

function selectLane(laneNum) {
    console.log('[SelectLane] Lane clicked:', laneNum, 'discardLaneMode:', discardLaneMode, 'selectedCard:', selectedCard);

    // If in discard lane mode, we don't need a selected card
    if (discardLaneMode) {
        console.log('[SelectLane] Discard lane mode active, calling discardLaneAction');
        discardLaneAction(laneNum);
        return;
    }

    // For normal card placement, we need a selected card
    if (!selectedCard) {
        showStatus("Please select a card first", "error");
        return;
    }

    // Validate that player is placing card in their own lane
    const myPlayerNumber = getMyPlayerNumber(gameState);
    const isMyLane = (myPlayerNumber === 1 && laneNum <= 3) || (myPlayerNumber === 2 && laneNum >= 4);

    if (!isMyLane) {
        showStatus("You can only place cards in your own lanes (Player 1: lanes 1-3, Player 2: lanes 4-6)", "error");
        return;
    }

    placeCardInLane(laneNum);
}

function placeCardInLane(laneNum) {
    if (!selectedCard || !gameConnection) return;

    gameConnection.invoke("PlaceCard", currentRoomId, selectedCard, laneNum)
        .then(() => {
            selectedCard = null;
            discardLaneMode = false;
            discardCardMode = false;
            const cards = document.querySelectorAll('.card');
            cards.forEach(c => c.classList.remove('selected'));
        })
        .catch(err => {
            console.error("Error placing card:", err);
            showStatus("Error placing card", "error");
        });
}

function cardClicked(face, suit, index, lane, event) {
    if (!selectedCard) {
        showStatus("Please select a card from your hand first", "error");
        return;
    }

    const targetCard = `${face} ${suit}`;

    if (gameConnection) {
        gameConnection.invoke("PlaceCardNextTo", currentRoomId, targetCard, selectedCard, lane, index)
            .then(() => {
                selectedCard = null;
                discardLaneMode = false;
                discardCardMode = false;
                const cards = document.querySelectorAll('.card');
                cards.forEach(c => c.classList.remove('selected'));
            })
            .catch(err => {
                console.error("Error placing card:", err);
                showStatus("Error placing card next to target", "error");
            });
    }
}

function activateDiscardLane() {
    console.log('=== BUTTON CLICKED: Discard Lane (GAME.JS VERSION) ===');
    console.log('[DiscardLane] Activating discard lane mode');
    console.log('[DiscardLane] Current state - gameConnection:', gameConnection, 'currentRoomId:', currentRoomId);
    console.log('[DiscardLane] Confirming this is GAME.JS version - USING_ONLINE_GAME:', window.USING_ONLINE_GAME);
    discardLaneMode = true;
    discardCardMode = false;
    selectedCard = null; // Clear any selected card
    const cards = document.querySelectorAll('.card');
    cards.forEach(c => c.classList.remove('selected'));
    showStatus("Select a lane to discard", "info");
}

function activateDiscardCard() {
    console.log('=== BUTTON CLICKED: Discard Card (GAME.JS VERSION) ===');
    console.log('[DiscardCard] Activating discard card mode');
    console.log('[DiscardCard] Current state - gameConnection:', gameConnection, 'currentRoomId:', currentRoomId);
    console.log('[DiscardCard] Confirming this is GAME.JS version - USING_ONLINE_GAME:', window.USING_ONLINE_GAME);
    discardCardMode = true;
    discardLaneMode = false;
    showStatus("Select a card from your hand to discard", "info");
}

function discardLaneAction(laneNum) {
    console.log('[DiscardLane] Starting discard lane action for lane:', laneNum);

    if (!gameConnection) {
        console.error('[DiscardLane] No game connection available');
        showStatus("Not connected to game", "error");
        return;
    }

    if (!currentRoomId) {
        console.error('[DiscardLane] No room ID available');
        showStatus("Room not found", "error");
        return;
    }

    console.log('[DiscardLane] Invoking DiscardLane with roomId:', currentRoomId, 'laneNum:', laneNum);

    gameConnection.invoke("DiscardLane", currentRoomId, laneNum)
        .then(() => {
            console.log('[DiscardLane] Successfully discarded lane', laneNum);
            discardLaneMode = false;
            showStatus(`Lane ${laneNum} discarded`, "success");
        })
        .catch(err => {
            console.error('[DiscardLane] Error discarding lane:', err);
            console.error('[DiscardLane] Error details:', err.message, err.stack);
            showStatus(`Error discarding lane: ${err.message || 'Unknown error'}`, "error");
            discardLaneMode = false;
        });
}

function discardCardAction() {
    console.log('[DiscardCard] Starting discard card action with selectedCard:', selectedCard);

    if (!selectedCard) {
        console.error('[DiscardCard] No card selected');
        showStatus("Please select a card to discard", "error");
        discardCardMode = false;
        return;
    }

    if (!gameConnection) {
        console.error('[DiscardCard] No game connection available');
        showStatus("Not connected to game", "error");
        discardCardMode = false;
        return;
    }

    if (!currentRoomId) {
        console.error('[DiscardCard] No room ID available');
        showStatus("Room not found", "error");
        discardCardMode = false;
        return;
    }

    const parts = selectedCard.split(' ');
    if (parts.length < 2) {
        console.error('[DiscardCard] Invalid card format:', selectedCard);
        showStatus("Invalid card format", "error");
        discardCardMode = false;
        return;
    }

    const face = parts[0];
    const suit = parts[1];

    console.log('[DiscardCard] Invoking DiscardCard with roomId:', currentRoomId, 'face:', face, 'suit:', suit);

    gameConnection.invoke("DiscardCard", currentRoomId, face, suit)
        .then(() => {
            console.log('[DiscardCard] Successfully discarded card:', face, suit);
            discardCardMode = false;
            selectedCard = null;
            const cards = document.querySelectorAll('.card');
            cards.forEach(c => c.classList.remove('selected'));
            showStatus(`Card ${face} ${suit} discarded`, "success");
        })
        .catch(err => {
            console.error('[DiscardCard] Error discarding card:', err);
            console.error('[DiscardCard] Error details:', err.message, err.stack);
            showStatus(`Error discarding card: ${err.message || 'Unknown error'}`, "error");
            discardCardMode = false;
        });
}

function showStatus(message, type) {
    const statusDiv = document.getElementById("statusMessage");
    statusDiv.textContent = message;
    statusDiv.style.display = "block";

    const colors = {
        success: { bg: "#d4edda", color: "#155724", border: "#c3e6cb" },
        error: { bg: "#f8d7da", color: "#721c24", border: "#f5c6cb" },
        info: { bg: "#d1ecf1", color: "#0c5460", border: "#bee5eb" }
    };

    const style = colors[type] || colors.info;
    statusDiv.style.backgroundColor = style.bg;
    statusDiv.style.color = style.color;
    statusDiv.style.border = `1px solid ${style.border}`;

    setTimeout(() => {
        if (type !== "info") {
            statusDiv.style.display = "none";
        }
    }, 3000);
}

// Expose functions that are called from HTML inline event handlers
// These will OVERWRITE the site.js versions for the online game
console.log('[Exposure] BEFORE exposure - window.activateDiscardCard:', typeof window.activateDiscardCard);
console.log('[Exposure] Checking source of current activateDiscardCard:', window.activateDiscardCard ? window.activateDiscardCard.toString().substring(0, 100) : 'undefined');

// Mark that we're using the online game version
window.USING_ONLINE_GAME = true;

// Force overwrite the functions
window.highlightCards = highlightCards;
window.selectLane = selectLane;
window.cardClicked = cardClicked;
window.activateDiscardLane = activateDiscardLane;
window.activateDiscardCard = activateDiscardCard;
window.resetClientState = resetClientState;

// Verify functions are exposed
console.log('[Exposure] AFTER exposure - Functions exposed to window (ONLINE GAME):');
console.log('  - window.USING_ONLINE_GAME:', window.USING_ONLINE_GAME);
console.log('  - window.activateDiscardLane:', typeof window.activateDiscardLane);
console.log('  - window.activateDiscardCard:', typeof window.activateDiscardCard);
console.log('  - window.activateDiscardCard source:', window.activateDiscardCard.toString().substring(0, 150));
console.log('  - window.highlightCards:', typeof window.highlightCards);
console.log('  - window.selectLane:', typeof window.selectLane);

// Double-check after a short delay to ensure overwrite persists
setTimeout(() => {
    console.log('[Exposure Verification] Checking after 100ms:');
    console.log('  - window.USING_ONLINE_GAME:', window.USING_ONLINE_GAME);
    console.log('  - window.activateDiscardCard source check:', window.activateDiscardCard.toString().substring(0, 150));

    // Force re-assignment if needed
    if (!window.USING_ONLINE_GAME || window.activateDiscardCard.toString().indexOf('setUserMessage') !== -1) {
        console.warn('[Exposure] SITE.JS VERSION DETECTED! Re-assigning functions...');
        window.USING_ONLINE_GAME = true;
        window.activateDiscardLane = activateDiscardLane;
        window.activateDiscardCard = activateDiscardCard;
        window.highlightCards = highlightCards;
        window.selectLane = selectLane;
        window.cardClicked = cardClicked;
        console.log('[Exposure] Functions RE-ASSIGNED to game.js versions');
    } else {
        console.log('[Exposure] Correct game.js versions are active');
    }
}, 100);

document.addEventListener("DOMContentLoaded", () => {
    console.log('[Init] DOM Content Loaded, initializing game connection');

    // Add event listeners to buttons for debugging
    const discardLaneBtn = document.getElementById('discardLaneBtn');
    const discardCardBtn = document.getElementById('discardCardBtn');

    if (discardLaneBtn) {
        discardLaneBtn.addEventListener('click', function(e) {
            console.log('[Button] Discard Lane button clicked (event listener)', 'disabled:', this.disabled);
        });
        console.log('[Init] Discard Lane button found and event listener attached');
    } else {
        console.error('[Init] Discard Lane button NOT FOUND!');
    }

    if (discardCardBtn) {
        discardCardBtn.addEventListener('click', function(e) {
            console.log('[Button] Discard Card button clicked (event listener)', 'disabled:', this.disabled);
        });
        console.log('[Init] Discard Card button found and event listener attached');
    } else {
        console.error('[Init] Discard Card button NOT FOUND!');
    }

    initializeGameConnection();
});

})(); // End of IIFE

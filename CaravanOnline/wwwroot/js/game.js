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

    // Show loading state
    document.getElementById('loadingRoomId').textContent = currentRoomId;
    document.getElementById('roomIdDisplay').textContent = currentRoomId;

    gameConnection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub")
        .build();

    gameConnection.on("GameStateUpdated", (state) => {
        gameState = state;
        updateGameUI(state);
    });

    gameConnection.on("Error", (message) => {
        showStatus(message, "error");
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

    // Update game info
    document.getElementById('currentPlayerName').textContent = state.currentPlayerName;
    document.getElementById('playerRole').textContent = myPlayerNumber === 1 ? state.player1Name : state.player2Name;

    // Update message with phase info
    const phaseText = state.phase === 1 ? `Phase 1 - Lane ${state.currentLane}` : 'Phase 2';
    const messageText = state.message || 'Welcome to Caravan Online!';
    document.getElementById('userMessage').textContent = `${phaseText}: ${messageText}`;

    // Check if game is complete
    if (state.isGameComplete) {
        showStatus(state.gameResult, "success");
        document.getElementById('discardLaneBtn').disabled = true;
        document.getElementById('discardCardBtn').disabled = true;
    } else {
        renderPlayerHand(myPlayerNumber === 1 ? state.player1Cards : state.player2Cards, isMyTurn);
        renderLanes(state.lanes, state.laneScores, isMyTurn, myPlayerNumber);

        document.getElementById('discardLaneBtn').disabled = !isMyTurn;
        document.getElementById('discardCardBtn').disabled = !isMyTurn;
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
    
    const canClick = isMyTurn && (
        (myPlayerNumber === 1 && laneNum <= 3) || 
        (myPlayerNumber === 2 && laneNum >= 4)
    );
    
    const header = document.createElement('h4');
    header.textContent = `Lane ${laneNum} (${score})`;
    header.style.cursor = canClick ? 'pointer' : 'default';
    if (canClick) {
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
    selectedCard = fullCard;
    
    const cards = document.querySelectorAll('.card');
    cards.forEach(c => c.classList.remove('selected'));
    
    const matchingCards = document.querySelectorAll(`[data-full="${fullCard}"]`);
    matchingCards.forEach(c => c.classList.add('selected'));
    
    if (discardCardMode) {
        discardCardAction();
    }
}

function selectLane(laneNum) {
    if (!selectedCard) {
        showStatus("Please select a card first", "error");
        return;
    }
    
    if (discardLaneMode) {
        discardLaneAction(laneNum);
    } else {
        placeCardInLane(laneNum);
    }
}

function placeCardInLane(laneNum) {
    if (!selectedCard || !gameConnection) return;
    
    gameConnection.invoke("PlaceCard", currentRoomId, selectedCard, laneNum)
        .then(() => {
            selectedCard = null;
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
    discardLaneMode = true;
    discardCardMode = false;
    showStatus("Select a lane to discard", "info");
}

function activateDiscardCard() {
    discardCardMode = true;
    discardLaneMode = false;
    showStatus("Select a card to discard", "info");
}

function discardLaneAction(laneNum) {
    if (!gameConnection) return;
    
    gameConnection.invoke("DiscardLane", currentRoomId, laneNum)
        .then(() => {
            discardLaneMode = false;
            showStatus(`Lane ${laneNum} discarded`, "success");
        })
        .catch(err => {
            console.error("Error discarding lane:", err);
            showStatus("Error discarding lane", "error");
        });
}

function discardCardAction() {
    if (!selectedCard || !gameConnection) return;
    
    const parts = selectedCard.split(' ');
    if (parts.length < 2) return;
    
    gameConnection.invoke("DiscardCard", currentRoomId, parts[0], parts[1])
        .then(() => {
            discardCardMode = false;
            selectedCard = null;
            const cards = document.querySelectorAll('.card');
            cards.forEach(c => c.classList.remove('selected'));
            showStatus("Card discarded", "success");
        })
        .catch(err => {
            console.error("Error discarding card:", err);
            showStatus("Error discarding card", "error");
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
window.highlightCards = highlightCards;
window.selectLane = selectLane;
window.cardClicked = cardClicked;
window.activateDiscardLane = activateDiscardLane;
window.activateDiscardCard = activateDiscardCard;

document.addEventListener("DOMContentLoaded", () => {
    initializeGameConnection();
});

})(); // End of IIFE

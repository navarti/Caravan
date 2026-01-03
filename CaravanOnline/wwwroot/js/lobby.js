// Wrap in IIFE to avoid global scope conflicts with site.js
(function() {
    'use strict';

let connection = null;
let currentRoomId = null;

function initializeLobbyConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub")
        .build();

    connection.on("RoomCreated", (roomId, playerName) => {
        currentRoomId = roomId;
        document.getElementById("roomCode").textContent = roomId;
        document.getElementById("roomCreatedInfo").style.display = "block";
        showStatus(`Room ${roomId} created! Waiting for opponent...`, "success");
    });

    connection.on("PlayerJoined", (playerName, player1Name, player2Name) => {
        showStatus(`Game starting! ${player1Name} vs ${player2Name}`, "success");
        setTimeout(() => {
            window.location.href = `/IndexOnline?roomId=${currentRoomId}`;
        }, 1500);
    });

    connection.on("JoinFailed", (message) => {
        showStatus(`Failed to join room: ${message}`, "error");
    });

    connection.on("AvailableRooms", (rooms) => {
        displayAvailableRooms(rooms);
    });

    connection.start()
        .then(() => {
            console.log("Connected to lobby");
            refreshRooms();
        })
        .catch(err => {
            console.error("Connection error:", err);
            showStatus("Failed to connect to server", "error");
        });
}

function createRoom() {
    const playerName = document.getElementById("playerName").value.trim();
    if (!playerName) {
        showStatus("Please enter your name", "error");
        return;
    }

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        // Store player name in session storage
        sessionStorage.setItem('playerName', playerName);

        connection.invoke("CreateRoom", playerName)
            .catch(err => console.error("Error creating room:", err));
    }
}

function joinRoom() {
    const playerName = document.getElementById("playerName").value.trim();
    const roomId = document.getElementById("roomIdInput").value.trim().toUpperCase();

    if (!playerName) {
        showStatus("Please enter your name", "error");
        return;
    }

    if (!roomId) {
        showStatus("Please enter a room code", "error");
        return;
    }

    // Store player name in session storage
    sessionStorage.setItem('playerName', playerName);

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        currentRoomId = roomId;
        connection.invoke("JoinRoom", roomId, playerName)
            .catch(err => {
                console.error("Error joining room:", err);
                showStatus("Error joining room", "error");
            });
    }
}

function joinRoomById(roomId) {
    const playerName = document.getElementById("playerName").value.trim();
    if (!playerName) {
        showStatus("Please enter your name first", "error");
        return;
    }

    // Store player name in session storage
    sessionStorage.setItem('playerName', playerName);

    currentRoomId = roomId;
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("JoinRoom", roomId, playerName)
            .catch(err => {
                console.error("Error joining room:", err);
                showStatus("Error joining room", "error");
            });
    }
}

function refreshRooms() {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("GetAvailableRooms")
            .catch(err => console.error("Error getting rooms:", err));
    }
}

function displayAvailableRooms(rooms) {
    const container = document.getElementById("availableRooms");
    
    if (!rooms || rooms.length === 0) {
        container.innerHTML = "<p>No available rooms. Create a new one!</p>";
        return;
    }

    let html = "<ul style='list-style: none; padding: 0;'>";
    rooms.forEach(room => {
        html += `
            <li style='margin-bottom: 10px; padding: 10px; border: 1px solid #ddd; border-radius: 5px;'>
                <strong>Room ${room.roomId}</strong> - Host: ${room.player1Name}
                <button onclick="joinRoomById('${room.roomId}')" style='margin-left: 15px; padding: 5px 15px; cursor: pointer;'>
                    Join
                </button>
            </li>
        `;
    });
    html += "</ul>";
    container.innerHTML = html;
}

function showStatus(message, type) {
    const statusDiv = document.getElementById("statusMessage");
    statusDiv.textContent = message;
    statusDiv.style.display = "block";
    statusDiv.style.backgroundColor = type === "success" ? "#d4edda" : "#f8d7da";
    statusDiv.style.color = type === "success" ? "#155724" : "#721c24";
    statusDiv.style.border = type === "success" ? "1px solid #c3e6cb" : "1px solid #f5c6cb";

    setTimeout(() => {
        if (type === "error") {
            statusDiv.style.display = "none";
        }
    }, 5000);
}

// Expose functions that are called from HTML inline event handlers
window.createRoom = createRoom;
window.joinRoom = joinRoom;
window.joinRoomById = joinRoomById;
window.refreshRooms = refreshRooms;

// Initialize connection when page loads
document.addEventListener("DOMContentLoaded", () => {
    initializeLobbyConnection();
});

})(); // End of IIFE

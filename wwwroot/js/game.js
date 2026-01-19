// Game state
let gameState = null;
let connection = null;
let roomCode = null;
let playerSymbol = null;
let playerName = null;
let isMyTurn = false;
let isMultiplayer = false;

// DOM Elements
const lobbyScreen = document.getElementById('lobby-screen');
const gameScreen = document.getElementById('game-screen');
const boardContainer = document.getElementById('board-container');
const messageEl = document.getElementById('message');
const boardSizeEl = document.getElementById('board-size');
const currentPlayerEl = document.getElementById('current-player');
const newGameBtn = document.getElementById('new-game-btn');
const roomCodeDisplay = document.getElementById('room-code-display');
const roomCodeText = document.getElementById('room-code-text');
const playerSymbolEl = document.getElementById('player-symbol');
const opponentNameEl = document.getElementById('opponent-name');
const copyCodeBtn = document.getElementById('copy-code-btn');
const leaveRoomBtn = document.getElementById('leave-room-btn');

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    initializeSignalR();
});

function setupEventListeners() {
    // Lobby buttons
    document.getElementById('create-room-btn').addEventListener('click', createRoom);
    document.getElementById('join-room-btn').addEventListener('click', joinRoom);
    document.getElementById('local-play-btn').addEventListener('click', startLocalGame);
    
    // Game buttons
    newGameBtn.addEventListener('click', requestRematch);
    copyCodeBtn?.addEventListener('click', copyRoomCode);
    leaveRoomBtn?.addEventListener('click', leaveRoom);
}

// Initialize SignalR connection
async function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub")
        .withAutomaticReconnect()
        .build();

    // Event handlers
    connection.on("RoomCreated", handleRoomCreated);
    connection.on("RoomJoined", handleRoomJoined);
    connection.on("OpponentJoined", handleOpponentJoined);
    connection.on("GameUpdated", handleGameUpdated);
    connection.on("OpponentDisconnected", handleOpponentDisconnected);
    connection.on("RematchRequested", handleRematchRequested);
    connection.on("RematchAccepted", handleRematchAccepted);
    connection.on("Error", handleError);

    try {
        await connection.start();
        console.log("SignalR Connected");
    } catch (err) {
        console.error("SignalR Connection Error:", err);
        showError("שגיאה בהתחברות לשרת. נסה לרענן את הדף.");
    }
}

// Create a new room
async function createRoom() {
    playerName = document.getElementById('player-name').value.trim() || 'שחקן';
    
    try {
        await connection.invoke("CreateRoom", playerName);
    } catch (err) {
        showError("שגיאה ביצירת חדר");
    }
}

// Join existing room
async function joinRoom() {
    playerName = document.getElementById('player-name').value.trim() || 'שחקן';
    const code = document.getElementById('room-code-input').value.trim().toUpperCase();
    
    if (!code) {
        showError("נא להזין קוד חדר");
        return;
    }
    
    try {
        await connection.invoke("JoinRoom", code, playerName);
    } catch (err) {
        showError("שגיאה בהצטרפות לחדר");
    }
}

// Start local game (same device)
function startLocalGame() {
    isMultiplayer = false;
    playerName = document.getElementById('player-name').value.trim() || 'שחקן';
    
    gameState = {
        board: [['', '', ''], ['', '', ''], ['', '', '']],
        currentPlayer: 'X',
        boardSize: 3,
        gameOver: false,
        winner: null,
        message: 'תור השחקן: X',
        boardExpanded: false
    };
    
    showGameScreen();
    roomCodeDisplay.style.display = 'none';
    opponentNameEl.parentElement.style.display = 'none';
    newGameBtn.textContent = 'משחק חדש';
    renderGame();
}

// Handle room created
function handleRoomCreated(room) {
    isMultiplayer = true;
    roomCode = room.roomCode;
    playerSymbol = room.playerSymbol;
    isMyTurn = room.isYourTurn;
    gameState = room.gameState;
    
    showGameScreen();
    roomCodeText.textContent = roomCode;
    roomCodeDisplay.style.display = 'flex';
    playerSymbolEl.textContent = playerSymbol;
    playerSymbolEl.className = `info-value player-${playerSymbol.toLowerCase()}`;
    opponentNameEl.textContent = 'מחכה ליריב...';
    opponentNameEl.parentElement.style.display = 'block';
    newGameBtn.style.display = 'none';
    
    messageEl.textContent = 'מחכה ליריב להצטרף... שתף את הקוד!';
    renderGame();
}

// Handle room joined
function handleRoomJoined(room) {
    isMultiplayer = true;
    roomCode = room.roomCode;
    playerSymbol = room.playerSymbol;
    isMyTurn = room.isYourTurn;
    gameState = room.gameState;
    
    showGameScreen();
    roomCodeText.textContent = roomCode;
    roomCodeDisplay.style.display = 'flex';
    playerSymbolEl.textContent = playerSymbol;
    playerSymbolEl.className = `info-value player-${playerSymbol.toLowerCase()}`;
    opponentNameEl.textContent = room.opponentName;
    opponentNameEl.parentElement.style.display = 'block';
    newGameBtn.style.display = 'none';
    
    renderGame();
}

// Handle opponent joined
function handleOpponentJoined(room) {
    isMyTurn = room.isYourTurn;
    gameState = room.gameState;
    opponentNameEl.textContent = room.opponentName;
    newGameBtn.style.display = 'none';
    
    messageEl.textContent = `${room.opponentName} הצטרף! תורך לשחק (X)`;
    renderGame();
}

// Handle game update
function handleGameUpdated(room) {
    const boardExpanded = room.gameState.boardExpanded;
    isMyTurn = room.isYourTurn;
    gameState = room.gameState;
    
    if (boardExpanded) {
        boardContainer.classList.add('board-expanding');
        setTimeout(() => {
            renderGame();
            boardContainer.classList.remove('board-expanding');
            boardContainer.classList.add('board-expanded');
            setTimeout(() => {
                boardContainer.classList.remove('board-expanded');
            }, 500);
        }, 300);
    } else {
        renderGame();
    }
    
    // Show rematch button when game is over
    if (gameState.gameOver) {
        newGameBtn.style.display = 'block';
        newGameBtn.textContent = 'בקש משחק חוזר';
    }
}

// Handle opponent disconnected
function handleOpponentDisconnected(opponentName) {
    showError(`${opponentName} התנתק מהמשחק`);
    setTimeout(() => {
        showLobbyScreen();
    }, 3000);
}

// Handle rematch request
function handleRematchRequested(fromPlayer) {
    if (confirm(`${fromPlayer} מבקש משחק חוזר. להסכים?`)) {
        connection.invoke("AcceptRematch");
    }
}

// Handle rematch accepted
function handleRematchAccepted() {
    newGameBtn.style.display = 'none';
}

// Handle error
function handleError(error) {
    showError(error);
}

// Make a move
async function makeMove(row, col) {
    if (isMultiplayer) {
        if (!isMyTurn || gameState.gameOver) return;
        
        try {
            await connection.invoke("MakeMove", row, col);
        } catch (err) {
            showError("שגיאה בביצוע המהלך");
        }
    } else {
        // Local game
        if (gameState.gameOver) return;
        if (gameState.board[row][col] !== '') return;
        
        gameState.board[row][col] = gameState.currentPlayer;
        
        if (checkWinLocal(row, col)) {
            gameState.gameOver = true;
            gameState.winner = gameState.currentPlayer;
            gameState.message = `🎉 שחקן ${gameState.currentPlayer} ניצח!`;
        } else if (isBoardFullLocal()) {
            expandBoardLocal();
            gameState.boardExpanded = true;
            gameState.message = `הלוח מלא! הלוח הורחב ל-${gameState.boardSize}x${gameState.boardSize}`;
            
            boardContainer.classList.add('board-expanding');
            setTimeout(() => {
                renderGame();
                boardContainer.classList.remove('board-expanding');
            }, 300);
            return;
        } else {
            gameState.currentPlayer = gameState.currentPlayer === 'X' ? 'O' : 'X';
            gameState.message = `תור השחקן: ${gameState.currentPlayer}`;
        }
        
        renderGame();
    }
}

// Request rematch
async function requestRematch() {
    if (isMultiplayer) {
        try {
            await connection.invoke("RequestRematch");
            newGameBtn.textContent = 'ממתין לתגובה...';
            newGameBtn.disabled = true;
            setTimeout(() => {
                newGameBtn.disabled = false;
                newGameBtn.textContent = 'בקש משחק חוזר';
            }, 5000);
        } catch (err) {
            showError("שגיאה בבקשת משחק חוזר");
        }
    } else {
        // Local new game
        gameState = {
            board: [['', '', ''], ['', '', ''], ['', '', '']],
            currentPlayer: 'X',
            boardSize: 3,
            gameOver: false,
            winner: null,
            message: 'תור השחקן: X',
            boardExpanded: false
        };
        renderGame();
    }
}

// Copy room code
function copyRoomCode() {
    navigator.clipboard.writeText(roomCode).then(() => {
        const originalText = copyCodeBtn.textContent;
        copyCodeBtn.textContent = 'הועתק!';
        setTimeout(() => {
            copyCodeBtn.textContent = originalText;
        }, 2000);
    });
}

// Leave room
function leaveRoom() {
    if (confirm('בטוח שברצונך לעזוב את המשחק?')) {
        location.reload();
    }
}

// Show/hide screens
function showGameScreen() {
    lobbyScreen.style.display = 'none';
    gameScreen.style.display = 'block';
}

function showLobbyScreen() {
    lobbyScreen.style.display = 'block';
    gameScreen.style.display = 'none';
    isMultiplayer = false;
    roomCode = null;
    playerSymbol = null;
}

// Show error message
function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'error-toast';
    errorDiv.textContent = message;
    document.body.appendChild(errorDiv);
    
    setTimeout(() => {
        errorDiv.classList.add('fade-out');
        setTimeout(() => errorDiv.remove(), 500);
    }, 3000);
}

// Render the game
function renderGame() {
    if (!gameState) return;
    
    // Update info
    boardSizeEl.textContent = `${gameState.boardSize}x${gameState.boardSize}`;
    
    if (isMultiplayer) {
        currentPlayerEl.textContent = isMyTurn ? 'תורך!' : 'תור היריב';
        currentPlayerEl.className = `info-value ${isMyTurn ? 'your-turn' : 'opponent-turn'}`;
    } else {
        currentPlayerEl.textContent = gameState.currentPlayer;
        currentPlayerEl.className = `info-value player-${gameState.currentPlayer.toLowerCase()}`;
    }
    
    messageEl.textContent = gameState.message;
    messageEl.className = 'message';
    
    if (gameState.gameOver && gameState.winner) {
        messageEl.classList.add('winner-message');
    } else if (gameState.boardExpanded) {
        messageEl.classList.add('expanded-message');
    }
    
    renderBoard();
}

// Render the board
function renderBoard() {
    boardContainer.innerHTML = '';
    
    const board = document.createElement('div');
    board.className = 'board';
    board.style.gridTemplateColumns = `repeat(${gameState.boardSize}, 1fr)`;
    board.style.gridTemplateRows = `repeat(${gameState.boardSize}, 1fr)`;
    
    const cellSize = calculateCellSize(gameState.boardSize);
    board.style.setProperty('--cell-size', `${cellSize}px`);
    
    for (let i = 0; i < gameState.boardSize; i++) {
        for (let j = 0; j < gameState.boardSize; j++) {
            const cell = document.createElement('div');
            cell.className = 'cell';
            cell.dataset.row = i;
            cell.dataset.col = j;
            
            const value = gameState.board[i][j];
            if (value) {
                cell.textContent = value;
                cell.classList.add(`cell-${value.toLowerCase()}`);
                cell.classList.add('filled');
            } else if (!gameState.gameOver && (!isMultiplayer || isMyTurn)) {
                cell.classList.add('empty');
                cell.addEventListener('click', () => makeMove(i, j));
            }
            
            board.appendChild(cell);
        }
    }
    
    boardContainer.appendChild(board);
}

// Calculate cell size
function calculateCellSize(boardSize) {
    const maxBoardWidth = Math.min(window.innerWidth - 40, 500);
    const cellSize = Math.floor(maxBoardWidth / boardSize) - 4;
    return Math.max(cellSize, 25);
}

// Local game logic
function checkWinLocal(row, col) {
    const player = gameState.currentPlayer;
    const size = gameState.boardSize;
    const board = gameState.board;
    
    // Check horizontal
    for (let startCol = Math.max(0, col - 2); startCol <= Math.min(size - 3, col); startCol++) {
        if (board[row][startCol] === player &&
            board[row][startCol + 1] === player &&
            board[row][startCol + 2] === player)
            return true;
    }
    
    // Check vertical
    for (let startRow = Math.max(0, row - 2); startRow <= Math.min(size - 3, row); startRow++) {
        if (board[startRow][col] === player &&
            board[startRow + 1][col] === player &&
            board[startRow + 2][col] === player)
            return true;
    }
    
    // Check diagonal
    for (let offset = -2; offset <= 0; offset++) {
        const startRow = row + offset;
        const startCol = col + offset;
        if (startRow >= 0 && startCol >= 0 && startRow + 2 < size && startCol + 2 < size) {
            if (board[startRow][startCol] === player &&
                board[startRow + 1][startCol + 1] === player &&
                board[startRow + 2][startCol + 2] === player)
                return true;
        }
    }
    
    // Check anti-diagonal
    for (let offset = -2; offset <= 0; offset++) {
        const startRow = row + offset;
        const startCol = col - offset;
        if (startRow >= 0 && startCol < size && startRow + 2 < size && startCol - 2 >= 0) {
            if (board[startRow][startCol] === player &&
                board[startRow + 1][startCol - 1] === player &&
                board[startRow + 2][startCol - 2] === player)
                return true;
        }
    }
    
    return false;
}

function isBoardFullLocal() {
    for (let i = 0; i < gameState.boardSize; i++)
        for (let j = 0; j < gameState.boardSize; j++)
            if (gameState.board[i][j] === '')
                return false;
    return true;
}

function expandBoardLocal() {
    const oldSize = gameState.boardSize;
    const newSize = oldSize * 2;
    const oldBoard = gameState.board;
    const newBoard = Array(newSize).fill(null).map(() => Array(newSize).fill(''));
    
    const offset = Math.floor(oldSize / 2);
    for (let i = 0; i < oldSize; i++)
        for (let j = 0; j < oldSize; j++)
            newBoard[i + offset][j + offset] = oldBoard[i][j];
    
    gameState.board = newBoard;
    gameState.boardSize = newSize;
}

// Handle window resize
window.addEventListener('resize', () => {
    if (gameState) renderBoard();
});

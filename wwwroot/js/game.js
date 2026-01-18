// Game state
let gameState = null;
let isProcessing = false;

// DOM Elements
const boardContainer = document.getElementById('board-container');
const messageEl = document.getElementById('message');
const boardSizeEl = document.getElementById('board-size');
const currentPlayerEl = document.getElementById('current-player');
const newGameBtn = document.getElementById('new-game-btn');

// Initialize game on page load
document.addEventListener('DOMContentLoaded', () => {
    fetchGameState();
    newGameBtn.addEventListener('click', startNewGame);
});

// Fetch current game state from server
async function fetchGameState() {
    try {
        const response = await fetch('/Game/GetState', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        gameState = await response.json();
        renderGame();
    } catch (error) {
        console.error('Error fetching game state:', error);
        messageEl.textContent = 'שגיאה בטעינת המשחק';
    }
}

// Start a new game
async function startNewGame() {
    try {
        const response = await fetch('/Game/NewGame', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        gameState = await response.json();
        renderGame();
        
        // Add animation class
        boardContainer.classList.add('new-game-animation');
        setTimeout(() => {
            boardContainer.classList.remove('new-game-animation');
        }, 500);
    } catch (error) {
        console.error('Error starting new game:', error);
        messageEl.textContent = 'שגיאה בהתחלת משחק חדש';
    }
}

// Make a move
async function makeMove(row, col) {
    if (isProcessing || gameState.gameOver) return;
    
    isProcessing = true;
    
    try {
        const response = await fetch('/Game/MakeMove', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ row, col })
        });
        
        const newState = await response.json();
        const boardExpanded = newState.boardExpanded;
        
        gameState = newState;
        
        if (boardExpanded) {
            // Show expansion animation
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
    } catch (error) {
        console.error('Error making move:', error);
        messageEl.textContent = 'שגיאה בביצוע המהלך';
    } finally {
        isProcessing = false;
    }
}

// Render the game board and UI
function renderGame() {
    if (!gameState) return;
    
    // Update info
    boardSizeEl.textContent = `${gameState.boardSize}x${gameState.boardSize}`;
    currentPlayerEl.textContent = gameState.currentPlayer;
    currentPlayerEl.className = `info-value player-${gameState.currentPlayer.toLowerCase()}`;
    messageEl.textContent = gameState.message;
    
    // Update message style based on game state
    messageEl.className = 'message';
    if (gameState.gameOver && gameState.winner) {
        messageEl.classList.add('winner-message');
    } else if (gameState.boardExpanded) {
        messageEl.classList.add('expanded-message');
    }
    
    // Generate board
    renderBoard();
}

// Render the game board
function renderBoard() {
    boardContainer.innerHTML = '';
    
    const board = document.createElement('div');
    board.className = 'board';
    board.style.gridTemplateColumns = `repeat(${gameState.boardSize}, 1fr)`;
    board.style.gridTemplateRows = `repeat(${gameState.boardSize}, 1fr)`;
    
    // Adjust cell size based on board size
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
            } else if (!gameState.gameOver) {
                cell.classList.add('empty');
                cell.addEventListener('click', () => makeMove(i, j));
            }
            
            board.appendChild(cell);
        }
    }
    
    boardContainer.appendChild(board);
}

// Calculate cell size based on board size
function calculateCellSize(boardSize) {
    const maxBoardWidth = Math.min(window.innerWidth - 40, 600);
    const cellSize = Math.floor(maxBoardWidth / boardSize) - 4;
    return Math.max(cellSize, 25); // Minimum cell size of 25px
}

// Handle window resize
window.addEventListener('resize', () => {
    if (gameState) {
        renderBoard();
    }
});

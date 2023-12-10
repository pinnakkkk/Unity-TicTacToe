using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public enum TicTacToeState { none, cross, circle }

[Serializable]
public class GameOverEvent : UnityEvent<int> { }

public class TicTacToeAI : MonoBehaviour
{
    private const int GridSize = 3;

    #region Fields
    // 0 is easy, 1 is hard
    int _aiLevel;
    ClickTrigger[,] _clickTriggers;
    [SerializeField] private bool _isPlayerTurn = true;
    #endregion

    #region States
    TicTacToeState _aiState = TicTacToeState.cross;
    TicTacToeState _playerState = TicTacToeState.circle;
    TicTacToeState[,] _boardState;
    #endregion

    #region Prefabs
    [SerializeField]
    private GameObject _xPrefab;

    [SerializeField]
    private GameObject _oPrefab;
    #endregion

    #region UnityEvents
    public UnityEvent onGameStarted;

    /* Call This event with the event number to denote the winner/state (-1 for tie, 0 for player, 1 for AI)
    updated name for better readability */
    public GameOverEvent onGameOver;
    #endregion

    private void Awake()
    {
        if (onGameOver == null)
        {
            onGameOver = new GameOverEvent();
        }
    }

    public void StartAI(int AILevel)
    {
        _aiLevel = AILevel;
        StartGame();
    }

    public void RegisterTransform(int myCoordX, int myCoordY, ClickTrigger clickTrigger)
    {
        _clickTriggers[myCoordX, myCoordY] = clickTrigger;
    }

    private void StartGame()
    {
        // set up the grid using the grid size
        _clickTriggers = new ClickTrigger[GridSize, GridSize];

        onGameStarted.Invoke();

        // Populate the board
        PopulateBoardState();

        if (!_isPlayerTurn)
        {
            MinimaxGenAI();
        }
    }

    public void PlayerSelects(int coordX, int coordY)
    {
        if (_isPlayerTurn)
        {
            // Update the board state
            UpdateBoardState(coordX, coordY, _playerState);

            // Disable the click trigger
            DisableClickTriggers(_clickTriggers[coordX, coordY]);

            SetVisual(coordX, coordY, _playerState);

            _isPlayerTurn = false;

            // Check for win
            if (CheckForWin(_playerState))
            {
                onGameOver.Invoke(0); // Invoke onGameOver with 0 for human
                Debug.Log("Player won!");
            }
            else if (!EmptySquaresExist())
            {
                onGameOver.Invoke(-1); // it's a tie
            }
            else
            {
				// Trigger AI mode based on AI level
                if (_aiLevel == 0)
                {
                    RandomGenAI();
                }
                else
                {
                    MinimaxGenAI();
                }
            }
        }
    }

    public async void AiSelects(int coordX, int coordY)
    {
		// A delay to simulate thinking, better to add delay here than in the PlayerSelects method
        await Task.Delay(500); 

        // Update the board state
        UpdateBoardState(coordX, coordY, _aiState);

        // Disable the click trigger
        DisableClickTriggers(_clickTriggers[coordX, coordY]);

        SetVisual(coordX, coordY, _aiState);

        _isPlayerTurn = true;

        // Check for win
        if (CheckForWin(_aiState))
        {
            onGameOver.Invoke(1); // Invoke onGameOver with 1 for AI
            Debug.Log("AI won!");
        }
        else if (!EmptySquaresExist())
        {
            onGameOver.Invoke(-1); // it's a tie
        }
    }

    private void SetVisual(int coordX, int coordY, TicTacToeState targetState)
    {
        Instantiate(
            targetState == TicTacToeState.circle ? _oPrefab : _xPrefab,
            _clickTriggers[coordX, coordY].transform.position,
            Quaternion.identity
        );
    }

	// Populates the board state with empty squares.
    private void PopulateBoardState()
    {
        _boardState = new TicTacToeState[GridSize, GridSize];

        // Initialize the board with TicTacToeState.none to represent empty squares
        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                _boardState[i, j] = TicTacToeState.none;
            }
        }
    }

	// Check if there are any empty squares on the board.
	// Returns: True if there is at least one empty square, False otherwise.
    public bool EmptySquaresExist()
    {
        int gridSize = _boardState.GetLength(0);

        // Loop through the board to check for empty squares (TicTacToeState.none)
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (_boardState[i, j] == TicTacToeState.none)
                {
                    return true; // If an empty square is found, return true
                }
            }
        }

        return false; // If no empty squares are found, return false
    }
	
	// Disables click triggers.
    private void DisableClickTriggers(ClickTrigger trigger)
    {
        trigger.SetInputEndabled(false);
    }

    // Returns: a list of available moves on the board 
    public List<int> GetAvailableMoves()
    {
        List<int> moves = new List<int>();

        for (int i = 0; i < GridSize * GridSize; i++)
        {
            int row = i / GridSize; // Get the row index
            int col = i % GridSize; // Get the column index

            if (_boardState[row, col] == TicTacToeState.none)
            {
                moves.Add(i);
            }
        }

        return moves;
    }

    // Return number of empty squares 
    public int NumEmptySquares()
    {
        int countEmpty = 0;

        for (int i = 0; i < _boardState.GetLength(0); i++)
        {
            for (int j = 0; j < _boardState.GetLength(1); j++)
            {
                if (_boardState[i, j] == TicTacToeState.none)
                {
                    countEmpty++;
                }
            }
        }

        return countEmpty;
    }

	// AI moves based on Minimax
	private void MinimaxGenAI()
    {
		// check if the board is fully empty
        if (GetAvailableMoves().Count == 9)
        {
			// since the board is empty, pick a random move
            RandomGenAI();
        }
        else
        {
			// otherwise, use Minimax
            int move = Minimax(_playerState);
            AiSelects(move / GridSize, move % GridSize);
        }
    }

	// Calculates the best move for the current player using the minimax algorithm.
	// Returns: The best move for the current player.
    private int Minimax(TicTacToeState currentPlayer)
    {
		// Check if the current player has won the game
        if (CheckForWin(currentPlayer))
        {
            return 1; // player wins
        }
		// check if opponent has won the game
        else if (CheckForWin(GetOpponent(currentPlayer)))
        {
            return -1; // opponent wins
        }
		// last check if there are no empty squares
        else if (!EmptySquaresExist())
        {
            return 0; // it's a tie
        }

		// set initial best score & move
        int bestScore = int.MinValue;
        int bestMove = -1;

		// loop through available moves
        foreach (int move in GetAvailableMoves())
        {
            int row = move / GridSize;
            int col = move % GridSize;

            _boardState[row, col] = currentPlayer;

			// get score
            int score = -Minimax(GetOpponent(currentPlayer));

			// undo move
            _boardState[row, col] = TicTacToeState.none;

			// if score is better than best score
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    // Method to check if a player has won the game
    private bool CheckForWin(TicTacToeState _playerState)
    {
        // Check rows
        for (int row = 0; row < GridSize; row++)
        {
            if (_boardState[row, 0] == _playerState &&
                _boardState[row, 1] == _playerState &&
                _boardState[row, 2] == _playerState)
            {
                return true;
            }
        }

        // Check columns
        for (int col = 0; col < GridSize; col++)
        {
            if (_boardState[0, col] == _playerState &&
                _boardState[1, col] == _playerState &&
                _boardState[2, col] == _playerState)
            {
                return true;
            }
        }

        // Check diagonals
        if (_boardState[0, 0] == _playerState &&
            _boardState[1, 1] == _playerState &&
            _boardState[2, 2] == _playerState)
        {
            return true;
        }

        if (_boardState[0, 2] == _playerState &&
            _boardState[1, 1] == _playerState &&
            _boardState[2, 0] == _playerState)
        {
            return true;
        }

        return false;
    }

    // Method to get the opponent player
    private TicTacToeState GetOpponent(TicTacToeState currentPlayer)
    {
        if (currentPlayer == TicTacToeState.circle)
        {
            return TicTacToeState.cross;
        }
        else if (currentPlayer == TicTacToeState.cross)
        {
            return TicTacToeState.circle;
        }
        else
        {
            return TicTacToeState.none;
        }
    }

    // Method to update the board state
    private void UpdateBoardState(int coordX, int coordY, TicTacToeState newState)
    {
        _boardState[coordX, coordY] = newState;
    }

    private void RandomGenAI()
    {
        List<int> moves = GetAvailableMoves();
        int square = moves[UnityEngine.Random.Range(0, moves.Count)];
        AiSelects(square / GridSize, square % GridSize);
    }
}

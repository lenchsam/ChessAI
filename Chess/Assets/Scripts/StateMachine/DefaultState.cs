using UnityEngine;
using UnityEngine.InputSystem;

public class DefaultState : IState
{
    private PlayerController _playerController;
    private Camera _mainCamera;

    public DefaultState(PlayerController playerController)
    {
        _playerController = playerController;
        _mainCamera = Camera.main;
    }
    public void Enter()
    {
        Debug.Log("entered default state");
    }

    public void Exit()
    {
        Debug.Log("exited default state");
    }

    public void UpdateState()
    {

    }

    public void OnCellClicked()
    {
        Debug.Log("Cell clicked in Default State");
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

        int x = Mathf.RoundToInt(mouseWorldPos.x);
        int y = Mathf.RoundToInt(mouseWorldPos.y);

        if(x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            Debug.Log("Clicked outside the board!");
            return;
        }

        char pieceChar = _playerController.BoardScript.GetPieceAt(x, y);

        if (pieceChar != '\0')
        {
            Debug.Log($"Found piece: {pieceChar}");

            //if belongs to the player
            bool isWhitePiece = char.IsUpper(pieceChar);

            if (isWhitePiece == _playerController.IsPlayerWhite)
            {
                Debug.Log("Selected own piece! Transitioning state...");
                _playerController.ChangeState(new PieceSelectedState(_playerController, new Vector2Int(x, y)));
            }
        }


    }


}

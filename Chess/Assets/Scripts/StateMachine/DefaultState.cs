using UnityEngine;

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
        
    }

    public void Exit()
    {
        
    }

    public void UpdateState()
    {

    }

    public void OnCellClicked()
    {
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

        int x = Mathf.RoundToInt(mouseWorldPos.x);
        int y = Mathf.RoundToInt(mouseWorldPos.y);

        if(x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            return;
        }

        Piece pieceChar = _playerController.Game_Manager.BitboardScript.GetPieceOnSquare((y * 8) + x);

        if (pieceChar != Piece.None)
        {

            //if belongs to the player
            bool isWhitePiece = false;

            if ((int)pieceChar <= 5)
            {
                isWhitePiece = true;
            }

            if (isWhitePiece == _playerController.IsPlayerWhite)
            {
                int newSquareIndex = (y * 8) + x;
                _playerController.ChangeState(new PieceSelectedState(_playerController, newSquareIndex));
            }
        }
    }
}

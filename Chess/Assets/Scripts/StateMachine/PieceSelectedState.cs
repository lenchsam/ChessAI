using UnityEngine;

public class PieceSelectedState : IState
{
    private PlayerController _playerController;
    private int _pieceCoords;
    private GameObject _selectedPiece;
    private Camera _mainCamera;

    public PieceSelectedState(PlayerController playerController, int pieceCoords)
    {
        _playerController = playerController;
        _pieceCoords = pieceCoords;
        _mainCamera = Camera.main;

        _selectedPiece = _playerController.BoardScript.GetPieceFromPosition(_pieceCoords);
    }

    public void Enter()
    {
        _playerController.Game_Manager.BitboardScript.InvokeEvent(_pieceCoords);
    }

    public void Exit()
    {

    }

    public void UpdateState()
    {
        Vector3 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

        _selectedPiece.transform.position = new Vector3(mouseWorld.x, mouseWorld.y, -2);

        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        {
            int x = Mathf.RoundToInt(mouseWorld.x);
            int y = Mathf.RoundToInt(mouseWorld.y);

            if (x < 0 || x >= 8 || y < 0 || y >= 8)
            {
                int pieceX = _pieceCoords % 8;
                int pieceY = _pieceCoords / 8;

                _selectedPiece.transform.position = new Vector3(pieceX, pieceY, -1);
                _playerController.ChangeState(new DefaultState(_playerController));
                return;
            }

            int newSquareIndex = (y * 8) + x;

            _playerController.Game_Manager.OnMoveRequested?.Invoke(_pieceCoords, newSquareIndex);

            _playerController.ChangeState(new DefaultState(_playerController));
        }
    }

    public void OnCellClicked()
    {
        
    }
}

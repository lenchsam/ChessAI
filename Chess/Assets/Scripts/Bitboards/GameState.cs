using UnityEngine;

public struct GameState
{
    public Move _Move;
    public Piece CapturedPiece;
    public Castling CastlingRights;
    public ushort EnPassantMask;

    public GameState(Move move, Piece captured, Castling rights, ushort enPassantMask)
    {
        _Move = move;
        CapturedPiece = captured;
        CastlingRights = rights;
        EnPassantMask = enPassantMask;
    }
}


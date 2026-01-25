using UnityEngine;

public struct GameState
{
    public Piece CapturedPiece;
    public Castling CastlingRights;
    public ushort EnPassantMask;

    public GameState(Piece captured, Castling rights, ushort enPassantMask)
    {
        CapturedPiece = captured;
        CastlingRights = rights;
        EnPassantMask = enPassantMask;
    }
}

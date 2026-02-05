public class CustomMovesList
{
    //this implementation works well with my code as a list is slower than an array,
    //but an array doesnt allow me to easily add a value to the next null value.
    //this class gives me all of that functionality

    //max possible moves in a single chess position is 218
    //initialised to 256 to make sure we have enough space.
    public Move[] Moves = new Move[256];
    public int Length = 0;

    public void Add(Move move)
    {
        Moves[Length] = move;
        Length++;
    }

    public void Clear()
    {
        Length = 0;
    }
}

namespace DefaultNamespace
{
    public class BoardIndex
    {
        public int BoardX { get; private set; }
        public int BoardY { get; private set; }


        public BoardIndex(int x, int y)
        {
            BoardX = x;
            BoardY = y;
        }

        public BoardIndex Clone()
        {
            return new BoardIndex(BoardX, BoardY);
        }
    }
}
namespace DefaultNamespace
{
    public class GridIndex
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }


        public GridIndex(int x, int y)
        {
            GridX = x;
            GridY = y;
        }

        public GridIndex Clone()
        {
            return new GridIndex(GridX, GridY);
        }
    }
}
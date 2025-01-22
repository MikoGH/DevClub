public static class Renderer
{
    public static void RenderBooleanMap(Map<bool> map)
    {
        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                Console.Write(map[row, column] ? ' ' : '#');
            }
            Console.WriteLine();
        }
    }

    public static void RenderCellTypeMap(Map<CellType> map)
    {
        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                if (map[row, column] is CellType.Floor)
                {
                    Console.Write(' ');
                }
                else if (map[row, column] is CellType.Wall)
                {
                    Console.Write('#');
                }
                else if (map[row, column] is CellType.Visited)
                {
                    Console.Write('·');
                }
                else if (map[row, column] is CellType.PathStart)
                {
                    Console.Write('♦');
                }
                else if (map[row, column] is CellType.PathEnd)
                {
                    Console.Write('♣');
                }
                else if (map[row, column] is CellType.PathPoint)
                {
                    Console.Write('◌');
                }
                else
                {
                    Console.Write('?');
                }
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}

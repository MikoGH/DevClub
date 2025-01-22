public static class MapGenerator
{
    public static Map<CellType> CreateMapFromGraph(int height, int width, params Graph<PlanarPoint, CellType, Empty>[] graphs)
    {
        var result = new Map<CellType>(height, width);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                result[row, column] = CellType.Wall;
            }
        }

        for (var graphIndex = 0; graphIndex < graphs.Length; graphIndex++)
        {
            var graph = graphs[graphIndex];
            var mapNodes = graph.Nodes
                .Where(val => val.Key.Y < height && val.Key.X < width
                    && val.Key.Y >= 0 && val.Key.X >= 0)
                .ToDictionary(x => x.Key, x => x.Value.Value);

            foreach (var node in mapNodes)
            {
                result[node.Key.Y, node.Key.X] = node.Value;
            }
        }
        return result;
    }
    
    public static Map<CellType> CreateCellularMap(int height, int width)
    {
        var map = new Map<CellType>(height, width);
        var cellsCount = map.Height * map.Width;
        for (var index = 0; index < cellsCount; index++)
            // map[index] = CellType.Floor; // Random.Shared.NextDouble() < 0.4 ? CellType.Wall : CellType.Floor;
            map[index] = Random.Shared.NextDouble() < 0.4 ? CellType.Wall : CellType.Floor;

        var iterationsCount = 3;
        for (var index = 0; index < iterationsCount; index++)
        {
            var result = new Map<CellType>(map.Height, map.Width);
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    var adjacentCount = map.CountAdjacent(row, column, val => val is CellType.Wall, true);
                    var cell = adjacentCount switch
                    {
                        // >= 4 when map[row, column] == CellType.Wall => CellType.Wall,
                        >= 5 => CellType.Wall,
                        <= 3 => CellType.Floor,
                        _ => map[row, column],
                    };
                    result[row, column] = cell;
                }
            }

            map = result;
        }

        return map;
    }
}

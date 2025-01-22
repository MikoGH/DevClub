using MapNode = Node<PlanarPoint, CellType>;
using MapEdge = Edge<Node<PlanarPoint, CellType>, Empty>;

public static class GraphGenerator
{
    public static MapNode GetRandomNode(Graph<PlanarPoint, CellType, Empty> graph, Func<MapNode, bool> predicate)
    {
        var cells = graph.Nodes.Values
            .Where(predicate)
            .ToList();

        var result = cells[Random.Shared.Next(0, cells.Count)];
        return result;
    }

    public static Graph<PlanarPoint, CellType, Empty> CreateGraphFromMap(Map<CellType> map)
    {
        var nodes = new List<MapNode>(map.Length);
        var edges = new List<MapEdge>(map.Length);
        for (var row = 0; row < map.Height; row++)
        {
            for (var column = 0; column < map.Width; column++)
            {
                if (map[row, column] is CellType.Wall)
                    continue;

                nodes.Add(
                    new MapNode(
                        new PlanarPoint(row, column),
                        CellType.Floor));
            }
        }

        foreach (var node in nodes)
        {
            if (map[node.Index.Y, node.Index.X] is CellType.Wall)
                continue;

            var adjacentCells = map.GetAdjacent(node.Index.Y, node.Index.X, val => val is CellType.Floor, false, false).ToList();
            var adjacentNodes = nodes.Where(val => adjacentCells.Contains(val.Index)).ToList();
            var currentEdges = adjacentNodes.Select(val => new MapEdge(node, val, Empty.Inst)).ToList();
            edges.AddRange(currentEdges);
        }

        var result = new Graph<PlanarPoint, CellType, Empty>(nodes, edges);
        return result;
    }
}

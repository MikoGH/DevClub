using MapNode = Node<PlanarPoint, CellType>;
using MapEdge = Edge<Node<PlanarPoint, CellType>, Empty>;
using EmptyNode = Node<int, Empty>;
using EmptyEdge = Edge<Node<int, Empty>, Empty>;
using EmptyGraph = Graph<int, Empty, Empty>;
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser, RankColumn]
public class Bench
{
    private PlanarPoint _startIndex;
    private PlanarPoint _endIndex;

    public Graph<PlanarPoint, CellType, Empty> Graph = Graph<PlanarPoint, CellType, Empty>.Empty;

    public int Width = 240;
    public int Height = 120;

    [IterationSetup]
    public void Setup()
    {
        var map = CreateCellularMap(Height, Width);
        Graph = CreateGraphFromMap(map);
        _startIndex = GetRandomNode(Graph, x => x.Value == CellType.Floor).Index;
        _endIndex = GetRandomNode(Graph, x => x.Value == CellType.Floor).Index;
    }

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

        // for (var row = 0; row < map.Height; row++)
        // {
        //     for (var column = 0; column < map.Width; column++)
        //     {
        foreach (var node in nodes)
        {
            // if (map[row, column] is CellType.Wall)
            //     continue;
            if (map[node.Index.Y, node.Index.X] is CellType.Wall)
                continue;

            // var node = nodes.First(val => val.Index.Y == row && val.Index.X == column);
            // var adjacentCells = map.GetAdjacent(row, column, val => val is CellType.Floor, false, false).ToList();
            var adjacentCells = map.GetAdjacent(node.Index.Y, node.Index.X, val => val is CellType.Floor, false, false).ToList();
            var adjacentNodes = nodes.Where(val => adjacentCells.Contains(val.Index)).ToList();
            var currentEdges = adjacentNodes.Select(val => new MapEdge(node, val, Empty.Inst)).ToList();
            edges.AddRange(currentEdges);
        }

        var result = new Graph<PlanarPoint, CellType, Empty>(nodes, edges);
        return result;
    }

    public static Map<CellType> CreateCellularMap(int height, int width)
    {
        var map = new Map<CellType>(height, width);
        var cellsCount = map.Height * map.Width;
        for (var index = 0; index < cellsCount; index++)
            map[index] = CellType.Floor; // Random.Shared.NextDouble() < 0.4 ? CellType.Wall : CellType.Floor;

        // var iterationsCount = 5;
        // for (var index = 0; index < iterationsCount; index++)
        // {
        //     var result = new Map<CellType>(map.Height, map.Width);
        //     for (int row = 0; row < map.Height; row++)
        //     {
        //         for (int column = 0; column < map.Width; column++)
        //         {
        //             var adjacentCount = map.CountAdjacent(row, column, val => val is CellType.Wall, true);
        //             var cell = adjacentCount switch
        //             {
        //                 // >= 4 when map[row, column] == CellType.Wall => CellType.Wall,
        //                 >= 5 => CellType.Wall,
        //                 <= 3 => CellType.Floor,
        //                 _ => map[row, column],
        //             };
        //             result[row, column] = cell;
        //         }
        //     }

        //     map = result;
        // }

        return map;
    }

    public Graph<PlanarPoint, CellType, Empty> Astar()
    {
        var visited = Graph.Nodes.ToDictionary(x => x.Key, x => false);
        var distances = Graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);

        var result = new Graph<PlanarPoint, CellType, Empty>();

        var nextCells = new PriorityQueue<PlanarPoint, int>();
        nextCells.Enqueue(_startIndex, CountDistance(_startIndex, distances));
        visited[_startIndex] = true;

        while (nextCells.Count > 0)
        {
            var currentCell = nextCells.Dequeue();

            if (Graph[currentCell] == CellType.PathEnd)
                break;

            foreach (var adjacentCell in Graph.AdjacentNodes[currentCell])
            {
                distances[adjacentCell.Index] = distances[currentCell] + 1;
                visited[adjacentCell.Index] = true;
                nextCells.Enqueue(adjacentCell.Index, CountDistance(adjacentCell.Index, distances));
            }
        }

        // var nodeIndex = _endIndex;
        // while (nodeIndex != _startIndex)
        // {
        //     var minDistances = Graph.AdjacentNodes[nodeIndex]
        //         .GroupBy(val => distances[val.Index])
        //         .OrderBy(x => x.Key)
        //         .First();
        //     var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
        //     result[randomNode.Index] = CellType.PathPoint;
        //     nodeIndex = randomNode.Index;
        // }

        // result[_startIndex] = CellType.PathStart;
        // result[_endIndex] = CellType.PathEnd;

        return result;
    }

    private int CountDistance(PlanarPoint planarPoint, Dictionary<PlanarPoint, int> distances)
    {
        return distances[planarPoint] + Math.Abs(_endIndex.Y - planarPoint.Y) + Math.Abs(_endIndex.X - planarPoint.X);
    }

    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> Bfs()
    {
        var visited = Graph.Nodes.ToDictionary(x => x.Key, x => false);
        var distances = Graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(_startIndex, CellType.Visited);
        var queue = new Queue<PlanarPoint>();
        queue.Enqueue(_startIndex);
        visited[_startIndex] = true;
        distances[_startIndex] = 0;

        while (queue.Count > 0)
        {
            var currentV = queue.Dequeue();

            if (currentV == _endIndex)
                break;

            foreach (var v in Graph.AdjacentNodes[currentV])
            {
                if (visited[v.Index])
                    continue;

                visited[v.Index] = true;
                result.AddNode(v.Index, CellType.Visited);
                distances[v.Index] = distances[currentV] + 1;
                queue.Enqueue(v.Index);
            }
        }

        var nodeIndex = _endIndex;
        while (nodeIndex != _startIndex)
        {
            var minDistances = Graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[_startIndex] = CellType.PathStart;
        result[_endIndex] = CellType.PathEnd;
        return result;
    }

    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> Dfs()
    {
        var distances = Graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);
        distances[_startIndex] = 0;

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(_startIndex, CellType.Visited);

        Dfs(Graph, result, distances, _startIndex, _endIndex);

        var nodeIndex = _endIndex;
        while (nodeIndex != _startIndex)
        {
            var minDistances = Graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[_startIndex] = CellType.PathStart;
        result[_endIndex] = CellType.PathEnd;
        return result;
    }

    public static void Dfs(
        Graph<PlanarPoint, CellType, Empty> graph,
        Graph<PlanarPoint, CellType, Empty> result,
        Dictionary<PlanarPoint, int> distances,
        PlanarPoint currentIndex,
        PlanarPoint endIndex)
    {
        if (currentIndex == endIndex || distances[currentIndex] > distances[endIndex])
            return;

        foreach (var adjacentNode in graph.AdjacentNodes[currentIndex])
        {
            var currAdjDist = distances[adjacentNode.Index];
            var currDist = distances[currentIndex];
            if (distances[adjacentNode.Index] <= distances[currentIndex] + 1)
                continue;

            distances[adjacentNode.Index] = distances[currentIndex] + 1;

            result.AddNode(adjacentNode.Index, CellType.Visited);
            Dfs(graph, result, distances, adjacentNode.Index, endIndex);
        }
    }

    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> DfsStack()
    {
        var distances = Graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);
        distances[_startIndex] = 0;

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(_startIndex, CellType.Visited);

        var stack = new Stack<PlanarPoint>();
        stack.Push(_startIndex);

        while (stack.Count > 0)
        {
            var currentIndex = stack.Pop();
            if (currentIndex == _endIndex
                || distances[currentIndex] > distances[_endIndex])
                continue;

            foreach (var adjacentNode in Graph.AdjacentNodes[currentIndex])
            {
                var currAdjDist = distances[adjacentNode.Index];
                var currDist = distances[currentIndex];
                if (distances[adjacentNode.Index] <= distances[currentIndex] + 1)
                    continue;

                distances[adjacentNode.Index] = distances[currentIndex] + 1;

                result.AddNode(adjacentNode.Index, CellType.Visited);
                stack.Push(adjacentNode.Index);
            }
        }

        var nodeIndex = _endIndex;
        while (nodeIndex != _startIndex)
        {
            var minDistances = Graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[_startIndex] = CellType.PathStart;
        result[_endIndex] = CellType.PathEnd;
        return result;
    }

    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> DfsBfs()
    {
        var visited = Graph.Nodes.ToDictionary(x => x.Key, x => false);
        var distances = Graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(_startIndex, CellType.Visited);
        var stack = new Stack<PlanarPoint>();
        stack.Push(_startIndex);
        visited[_startIndex] = true;
        distances[_startIndex] = 0;

        while (stack.Count > 0)
        {
            var currentV = stack.Pop();

            if (currentV == _endIndex)
                break;

            foreach (var v in Graph.AdjacentNodes[currentV])
            {
                if (visited[v.Index])
                    continue;

                visited[v.Index] = true;
                result.AddNode(v.Index, CellType.Visited);
                distances[v.Index] = distances[currentV] + 1;
                stack.Push(v.Index);
            }
        }

        var nodeIndex = _endIndex;
        while (nodeIndex != _startIndex)
        {
            var minDistances = Graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[_startIndex] = CellType.PathStart;
        result[_endIndex] = CellType.PathEnd;
        return result;
    }
}

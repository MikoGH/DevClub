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
        var map = MapGenerator.CreateCellularMap(Height, Width);
        Graph = GraphGenerator.CreateGraphFromMap(map);
        _startIndex = GraphGenerator.GetRandomNode(Graph, x => x.Value == CellType.Floor).Index;
        _endIndex = GraphGenerator.GetRandomNode(Graph, x => x.Value == CellType.Floor).Index;
    }

    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> Bfs()
    {
        return Solver.Bfs(Graph, _startIndex, _endIndex);
    }

    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> Dfs()
    {
        return Solver.Dfs(Graph, _startIndex, _endIndex);
    }


    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> DfsStack()
    {
        return Solver.DfsStack(Graph, _startIndex, _endIndex);
    }


    [Benchmark]
    public Graph<PlanarPoint, CellType, Empty> Astar()
    {
        return Solver.Astar(Graph, _startIndex, _endIndex);
    }
}

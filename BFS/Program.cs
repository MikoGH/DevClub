using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MapNode = Node<PlanarPoint, CellType>;
using MapEdge = Edge<Node<PlanarPoint, CellType>, Empty>;

public class Program
{
    public static void Main(string[] args)
    {
        var width = 60;
        var height = 40;
        var map = MapGenerator.CreateCellularMap(height, width);
        var graph = GraphGenerator.CreateGraphFromMap(map);
        var startIndex = GraphGenerator.GetRandomNode(graph, x => x.Value == CellType.Floor).Index;
        var endIndex = GraphGenerator.GetRandomNode(graph, x => x.Value == CellType.Floor).Index;
        
        var astarMap = Solver.Astar(graph, startIndex, endIndex);
        
        var astarMapToDraw = MapGenerator.CreateMapFromGraph(height, width, graph, astarMap);
        Renderer.RenderCellTypeMap(astarMapToDraw);

        // dotnet run --configuration Release
        // BenchmarkRunner.Run<Bench>();
    }
}

// 80 x 40
// | Method | Mean       | Error    | StdDev   | Rank | Gen0     | Gen1   | Allocated |
// |------- |-----------:|---------:|---------:|-----:|---------:|-------:|----------:|
// | Bfs    |   358.4 us |  6.35 us |  6.80 us |    1 | 103.0273 | 6.3477 | 433.08 KB |
// | Dfs    | 1,770.7 us | 35.37 us | 79.83 us |    2 |  46.8750 | 1.9531 | 199.49 KB |

// 160 x 80
// | Method | Mean        | Error     | StdDev      | Rank | Gen0     | Gen1     | Gen2    | Allocated |
// |------- |------------:|----------:|------------:|-----:|---------:|---------:|--------:|----------:|
// | Bfs    |    694.5 us |  13.84 us |    12.27 us |    1 | 132.8125 | 131.8359 | 66.4063 | 734.81 KB |
// | Dfs    | 33,533.4 us | 669.88 us | 1,717.16 us |    2 | 125.0000 |  62.5000 | 62.5000 | 703.14 KB |

// 240 * 120
// | Method   | Mean          | Error        | StdDev        | Median        | Rank | Allocated |
// |--------- |--------------:|-------------:|--------------:|--------------:|-----:|----------:|
// | Bfs      |      6.141 ms |     1.101 ms |      3.246 ms |      5.274 ms |    1 |   3.91 MB |
// | Dfs      |            NA |           NA |            NA |            NA |    ? |        NA |
// | DfsStack | 26,944.792 ms | 6,053.919 ms | 17,850.119 ms | 23,644.562 ms |    3 |   6.73 MB |
// | DfsBfs   |     10.637 ms |     1.313 ms |      3.850 ms |     12.213 ms |    2 |   7.53 MB |




// TODO: 0. [+] Переопределить сигнатуру индексера ребра на композит индексатора вершин, агностичный положению
//       1. [+] Переопределить сигнатуры конструкторов и добавления графа
//         1.1 [-] Создать псевдонимы для типов для того, чтобы было читаемо (красиво)
//       2. [+] Переписать изначальную задачу BFS на новой структуре графа
//         2.1 [+] Напилить бенчмарки
//       3. [+] Реализовать простенький mapgen -> поверх нашего графа? поверх базовых структур?
//         3.1 [+] Визуализация
//         3.2 [+] Транслятор
//       4. Реализовать A*, BFS, DFS
//       5. Заменить мат.операции на Intrinsics, Parallel, TPL/Dataflow, и CUDA/NVidia
//         5.1 Бенчмарки
//       6. Улучшение клеточных автоматов?
//       6. Простая нейронка?
//       CONCL: Обсудить, почему не вышли те или иные имплементации; обсудить бенчмарки

//      !!. Создать веб-морду для асинхронного взаимодействия с движком

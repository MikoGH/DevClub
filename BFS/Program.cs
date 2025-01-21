using System;
using System.Reflection.Metadata.Ecma335;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using static Program;
using MapNode = Node<PlanarPoint, CellType>;
using MapEdge = Edge<Node<PlanarPoint, CellType>, Empty>;

public record struct PlanarPoint(int Y, int X);

public class Program
{
    public static void Main(string[] args)
    {
        // var width = 160;
        // var height = 80;
        // var map = Bench.CreateCellularMap(height, width);
        // var graph = Bench.CreateGraphFromMap(map);
        // var startIndex = Bench.GetRandomNode(graph, x => x.Value == CellType.Floor).Index;
        // var endIndex = Bench.GetRandomNode(graph, x => x.Value == CellType.Floor).Index;
        // var dfsStackMap = Bench.DfsStack();

        // var bench = new Bench();
        // bench.Setup();
        // var dfsStackMap = bench.DfsStack();
        // var mapToDraw = CreateMapFromGraph(bench.Height, bench.Width, bench.Graph, dfsStackMap);
        // Renderer.RenderCellTypeMap(mapToDraw);

        // dotnet run --configuration Release
        BenchmarkRunner.Run<Bench>();
    }

    private static Map<CellType> CreateMapFromGraph(int height, int width, params Graph<PlanarPoint, CellType, Empty>[] graphs)
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

public enum CellType : byte
{
    Floor = 0,
    PathStart = 64,
    PathEnd = 65,
    PathPoint = 66,
    Visited = 128,
    Wall = 255,
}

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

public class Map<T>
{
    private readonly int _width;
    private readonly int _height;
    private readonly T[] _array;

    public int Width => _width;
    public int Height => _height;

    public int Length => _array.Length;

    public T this[int row, int column]
    {
        get => _array[row * _width + column];
        set => _array[row * _width + column] = value;
    }

    public T this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    public Map(int height, int width)
    {
        if (height <= 0) throw new ArgumentNullException(nameof(height));
        if (width <= 0) throw new ArgumentNullException(nameof(width));

        // TODO: Проверить overflow._adjacentNodes

        _height = height;
        _width = width;

        _array = new T[height * width];
    }

    public int CountAdjacent(int y, int x, Func<T, bool> predicate, bool countOutOfBounds)
    {
        // TODO: Добавить радиус
        int result = 0;
        for (int row = y - 1; row <= y + 1; row++)
        {
            for (int column = x - 1; column <= x + 1; column++)
            {
                if (row == y && column == x)
                    continue;

                var isOutOfBounds = (row < 0 || column < 0 || row >= _height || column >= _width);
                if (isOutOfBounds && !countOutOfBounds)
                    continue;

                var hasValue = (isOutOfBounds && countOutOfBounds)
                    || predicate(_array[row * Width + column]);
                if (!hasValue)
                    continue;

                result++;
            }
        }

        return result;
    }

    public IEnumerable<PlanarPoint> GetAdjacent(int y, int x, Func<T, bool> predicate, bool includeOutOfBounds, bool includeDiagonals)
    {
        // TODO: Добавить радиус
        for (int row = y - 1; row <= y + 1; row++)
        {
            for (int column = x - 1; column <= x + 1; column++)
            {
                if (!includeDiagonals && row != y && column != x)
                    continue;

                if (row == y && column == x)
                    continue;

                var isOutOfBounds = (row < 0 || column < 0 || row >= _height || column >= _width);
                if (isOutOfBounds && !includeOutOfBounds)
                    continue;

                var hasValue = (isOutOfBounds && includeOutOfBounds)
                    || predicate(_array[row * Width + column]);
                if (!hasValue)
                    continue;

                yield return new PlanarPoint(row, column);
            }
        }
    }
}

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

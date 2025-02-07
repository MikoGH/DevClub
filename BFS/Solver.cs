using OpenCL.Net;

public static class Solver
{
    private const string KernelSource = @"
    __kernel void square(__global float* input, __global float* output) {
        int i = get_global_id(0);
        output[i] = input[i] * input[i];
    }";

    public static void BaselineBfs()
    {
        int n = 7;  // кол-во вершин
        var input = new List<(int v1, int v2)>()
        {
            (1, 2),
            (1, 3),
            (2, 3),
            (2, 5),
            (0, 6),
            (0, 1)
        };
        int startV = 1;

        var connections = input
            .Select(x => (v1: x.v2, v2: x.v1))
            .Concat(input)
            .Distinct()
            .ToLookup(x => x.v1, x => x.v2);

        var visited = Enumerable.Repeat(false, n).ToArray();

        var verts = Enumerable.Repeat(-1, n).ToArray();

        var queue = new Queue<int>();
        queue.Enqueue(startV);
        visited[startV] = true;
        verts[startV] = 0;

        while (queue.Count > 0)
        {
            int currentV = queue.Dequeue();
            foreach (int v in connections[currentV])
            {
                if (visited[v])
                    continue;

                visited[v] = true;
                verts[v] = verts[currentV] + 1;
                queue.Enqueue(v);
            }
        }

        for (int i = 0; i < n; i++)
        {
            Console.WriteLine($"{i}: {verts[i]}");
        }

        var set = new HashSet<int>() { 1, 2 };
        var set2 = set.Append(1);
        var set3 = set2.Append(3);

        Console.WriteLine("set");
        foreach (var item in set)
        {
            Console.WriteLine(item);
        }

        Console.WriteLine("set2");
        foreach (var item in set2)
        {
            Console.WriteLine(item);
        }

        Console.WriteLine("set3");
        foreach (var item in set3)
        {
            Console.WriteLine(item);

        }
    }

    public static float[] GpuBfs(float[] inputArray)
    {
        int length = inputArray.Length;
        float[] outputArray = new float[length];

        // Получаем платформу OpenCL
        ErrorCode error;
        Platform platform = Cl.GetPlatformIDs(out error).First();
        Device device = Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error).First();
        Context context = Cl.CreateContext(null, 1, new[] { device }, null, IntPtr.Zero, out error);
        CommandQueue commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.None, out error);

        // Создаем буферы
        IMem<float> inputBuffer = Cl.CreateBuffer<float>(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, inputArray, out error);
        IMem<float> outputBuffer = Cl.CreateBuffer<float>(context, MemFlags.WriteOnly, length, out error);

        // Создаем и компилируем программу
        var program = Cl.CreateProgramWithSource(context, 1, new[] { KernelSource }, null, out error);
        Cl.BuildProgram(program, 1, new[] { device }, string.Empty, null, IntPtr.Zero);
        Kernel kernel = Cl.CreateKernel(program, "square", out error);

        // Привязываем аргументы
        Cl.SetKernelArg(kernel, 0, inputBuffer);
        Cl.SetKernelArg(kernel, 1, outputBuffer);

        // Запускаем выполнение
        Event kernelEvent;
        Cl.EnqueueNDRangeKernel(commandQueue, kernel, 1, null, new[] { (IntPtr)length }, null, 0, null, out kernelEvent);

        // Читаем результаты
        Cl.EnqueueReadBuffer(commandQueue, outputBuffer, Bool.True, IntPtr.Zero, length * sizeof(float), outputArray, 0, null, out kernelEvent);

        // Освобождаем ресурсы
        Cl.ReleaseKernel(kernel);
        Cl.ReleaseProgram(program);
        Cl.ReleaseMemObject(inputBuffer);
        Cl.ReleaseMemObject(outputBuffer);
        Cl.ReleaseCommandQueue(commandQueue);
        Cl.ReleaseContext(context);

        return outputArray;
    }

    public static Graph<PlanarPoint, CellType, Empty> Bfs(Graph<PlanarPoint, CellType, Empty> graph, PlanarPoint startIndex, PlanarPoint endIndex)
    {
        var visited = graph.Nodes.ToDictionary(x => x.Key, x => false);
        var distances = graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(startIndex, CellType.Visited);
        var queue = new Queue<PlanarPoint>();
        queue.Enqueue(startIndex);
        visited[startIndex] = true;
        distances[startIndex] = 0;

        while (queue.Count > 0)
        {
            var currentV = queue.Dequeue();

            if (currentV == endIndex)
                break;

            foreach (var v in graph.AdjacentNodes[currentV])
            {
                if (visited[v.Index])
                    continue;

                visited[v.Index] = true;
                result.AddNode(v.Index, CellType.Visited);
                distances[v.Index] = distances[currentV] + 1;
                queue.Enqueue(v.Index);
            }
        }

        var nodeIndex = endIndex;
        while (nodeIndex != startIndex)
        {
            var minDistances = graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[startIndex] = CellType.PathStart;
        result[endIndex] = CellType.PathEnd;
        return result;
    }

    public static Graph<PlanarPoint, CellType, Empty> Dfs(Graph<PlanarPoint, CellType, Empty> graph, PlanarPoint startIndex, PlanarPoint endIndex)
    {
        var distances = graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);
        distances[startIndex] = 0;

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(startIndex, CellType.Visited);

        Dfs(graph, result, distances, startIndex, endIndex);

        var nodeIndex = endIndex;
        while (nodeIndex != startIndex)
        {
            var minDistances = graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[startIndex] = CellType.PathStart;
        result[endIndex] = CellType.PathEnd;
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

    public static Graph<PlanarPoint, CellType, Empty> DfsStack(Graph<PlanarPoint, CellType, Empty> graph, PlanarPoint startIndex, PlanarPoint endIndex)
    {
        var distances = graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);
        distances[startIndex] = 0;

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(startIndex, CellType.Visited);

        var stack = new Stack<PlanarPoint>();
        stack.Push(startIndex);

        while (stack.Count > 0)
        {
            var currentIndex = stack.Pop();
            if (currentIndex == endIndex
                || distances[currentIndex] > distances[endIndex])
                continue;

            foreach (var adjacentNode in graph.AdjacentNodes[currentIndex])
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

        var nodeIndex = endIndex;
        while (nodeIndex != startIndex)
        {
            var minDistances = graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[startIndex] = CellType.PathStart;
        result[endIndex] = CellType.PathEnd;
        return result;
    }

    public static Graph<PlanarPoint, CellType, Empty> DfsBfs(Graph<PlanarPoint, CellType, Empty> graph, PlanarPoint startIndex, PlanarPoint endIndex)
    {
        var visited = graph.Nodes.ToDictionary(x => x.Key, x => false);
        var distances = graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);

        var result = new Graph<PlanarPoint, CellType, Empty>();
        result.AddNode(startIndex, CellType.Visited);
        var stack = new Stack<PlanarPoint>();
        stack.Push(startIndex);
        visited[startIndex] = true;
        distances[startIndex] = 0;

        while (stack.Count > 0)
        {
            var currentV = stack.Pop();

            if (currentV == endIndex)
                break;

            foreach (var v in graph.AdjacentNodes[currentV])
            {
                if (visited[v.Index])
                    continue;

                visited[v.Index] = true;
                result.AddNode(v.Index, CellType.Visited);
                distances[v.Index] = distances[currentV] + 1;
                stack.Push(v.Index);
            }
        }

        var nodeIndex = endIndex;
        while (nodeIndex != startIndex)
        {
            var minDistances = graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[startIndex] = CellType.PathStart;
        result[endIndex] = CellType.PathEnd;
        return result;
    }

    public static Graph<PlanarPoint, CellType, Empty> Astar(Graph<PlanarPoint, CellType, Empty> graph, PlanarPoint startIndex, PlanarPoint endIndex)
    {
        var visited = graph.Nodes.ToDictionary(x => x.Key, x => false);
        var distances = graph.Nodes.ToDictionary(x => x.Key, x => int.MaxValue);

        var result = new Graph<PlanarPoint, CellType, Empty>();

        var nextIndexes = new PriorityQueue<PlanarPoint, int>();
        nextIndexes.Enqueue(startIndex, CountDistance(startIndex, distances, endIndex));
        visited[startIndex] = true;
        distances[startIndex] = 0;
        result.AddNode(startIndex, CellType.Visited);

        while (nextIndexes.Count > 0)
        {
            var currentIndex = nextIndexes.Dequeue();

            if (currentIndex == endIndex)
                break;

            foreach (var adjacentCell in graph.AdjacentNodes[currentIndex])
            {
                if (visited[adjacentCell.Index])
                    continue;

                distances[adjacentCell.Index] = distances[currentIndex] + 1;
                visited[adjacentCell.Index] = true;
                nextIndexes.Enqueue(adjacentCell.Index, CountDistance(adjacentCell.Index, distances, endIndex));
                result.AddNode(adjacentCell.Index, CellType.Visited);
            }
        }

        var nodeIndex = endIndex;
        while (nodeIndex != startIndex)
        {
            var minDistances = graph.AdjacentNodes[nodeIndex]
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[startIndex] = CellType.PathStart;
        result[endIndex] = CellType.PathEnd;

        return result;
    }

    public static Graph<PlanarPoint, CellType, Empty> AstarDuke(Graph<PlanarPoint, CellType, Empty> graph, PlanarPoint startIndex, PlanarPoint endIndex)
    {
        var result = new Graph<PlanarPoint, CellType, Empty>();
        var distances = new Graph<PlanarPoint, int, Empty>();
        distances.AddNode(startIndex, 0);

        var nextIndexes = new PriorityQueue<PlanarPoint, int>();
        nextIndexes.Enqueue(startIndex, CountDistance(startIndex, distances[startIndex], endIndex));
        result.AddNode(startIndex, CellType.Visited);

        while (nextIndexes.Count > 0)
        {
            var currentIndex = nextIndexes.Dequeue();

            if (currentIndex == endIndex)
                break;

            foreach (var adjacentCell in graph.AdjacentNodes[currentIndex])
            {
                var adjacentIndex = adjacentCell.Index;
                if (distances.ContainsNode(adjacentIndex))
                    continue;

                distances.AddNode(adjacentIndex, distances[currentIndex] + 1);
                nextIndexes.Enqueue(adjacentIndex, CountDistance(adjacentIndex, distances[adjacentIndex], endIndex));
                result.AddNode(adjacentIndex, CellType.Visited);
            }
        }

        var nodeIndex = endIndex;
        while (nodeIndex != startIndex)
        {
            var minDistances = graph.AdjacentNodes[nodeIndex]
                .Where(val => distances.ContainsNode(val.Index))
                .GroupBy(val => distances[val.Index])
                .OrderBy(x => x.Key)
                .First();
            var randomNode = minDistances.ElementAt(Random.Shared.Next(0, minDistances.Count()));
            result[randomNode.Index] = CellType.PathPoint;
            nodeIndex = randomNode.Index;
        }

        result[startIndex] = CellType.PathStart;
        result[endIndex] = CellType.PathEnd;

        return result;
    }

    private static int CountDistance(PlanarPoint planarPoint, Dictionary<PlanarPoint, int> distances, PlanarPoint endIndex)
    {
        return distances[planarPoint] + Math.Abs(endIndex.Y - planarPoint.Y) + Math.Abs(endIndex.X - planarPoint.X);
    }

    private static int CountDistance(PlanarPoint planarPoint, int distance, PlanarPoint endIndex)
    {
        return distance + Math.Abs(endIndex.Y - planarPoint.Y) + Math.Abs(endIndex.X - planarPoint.X);
    }
}

public class BaselineBfs
{
    public void Bfs()
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

        // var connections = Enumerable.Range(0, n)
        //     .Select(i => new KeyValuePair<int, List<int>>(i, new List<int>()))
        //     .ToArray();

        var connections = input
            .Select(x => (v1: x.v2, v2: x.v1))
            .Concat(input)
            .Distinct()
            .ToLookup(x => x.v1, x => x.v2);

        // for (int i = 0; i < input.Count; i++)
        // {
        //     connections[input[i].v1].Value.Add(input[i].v2);
        //     connections[input[i].v2].Value.Add(input[i].v1);
        // }

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
}
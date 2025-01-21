
public record struct Node<TIndex, TValue>(TIndex Index, TValue Value)
: IEquatable<Node<TIndex, TValue>> where TIndex : struct, IEquatable<TIndex>
{
    public bool Equals(Node<TIndex, TValue> other)
        => Index.Equals(other.Index);
}

public record Empty()
{
    public static readonly Empty Inst = new Empty();
}

// public record struct Edge<TIndex, TNodeValue, TValue>(Node<TIndex, TNodeValue> N1, Node<TIndex, TNodeValue> N2, TValue Value)
// : IEquatable<Edge<TIndex, TNodeValue, TValue>> where TIndex : struct, IEquatable<TIndex>
// {
//     public bool Equals(Edge<TIndex, TNodeValue, TValue> other)
//         => N1.Equals(other.N1) && N2.Equals(other.N2)
//         || N2.Equals(other.N1) && N1.Equals(other.N2);
// }

public record struct Edge<TNode, TValue>(TNode N1, TNode N2, TValue Value)
: IEquatable<Edge<TNode, TValue>> where TNode : struct
{
    public bool Equals(Edge<TNode, TValue> other)
        => N1.Equals(other.N1) && N2.Equals(other.N2)
        || N2.Equals(other.N1) && N1.Equals(other.N2);

    public bool Contains(TNode node) => node.Equals(N1) || node.Equals(N2);
}

public record struct EdgeIndex<TIndex>(TIndex Ind1, TIndex Ind2)
: IEquatable<EdgeIndex<TIndex>> where TIndex : struct, IEquatable<TIndex>
{
    public bool Equals(EdgeIndex<TIndex> other)
        => Ind1.Equals(other.Ind1) && Ind2.Equals(other.Ind2)
        || Ind2.Equals(other.Ind1) && Ind1.Equals(other.Ind2);

    public override int GetHashCode()
    {
        unchecked
        {
            return HashCode.Combine(Ind1) + HashCode.Combine(Ind2);
        }
    }

    public static implicit operator EdgeIndex<TIndex>((TIndex ind1, TIndex ind2) t) => new(t.ind1, t.ind2);
}

// TODO: Impl later
public record struct DirectedIndex<TIndex>(TIndex Ind1, TIndex Ind2)
{
    public static implicit operator DirectedIndex<TIndex>((TIndex ind1, TIndex ind2) t) => new(t.ind1, t.ind2);
}

public class Graph<TIndex, TNodeValue, TEdgeValue>
where TIndex : struct, IEquatable<TIndex>
{
    public static Graph<TIndex, TNodeValue, TEdgeValue> Empty = new();

    private readonly Dictionary<TIndex, HashSet<Node<TIndex, TNodeValue>>> _adjacentNodes;
    private readonly Dictionary<TIndex, Node<TIndex, TNodeValue>> _nodes = new();
    private readonly Dictionary<EdgeIndex<TIndex>, Edge<Node<TIndex, TNodeValue>, TEdgeValue>> _edges = new();
    public Dictionary<TIndex, Node<TIndex, TNodeValue>> Nodes => _nodes;
    public Dictionary<EdgeIndex<TIndex>, Edge<Node<TIndex, TNodeValue>, TEdgeValue>> Edges => _edges;
    public Dictionary<TIndex, HashSet<Node<TIndex, TNodeValue>>> AdjacentNodes => _adjacentNodes;

    public TNodeValue this[in TIndex ind]
    {
        get => _nodes[ind].Value;
        set => _nodes[ind] = _nodes[ind] with { Value = value };
    }

    public TEdgeValue this[in TIndex ind1, in TIndex ind2]
    {
        get => _edges[(ind1, ind2)].Value;
        set => _edges[(ind1, ind2)] = _edges[(ind1, ind2)] with { Value = value };
    }

    public Graph() => _adjacentNodes = new();

    public Graph(
        IEnumerable<Node<TIndex, TNodeValue>> nodes,
        IEnumerable<Edge<Node<TIndex, TNodeValue>, TEdgeValue>> edges)
    {
        _nodes = nodes.ToDictionary(x => x.Index);
        _edges = edges
            .Select(x => (Key: new EdgeIndex<TIndex>(x.N1.Index, x.N2.Index), Value: x))
            .DistinctBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
        // _adjacentNodes = new(_nodes.Count);

        var edgesTuple = _edges.Values.Select(edge => (v1: edge.N1, v2: edge.N2));
        _adjacentNodes = _edges.Values
            .Select(edge => (v1: edge.N2, v2: edge.N1))
            .Concat(edgesTuple)
            .Distinct()
            .GroupBy(x => x.v1, x => x.v2)
            .ToDictionary(
                x => x.Key.Index,
                x => x.ToHashSet()
            );

        // foreach (var node in _nodes.Values)
        // {
        //     var adjacentNodes = _edges.Values
        //         .Where(edge => edge.Contains(node))
        //         .Select(edge => edge.N1.Equals(node) ? edge.N2 : edge.N1)
        //         .ToHashSet();
        //     _adjacentNodes[node.Index] = adjacentNodes;
        // }
    }

    // TODO: Возвращать коллекцию связанных вершин по индексу вершины.

    public Node<TIndex, TNodeValue> AddNode(TIndex index, TNodeValue value)
    {
        if (_nodes.TryGetValue(index, out var existingNode))
        {
            return existingNode;
        }

        var node = new Node<TIndex, TNodeValue>(index, value);
        _nodes.Add(node.Index, node);
        _adjacentNodes[node.Index] = [];
        return node;
    }

    private bool ContainsNode(Node<TIndex, TNodeValue> node) => _nodes.ContainsKey(node.Index);

    public bool TryAddEdge(
        Node<TIndex, TNodeValue> node1,
        Node<TIndex, TNodeValue> node2,
        TEdgeValue value,
        out Edge<Node<TIndex, TNodeValue>, TEdgeValue>? addedEdge)
    {
        if (!ContainsNode(node1) || !ContainsNode(node2))
        {
            addedEdge = null;
            return false;
        }

        if (_edges.TryGetValue((node1.Index, node2.Index), out var existingEdge))
        {
            addedEdge = existingEdge;
            return true;
        }

        var edge = new Edge<Node<TIndex, TNodeValue>, TEdgeValue>(node1, node2, value);
        _edges.Add((node1.Index, node2.Index), edge);

        _adjacentNodes[node1.Index].Add(node2);
        _adjacentNodes[node2.Index].Add(node1);

        addedEdge = edge;
        return true;
    }
}
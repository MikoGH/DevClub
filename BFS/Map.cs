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

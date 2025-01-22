public enum CellType : byte
{
    Floor = 0,
    PathStart = 64,
    PathEnd = 65,
    PathPoint = 66,
    Visited = 128,
    Wall = 255,
}

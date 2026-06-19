using OpenCase.Shared.Dtos;

namespace OpenCase.Web.Components.Game;

public static class BoardMovementPaths
{
    public static IReadOnlyDictionary<PositionDto, List<PositionDto>> FindLegalPaths(
        BoardDto board,
        PositionDto start,
        int diceValue)
    {
        if (diceValue <= 0)
        {
            return new Dictionary<PositionDto, List<PositionDto>>();
        }

        var cells = board.Cells.ToDictionary(cell => (cell.X, cell.Y));
        var origin = new SearchState(start.X, start.Y, 0);
        var queue = new Queue<SearchState>();
        var visited = new HashSet<SearchState> { origin };
        var previous = new Dictionary<SearchState, SearchState>();
        var paths = new Dictionary<PositionDto, List<PositionDto>>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Steps >= diceValue)
            {
                continue;
            }

            foreach (var (x, y) in Neighbors(current.X, current.Y))
            {
                if (!cells.TryGetValue((x, y), out var cell)
                    || cell.Type is not ("Path" or "Entrance"))
                {
                    continue;
                }

                var next = new SearchState(x, y, current.Steps + 1);
                if (!visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;
                var destination = new PositionDto(x, y);

                if (cell.Type == "Entrance")
                {
                    if (destination != start)
                    {
                        paths.TryAdd(destination, Reconstruct(previous, origin, next));
                    }

                    continue;
                }

                if (next.Steps == diceValue)
                {
                    if (destination != start)
                    {
                        paths.TryAdd(destination, Reconstruct(previous, origin, next));
                    }
                }
                else
                {
                    queue.Enqueue(next);
                }
            }
        }

        return paths;
    }

    private static List<PositionDto> Reconstruct(
        IReadOnlyDictionary<SearchState, SearchState> previous,
        SearchState origin,
        SearchState destination)
    {
        var path = new List<PositionDto>();
        var current = destination;

        while (current != origin)
        {
            path.Add(new PositionDto(current.X, current.Y));
            current = previous[current];
        }

        path.Reverse();
        return path;
    }

    private static IEnumerable<(int X, int Y)> Neighbors(int x, int y)
    {
        yield return (x + 1, y);
        yield return (x - 1, y);
        yield return (x, y + 1);
        yield return (x, y - 1);
    }

    private readonly record struct SearchState(int X, int Y, int Steps);
}

using System.Collections.Generic;

/// <summary>
/// Pure C# maze grid logic — no Unity dependencies.
/// Generates a perfect maze using iterative Recursive Backtracker (DFS).
/// </summary>
public class MazeGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    // true = wall present
    public bool[,] HorizontalWalls { get; private set; } // [Height+1, Width]
    public bool[,] VerticalWalls { get; private set; }   // [Height, Width+1]
    public bool[,] Visited { get; private set; }

    public MazeGrid(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Generate(int seed)
    {
        var rng = new System.Random(seed);
        Generate(rng);
    }

    public void Generate(System.Random rng)
    {
        HorizontalWalls = new bool[Height + 1, Width];
        VerticalWalls = new bool[Height, Width + 1];
        Visited = new bool[Height, Width];

        // Initialize all walls
        for (int r = 0; r <= Height; r++)
            for (int c = 0; c < Width; c++)
                HorizontalWalls[r, c] = true;

        for (int r = 0; r < Height; r++)
            for (int c = 0; c <= Width; c++)
                VerticalWalls[r, c] = true;

        // Iterative Recursive Backtracker
        var stack = new Stack<(int row, int col)>();
        Visited[0, 0] = true;
        stack.Push((0, 0));

        while (stack.Count > 0)
        {
            var (cr, cc) = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(cr, cc, rng);

            if (neighbors.Count > 0)
            {
                var (nr, nc) = neighbors[0];
                RemoveWallBetween(cr, cc, nr, nc);
                Visited[nr, nc] = true;
                stack.Push((nr, nc));
            }
            else
            {
                stack.Pop();
            }
        }
    }

    public List<(int row, int col)> GetDeadEnds()
    {
        var deadEnds = new List<(int row, int col)>();
        for (int r = 0; r < Height; r++)
        {
            for (int c = 0; c < Width; c++)
            {
                if (CountOpenings(r, c) == 1)
                    deadEnds.Add((r, c));
            }
        }
        return deadEnds;
    }

    public int FloodFill(int startRow, int startCol)
    {
        if (startRow < 0 || startRow >= Height || startCol < 0 || startCol >= Width)
            return 0;

        var visited = new bool[Height, Width];
        var queue = new Queue<(int r, int c)>();
        visited[startRow, startCol] = true;
        queue.Enqueue((startRow, startCol));
        int count = 0;

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
            count++;

            // Check all 4 neighbors
            // Top (r+1)
            if (r + 1 < Height && !HorizontalWalls[r + 1, c] && !visited[r + 1, c])
            {
                visited[r + 1, c] = true;
                queue.Enqueue((r + 1, c));
            }
            // Bottom (r-1)
            if (r - 1 >= 0 && !HorizontalWalls[r, c] && !visited[r - 1, c])
            {
                visited[r - 1, c] = true;
                queue.Enqueue((r - 1, c));
            }
            // Right (c+1)
            if (c + 1 < Width && !VerticalWalls[r, c + 1] && !visited[r, c + 1])
            {
                visited[r, c + 1] = true;
                queue.Enqueue((r, c + 1));
            }
            // Left (c-1)
            if (c - 1 >= 0 && !VerticalWalls[r, c] && !visited[r, c - 1])
            {
                visited[r, c - 1] = true;
                queue.Enqueue((r, c - 1));
            }
        }

        return count;
    }

    public int CountOpenings(int row, int col)
    {
        int openings = 0;
        if (row + 1 <= Height && !HorizontalWalls[row + 1, col]) openings++;
        if (row >= 0 && !HorizontalWalls[row, col]) openings++;
        if (col + 1 <= Width && !VerticalWalls[row, col + 1]) openings++;
        if (col >= 0 && !VerticalWalls[row, col]) openings++;
        return openings;
    }

    private List<(int row, int col)> GetUnvisitedNeighbors(int row, int col, System.Random rng)
    {
        var neighbors = new List<(int, int)>();
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];
            if (nr >= 0 && nr < Height && nc >= 0 && nc < Width && !Visited[nr, nc])
                neighbors.Add((nr, nc));
        }

        // Fisher-Yates shuffle
        for (int i = neighbors.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = neighbors[i];
            neighbors[i] = neighbors[j];
            neighbors[j] = tmp;
        }

        return neighbors;
    }

    private void RemoveWallBetween(int r1, int c1, int r2, int c2)
    {
        if (r1 == r2)
        {
            // Same row — vertical wall
            int col = System.Math.Max(c1, c2);
            VerticalWalls[r1, col] = false;
        }
        else
        {
            // Same col — horizontal wall
            int row = System.Math.Max(r1, r2);
            HorizontalWalls[row, c1] = false;
        }
    }
}

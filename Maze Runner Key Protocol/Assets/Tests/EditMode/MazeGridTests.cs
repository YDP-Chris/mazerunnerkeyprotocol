using NUnit.Framework;
using System.Linq;

[TestFixture]
public class MazeGridTests
{
    [Test]
    public void FiveByFive_AllCellsVisited()
    {
        var grid = new MazeGrid(5, 5);
        grid.Generate(42);

        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                Assert.IsTrue(grid.Visited[r, c], $"Cell ({r},{c}) not visited");
    }

    [Test]
    public void TwentyByTwenty_AllCellsVisited()
    {
        var grid = new MazeGrid(20, 20);
        grid.Generate(42);

        for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
                Assert.IsTrue(grid.Visited[r, c], $"Cell ({r},{c}) not visited");
    }

    [Test]
    public void TenByFifteen_NonSquare_AllCellsVisited()
    {
        var grid = new MazeGrid(10, 15);
        grid.Generate(42);

        for (int r = 0; r < 15; r++)
            for (int c = 0; c < 10; c++)
                Assert.IsTrue(grid.Visited[r, c], $"Cell ({r},{c}) not visited");
    }

    [Test]
    public void FloodFill_FromOrigin_ReachesAllCells()
    {
        var grid = new MazeGrid(10, 10);
        grid.Generate(42);

        int reached = grid.FloodFill(0, 0);
        Assert.AreEqual(100, reached, "Not all cells reachable from (0,0)");
    }

    [Test]
    public void Connectivity_HoldsForMultipleSeeds()
    {
        int[] seeds = { 1, 42, 999, 12345, 99999 };
        foreach (int seed in seeds)
        {
            var grid = new MazeGrid(10, 10);
            grid.Generate(seed);

            int reached = grid.FloodFill(0, 0);
            Assert.AreEqual(100, reached, $"Connectivity failed for seed {seed}");
        }
    }

    [Test]
    public void DeadEndCount_GreaterThanZero()
    {
        var grid = new MazeGrid(10, 10);
        grid.Generate(42);

        var deadEnds = grid.GetDeadEnds();
        Assert.Greater(deadEnds.Count, 0);
    }

    [Test]
    public void EachDeadEnd_HasExactlyOneOpening()
    {
        var grid = new MazeGrid(10, 10);
        grid.Generate(42);

        var deadEnds = grid.GetDeadEnds();
        foreach (var (r, c) in deadEnds)
        {
            int openings = grid.CountOpenings(r, c);
            Assert.AreEqual(1, openings, $"Dead-end ({r},{c}) has {openings} openings");
        }
    }

    [Test]
    public void SameSeed_ProducesIdenticalWalls()
    {
        var grid1 = new MazeGrid(10, 10);
        grid1.Generate(12345);

        var grid2 = new MazeGrid(10, 10);
        grid2.Generate(12345);

        for (int r = 0; r <= 10; r++)
            for (int c = 0; c < 10; c++)
                Assert.AreEqual(grid1.HorizontalWalls[r, c], grid2.HorizontalWalls[r, c],
                    $"HWall mismatch at ({r},{c})");

        for (int r = 0; r < 10; r++)
            for (int c = 0; c <= 10; c++)
                Assert.AreEqual(grid1.VerticalWalls[r, c], grid2.VerticalWalls[r, c],
                    $"VWall mismatch at ({r},{c})");
    }

    [Test]
    public void DifferentSeeds_ProduceDifferentWalls()
    {
        var grid1 = new MazeGrid(10, 10);
        grid1.Generate(42);

        var grid2 = new MazeGrid(10, 10);
        grid2.Generate(99);

        bool anyDifference = false;
        for (int r = 0; r <= 10 && !anyDifference; r++)
            for (int c = 0; c < 10 && !anyDifference; c++)
                if (grid1.HorizontalWalls[r, c] != grid2.HorizontalWalls[r, c])
                    anyDifference = true;

        for (int r = 0; r < 10 && !anyDifference; r++)
            for (int c = 0; c <= 10 && !anyDifference; c++)
                if (grid1.VerticalWalls[r, c] != grid2.VerticalWalls[r, c])
                    anyDifference = true;

        Assert.IsTrue(anyDifference, "Seeds 42 and 99 produced identical mazes");
    }
}

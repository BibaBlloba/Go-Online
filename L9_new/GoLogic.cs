using System;
using System.Collections.Generic;
using System.Linq;

namespace L9_new
{
    public class GoLogic
    {
        private readonly int boardSize;
        private readonly int[,] board;

        public GoLogic(int size)
        {
            boardSize = size;
            board = new int[size, size];
        }

        public int[,] Board => (int[,])board.Clone();

        public bool TryPlaceStone(int x, int y, int color)
        {
            if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
                return false;

            if (board[x, y] != 0)
                return false;

            board[x, y] = color;

            var oppositeColor = color == (int)PlayerColor.Black ? (int)PlayerColor.White : (int)PlayerColor.Black;
            CaptureAdjacentGroups(x, y, oppositeColor);

            if (IsGroupCaptureable(x, y, color))
            {
                board[x, y] = 0;
                return false;
            }

            return true;
        }

        private void CaptureAdjacentGroups(int x, int y, int oppositeColor)
        {
            int[][] directions = new[] { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 } };

            foreach (var dir in directions)
            {
                int nx = x + dir[0];
                int ny = y + dir[1];

                if (nx >= 0 && nx < boardSize && ny >= 0 && ny < boardSize && board[nx, ny] == oppositeColor)
                {
                    if (IsGroupCaptureable(nx, ny, oppositeColor))
                    {
                        CaptureGroup(nx, ny, oppositeColor);
                    }
                }
            }
        }

        private bool IsGroupCaptureable(int x, int y, int color)
        {
            var visited = new bool[boardSize, boardSize];
            return !HasLiberty(x, y, color, visited);
        }

        private bool HasLiberty(int x, int y, int color, bool[,] visited)
        {
            if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
                return false;

            if (visited[x, y])
                return false;

            int cellValue = board[x, y];
            if (cellValue == 0)
                return true;

            if (cellValue != color)
                return false;

            visited[x, y] = true;

            int[][] directions = new[] { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 } };
            foreach (var dir in directions)
            {
                if (HasLiberty(x + dir[0], y + dir[1], color, visited))
                    return true;
            }

            return false;
        }

        private void CaptureGroup(int x, int y, int color)
        {
            var visited = new bool[boardSize, boardSize];
            RemoveGroup(x, y, color, visited);
        }

        private void RemoveGroup(int x, int y, int color, bool[,] visited)
        {
            if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
                return;

            if (visited[x, y] || board[x, y] != color)
                return;

            visited[x, y] = true;
            board[x, y] = 0;

            int[][] directions = new[] { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 } };
            foreach (var dir in directions)
            {
                RemoveGroup(x + dir[0], y + dir[1], color, visited);
            }
        }

        public int[] CalculateScore(float komi = 6.5f)
        {
            var blackTerritory = 0;
            var whiteTerritory = 0;
            var blackStones = 0;
            var whiteStones = 0;
            var visited = new bool[boardSize, boardSize];

            for (int x = 0; x < boardSize; x++)
            {
                for (int y = 0; y < boardSize; y++)
                {
                    if (!visited[x, y])
                    {
                        if (board[x, y] == (int)PlayerColor.Black)
                        {
                            blackStones++;
                            visited[x, y] = true;
                        }
                        else if (board[x, y] == (int)PlayerColor.White)
                        {
                            whiteStones++;
                            visited[x, y] = true;
                        }
                        else
                        {
                            var (territory, owner) = FloodFillTerritory(x, y, visited);
                            if (owner == (int)PlayerColor.Black)
                                blackTerritory += territory;
                            else if (owner == (int)PlayerColor.White)
                                whiteTerritory += territory;
                        }
                    }
                }
            }

            var blackScore = blackStones + blackTerritory;
            var whiteScore = whiteStones + whiteTerritory + (int)komi;

            return new[] { blackScore, whiteScore };
        }

        private (int territory, int owner) FloodFillTerritory(int startX, int startY, bool[,] visited)
        {
            var queue = new Queue<(int, int)>();
            var emptyPoints = new List<(int, int)>();
            var borders = new HashSet<int>();

            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            int[][] directions = new[] { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 } };

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                emptyPoints.Add((x, y));

                foreach (var dir in directions)
                {
                    int nx = x + dir[0];
                    int ny = y + dir[1];

                    if (nx >= 0 && nx < boardSize && ny >= 0 && ny < boardSize)
                    {
                        if (board[nx, ny] == 0 && !visited[nx, ny])
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                        else if (board[nx, ny] != 0)
                        {
                            borders.Add(board[nx, ny]);
                        }
                    }
                }
            }

            int owner = 0;
            if (borders.Count == 1)
            {
                owner = borders.First();
            }

            return (emptyPoints.Count, owner);
        }

        public void CopyBoardFrom(int[,] source)
        {
            for (int x = 0; x < boardSize && x < source.GetLength(0); x++)
            {
                for (int y = 0; y < boardSize && y < source.GetLength(1); y++)
                {
                    board[x, y] = source[x, y];
                }
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sudoku.Interfaces;

namespace Sudoku.Solvers
{
    public class BruteForce : ISolver
    {
        protected Node[][] data;
        protected int size;
        protected int square;

        protected Stopwatch timerInit;
        protected Stopwatch timerSolve;

        protected Queue<IPlaybackStep> playbackData;

        public bool Ready { get; set; } = false;

        public BruteForce()
        {
            timerInit = new Stopwatch();
            timerSolve = new Stopwatch();
        }

        public async Task Init(INode[][] rawGrid)
        {
            if (!Helper.IsValidSudoku(rawGrid))
            {
                throw new ArgumentException("Provided data is not a valid sudoku grid.");
            }
            timerInit.Start();

            size = rawGrid.Length;
            square = (int)Math.Sqrt(size);
            data = new Node[size][];
            for (var  i = 0; i < size; i++)
            {
                data[i] = new Node[size];
                for (var ii = 0; ii < size; ii++)
                {
                    data[i][ii] = new Node() {
                        Value = rawGrid[i][ii].Value,
                        Starting = rawGrid[i][ii].Starting,
                        X = rawGrid[i][ii].X,
                        Y = rawGrid[i][ii].Y,
                        Z = rawGrid[i][ii].Z
                    };
                }
            }
            playbackData = new Queue<IPlaybackStep>();

            timerInit.Stop(); 
            Ready = true;
        }

        public async Task<ISolution> Solve(bool enablePlayback)
        {
            if (!Ready)
            {
                throw new InvalidOperationException("Solve() cannot be called befgore Init().");
            }
            timerSolve.Start();

            var solvedGrid = await RecursiveSolve(0, 0, enablePlayback);

            timerSolve.Stop();
            return new Solution()
            {
                Solved = solvedGrid,
                Grid = data,
                Playback = playbackData,
                TimeToInit = timerInit.ElapsedMilliseconds,
                TimeToSolve = timerSolve.ElapsedMilliseconds,
                TimeTotal = timerInit.ElapsedMilliseconds + timerSolve.ElapsedMilliseconds
            };
        }

        protected async Task<bool> RecursiveSolve(int x, int y, bool playback)
        {
            if (y == size)
            {
                return true;
            }
            var cell = data[x][y];
            var nextX = (x + 1) % size;
            var nextY = (x == size - 1) ? y + 1 : y;

            var row = GetRowData(y);
            var col = GetColData(x);
            var box = GetBoxData(x, y);

            if (cell.Value != 0 || cell.Starting)
            {
                if (playback)
                {
                    playbackData.Enqueue(new PlaybackStep()
                    {
                        ActionType = IPlaybackStep.PlaybackAction.Add,
                        X = x,
                        Y = y,
                        Value = cell.Value
                    });
                }
                return await RecursiveSolve(nextX, nextY, playback);
            }

            for (var val = 1; val <= size; val++)
            {
                if (playback)
                {
                    playbackData.Enqueue(new PlaybackStep()
                    {
                        ActionType = IPlaybackStep.PlaybackAction.Try,
                        X = x,
                        Y = y,
                        Value = val
                    });
                }
                if (!(row.Contains(val) || col.Contains(val) || box.Contains(val)))
                {
                    if (playback)
                    {
                        playbackData.Enqueue(new PlaybackStep()
                        {
                            ActionType = IPlaybackStep.PlaybackAction.Add,
                            X = x,
                            Y = y,
                            Value = val
                        });
                    }
                    cell.Value = val;
                    if (await RecursiveSolve(nextX, nextY, playback))
                    {
                        if (playback)
                        {
                            playbackData.Enqueue(new PlaybackStep()
                            {
                                ActionType = IPlaybackStep.PlaybackAction.Remove,
                                X = x,
                                Y = y,
                                Value = 0
                            });
                        }
                        return true;
                    }
                }
                cell.Value = 0;
            }
            if (playback)
            {
                playbackData.Enqueue(new PlaybackStep()
                {
                    ActionType = IPlaybackStep.PlaybackAction.Remove,
                    X = x,
                    Y = y,
                    Value = 0
                });
            }
            return false;
        }

        protected List<int> GetBoxData(int x, int y)
        {
            var z = data[x][y].Z;
            var startX = (z * square) % size;
            var startY = (int)Math.Floor(z / (decimal)square) * square;
            var boxData = new List<int>();
            for (var ii = startY; ii < startY + square; ii++)
            {
                var str = string.Empty;
                for (var i = startX; i < startX + square; i++)
                {
                    boxData.Add(data[i][ii].Value);
                    str += data[i][ii].Value + " ";
                }
            }
            return boxData;
        }

        protected List<int> GetRowData(int y)
        {
            var rowData = new List<int>();
            for (var i = 0; i < size; i++)
            {
                rowData.Add(data[i][y].Value);
            }
            return rowData;
        }

        protected List<int> GetColData(int x)
        {
            var colData = new List<int>();
            for (var i = 0; i < size; i++)
            {
                colData.Add(data[x][i].Value);
            }
            return colData;
        }
    }
}

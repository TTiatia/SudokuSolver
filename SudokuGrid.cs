﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku
{
    public class SudokuGrid
    {
        // Meta data
        public int Size { get; }
        public int SquareSize { get; }
        public int[][] Start { get; }

        // Node lists
        public SudokuNode[][] Nodes { get; private set; }
        private List<SudokuNode>[] rows;
        private List<SudokuNode>[] cols;
        private List<SudokuNode>[] sqrs;
        public int[][] SqrLookup { get; private set; }

        // Optimisation arrays
        private List<int>[] rowTaboo;
        private List<int>[] colTaboo;
        private List<int>[] sqrTaboo;
        private int[] colRisks;
        private int[] rowRisks;
        private int[] sqrRisks;

        public SudokuGrid(int[][] startingValues)
        {
            // Store starting statre
            Start = startingValues;

            // Determine grid size
            Size = startingValues.Length;
            SquareSize = (int)Math.Sqrt(Size);

            // Init Lists
            Nodes = new SudokuNode[Size][];
            cols = new List<SudokuNode>[Size];
            rows = new List<SudokuNode>[Size];
            sqrs = new List<SudokuNode>[Size];
            SqrLookup = new int[Size][];

            // Init optimisation arrays
            colTaboo = new List<int>[Size];
            rowTaboo = new List<int>[Size];
            sqrTaboo = new List<int>[Size];
            colRisks = new int[Size];
            rowRisks = new int[Size];
            sqrRisks = new int[Size];

            // Create grid structure
            for (var i = 0; i < Size; i++)
            {
                Nodes[i] = new SudokuNode[Size];
                cols[i] = new List<SudokuNode>();
                SqrLookup[i] = new int[Size];

                for (var ii = 0; ii < Size; ii++)
                {
                    // Create Square lookup
                    var sId = (int)(Math.Floor(i / (decimal)SquareSize) * SquareSize + Math.Floor(ii / (decimal)SquareSize));
                    SqrLookup[i][ii] = sId;

                    // Create new node
                    var node = new SudokuNode(Size, Start[i][ii], i, ii, sId);
                    Nodes[i][ii] = node;

                    // Add node to column (x)
                    cols[i].Add(node);

                    // Add node to row (y)
                    rows[ii] ??= new List<SudokuNode>();
                    rows[ii].Add(node);

                    // Add node to square (z)
                    sqrs[sId] ??= new List<SudokuNode>();
                    sqrs[sId].Add(node);
                }
            }
        }

        public int GetSquare(int col, int row) => SqrLookup[col][row];

        /// <summary>
        /// Checks if the specified value is valid to place in the cell indicated
        /// </summary>
        public async Task<bool> CheckValidAsync(int col, int row, int sqr, int value) => !(colTaboo[col].Contains(value) || rowTaboo[row].Contains(value) || sqrTaboo[sqr].Contains(value));

        /// <summary>Asynchronously updates all Risk values.</summary>
        protected async Task UpdateRiskAsync()
        {
            // Update lists in parallel
            var updates = new Task[]
            {
                UpdateRowRiskAsync(),
                UpdateColRiskAsync(),
                UpdateSquareRiskAsync(),
                UpdateColTabooAsync(),
                UpdateRowTabooAsync(),
                UpdateSquareTabooAsync(),
            };
            Task.WaitAll(updates);

            await UpdateNodesRiskAsync();
        }

        /// <summary>Asynchronously updates all Column Risk values.</summary>
        protected async Task UpdateColRiskAsync()
        {
            for (var i = 0; i < Size; i++)
            {
                colRisks[i] = Fibonacci.Rank(cols[i].Count(a => a.Value != 0));
            }
        }

        /// <summary>Asynchronously updates all Row Risk values.</summary>
        protected async Task UpdateRowRiskAsync()
        {
            for (var i = 0; i < Size; i++)
            {
                rowRisks[i] = Fibonacci.Rank(rows[i].Count(a => a.Value != 0));
            }
        }

        /// <summary>Asynchronously updates all Square Risk values.</summary>
        protected async Task UpdateSquareRiskAsync()
        {
            for (var i = 0; i < Size; i++)
            {
                sqrRisks[i] = Fibonacci.Rank(sqrs[i].Count(a => a.Value != 0));
            }
        }

        /// <summary>Asynchronously updates all Column Risk values.</summary>
        protected async Task UpdateNodesRiskAsync()
        {
            var updates = new List<Task>();
            for (var i = 0; i < Size; i++)
            {
                for (var ii = 0; ii < Size; ii++)
                {
                    if (Nodes[i][ii].Value == 0)
                    {
                        updates.Add(Nodes[i][ii].UpdateRiskAsync(rowRisks[i], colRisks[ii], sqrRisks[SqrLookup[i][ii]]));
                    }
                }
            }
            Task.WaitAll(updates.ToArray());
        }

        protected async Task UpdateColTabooAsync()
        {
            for (var i = 0; i < Size; i++)
            {
                colTaboo[i] = cols[i].Where(a => a.Value > 0).Select(a => a.Value).ToList();
            }
        }

        protected async Task UpdateRowTabooAsync()
        {
            for (var i = 0; i < Size; i++)
            {
                rowTaboo[i] = rows[i].Where(a => a.Value > 0).Select(a => a.Value).ToList();
            }
        }

        protected async Task UpdateSquareTabooAsync()
        {
            for (var i = 0; i < Size; i++)
            {
                sqrTaboo[i] = sqrs[i].Where(a => a.Value > 0).Select(a => a.Value).ToList();
            }
        }

        public int NodeCount() => Size * Size;
    }

}
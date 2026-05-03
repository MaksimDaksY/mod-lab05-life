using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LifeTests
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void Board_Constructor_CreatesCorrectDimensions()
        {
            int width = 80, height = 40, cellSize = 1;
            var board = new Board(width, height, cellSize, 0.1);
            Assert.AreEqual(width / cellSize, board.Columns);
            Assert.AreEqual(height / cellSize, board.Rows);
        }

        [TestMethod]
        public void Randomize_DoesNotThrow()
        {
            var board = new Board(10, 10, 1, 0.5);
            board.Randomize(0.5);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetLiveCellCount_AfterRandomize_IsWithinRange()
        {
            var board = new Board(100, 100, 1, 0.2);
            int live = board.GetLiveCellCount();
            Assert.IsTrue(live >= 0 && live <= 10000);
        }

        [TestMethod]
        public void DetermineNextLiveState_BirthRuleWorks()
        {
            var board = new Board(3, 3, 1, 0.0);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    board.Cells[x, y].IsAlive = false;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 1].DetermineNextLiveState();
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void DetermineNextLiveState_SurvivalRuleWorks()
        {
            var board = new Board(3, 3, 1, 0.0);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    board.Cells[x, y].IsAlive = false;
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            var oldState = board.Cells[1, 1].IsAlive;
            foreach (var cell in board.Cells) cell.DetermineNextLiveState();
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void DetermineNextLiveState_UnderpopulationDies()
        {
            var board = new Board(3, 3, 1, 0.0);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    board.Cells[x, y].IsAlive = false;
            board.Cells[1, 1].IsAlive = true;
            foreach (var cell in board.Cells) cell.DetermineNextLiveState();
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void DetermineNextLiveState_OverpopulationDies()
        {
            var board = new Board(3, 3, 1, 0.0);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    board.Cells[x, y].IsAlive = false;
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    if (!(x == 1 && y == 1))
                        board.Cells[x, y].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;
            foreach (var cell in board.Cells) cell.DetermineNextLiveState();
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void SaveLoadFile_RestoresState()
        {
            var board1 = new Board(10, 10, 1, 0.3);
            string tempFile = Path.GetTempFileName();
            board1.SaveToFile(tempFile);
            var board2 = new Board(10, 10, 1, 0.0);
            board2.LoadFromFile(tempFile);
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    Assert.AreEqual(board1.Cells[x, y].IsAlive, board2.Cells[x, y].IsAlive);
            File.Delete(tempFile);
        }

        [TestMethod]
        public void FindConnectedComponents_Block2x2_OneComponent()
        {
            var board = new Board(5, 5, 1, 0.0);
            board.Cells[0, 0].IsAlive = board.Cells[1, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = board.Cells[1, 1].IsAlive = true;
            var components = board.FindConnectedComponents();
            Assert.AreEqual(1, components.Count);
            Assert.AreEqual(4, components[0].Count);
        }

        [TestMethod]
        public void ClassifyFigure_Block_ReturnsBlock()
        {
            HashSet<Point> block = new HashSet<Point>
            {
                new Point(0,0), new Point(1,0),
                new Point(0,1), new Point(1,1)
            };
            string type = Board.ClassifyFigure(block);
            Assert.AreEqual("Блок", type);
        }

        [TestMethod]
        public void ClassifyFigure_Hive_ReturnsHive()
        {
            var hive = new HashSet<Point>
            {
                new Point(1,0), new Point(0,1), new Point(2,1),
                new Point(0,2), new Point(2,2), new Point(1,3)
            };
            string type = Board.ClassifyFigure(hive);
            Assert.AreEqual("Улей", type);
        }

        [TestMethod]
        public void ClassifyFigure_Boat_ReturnsBoat()
        {
            var boat = new HashSet<Point>
            {
                new Point(1,0), new Point(0,1), new Point(2,1), new Point(1,2)
            };
            string type = Board.ClassifyFigure(boat);
            Assert.AreEqual("Ящик", type);
        }

        [TestMethod]
        public void ClassifyFigure_Pond_ReturnsPond()
        {
            var pond = new HashSet<Point>
            {
                new Point(1,0), new Point(2,0), new Point(0,1), new Point(3,1),
                new Point(0,2), new Point(3,2), new Point(1,3), new Point(2,3)
            };
            string type = Board.ClassifyFigure(pond);
            Assert.AreEqual("Пруд", type);
        }

        [TestMethod]
        public void ClassifyFigure_Loaf_ReturnsLoaf()
        {
            var loaf = new HashSet<Point>
            {
                new Point(1,0), new Point(2,0), new Point(0,1), new Point(3,1),
                new Point(1,2), new Point(3,2), new Point(2,3)
            };
            string type = Board.ClassifyFigure(loaf);
            Assert.AreEqual("Каравай", type);
        }

        [TestMethod]
        public void Normalize_ShiftsToOrigin()
        {
            var points = new HashSet<Point> { new Point(5, 5), new Point(6, 5), new Point(5, 6) };
            var norm = Board.Normalize(points);
            Assert.IsTrue(norm.Contains(new Point(0, 0)));
            Assert.IsTrue(norm.Contains(new Point(1, 0)));
            Assert.IsTrue(norm.Contains(new Point(0, 1)));
        }

        [TestMethod]
        public void FindConnectedComponents_TwoSeparateBlocks_TwoComponents()
        {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[0, 0].IsAlive = board.Cells[1, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = board.Cells[1, 1].IsAlive = true;
            board.Cells[5, 5].IsAlive = board.Cells[6, 5].IsAlive = true;
            board.Cells[5, 6].IsAlive = board.Cells[6, 6].IsAlive = true;
            var comps = board.FindConnectedComponents();
            Assert.AreEqual(2, comps.Count);
            Assert.AreEqual(4, comps[0].Count);
            Assert.AreEqual(4, comps[1].Count);
        }

        [TestMethod]
        public void Advance_ZeroDensity_NoChange()
        {
            var board = new Board(10, 10, 1, 0.0);
            int liveBefore = board.GetLiveCellCount();
            board.Advance();
            int liveAfter = board.GetLiveCellCount();
            Assert.AreEqual(liveBefore, liveAfter);
        }

        [TestMethod]
        public void Advance_FullDensity_AllDie()
        {
            var board = new Board(5, 5, 1, 1.0);
            board.Advance();
            Assert.AreEqual(0, board.GetLiveCellCount());
        }

        [TestMethod]
        public void ConnectNeighbors_ToroidalWrapping_WorksForCorner()
        {
            var board = new Board(5, 5, 1, 0.0);
            var corner = board.Cells[0, 0];
            Assert.AreEqual(8, corner.neighbors.Count);
        }
    }
}
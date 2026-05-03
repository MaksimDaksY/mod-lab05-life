using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace cli_life
{
    public class SimulationConfig
    {
        public int Width { get; set; } = 40;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.2;
        public int SimulationDelayMs { get; set; } = 500;
    }

    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Count(x => x.IsAlive);
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;

        public Board(int width, int height, int cellSize, double liveDensity)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        private readonly Random rand = new Random();

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public void SaveToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                        writer.Write(Cells[x, y].IsAlive ? '*' : ' ');
                    writer.WriteLine();
                }
            }
            Console.WriteLine($"Состояние сохранено в файл: {filename}");
        }

        public void LoadFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"Файл {filename} не найден");
                return;
            }

            string[] lines = File.ReadAllLines(filename);
            if (lines.Length == 0) return;

            int loadedRows = lines.Length;
            int loadedCols = lines[0].Length;

            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y].IsAlive = false;

            for (int y = 0; y < Math.Min(loadedRows, Rows); y++)
            {
                string line = lines[y];
                for (int x = 0; x < Math.Min(loadedCols, Columns); x++)
                {
                    if (x < line.Length && line[x] == '*')
                        Cells[x, y].IsAlive = true;
                }
            }

            Console.WriteLine($"Состояние загружено из {filename}");
        }

        private static readonly Dictionary<string, HashSet<Point>> FigurePatterns = new Dictionary<string, HashSet<Point>>();

        static Board()
        {
            FigurePatterns["Блок"] = new HashSet<Point>
            {
                new Point(0,0), new Point(1,0),
                new Point(0,1), new Point(1,1)
            };
            FigurePatterns["Улей"] = new HashSet<Point>
            {
                new Point(1,0),
                new Point(0,1), new Point(2,1),
                new Point(0,2), new Point(2,2),
                new Point(1,3)
            };
            FigurePatterns["Ящик"] = new HashSet<Point>
            {
                new Point(1,0),
                new Point(0,1), new Point(2,1),
                new Point(1,2)
            };
            FigurePatterns["Пруд"] = new HashSet<Point>
            {
                new Point(1,0), new Point(2,0),
                new Point(0,1), new Point(3,1),
                new Point(0,2), new Point(3,2),
                new Point(1,3), new Point(2,3)
            };
            FigurePatterns["Каравай"] = new HashSet<Point>
            {
                new Point(1,0), new Point(2,0),
                new Point(0,1), new Point(3,1),
                new Point(1,2), new Point(3,2),
                new Point(2,3)
            };
        }

        public List<HashSet<Point>> FindConnectedComponents()
        {
            bool[,] visited = new bool[Columns, Rows];
            List<HashSet<Point>> components = new List<HashSet<Point>>();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        HashSet<Point> component = new HashSet<Point>();
                        Queue<Point> queue = new Queue<Point>();
                        queue.Enqueue(new Point(x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            Point p = queue.Dequeue();
                            component.Add(p);

                            for (int dx = -1; dx <= 1; dx++)
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = p.X + dx;
                                    int ny = p.Y + dy;
                                    if (nx >= 0 && nx < Columns && ny >= 0 && ny < Rows &&
                                        Cells[nx, ny].IsAlive && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue(new Point(nx, ny));
                                    }
                                }
                        }
                        components.Add(component);
                    }
                }
            }
            return components;
        }

        public static HashSet<Point> Normalize(HashSet<Point> figure)
        {
            int minX = figure.Min(p => p.X);
            int minY = figure.Min(p => p.Y);
            HashSet<Point> norm = new HashSet<Point>();
            foreach (var p in figure)
                norm.Add(new Point(p.X - minX, p.Y - minY));
            return norm;
        }

        private static bool AreEqual(HashSet<Point> a, HashSet<Point> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var p in a)
                if (!b.Contains(p)) return false;
            return true;
        }

        public static string ClassifyFigure(HashSet<Point> figure)
        {
            var norm = Normalize(figure);
            foreach (var pattern in FigurePatterns)
                if (AreEqual(norm, pattern.Value))
                    return pattern.Key;
            return "Неизвестная";
        }

        public void PrintStatistics()
        {
            var components = FindConnectedComponents();
            int totalLiveCells = components.Sum(c => c.Count);
            Console.WriteLine($"\nСтатистика");
            Console.WriteLine($"Всего живых клеток: {totalLiveCells}");
            Console.WriteLine($"Количество фигур: {components.Count}");
            var classification = new Dictionary<string, int>();
            foreach (var fig in components)
            {
                string type = ClassifyFigure(fig);
                classification[type] = classification.GetValueOrDefault(type) + 1;
            }
            Console.WriteLine("Типы фигур:");
            foreach (var kv in classification)
                Console.WriteLine($"  {kv.Key}: {kv.Value} шт");
        }
        public int GetLiveCellCount()
        {
            int count = 0;
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive) count++;
            return count;
        }
        public static HashSet<Point> NormalizeComponent(HashSet<Point> component) => Normalize(component);
        public static Dictionary<string, HashSet<Point>> GetPatterns() => FigurePatterns;
    }

    class Program
    {
        static SimulationConfig config;
        static Board board;
        static int generation = 0;

        static void LoadConfig(string configPath = "settings.json")
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<SimulationConfig>(json);
                Console.WriteLine("Настройки загружены из settings.json");
            }
            else
            {
                config = new SimulationConfig();
                string defaultJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, defaultJson);
                Console.WriteLine("Создан файл настроек по умолчанию: settings.json");
            }
        }

        static void Reset()
        {
            board = new Board(config.Width, config.Height, config.CellSize, config.LiveDensity);
        }

        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                    Console.Write(board.Cells[col, row].IsAlive ? '*' : ' ');
                Console.WriteLine();
            }
            Console.WriteLine($"Поколение: {generation}");
            Console.WriteLine("Управление: [S] сохранить | [L] загрузить | [P] статистика | [F] фигуры | [A] анализ | [G] график | [Q] выход");
        }

        static void ShowFiguresMenu()
        {
            string figuresDir = "figures";
            if (!Directory.Exists(figuresDir))
            {
                Console.WriteLine($"Папка {figuresDir} не найдена, создайте её и поместите туда файлы фигур (*.txt)");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey(true);
                return;
            }

            var files = Directory.GetFiles(figuresDir, "*.txt").OrderBy(f => f).ToList();
            if (files.Count == 0)
            {
                Console.WriteLine($"В папке {figuresDir} нет файлов фигур (*.txt)");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey(true);
                return;
            }

            Console.Clear();
            Console.WriteLine("Доступные фигуры:\n");
            for (int i = 0; i < files.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileNameWithoutExtension(files[i])}");
            }
            Console.WriteLine("\n0. Отмена");
            Console.Write("Ваш выбор: ");

            string input = Console.ReadLine();
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= files.Count)
            {
                string fullPath = Path.GetFullPath(files[choice - 1]);
                board.LoadFromFile(fullPath);
                Console.WriteLine($"Фигура {Path.GetFileNameWithoutExtension(files[choice - 1])} загружена");
            }
            else
            {
                Console.WriteLine("Загрузка отменена");
            }
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey(true);
        }

        static int RunStabilityExperiment(double density, int maxGenerations = 10000, int stabilityWindow = 50)
        {
            var testBoard = new Board(config.Width, config.Height, 1, density);
            Queue<int> liveHistory = new Queue<int>();
            int gen = 0;

            while (gen < maxGenerations)
            {
                int liveCount = 0;
                for (int x = 0; x < testBoard.Columns; x++)
                    for (int y = 0; y < testBoard.Rows; y++)
                        if (testBoard.Cells[x, y].IsAlive)
                            liveCount++;

                liveHistory.Enqueue(liveCount);
                if (liveHistory.Count > stabilityWindow + 1)
                    liveHistory.Dequeue();

                if (liveHistory.Count == stabilityWindow + 1)
                {
                    bool stable = true;
                    int first = liveHistory.Peek();
                    foreach (int val in liveHistory)
                        if (val != first) { stable = false; break; }
                    if (stable)
                        return gen - stabilityWindow;
                }

                testBoard.Advance();
                gen++;
            }
            return -1;
        }

        static void AnalyzeStability()
        {
            List<double> densities = new List<double>();
            for (double d = 0.05; d <= 0.95; d += 0.05)
                densities.Add(d);

            int experimentsPerDensity = 20;
            string outputFile = "stability_data.txt";

            Console.WriteLine("Анализ стабилизации запущен...");
            using (StreamWriter writer = new StreamWriter(outputFile))
            {
                writer.WriteLine("density;average_generations");
                foreach (double density in densities)
                {
                    Console.Write($"Плотность {density:F2}... ");
                    List<int> results = new List<int>();
                    for (int i = 0; i < experimentsPerDensity; i++)
                    {
                        int gens = RunStabilityExperiment(density);
                        if (gens > 0)
                            results.Add(gens);
                    }
                    double avg = results.Count > 0 ? results.Average() : -1;
                    writer.WriteLine($"{density.ToString(CultureInfo.InvariantCulture)};{avg.ToString(CultureInfo.InvariantCulture)}");
                    Console.WriteLine($" среднее = {avg:F2}");
                }
            }
            Console.WriteLine($"\nРезультаты сохранены в {outputFile}");
        }

        static void PlotGraphToPng()
        {
            string dataFile = "stability_data.txt";
            if (!File.Exists(dataFile))
            {
                Console.WriteLine($"Файл {dataFile} не найден, сначала выполните анализ (клавиша A)");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey(true);
                return;
            }

            var densities = new List<double>();
            var averages = new List<double>();
            foreach (var line in File.ReadLines(dataFile).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(';');
                if (parts.Length != 2) continue;
                if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double d) &&
                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                {
                    densities.Add(d);
                    averages.Add(v);
                }
            }

            if (densities.Count == 0)
            {
                Console.WriteLine("Нет данных для построения графика");
                Console.ReadKey(true);
                return;
            }

            int width = 1024;
            int height = 768;
            using (var bmp = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                int marginLeft = 80;
                int marginRight = 60;
                int marginTop = 70;
                int marginBottom = 80;
                int plotWidth = width - marginLeft - marginRight;
                int plotHeight = height - marginTop - marginBottom;

                using (var penAxis = new Pen(Color.Black, 2))
                using (var penGrid = new Pen(Color.LightGray, 1))
                using (var penData = new Pen(Color.FromArgb(70, 130, 180), 2.5f))
                using (var fontTitle = new Font("Arial", 16, FontStyle.Bold))
                using (var fontLabel = new Font("Arial", 14))
                using (var fontTick = new Font("Arial", 12))
                using (var brushBlack = new SolidBrush(Color.Black))
                {
                    g.DrawLine(penAxis, marginLeft, marginTop, marginLeft, height - marginBottom);
                    g.DrawLine(penAxis, marginLeft, height - marginBottom, width - marginRight, height - marginBottom);

                    double maxY = averages.Max();
                    double minY = 0;
                    double maxYrounded = Math.Ceiling(maxY / 50) * 50;
                    if (maxYrounded <= 0) maxYrounded = 50;
                    double minYrounded = 0;

                    double xStep = 0.05;
                    int xTicks = (int)(1.0 / xStep);

                    for (int i = 0; i <= xTicks; i++)
                    {
                        double xVal = i * xStep;
                        int xPos = marginLeft + (int)(xVal * plotWidth);
                        g.DrawLine(penGrid, xPos, marginTop, xPos, height - marginBottom);
                        string label = xVal.ToString("F2", CultureInfo.InvariantCulture);
                        var size = g.MeasureString(label, fontTick);
                        g.DrawString(label, fontTick, brushBlack, xPos - size.Width / 2, height - marginBottom + 6);
                    }

                    double yStep = 50;
                    int yMaxTick = (int)(maxYrounded / yStep);
                    for (int i = 0; i <= yMaxTick; i++)
                    {
                        double yVal = i * yStep;
                        int yPos = height - marginBottom - (int)((yVal - minY) * plotHeight / (maxYrounded - minY));
                        if (yPos < marginTop || yPos > height - marginBottom) continue;
                        g.DrawLine(penGrid, marginLeft, yPos, width - marginRight, yPos);
                        string label = yVal.ToString("F0", CultureInfo.InvariantCulture);
                        var size = g.MeasureString(label, fontTick);
                        g.DrawString(label, fontTick, brushBlack, marginLeft - size.Width - 8, yPos - size.Height / 2);
                    }

                    string title = "Зависимость времени стабилизации от плотности";
                    var titleSize = g.MeasureString(title, fontTitle);
                    g.DrawString(title, fontTitle, brushBlack, (width - titleSize.Width) / 2, 20);

                    g.DrawString("Плотность", fontLabel, brushBlack, width / 2 - 30, height - 30);

                    var yLabelSize = g.MeasureString("Среднее поколение стабилизации", fontLabel);
                    GraphicsState state = g.Save();
                    g.TranslateTransform(25, height / 2);
                    g.RotateTransform(-90);
                    g.DrawString("Среднее поколение стабилизации", fontLabel, brushBlack,
                        -yLabelSize.Width / 2, -yLabelSize.Height / 2);
                    g.Restore(state);

                    PointF[] points = new PointF[densities.Count];
                    for (int i = 0; i < densities.Count; i++)
                    {
                        int x = marginLeft + (int)(densities[i] * plotWidth);
                        int y = height - marginBottom - (int)((averages[i] - minY) * plotHeight / (maxYrounded - minY));
                        x = Math.Clamp(x, marginLeft, width - marginRight);
                        y = Math.Clamp(y, marginTop, height - marginBottom);
                        points[i] = new PointF(x, y);
                    }
                    g.DrawLines(penData, points);
                    foreach (var p in points)
                    {
                        g.FillEllipse(Brushes.Red, p.X - 4, p.Y - 4, 8, 8);
                        g.DrawEllipse(Pens.DarkRed, p.X - 4, p.Y - 4, 8, 8);
                    }

                    string outputFile = "stability_graph.png";
                    bmp.Save(outputFile, ImageFormat.Png);
                    Console.WriteLine($"График сохранён в {outputFile}");
                }
            }
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey(true);
        }

        static void Main(string[] args)
        {
            LoadConfig();
            Reset();

            string defaultSaveFile = "saved_state.txt";

            while (true)
            {
                Console.Clear();
                Render();
                board.Advance();
                generation++;
                Thread.Sleep(config.SimulationDelayMs);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.S:
                            board.SaveToFile(defaultSaveFile);
                            Console.WriteLine("Сохранено. Нажмите любую клавишу...");
                            Console.ReadKey(true);
                            break;
                        case ConsoleKey.L:
                            board.LoadFromFile(defaultSaveFile);
                            generation = 0;
                            Console.WriteLine("Загружено. Нажмите любую клавишу...");
                            Console.ReadKey(true);
                            break;
                        case ConsoleKey.P:
                            board.PrintStatistics();
                            Console.WriteLine("Нажмите любую клавишу...");
                            Console.ReadKey(true);
                            break;
                        case ConsoleKey.F:
                            ShowFiguresMenu();
                            generation = 0;
                            break;
                        case ConsoleKey.A:
                            Console.Clear();
                            Console.WriteLine("Запуск анализа стабилизации...");
                            AnalyzeStability();
                            Console.WriteLine("Анализ завершён. Нажмите любую клавишу для продолжения...");
                            Console.ReadKey(true);
                            generation = 0;
                            break;
                        case ConsoleKey.G:
                            PlotGraphToPng();
                            break;
                        case ConsoleKey.Q:
                            return;
                    }
                }
            }
        }
    }
}
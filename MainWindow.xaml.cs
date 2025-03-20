using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace td1
{
    public enum SearchAlgorithm
    {
        DFS,
        BFS,
        AStar
    }

    public partial class MainWindow : Window
    {
        private const int CellSize = 40;
        private Dictionary<string, List<string>> graph;
        private Dictionary<string, (int, int)> nodePositions;
        private Dictionary<string, Rectangle> nodeCells;
        private Dictionary<(int, int), Rectangle> allCells;
        private Dictionary<string, int> heuristics;

        private int[,] mazeLayout =
        {
            { 1, 0, 1, 0, 1, 0, 0, 0, 1, 1, 0, 1 },
            { 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 0, 1 },
            { 1, 1, 1, 0, 1, 0, 1, 1, 1, 0, 0, 1 },
            { 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1 },
            { 0, 1, 1, 1, 1, 0, 1, 0, 1, 1, 1, 1 },
            { 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0 },
            { 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1 }
        };

        private (int, int, string)[] labels =
        {
            (0, 0, "8"), (0, 2, "9"), (0, 4, "11"), (0, 8, "18"), (0, 9, "19"), (0, 11, "B"),
            (1, 4, "10"), (1, 5, "12"), (1, 6, "13"),
            (2, 0, "6"), (2, 2, "5"), (2, 1, "7"), (2, 6, "14"), (2, 8, "17"),
            (4, 1, "3"), (4, 3, "2"), (4, 4, "4"), (4, 8, "20"), (4, 11, "21"),
            (6, 0, "A"), (6, 3, "1"), (6, 6, "15"), (6, 11, "16")
        };

        private Canvas mazeCanvas;
        private StackPanel controlPanel;
        private Button startDfsButton;
        private Button startBfsButton;
        private Button startAStarButton;
        private ComboBox speedComboBox;
        private TextBlock statusText;
        private TextBlock pathText;
        private int animationDelay = 500;
        private bool animationInProgress = false;
        private List<string> searchPath = new List<string>();
        private Dictionary<string, string> parentMap = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            Title = "Maze Traversal Algorithms";
            Width = 800;
            Height = 600;
            SetupUI();
            InitializeGraph();
            InitializeHeuristics();
            DrawMaze();
        }

        private void InitializeHeuristics()
        {
            // A* heuristics - distance to goal 'B'
            heuristics = new Dictionary<string, int>
            {
                { "A", 8 }, { "1", 6 }, { "2", 6 }, { "3", 6 }, { "4", 7 }, { "5", 4 }, { "6", 12 },
                { "7", 7 }, { "8", 15 }, { "9", 18 }, { "10", 6 }, { "11", 8 }, { "12", 6 }, { "13", 5 },
                { "14", 4 }, { "15", 8 }, { "16", 6 }, { "17", 3 }, { "18", 5 }, { "19", 5 },
                { "20", 2 }, { "21", 1 }, { "B", 0 }
            };
        }

        private void SetupUI()
        {
            // Create a main grid to hold both the maze and controls
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });

            // Create the maze canvas with a border
            Border mazeBorder = new Border
            {
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create a viewbox to automatically scale and center the maze
            Viewbox mazeViewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                Margin = new Thickness(10)
            };

            mazeCanvas = new Canvas();
            mazeViewbox.Child = mazeCanvas;
            mazeBorder.Child = mazeViewbox;
            Grid.SetRow(mazeBorder, 0);
            mainGrid.Children.Add(mazeBorder);

            // Create the control panel
            controlPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Margin = new Thickness(10)
            };
            Grid.SetRow(controlPanel, 1);
            mainGrid.Children.Add(controlPanel);

            // Create button panel
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };
            controlPanel.Children.Add(buttonPanel);

            // Add DFS button
            startDfsButton = CreateButton("Start DFS", SearchAlgorithm.DFS);
            buttonPanel.Children.Add(startDfsButton);

            // Add BFS button
            startBfsButton = CreateButton("Start BFS", SearchAlgorithm.BFS);
            buttonPanel.Children.Add(startBfsButton);

            // Add A* button
            startAStarButton = CreateButton("Start A*", SearchAlgorithm.AStar);
            buttonPanel.Children.Add(startAStarButton);

            // Add speed selection
            StackPanel speedPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 10)
            };

            Label speedLabel = new Label
            {
                Content = "Animation Speed:",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };
            speedPanel.Children.Add(speedLabel);

            speedComboBox = new ComboBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            speedComboBox.Items.Add(new ComboBoxItem { Content = "Slow", Tag = 800 });
            speedComboBox.Items.Add(new ComboBoxItem { Content = "Medium", Tag = 500 });
            speedComboBox.Items.Add(new ComboBoxItem { Content = "Fast", Tag = 200 });
            speedComboBox.Items.Add(new ComboBoxItem { Content = "Very Fast", Tag = 100 });
            speedComboBox.SelectedIndex = 2; // Default to medium
            speedComboBox.SelectionChanged += SpeedComboBox_SelectionChanged;
            speedPanel.Children.Add(speedComboBox);
            


          

            // Add status text
            statusText = new TextBlock
            {
                Text = "Ready to start traversal",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            controlPanel.Children.Add(statusText);

            // Add path text
            pathText = new TextBlock
            {
                Text = "Path: ",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            controlPanel.Children.Add(pathText);

            this.Content = mainGrid;
        }

        private Button CreateButton(string content, SearchAlgorithm algorithm)
        {
            Button button = new Button
            {
                Content = content,
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(10),
                MinWidth = 100,
                Background = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.SemiBold
            };
            button.Click += (s, e) => StartSearch(algorithm);
            return button;
        }

        private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (speedComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                animationDelay = (int)selectedItem.Tag;
            }
        }

        private void StartSearch(SearchAlgorithm algorithm)
        {
            if (!animationInProgress)
            {
                ResetMaze();
                switch (algorithm)
                {
                    case SearchAlgorithm.DFS:
                        RunDFS("A");
                        break;
                    case SearchAlgorithm.BFS:
                        RunBFS("A");
                        break;
                    case SearchAlgorithm.AStar:
                        RunAStar("A", "B");
                        break;
                }
            }
        }

        private void ResetMaze()
        {
            // Reset all cells to default colors
            foreach (var cell in allCells.Values)
            {
                cell.Fill = Brushes.White;
            }

            // Reset path cells
            foreach (var pos in nodePositions.Values)
            {
                if (allCells.ContainsKey(pos))
                {
                    allCells[pos].Fill = Brushes.White;
                }
            }

            // Reset node cells
            foreach (var node in nodeCells.Keys)
            {
                nodeCells[node].Fill = node == "A" ? Brushes.LimeGreen : (node == "B" ? Brushes.Red : Brushes.White);
            }

            searchPath.Clear();
            parentMap.Clear();
            pathText.Text = "Path: ";
            statusText.Text = "Starting traversal...";
        }

        private void InitializeGraph()
        {
            graph = new Dictionary<string, List<string>>
            {
                { "A", new List<string> { "1" } },
                { "1", new List<string> { "2" } },
                { "2", new List<string> { "3", "4" } },
                { "3", new List<string> { "5" } },
                { "4", new List<string> { "10" } },
                { "5", new List<string> { "6", "7" } },
                { "6", new List<string> { "8" } },
                { "7", new List<string> { "9" } },
                { "8", new List<string>() },
                { "9", new List<string>() },
                { "10", new List<string> { "11", "12" } },
                { "11", new List<string>() },
                { "12", new List<string> { "13" } },
                { "13", new List<string> { "14" } },
                { "14", new List<string> { "15", "17" } },
                { "15", new List<string> { "16" } },
                { "16", new List<string>() },
                { "17", new List<string> { "18", "20" } },
                { "18", new List<string> { "19" } },
                { "19", new List<string>() },
                { "20", new List<string> { "21" } },
                { "21", new List<string> { "B" } },
                { "B", new List<string>() }
            };
        }

        private void DrawMaze()
        {
            nodePositions = new Dictionary<string, (int, int)>();
            nodeCells = new Dictionary<string, Rectangle>();
            allCells = new Dictionary<(int, int), Rectangle>();

            // Calculate maze dimensions
            int mazeWidth = mazeLayout.GetLength(1) * CellSize;
            int mazeHeight = mazeLayout.GetLength(0) * CellSize;
            mazeCanvas.Width = mazeWidth;
            mazeCanvas.Height = mazeHeight;

            // Draw the maze background
            for (int row = 0; row < mazeLayout.GetLength(0); row++)
            {
                for (int col = 0; col < mazeLayout.GetLength(1); col++)
                {
                    Rectangle cell = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Fill = mazeLayout[row, col] == 1 ? Brushes.White : Brushes.DarkSlateGray,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(cell, col * CellSize);
                    Canvas.SetTop(cell, row * CellSize);
                    mazeCanvas.Children.Add(cell);

                    if (mazeLayout[row, col] == 1)
                    {
                        allCells[(row, col)] = cell;
                    }
                }
            }

            // Add node labels and store their positions
            foreach (var (row, col, text) in labels)
            {
                // Create colored cell for nodes with rounded corners
                Rectangle nodeCell = new Rectangle
                {
                    Width = CellSize,
                    Height = CellSize,
                    Fill = text == "A" ? Brushes.LimeGreen : (text == "B" ? Brushes.Red : Brushes.White),
                    Stroke = text == "A" || text == "B" ? Brushes.Black : Brushes.Gray,
                    StrokeThickness = text == "A" || text == "B" ? 2 : 1,
                    RadiusX = 5,
                    RadiusY = 5
                };
                Canvas.SetLeft(nodeCell, col * CellSize);
                Canvas.SetTop(nodeCell, row * CellSize);
                mazeCanvas.Children.Add(nodeCell);
                nodeCells[text] = nodeCell;

                // Add node label text
                TextBlock label = new TextBlock
                {
                    Text = text,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(label, col * CellSize + (CellSize - label.FontSize) / 2);
                Canvas.SetTop(label, row * CellSize + (CellSize - label.FontSize) / 2);
                mazeCanvas.Children.Add(label);

                nodePositions[text] = (row, col);
                allCells[(row, col)] = nodeCell;
            }

            // Add heuristic values as small labels for A*
            foreach (var (row, col, node) in labels)
            {
                if (heuristics.ContainsKey(node))
                {
                    TextBlock heuristicLabel = new TextBlock
                    {
                        Text = $"h={heuristics[node]}",
                        FontSize = 10,
                        Foreground = Brushes.DarkBlue,
                        FontStyle = FontStyles.Italic
                    };
                    Canvas.SetLeft(heuristicLabel, col * CellSize + 2);
                    Canvas.SetTop(heuristicLabel, row * CellSize + CellSize - 12);
                    mazeCanvas.Children.Add(heuristicLabel);
                }
            }
        }

        private async void RunDFS(string start)
        {
            animationInProgress = true;
            DisableButtons();
            statusText.Text = "Running DFS traversal...";

            HashSet<string> visited = new HashSet<string>();
            HashSet<string> onPath = new HashSet<string>();

            bool pathFound = await DFS(start, visited, onPath);

            // Show the final path from A to B if found
            if (pathFound)
            {
                List<string> finalPath = ReconstructPath("B");
                await HighlightFinalPath(finalPath);
            }
            else
            {
                statusText.Text = "DFS traversal complete. No path to B was found.";
            }

            EnableButtons();
        }

        private async Task<bool> DFS(string node, HashSet<string> visited, HashSet<string> onPath)
        {
            if (visited.Contains(node)) return onPath.Contains(node);

            // Mark the current node as visited
            visited.Add(node);
            onPath.Add(node);
            searchPath.Add(node);

            // Visualize the current node
            if (nodePositions.ContainsKey(node))
            {
                // Only change color if not the start or end node
                if (node != "A" && node != "B")
                {
                    nodeCells[node].Fill = Brushes.DeepSkyBlue;
                }

                // Create a pulse animation
                DoubleAnimation pulseAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.5,
                    Duration = TimeSpan.FromMilliseconds(animationDelay / 2),
                    AutoReverse = true
                };

                nodeCells[node].BeginAnimation(OpacityProperty, pulseAnimation);

                // Update status and path text
                statusText.Text = $"DFS visiting node: {node}";
                pathText.Text = $"Path: {string.Join(" → ", onPath)}";

                await Task.Delay(animationDelay);
            }

            // Check if we've reached the target
            if (node == "B")
            {
                return true;
            }

            // Visit all neighbors
            foreach (string neighbor in graph[node])
            {
                if (!visited.Contains(neighbor))
                {
                    parentMap[neighbor] = node;

                    // Fill the path to the next node
                    await FillPathBetweenNodes(node, neighbor, Brushes.LightBlue);

                    bool foundPath = await DFS(neighbor, visited, onPath);

                    if (foundPath)
                    {
                        // Found path to B, exit early
                        return true;
                    }

                    // Reset path color for backtracking
                    await FillPathBetweenNodes(node, neighbor, Brushes.LightGray);
                }
            }

            // Backtrack - mark node as no longer on current path
            onPath.Remove(node);

            // Show backtracking visually
            if (nodeCells.ContainsKey(node) && node != "A" && node != "B")
            {
                nodeCells[node].Fill = Brushes.LightGray; // Mark as backtracked
                statusText.Text = $"Backtracking from node: {node}";
                await Task.Delay(animationDelay / 2);
            }

            return false;
        }

        private async void RunBFS(string start)
        {
            animationInProgress = true;
            DisableButtons();
            statusText.Text = "Running BFS traversal...";

            Queue<string> queue = new Queue<string>();
            HashSet<string> visited = new HashSet<string>();

            queue.Enqueue(start);
            visited.Add(start);

            bool pathFound = false;

            while (queue.Count > 0 && !pathFound)
            {
                string current = queue.Dequeue();
                searchPath.Add(current);

                // Visualize current node
                if (nodePositions.ContainsKey(current))
                {
                    if (current != "A" && current != "B")
                    {
                        nodeCells[current].Fill = Brushes.DeepSkyBlue;
                    }

                    // Create a pulse animation
                    DoubleAnimation pulseAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.5,
                        Duration = TimeSpan.FromMilliseconds(animationDelay / 2),
                        AutoReverse = true
                    };

                    nodeCells[current].BeginAnimation(OpacityProperty, pulseAnimation);

                    // Update status and path
                    statusText.Text = $"BFS visiting node: {current}";
                    pathText.Text = $"Visited: {string.Join(" → ", searchPath)}";

                    await Task.Delay(animationDelay);
                }

                // Check if we've reached the target
                if (current == "B")
                {
                    pathFound = true;
                    break;
                }

                // Visit all neighbors
                foreach (string neighbor in graph[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);

                        // Fill the path to the neighbor
                        await FillPathBetweenNodes(current, neighbor, Brushes.LightBlue);
                    }
                }
            }

            // Show the final path from A to B if found
            if (pathFound)
            {
                List<string> finalPath = ReconstructPath("B");
                await HighlightFinalPath(finalPath);
            }
            else
            {
                statusText.Text = "BFS traversal complete. No path to B was found.";
            }

            EnableButtons();
        }

        private async void RunAStar(string start, string goal)
        {
            animationInProgress = true;
            DisableButtons();
            statusText.Text = "Running A* traversal...";

            // Dictionary to store the cost from start to each node
            Dictionary<string, int> gScore = new Dictionary<string, int>();
            // Dictionary to store the estimated total cost from start to goal through each node
            Dictionary<string, int> fScore = new Dictionary<string, int>();
            // Priority queue to get the node with the lowest fScore
            List<string> openSet = new List<string>();
            // Set of visited nodes
            HashSet<string> closedSet = new HashSet<string>();

            // Initialize scores
            foreach (var node in graph.Keys)
            {
                gScore[node] = int.MaxValue;
                fScore[node] = int.MaxValue;
            }

            gScore[start] = 0;
            fScore[start] = heuristics[start];
            openSet.Add(start);

            bool pathFound = false;

            while (openSet.Count > 0)
            {
                // Get node with lowest fScore
                string current = openSet.OrderBy(n => fScore[n]).First();
                searchPath.Add(current);

                // Visualize current node
                if (nodePositions.ContainsKey(current))
                {
                    if (current != "A" && current != "B")
                    {
                        nodeCells[current].Fill = Brushes.MediumPurple;
                    }

                    // Create a pulse animation
                    DoubleAnimation pulseAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.5,
                        Duration = TimeSpan.FromMilliseconds(animationDelay / 2),
                        AutoReverse = true
                    };

                    nodeCells[current].BeginAnimation(OpacityProperty, pulseAnimation);

                    // Update status and information
                    statusText.Text = $"A* visiting node: {current} (f={fScore[current]}, g={gScore[current]}, h={heuristics[current]})";
                    pathText.Text = $"Visited: {string.Join(" → ", searchPath)}";

                    await Task.Delay(animationDelay);
                }

                // Check if we've reached the goal
                if (current == goal)
                {
                    pathFound = true;
                    break;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                // Visit all neighbors
                foreach (string neighbor in graph[current])
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    // The cost to reach neighbor from start through current
                    int tentativeGScore = gScore[current] + 1;  // Assuming uniform cost of 1

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGScore >= gScore[neighbor])
                    {
                        continue;  // This is not a better path
                    }

                    // This path is the best until now. Record it.
                    parentMap[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + heuristics[neighbor];

                    // Fill the path to the neighbor
                    await FillPathBetweenNodes(current, neighbor, Brushes.Lavender);
                }
            }

            // Show the final path from start to goal if found
            if (pathFound)
            {
                List<string> finalPath = ReconstructPath(goal);
                await HighlightFinalPath(finalPath);
            }
            else
            {
                statusText.Text = "A* traversal complete. No path to goal was found.";
            }

            EnableButtons();
        }

        private List<string> ReconstructPath(string target)
        {
            List<string> path = new List<string>();
            string current = target;

            while (parentMap.ContainsKey(current))
            {
                path.Add(current);
                current = parentMap[current];
            }

            path.Add("A"); // Add the start node
            path.Reverse(); // Reverse to get start-to-end order

            return path;
        }

        private async Task HighlightFinalPath(List<string> path)
        {
            statusText.Text = $"Path found! Highlighting solution: {string.Join(" → ", path)}";

            // Color all nodes on the path
            for (int i = 0; i < path.Count; i++)
            {
                if (i < path.Count - 1)
                {
                    await FillPathBetweenNodes(path[i], path[i + 1], Brushes.Gold);
                }

                // Highlight node (except start/end which keep their colors)
                if (path[i] != "A" && path[i] != "B")
                {
                    nodeCells[path[i]].Fill = Brushes.Gold;
                }
            }

            statusText.Text = $"Traversal complete! Solution path: {string.Join(" → ", path)}";
        }

        private async Task FillPathBetweenNodes(string from, string to, Brush color)
        {
            if (!nodePositions.ContainsKey(from) || !nodePositions.ContainsKey(to))
                return;

            var (fromRow, fromCol) = nodePositions[from];
            var (toRow, toCol) = nodePositions[to];

            // Simple case: nodes are adjacent
            if (Math.Abs(fromRow - toRow) <= 1 && Math.Abs(fromCol - toCol) <= 1)
            {
                return; // No need to fill path for adjacent cells
            }

            // Determine direction and steps between nodes
            int rowStep = Math.Sign(toRow - fromRow);
            int colStep = Math.Sign(toCol - fromCol);

            if (rowStep != 0 && colStep != 0)
            {
                // Diagonal movement - handle as special case if needed
                return;
            }

            int currentRow = fromRow;
            int currentCol = fromCol;

            // Move along row first if needed
            while (currentRow != toRow)
            {
                currentRow += rowStep;
                if (allCells.ContainsKey((currentRow, currentCol)))
                {
                    allCells[(currentRow, currentCol)].Fill = color;
                    await Task.Delay(animationDelay / 4); // Faster animation for path segments
                }
            }

            // Then move along column
            while (currentCol != toCol)
            {
                currentCol += colStep;
                if (allCells.ContainsKey((currentRow, currentCol)))
                {
                    allCells[(currentRow, currentCol)].Fill = color;
                    await Task.Delay(animationDelay / 4);
                }
            }
        }

        private void DisableButtons()
        {
            animationInProgress = true;
            startDfsButton.IsEnabled = false;
            startBfsButton.IsEnabled = false;
            startAStarButton.IsEnabled = false;
            
        }

        private void EnableButtons()
        {
            animationInProgress = false;
            startDfsButton.IsEnabled = true;
            startBfsButton.IsEnabled = true;
            startAStarButton.IsEnabled = true;
           
        }
    }
}
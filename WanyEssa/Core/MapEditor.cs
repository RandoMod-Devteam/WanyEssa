using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace WanyEssa.Core
{
    public class MapEditor
    {
        private bool _isEnabled;
        private bool _isVisible;
        private int _selectedTileId;
        private int _gridSize;
        private int _mapWidth;
        private int _mapHeight;
        private List<List<int>> _mapData;
        private Vector2 _cameraPosition;
        private float _cameraZoom;
        private bool _isDragging;
        private Vector2 _lastMousePosition;
        
        // UI states
        private enum UIMode
        {
            TileSelection,
            MapEdit,
            FileMenu
        }
        
        private UIMode _uiMode;
        
        public bool IsEnabled => _isEnabled;
        public bool IsVisible => _isVisible;
        public int GridSize => _gridSize;
        public int MapWidth => _mapWidth;
        public int MapHeight => _mapHeight;
        
        public MapEditor(int gridSize = 32)
        {
            _isEnabled = true;
            _isVisible = false;
            _selectedTileId = 1;
            _gridSize = gridSize;
            _mapWidth = 20;
            _mapHeight = 15;
            _cameraPosition = Vector2.Zero;
            _cameraZoom = 1.0f;
            _isDragging = false;
            _lastMousePosition = Vector2.Zero;
            _uiMode = UIMode.MapEdit;
            
            // Initialize map data with empty tiles (0 = empty, 1+ = tiles)
            _mapData = new List<List<int>>();
            for (int y = 0; y < _mapHeight; y++)
            {
                List<int> row = new List<int>();
                for (int x = 0; x < _mapWidth; x++)
                {
                    row.Add(0);
                }
                _mapData.Add(row);
            }
        }
        
        public void Toggle()
        {
            if (_isEnabled)
            {
                _isVisible = !_isVisible;
            }
        }
        
        public void Update(GameWindow window, float deltaTime)
        {
            if (!_isVisible || !_isEnabled)
                return;
            
            HandleInput(window, deltaTime);
        }
        
        private void HandleInput(GameWindow window, float deltaTime)
        {
            var keyboardState = window.KeyboardState;
            var mouseState = window.MouseState;
            
            // Handle keyboard input
            if (keyboardState.IsKeyPressed(Keys.T))
            {
                _uiMode = UIMode.TileSelection;
            }
            else if (keyboardState.IsKeyPressed(Keys.M))
            {
                _uiMode = UIMode.MapEdit;
            }
            else if (keyboardState.IsKeyPressed(Keys.F))
            {
                _uiMode = UIMode.FileMenu;
            }
            
            // Handle mouse input for map editing
            if (_uiMode == UIMode.MapEdit)
            {
                Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
                
                // Camera movement
                if (mouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (!_isDragging)
                    {
                        _isDragging = true;
                        _lastMousePosition = mousePos;
                    }
                    else
                    {
                        Vector2 delta = mousePos - _lastMousePosition;
                        _cameraPosition += delta;
                        _lastMousePosition = mousePos;
                    }
                }
                else
                {
                    _isDragging = false;
                }
                
                // Zoom with mouse wheel
                float zoomDelta = mouseState.ScrollDelta.Y;
                if (zoomDelta != 0)
                {
                    _cameraZoom = Math.Clamp(_cameraZoom + zoomDelta * 0.1f, 0.5f, 3.0f);
                }
                
                // Tile placement
                if (mouseState.IsButtonDown(MouseButton.Left))
                {
                    Vector2 worldPos = ScreenToWorld(mousePos);
                    PlaceTile(worldPos, _selectedTileId);
                }
                
                // Tile erasure
                if (mouseState.IsButtonDown(MouseButton.Right))
                {
                    Vector2 worldPos = ScreenToWorld(mousePos);
                    PlaceTile(worldPos, 0);
                }
            }
        }
        
        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            // Simple camera transformation
            return (screenPos - _cameraPosition) / _cameraZoom;
        }
        
        private Vector2 WorldToGrid(Vector2 worldPos)
        {
            return new Vector2(
                (int)(worldPos.X / _gridSize),
                (int)(worldPos.Y / _gridSize)
            );
        }
        
        private void PlaceTile(Vector2 worldPos, int tileId)
        {
            Vector2 gridPos = WorldToGrid(worldPos);
            
            // Check if grid position is within bounds
            if (gridPos.X >= 0 && gridPos.X < _mapWidth && gridPos.Y >= 0 && gridPos.Y < _mapHeight)
            {
                _mapData[(int)gridPos.Y][(int)gridPos.X] = tileId;
            }
        }
        
        public void SaveMap(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write map header
                    writer.WriteLine($"{_mapWidth} {_mapHeight} {_gridSize}");
                    
                    // Write map data
                    for (int y = 0; y < _mapHeight; y++)
                    {
                        for (int x = 0; x < _mapWidth; x++)
                        {
                            writer.Write($"{_mapData[y][x]} ");
                        }
                        writer.WriteLine();
                    }
                }
                
                System.Console.WriteLine($"Map saved to {filePath}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving map: {ex.Message}");
            }
        }
        
        public void LoadMap(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Console.WriteLine($"Map file not found: {filePath}");
                    return;
                }
                
                using (StreamReader reader = new StreamReader(filePath))
                {
                    // Read map header
                    string[] headerParts = reader.ReadLine()?.Split(' ') ?? Array.Empty<string>();
                    if (headerParts.Length < 3)
                    {
                        System.Console.WriteLine("Invalid map file format");
                        return;
                    }
                    
                    _mapWidth = int.Parse(headerParts[0]);
                    _mapHeight = int.Parse(headerParts[1]);
                    _gridSize = int.Parse(headerParts[2]);
                    
                    // Initialize map data
                    _mapData = new List<List<int>>();
                    for (int y = 0; y < _mapHeight; y++)
                    {
                        string line = reader.ReadLine() ?? string.Empty;
                        string[] tileParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        
                        List<int> row = new List<int>();
                        for (int x = 0; x < _mapWidth; x++)
                        {
                            if (x < tileParts.Length)
                            {
                                row.Add(int.Parse(tileParts[x]));
                            }
                            else
                            {
                                row.Add(0);
                            }
                        }
                        _mapData.Add(row);
                    }
                }
                
                System.Console.WriteLine($"Map loaded from {filePath}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading map: {ex.Message}");
            }
        }
        
        public void NewMap(int width, int height, int gridSize = 32)
        {
            _mapWidth = width;
            _mapHeight = height;
            _gridSize = gridSize;
            
            // Initialize empty map
            _mapData = new List<List<int>>();
            for (int y = 0; y < _mapHeight; y++)
            {
                List<int> row = new List<int>();
                for (int x = 0; x < _mapWidth; x++)
                {
                    row.Add(0);
                }
                _mapData.Add(row);
            }
            
            // Reset camera
            _cameraPosition = Vector2.Zero;
            _cameraZoom = 1.0f;
            
            System.Console.WriteLine($"New map created: {width}x{height} tiles, {gridSize}px grid");
        }
        
        public void Draw()
        {
            if (!_isVisible || !_isEnabled)
                return;
            
            // Draw grid
            DrawGrid();
            
            // Draw tiles
            DrawTiles();
            
            // Draw UI
            DrawUI();
        }
        
        private void DrawGrid()
        {
            // In a real implementation, this would use the renderer to draw grid lines
        }
        
        private void DrawTiles()
        {
            // In a real implementation, this would use the renderer to draw tiles based on _mapData
        }
        
        private void DrawUI()
        {
            // In a real implementation, this would draw the UI elements
            // like tile selection panel, file menu, etc.
        }
        
        public int GetTileAt(Vector2 worldPos)
        {
            Vector2 gridPos = WorldToGrid(worldPos);
            
            if (gridPos.X >= 0 && gridPos.X < _mapWidth && gridPos.Y >= 0 && gridPos.Y < _mapHeight)
            {
                return _mapData[(int)gridPos.Y][(int)gridPos.X];
            }
            
            return -1;
        }
        
        public void SetSelectedTileId(int tileId)
        {
            _selectedTileId = tileId;
        }
        
        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
        }
    }
}
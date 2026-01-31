using System;   
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace WanyEssa.Core
{
    public class Console
    {
        private bool _isOpen;
        private bool _isEnabled;
        private string _inputBuffer;
        private List<string> _history;
        private List<string> _messages;
        private int _historyIndex;
        private int _maxMessages = 50;
        
        public bool IsOpen => _isOpen;
        public bool IsEnabled => _isEnabled;
        
        public Console()
        {
            _isOpen = false;
            _isEnabled = Settings.Instance.ConsoleEnabled;
            _inputBuffer = string.Empty;
            _history = new List<string>();
            _messages = new List<string>();
            _historyIndex = -1;
        }
        
        public void Toggle()
        {
            if (_isEnabled)
            {
                _isOpen = !_isOpen;
                if (_isOpen)
                {
                    Log("Console opened. Type commands or press UP/DOWN for history.");
                }
            }
        }
        
        public void Update(GameWindow window)
        {
            if (!_isOpen || !_isEnabled)
                return;
            
            // Handle keyboard input
            var keyboardState = window.KeyboardState;
            
            // Handle character input
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (keyboardState.IsKeyPressed(key))
                {
                    char? c = KeyToChar(key, keyboardState);
                    if (c.HasValue)
                    {
                        _inputBuffer += c.Value;
                    }
                }
            }
            
            // Handle backspace
            if (keyboardState.IsKeyPressed(Keys.Backspace) && _inputBuffer.Length > 0)
            {
                _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
            }
            
            // Handle enter
            if (keyboardState.IsKeyPressed(Keys.Enter))
            {
                if (!string.IsNullOrWhiteSpace(_inputBuffer))
                {
                    ExecuteCommand(_inputBuffer);
                    _history.Add(_inputBuffer);
                    _historyIndex = -1;
                    _inputBuffer = string.Empty;
                }
            }
            
            // Handle escape
            if (keyboardState.IsKeyPressed(Keys.Escape))
            {
                _isOpen = false;
            }
            
            // Handle up/down arrow for history
            if (keyboardState.IsKeyPressed(Keys.Up))
            {
                if (_history.Count > 0)
                {
                    _historyIndex = System.Math.Clamp(_historyIndex + 1, 0, _history.Count - 1);
                    _inputBuffer = _history[_history.Count - 1 - _historyIndex];
                }
            }
            
            if (keyboardState.IsKeyPressed(Keys.Down))
            {
                if (_history.Count > 0)
                {
                    _historyIndex = System.Math.Clamp(_historyIndex - 1, -1, _history.Count - 1);
                    if (_historyIndex == -1)
                    {
                        _inputBuffer = string.Empty;
                    }
                    else
                    {
                        _inputBuffer = _history[_history.Count - 1 - _historyIndex];
                    }
                }
            }
        }
        
        private char? KeyToChar(Keys key, KeyboardState keyboardState)
        {
            bool shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            
            switch (key)
            {
                case Keys.A: return shift ? 'A' : 'a';
                case Keys.B: return shift ? 'B' : 'b';
                case Keys.C: return shift ? 'C' : 'c';
                case Keys.D: return shift ? 'D' : 'd';
                case Keys.E: return shift ? 'E' : 'e';
                case Keys.F: return shift ? 'F' : 'f';
                case Keys.G: return shift ? 'G' : 'g';
                case Keys.H: return shift ? 'H' : 'h';
                case Keys.I: return shift ? 'I' : 'i';
                case Keys.J: return shift ? 'J' : 'j';
                case Keys.K: return shift ? 'K' : 'k';
                case Keys.L: return shift ? 'L' : 'l';
                case Keys.M: return shift ? 'M' : 'm';
                case Keys.N: return shift ? 'N' : 'n';
                case Keys.O: return shift ? 'O' : 'o';
                case Keys.P: return shift ? 'P' : 'p';
                case Keys.Q: return shift ? 'Q' : 'q';
                case Keys.R: return shift ? 'R' : 'r';
                case Keys.S: return shift ? 'S' : 's';
                case Keys.T: return shift ? 'T' : 't';
                case Keys.U: return shift ? 'U' : 'u';
                case Keys.V: return shift ? 'V' : 'v';
                case Keys.W: return shift ? 'W' : 'w';
                case Keys.X: return shift ? 'X' : 'x';
                case Keys.Y: return shift ? 'Y' : 'y';
                case Keys.Z: return shift ? 'Z' : 'z';
                case Keys.D0: return shift ? ')' : '0';
                case Keys.D1: return shift ? '!' : '1';
                case Keys.D2: return shift ? '@' : '2';
                case Keys.D3: return shift ? '#' : '3';
                case Keys.D4: return shift ? '$' : '4';
                case Keys.D5: return shift ? '%' : '5';
                case Keys.D6: return shift ? '^' : '6';
                case Keys.D7: return shift ? '&' : '7';
                case Keys.D8: return shift ? '*' : '8';
                case Keys.D9: return shift ? '(' : '9';
                case Keys.Space: return ' ';
                case Keys.Minus: return shift ? '_' : '-';
                case Keys.Equal: return shift ? '+' : '=';
                case Keys.Slash: return shift ? '?' : '/';
                case Keys.Backslash: return shift ? '|' : '\\';
                case Keys.Semicolon: return shift ? ':' : ';';
                case Keys.Apostrophe: return shift ? '"' : '\'';
                case Keys.Comma: return shift ? '<' : ',';
                case Keys.Period: return shift ? '>' : '.';
                case Keys.GraveAccent: return shift ? '~' : '`';
                default: return null;
            }
        }
        
        private void ExecuteCommand(string command)
        {
            command = command.Trim();
            if (string.IsNullOrEmpty(command))
                return;
            
            Log($"> {command}");
            
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;
            
            string cmd = parts[0].ToLower();
            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
            
            switch (cmd)
            {
                case "help":
                    ShowHelp();
                    break;
                case "clear":
                    _messages.Clear();
                    break;
                case "exit":
                case "close":
                    _isOpen = false;
                    break;
                case "fps":
                    if (args.Length > 0)
                    {
                        if (int.TryParse(args[0], out int fps))
                        {
                            Settings.Instance.FpsLimit = System.Math.Clamp(fps, 15, 240);
                            Log($"FPS limit set to {Settings.Instance.FpsLimit}");
                        }
                        else
                        {
                            Log($"Current FPS limit: {Settings.Instance.FpsLimit}");
                        }
                    }
                    else
                    {
                        Log($"Current FPS limit: {Settings.Instance.FpsLimit}");
                    }
                    break;
                case "msaa":
                    if (args.Length > 0)
                    {
                        if (int.TryParse(args[0], out int msaa))
                        {
                            Settings.Instance.MsaaLevel = System.Math.Clamp(msaa, 0, 8);
                            Log($"MSAA set to {Settings.Instance.MsaaLevel}x");
                        }
                        else
                        {
                            Log($"Current MSAA: {Settings.Instance.MsaaLevel}x");
                        }
                    }
                    else
                    {
                        Log($"Current MSAA: {Settings.Instance.MsaaLevel}x");
                    }
                    break;
                case "fsa":
                    if (args.Length > 0)
                    {
                        bool fsa = args[0].ToLower() == "true" || args[0] == "1";
                        Settings.Instance.FsaEnabled = fsa;
                        Log($"FSA set to {fsa}");
                    }
                    else
                    {
                        Log($"Current FSA: {Settings.Instance.FsaEnabled}");
                    }
                    break;
                case "debug":
                    if (args.Length > 0)
                    {
                        bool debug = args[0].ToLower() == "true" || args[0] == "1";
                        Settings.Instance.DebugMode = debug;
                        Log($"Debug mode set to {debug}");
                    }
                    else
                    {
                        Log($"Current debug mode: {Settings.Instance.DebugMode}");
                    }
                    break;
                case "console":
                    if (args.Length > 0)
                    {
                        bool console = args[0].ToLower() == "true" || args[0] == "1";
                        _isEnabled = console;
                        Settings.Instance.ConsoleEnabled = console;
                        Log($"Console enabled: {console}");
                    }
                    else
                    {
                        Log($"Console enabled: {_isEnabled}");
                    }
                    break;
                case "log":
                    if (args.Length > 0)
                    {
                        string message = string.Join(" ", args);
                        Log(message);
                    }
                    break;
                default:
                    Log($"Unknown command: {cmd}");
                    Log("Type 'help' for available commands.");
                    break;
            }
        }
        
        private void ShowHelp()
        {
            Log("Available commands:");
            Log("help - Show this help message");
            Log("clear - Clear console messages");
            Log("exit/close - Close console");
            Log("fps [value] - Get/set FPS limit (15-240)");
            Log("msaa [value] - Get/set MSAA level (0-8)");
            Log("fsa [true/false] - Get/set FSA enabled");
            Log("debug [true/false] - Get/set debug mode");
            Log("console [true/false] - Get/set console enabled");
            Log("log [message] - Log a message");
        }
        
        public void Log(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _messages.Add(timestampedMessage);
            
            if (_messages.Count > _maxMessages)
            {
                _messages.RemoveAt(0);
            }
            
            System.Console.WriteLine(timestampedMessage);
        }
        
        public void Draw()
        {
            if (!_isOpen || !_isEnabled)
                return;
            
            // Draw console background (simplified for now)
            // In a real implementation, this would use the renderer to draw
            // a semi-transparent background with text
        }
    }
}
using System.Diagnostics;
using System.Text;
using WanyEssa.Graphics;
using OpenTK.Mathematics;

namespace WanyEssa.Core
{
    public class PerformanceAnalyzer : IDisposable
    {
        private Stopwatch _stopwatch;
        private Dictionary<string, PerformanceMetric> _metrics;
        private Dictionary<string, long> _startTimes;
        private float _frameTime;
        private float _fps;
        private int _frameCount;
        private float _frameTimer;
        private bool _enabled;
        private bool _disposed;
        
        public float FrameTime => _frameTime;
        public float FPS => _fps;
        public bool Enabled => _enabled;
        
        public PerformanceAnalyzer()
        {
            _stopwatch = new Stopwatch();
            _metrics = new Dictionary<string, PerformanceMetric>();
            _startTimes = new Dictionary<string, long>();
            _frameTime = 0.0f;
            _fps = 0.0f;
            _frameCount = 0;
            _frameTimer = 0.0f;
            _enabled = true;
            _disposed = false;
            
            _stopwatch.Start();
        }
        
        public void StartFrame()
        {
            if (!_enabled || _disposed)
                return;
            
            _stopwatch.Restart();
        }
        
        public void EndFrame()
        {
            if (!_enabled || _disposed)
                return;
            
            _frameTime = (float)_stopwatch.Elapsed.TotalSeconds;
            _frameTimer += _frameTime;
            _frameCount++;
            
            if (_frameTimer >= 1.0f)
            {
                _fps = _frameCount / _frameTimer;
                _frameCount = 0;
                _frameTimer = 0.0f;
            }
        }
        
        public void StartMetric(string name)
        {
            if (!_enabled || _disposed)
                return;
            
            _startTimes[name] = _stopwatch.ElapsedTicks;
        }
        
        public void EndMetric(string name)
        {
            if (!_enabled || _disposed || !_startTimes.ContainsKey(name))
                return;
            
            long elapsedTicks = _stopwatch.ElapsedTicks - _startTimes[name];
            float elapsedSeconds = elapsedTicks / (float)Stopwatch.Frequency;
            
            if (!_metrics.ContainsKey(name))
            {
                _metrics[name] = new PerformanceMetric(name);
            }
            
            _metrics[name].Record(elapsedSeconds);
            _startTimes.Remove(name);
        }
        
        public void Enable()
        {
            _enabled = true;
        }
        
        public void Disable()
        {
            _enabled = false;
        }
        
        public void Reset()
        {
            if (_disposed)
                return;
            
            _metrics.Clear();
            _startTimes.Clear();
            _frameTime = 0.0f;
            _fps = 0.0f;
            _frameCount = 0;
            _frameTimer = 0.0f;
        }
        
        public string GetReport()
        {
            if (_disposed)
                return "";
            
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Performance Report ===");
            report.AppendLine($"FPS: {_fps:F2}");
            report.AppendLine($"Frame Time: {_frameTime * 1000:F2} ms");
            
            foreach (var metric in _metrics.Values)
            {
                report.AppendLine(metric.GetReport());
            }
            
            return report.ToString();
        }
        
        public void LogReport()
        {
            if (!_enabled || _disposed)
                return;
            
            System.Console.WriteLine(GetReport());
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _metrics.Clear();
                _startTimes.Clear();
                _disposed = true;
            }
        }
    }
    
    public class PerformanceMetric
    {
        private string _name;
        private float _totalTime;
        private int _count;
        private float _minTime;
        private float _maxTime;
        private float _averageTime;
        private float _lastTime;
        
        public string Name => _name;
        public float TotalTime => _totalTime;
        public int Count => _count;
        public float MinTime => _minTime;
        public float MaxTime => _maxTime;
        public float AverageTime => _averageTime;
        public float LastTime => _lastTime;
        
        public PerformanceMetric(string name)
        {
            _name = name;
            _totalTime = 0.0f;
            _count = 0;
            _minTime = float.MaxValue;
            _maxTime = 0.0f;
            _averageTime = 0.0f;
            _lastTime = 0.0f;
        }
        
        public void Record(float time)
        {
            _totalTime += time;
            _count++;
            _minTime = MathF.Min(_minTime, time);
            _maxTime = MathF.Max(_maxTime, time);
            _averageTime = _totalTime / _count;
            _lastTime = time;
        }
        
        public string GetReport()
        {
            return $"{_name}: Avg={_averageTime * 1000:F2} ms, Min={_minTime * 1000:F2} ms, Max={_maxTime * 1000:F2} ms, Count={_count}";
        }
        
        public void Reset()
        {
            _totalTime = 0.0f;
            _count = 0;
            _minTime = float.MaxValue;
            _maxTime = 0.0f;
            _averageTime = 0.0f;
            _lastTime = 0.0f;
        }
    }
    
    public class DebugRenderer : IDisposable
    {
        private Renderer _renderer;
        private List<DebugLine> _lines;
        private List<DebugText> _texts;
        private List<DebugBounds> _bounds;
        private bool _disposed;
        
        public DebugRenderer(Renderer renderer)
        {
            _renderer = renderer;
            _lines = new List<DebugLine>();
            _texts = new List<DebugText>();
            _bounds = new List<DebugBounds>();
            _disposed = false;
        }
        
        public void DrawLine(Vector3 start, Vector3 end, OpenTK.Mathematics.Vector4 color, float duration = 0.0f)
        {
            if (_disposed)
                return;
            
            _lines.Add(new DebugLine(start, end, color, duration));
        }
        
        public void DrawText(string text, Vector2 position, OpenTK.Mathematics.Vector4 color, float duration = 0.0f)
        {
            if (_disposed)
                return;
            
            _texts.Add(new DebugText(text, position, color, duration));
        }
        
        public void DrawBounds(Bounds bounds, OpenTK.Mathematics.Vector4 color, float duration = 0.0f)
        {
            if (_disposed)
                return;
            
            _bounds.Add(new DebugBounds(bounds, color, duration));
        }
        
        public void Update(float deltaTime)
        {
            if (_disposed)
                return;
            
            // Update line durations
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                DebugLine line = _lines[i];
                line.Duration -= deltaTime;
                if (line.Duration <= 0 && line.Duration != -1)
                {
                    _lines.RemoveAt(i);
                }
            }
            
            // Update text durations
            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                DebugText text = _texts[i];
                text.Duration -= deltaTime;
                if (text.Duration <= 0 && text.Duration != -1)
                {
                    _texts.RemoveAt(i);
                }
            }
            
            // Update bounds durations
            for (int i = _bounds.Count - 1; i >= 0; i--)
            {
                DebugBounds bounds = _bounds[i];
                bounds.Duration -= deltaTime;
                if (bounds.Duration <= 0 && bounds.Duration != -1)
                {
                    _bounds.RemoveAt(i);
                }
            }
        }
        
        public void Render()
        {
            if (_disposed)
                return;
            
            // Draw lines
            foreach (DebugLine line in _lines)
            {
                // In a real implementation, you would draw the line here
                // For now, we'll just draw two circles connected by a rectangle
                _renderer.DrawCircle(line.Start, 0.02f, line.Color);
                _renderer.DrawCircle(line.End, 0.02f, line.Color);
                
                Vector3 direction = line.End - line.Start;
                float length = direction.Length;
                Vector3 midpoint = (line.Start + line.End) / 2;
                Vector3 up = new Vector3(0, 1, 0);
                Vector3 right = Vector3.Cross(direction, up).Normalized();
                
                // Create a rectangle for the line
                _renderer.DrawRectangle(midpoint, new Vector2(length, 0.01f), line.Color);
            }
            
            // Draw bounds
            foreach (DebugBounds bounds in _bounds)
            {
                // Draw bounding box
                OpenTK.Mathematics.Vector3 min = bounds.Bounds.Min;
                OpenTK.Mathematics.Vector3 max = bounds.Bounds.Max;
                OpenTK.Mathematics.Vector3 size = bounds.Bounds.Size;
                
                _renderer.DrawRectangle(new OpenTK.Mathematics.Vector3((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2), 
                                      new OpenTK.Mathematics.Vector2(size.X, size.Y), bounds.Color);
            }
        }
        
        public void Clear()
        {
            if (_disposed)
                return;
            
            _lines.Clear();
            _texts.Clear();
            _bounds.Clear();
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _lines.Clear();
                _texts.Clear();
                _bounds.Clear();
                _disposed = true;
            }
        }
    }
    
    public struct DebugLine
    {
        public Vector3 Start;
        public Vector3 End;
        public OpenTK.Mathematics.Vector4 Color;
        public float Duration;
        
        public DebugLine(Vector3 start, Vector3 end, OpenTK.Mathematics.Vector4 color, float duration)
        {
            Start = start;
            End = end;
            Color = color;
            Duration = duration;
        }
    }
    
    public struct DebugText
    {
        public string Text;
        public Vector2 Position;
        public OpenTK.Mathematics.Vector4 Color;
        public float Duration;
        
        public DebugText(string text, Vector2 position, OpenTK.Mathematics.Vector4 color, float duration)
        {
            Text = text;
            Position = position;
            Color = color;
            Duration = duration;
        }
    }
    
    public struct DebugBounds
    {
        public Bounds Bounds;
        public OpenTK.Mathematics.Vector4 Color;
        public float Duration;
        
        public DebugBounds(Bounds bounds, OpenTK.Mathematics.Vector4 color, float duration)
        {
            Bounds = bounds;
            Color = color;
            Duration = duration;
        }
    }
    
    public struct Bounds
    {
        public Vector3 Min;
        public Vector3 Max;
        public Vector3 Center => (Min + Max) / 2;
        public Vector3 Size => Max - Min;
        
        public Bounds(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }
        
        public Bounds(Vector3 center, float size)
        {
            Min = center - new OpenTK.Mathematics.Vector3(size / 2, size / 2, size / 2);
            Max = center + new OpenTK.Mathematics.Vector3(size / 2, size / 2, size / 2);
        }
        
        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }
        
        public bool Intersects(Bounds other)
        {
            return !(Max.X < other.Min.X || Min.X > other.Max.X ||
                     Max.Y < other.Min.Y || Min.Y > other.Max.Y ||
                     Max.Z < other.Min.Z || Min.Z > other.Max.Z);
        }
    }
}

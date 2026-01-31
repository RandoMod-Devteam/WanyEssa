using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using WanyEssa.Math;

namespace WanyEssa.Graphics
{
    public class Mesh
    {
        private int _vertexArray;
        private int _vertexBuffer;
        private int _indexBuffer;
        private int _vertexCount;
        private int _indexCount;
        private List<Vector3> _vertices;
        private List<Vector3> _normals;
        private List<Vector2> _uvs;
        private List<int> _indices;
        private int _textureId;
        
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;
        public bool Visible { get; set; } = true;
        
        public Mesh()
        {
            _vertices = new List<Vector3>();
            _normals = new List<Vector3>();
            _uvs = new List<Vector2>();
            _indices = new List<int>();
            _textureId = -1;
        }
        
        public void AddVertex(Vector3 vertex, Vector3 normal, Vector2 uv)
        {
            _vertices.Add(vertex);
            _normals.Add(normal);
            _uvs.Add(uv);
        }
        
        public void AddIndex(int index)
        {
            _indices.Add(index);
        }
        
        public void AddFace(int i1, int i2, int i3)
        {
            _indices.Add(i1);
            _indices.Add(i2);
            _indices.Add(i3);
        }
        
        public void SetTexture(int textureId)
        {
            _textureId = textureId;
        }
        
        public void Build()
        {
            // Generate and bind vertex array
            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            
            // Create vertex buffer
            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            
            // Create interleaved vertex data
            List<float> vertexData = new();
            for (int i = 0; i < _vertices.Count; i++)
            {
                // Position
                vertexData.Add(_vertices[i].X);
                vertexData.Add(_vertices[i].Y);
                vertexData.Add(_vertices[i].Z);
                
                // Normal
                vertexData.Add(_normals[i].X);
                vertexData.Add(_normals[i].Y);
                vertexData.Add(_normals[i].Z);
                
                // UV
                vertexData.Add(_uvs[i].X);
                vertexData.Add(_uvs[i].Y);
            }
            
            // Upload vertex data
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Count * sizeof(float), vertexData.ToArray(), BufferUsageHint.StaticDraw);
            
            // Create index buffer
            if (_indices.Count > 0)
            {
                _indexBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(int), _indices.ToArray(), BufferUsageHint.StaticDraw);
                _indexCount = _indices.Count;
            }
            else
            {
                _indexCount = 0;
            }
            
            _vertexCount = _vertices.Count;
            
            // Set up vertex attributes
            // Position (3 floats)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            
            // Normal (3 floats)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            // UV (2 floats)
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            
            // Unbind
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            if (_indexCount > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            }
        }
        
        public void Draw()
        {
            if (!Visible || _vertexCount == 0)
                return;
            
            GL.BindVertexArray(_vertexArray);
            
            if (_indexCount > 0)
            {
                GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            }
            else
            {
                GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
            }
            
            GL.BindVertexArray(0);
        }
        
        public void Dispose()
        {
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteBuffer(_vertexBuffer);
            if (_indexCount > 0)
            {
                GL.DeleteBuffer(_indexBuffer);
            }
        }
        
        // Static methods for creating primitive meshes
        public static Mesh CreateCube(float size = 1.0f)
        {
            Mesh cube = new Mesh();
            float halfSize = size * 0.5f;
            
            // Vertices
            cube.AddVertex(new Vector3(-halfSize, -halfSize, -halfSize), Vector3.Down, new Vector2(0, 0));
            cube.AddVertex(new Vector3(halfSize, -halfSize, -halfSize), Vector3.Down, new Vector2(1, 0));
            cube.AddVertex(new Vector3(halfSize, halfSize, -halfSize), Vector3.Down, new Vector2(1, 1));
            cube.AddVertex(new Vector3(-halfSize, halfSize, -halfSize), Vector3.Down, new Vector2(0, 1));
            
            cube.AddVertex(new Vector3(-halfSize, -halfSize, halfSize), Vector3.Up, new Vector2(0, 0));
            cube.AddVertex(new Vector3(halfSize, -halfSize, halfSize), Vector3.Up, new Vector2(1, 0));
            cube.AddVertex(new Vector3(halfSize, halfSize, halfSize), Vector3.Up, new Vector2(1, 1));
            cube.AddVertex(new Vector3(-halfSize, halfSize, halfSize), Vector3.Up, new Vector2(0, 1));
            
            // Faces
            // Front
            cube.AddFace(0, 1, 2);
            cube.AddFace(0, 2, 3);
            
            // Back
            cube.AddFace(4, 6, 5);
            cube.AddFace(4, 7, 6);
            
            // Left
            cube.AddFace(0, 3, 7);
            cube.AddFace(0, 7, 4);
            
            // Right
            cube.AddFace(1, 5, 6);
            cube.AddFace(1, 6, 2);
            
            // Bottom
            cube.AddFace(0, 4, 5);
            cube.AddFace(0, 5, 1);
            
            // Top
            cube.AddFace(2, 6, 7);
            cube.AddFace(2, 7, 3);
            
            cube.Build();
            return cube;
        }
        
        public static Mesh CreatePlane(float width = 1.0f, float height = 1.0f)
        {
            Mesh plane = new Mesh();
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            
            // Vertices
            plane.AddVertex(new Vector3(-halfWidth, 0, -halfHeight), Vector3.Up, new Vector2(0, 0));
            plane.AddVertex(new Vector3(halfWidth, 0, -halfHeight), Vector3.Up, new Vector2(1, 0));
            plane.AddVertex(new Vector3(halfWidth, 0, halfHeight), Vector3.Up, new Vector2(1, 1));
            plane.AddVertex(new Vector3(-halfWidth, 0, halfHeight), Vector3.Up, new Vector2(0, 1));
            
            // Faces
            plane.AddFace(0, 1, 2);
            plane.AddFace(0, 2, 3);
            
            plane.Build();
            return plane;
        }
        
        public static Mesh CreateSphere(float radius = 1.0f, int segments = 32)
        {
            Mesh sphere = new Mesh();
            
            for (int i = 0; i <= segments; i++)
            {
                float latitude = (float)System.Math.PI * i / segments;
                float sinLatitude = (float)System.Math.Sin(latitude);
                float cosLatitude = (float)System.Math.Cos(latitude);
                
                for (int j = 0; j <= segments; j++)
                {
                    float longitude = 2.0f * (float)System.Math.PI * j / segments;
                    float sinLongitude = (float)System.Math.Sin(longitude);
                    float cosLongitude = (float)System.Math.Cos(longitude);
                    
                    Vector3 position = new Vector3(
                        radius * sinLatitude * cosLongitude,
                        radius * cosLatitude,
                        radius * sinLatitude * sinLongitude
                    );
                    
                    sphere.AddVertex(position, position.Normalized, new Vector2((float)j / segments, (float)i / segments));
                }
            }
            
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int a = i * (segments + 1) + j;
                    int b = i * (segments + 1) + (j + 1);
                    int c = (i + 1) * (segments + 1) + (j + 1);
                    int d = (i + 1) * (segments + 1) + j;
                    
                    sphere.AddFace(a, b, c);
                    sphere.AddFace(a, c, d);
                }
            }
            
            sphere.Build();
            return sphere;
        }
    }
}
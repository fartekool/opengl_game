using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace OpenGL
{
    class GameObject
    {
        // --- OpenGL-related fields ---
        private int VAO, VBO, EBO;
        private int textureID, textureVBO;

        // --- Geometry and Texture Data ---
        private List<Vector3> vertices;
        private uint[] indices;
        List<Vector2> texCoords;
        private string texturePath; // Store path for potential reloading/debugging

        // --- Transformation Properties ---
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; } // Euler angles (Pitch, Yaw, Roll) in RADIANS
        public Vector3 Scale { get; set; }

        // --- Direction Vectors (calculated from Rotation) ---
        public Vector3 Front { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 Right { get; private set; }

        // Constructor updated to accept initial transformation and texture path
        public GameObject(List<Vector3> vert, uint[] ind, List<Vector2> crd, string path,
                          Vector3 initialPosition = default, // Default to (0,0,0)
                          Vector3 initialRotation = default, // Default to (0,0,0) radians
                          Vector3 initialScale = default)    // Default to (1,1,1)
        {
            vertices = vert ?? new List<Vector3>(); // Use null-coalescing for safety
            indices = ind ?? Array.Empty<uint>();
            texCoords = crd ?? new List<Vector2>();
            texturePath = path;

            // --- Initialize Transformation ---
            Position = initialPosition;
            Rotation = initialRotation;
            // Handle default scale: if Vector3.Zero is passed, set to (1,1,1)
            Scale = (initialScale == Vector3.Zero) ? Vector3.One : initialScale;

            // Calculate initial direction vectors based on initial rotation
            UpdateVectors();

            // --- Setup OpenGL resources ---
            setup_buffers();
            setup_texture(texturePath); // Pass the stored path
        }

        // Calculates and returns the Model matrix for this object
        public Matrix4 GetModelMatrix()
        {
            // Order: Scale -> Rotate -> Translate
            Matrix4 scaleMatrix = Matrix4.CreateScale(Scale);

            // Create rotation matrix from Euler angles (remember Rotation is in radians)
            // Order YXZ is common for FPS/Third-person views (Yaw, Pitch, Roll)
            Matrix4 rotationMatrix = Matrix4.CreateRotationY(Rotation.Y) *
                                     Matrix4.CreateRotationX(Rotation.X) *
                                     Matrix4.CreateRotationZ(Rotation.Z);

            Matrix4 translationMatrix = Matrix4.CreateTranslation(Position);

            // Combine them: model = translation * rotation * scale
            return scaleMatrix * rotationMatrix * translationMatrix;
        }

        // Updates the Front, Up, and Right vectors based on the current Rotation (Euler angles)
        public void UpdateVectors()
        {
            // Calculate new Front vector using trigonometry based on Yaw and Pitch
            // Note: We assume standard coordinate system (Y-up) and Yaw around Y, Pitch around X
            float pitch = Rotation.X;
            float yaw = Rotation.Y;
            // Roll (Rotation.Z) doesn't affect the 'Front' direction in this common setup

            Front = new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw), // X component
                MathF.Sin(pitch),                  // Y component
                MathF.Cos(pitch) * -MathF.Cos(yaw) // Z component (negative because +Z is often 'out of screen')
            );
            Front = Vector3.Normalize(Front);

            // Recalculate Right vector as cross product of Front and World Up
            // Using World Up (0,1,0) prevents unwanted rolling when looking straight up/down
            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));

            // Recalculate object's Up vector
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));

            // TODO: If you need Roll (Rotation.Z) to visually affect the object's orientation,
            // you would typically apply an additional rotation around the 'Front' axis
            // *after* calculating Front, Right, Up based on Pitch and Yaw. This can be done
            // by rotating the calculated 'Up' and 'Right' vectors around the 'Front' axis
            // by the Roll angle. For basic movement, Pitch and Yaw are often sufficient.
        }

        // --- Existing OpenGL Setup Methods ---

        private void setup_buffers()
        {
            if (vertices.Count == 0 || indices.Length == 0) return; // Don't create buffers if no data

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // Vertex Buffer
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0); // Use direct state access if preferred (GL.EnableVertexArrayAttrib(VAO, 0);)

            // Texture Coordinate Buffer (if texCoords exist)
            if (texCoords.Count > 0)
            {
                textureVBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(1); // Use direct state access if preferred (GL.EnableVertexArrayAttrib(VAO, 1);)
            }

            // Element Buffer
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Unbind buffers (optional but good practice)
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            // Do NOT unbind EBO while VAO is bound, VAO stores the EBO binding.
            // GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // Remove this line
        }

        private void setup_texture(String path)
        {
            if (string.IsNullOrEmpty(path) || texCoords.Count == 0) return; // No texture if no path or coords

            textureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0); // Activate unit 0 (can be changed)
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            // Texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Use mipmaps for better quality
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Load image using StbImageSharp
            try
            {
                StbImage.stbi_set_flip_vertically_on_load(1); // Flip because OpenGL UV origin is bottom-left
                using (Stream stream = File.OpenRead("../../../Textures/" + path))
                {
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    if (image != null && image.Data != null)
                    {
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); // Generate mipmaps
                    }
                    else
                    {
                         Console.WriteLine($"Failed to load texture: {path}. Image data is null.");
                         // Maybe load a default fallback texture here?
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load texture '{path}': {e.Message}");
                // Consider loading a default fallback texture here as well
            }

            // Unbind texture (optional)
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void del_resources()
        {
            GL.DeleteBuffer(VBO);
            if (textureVBO != 0) GL.DeleteBuffer(textureVBO); // Delete tex coord buffer if it exists
            GL.DeleteBuffer(EBO);
            GL.DeleteVertexArray(VAO); // <<< --- IMPORTANT: Delete the VAO!
            if (textureID != 0) GL.DeleteTexture(textureID); // Delete texture if it exists
        }

        public void draw()
        {
            if (VAO == 0 || indices.Length == 0) return; // Don't draw if not properly initialized

            // Bind Texture (if it exists)
            if (textureID != 0)
            {
                 GL.ActiveTexture(TextureUnit.Texture0); // Ensure correct texture unit is active
                 GL.BindTexture(TextureTarget.Texture2D, textureID);
            }

            // Bind VAO (which includes VBO bindings, EBO binding, and attribute pointers)
            GL.BindVertexArray(VAO);

            // Draw command
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            // Unbind VAO (optional but good practice)
            GL.BindVertexArray(0);

            // Unbind Texture (optional)
            if (textureID != 0)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
    }
}
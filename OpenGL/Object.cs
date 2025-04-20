using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp; // Keep for texture loading

namespace OpenGL
{
    class GameObject
    {
        // --- OpenGL Handles ---
        private int VAO;
        private int VBO_Vertices; // Renamed for clarity
        private int VBO_TexCoords; // Renamed for clarity
        private int VBO_Normals;   // New VBO for normals
        private int EBO;
        private int textureID;

        // --- Data ---
        // Store counts for drawing and cleanup
        private int indicesCount;
        private bool hasTexCoords = false;
        private bool hasNormals = false;
        private string texturePath; // Keep path for texture loading

        // --- Transformation Properties ---
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; } // Euler angles (Pitch, Yaw, Roll) in RADIANS
        public Vector3 Scale { get; set; }
        public Vector3 Front { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 Right { get; private set; }

        // --- Constructor for Loaded Models ---
        public GameObject(ModelLoader.ModelData modelData, string textureFilePath,
                          Vector3 initialPosition = default,
                          Vector3 initialRotation = default,
                          Vector3 initialScale = default)
        {
            this.texturePath = textureFilePath;

            // --- Initialize Transformation ---
            Position = initialPosition;
            Rotation = initialRotation;
            Scale = (initialScale == Vector3.Zero) ? Vector3.One : initialScale;
            UpdateVectors(); // Calculate initial direction vectors

            // --- Setup OpenGL Buffers ---
            // Pass the loaded data to setup_buffers
            setup_buffers(modelData);

            // --- Setup Texture ---
            // Load texture only if path is provided and model has texture coords
            if (!string.IsNullOrEmpty(texturePath) && hasTexCoords)
            {
                setup_texture(texturePath);
            }
            else
            {
                Console.WriteLine("GameObject Info: No texture path provided or model lacks texture coordinates. Texture not loaded.");
                textureID = 0; // Ensure textureID is 0 if not loaded
            }
        }

        // --- DEPRECATED CONSTRUCTOR (Keep temporarily or remove) ---

        public GameObject(List<Vector3> vert, uint[] ind, List<Vector2> crd, string path,
                  Vector3 initialPosition = default, Vector3 initialRotation = default, Vector3 initialScale = default)
        {
            this.texturePath = path;

            // --- Initialize Transformation ---
            Position = initialPosition;
            Rotation = initialRotation;
            Scale = (initialScale == Vector3.Zero) ? Vector3.One : initialScale;
            UpdateVectors(); // Calculate initial direction vectors

            // --- Создаем ModelData "на лету" для передачи в setup_buffers ---
            // Важно: Нормалей здесь нет (null)!
            ModelLoader.ModelData simpleModelData = new ModelLoader.ModelData
            {
                Vertices = vert,
                TexCoords = crd,
                Normals = null, // <--- Ключевой момент: нет нормалей
                Indices = ind.ToList()
            };

            // --- Setup OpenGL Buffers ---
            setup_buffers(simpleModelData); // Вызываем тот же метод настройки

            // --- Setup Texture ---
            // Загружаем текстуру, если есть путь и текстурные координаты
            if (!string.IsNullOrEmpty(texturePath) && hasTexCoords) // hasTexCoords установится в setup_buffers
            {
                setup_texture(texturePath);
            }
            else
            {
                textureID = 0;
            }
        }


        public Matrix4 GetModelMatrix()
        {
            Matrix4 scaleMatrix = Matrix4.CreateScale(Scale);
            Matrix4 rotationMatrix = Matrix4.CreateRotationY(Rotation.Y) *
                                     Matrix4.CreateRotationX(Rotation.X) *
                                     Matrix4.CreateRotationZ(Rotation.Z);
            Matrix4 translationMatrix = Matrix4.CreateTranslation(Position);
            return scaleMatrix * rotationMatrix * translationMatrix;
        }

        public void UpdateVectors()
        {
            float pitch = Rotation.X;
            float yaw = Rotation.Y;
            Front = new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * -MathF.Cos(yaw)
            );
            Front = Vector3.Normalize(Front);
            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }


        // --- OpenGL Setup Methods ---

        // Updated to accept ModelData
        private void setup_buffers(ModelLoader.ModelData modelData)
        {
            // Check if data is valid
            if (modelData.Vertices == null || modelData.Vertices.Count == 0 ||
                modelData.Indices == null || modelData.Indices.Count == 0)
            {
                Console.WriteLine("Error: Model data is invalid (missing vertices or indices). Buffers not created.");
                return;
            }

            indicesCount = modelData.Indices.Count; // Store count for drawing
            //hasTexCoords = modelData.TexCoords != null && modelData.TexCoords.Count == modelData.Vertices.Count; // Check consistency
            hasTexCoords = true;
            hasNormals = modelData.Normals != null && modelData.Normals.Count == modelData.Vertices.Count; // Check consistency

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // 1. VBO for Vertex Positions (Attribute Location 0)
            VBO_Vertices = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertices);
            GL.BufferData(BufferTarget.ArrayBuffer, modelData.Vertices.Count * Vector3.SizeInBytes,
                          modelData.Vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);

            // 2. VBO for Texture Coordinates (Attribute Location 1) - If they exist
            if (hasTexCoords)
            {
                VBO_TexCoords = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_TexCoords);
                GL.BufferData(BufferTarget.ArrayBuffer, modelData.TexCoords.Count * Vector2.SizeInBytes,
                              modelData.TexCoords.ToArray(), BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(1);
            }
            else { VBO_TexCoords = 0; } // Ensure handle is 0 if not created

            // 3. VBO for Normals (Attribute Location 2) - If they exist
            if (hasNormals)
            {
                VBO_Normals = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Normals);
                GL.BufferData(BufferTarget.ArrayBuffer, modelData.Normals.Count * Vector3.SizeInBytes,
                              modelData.Normals.ToArray(), BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0); // Location 2
                GL.EnableVertexAttribArray(2); // Enable Location 2
            }
            else { VBO_Normals = 0; } // Ensure handle is 0 if not created

            // 4. EBO for Indices
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indicesCount * sizeof(uint),
                          modelData.Indices.ToArray(), BufferUsageHint.StaticDraw);

            // Unbind VAO then Buffer (VBO was implicitly unbound by binding EBO to ElementArrayBuffer target)
            GL.BindVertexArray(0);
            // EBO remains bound to the VAO state, do not unbind it here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind VBO from ArrayBuffer target

            Console.WriteLine($"GameObject Buffers Created: Vertices={modelData.Vertices.Count}, Indices={indicesCount}, HasTexCoords={hasTexCoords}, HasNormals={hasNormals}");
        }

        // setup_texture remains largely the same
        private void setup_texture(String path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Texture setup skipped: No path provided.");
                textureID = 0;
                return;
            }

            textureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            try
            {
                StbImage.stbi_set_flip_vertically_on_load(1);
                // Construct the full path relative to the executable or use a fixed base path
                // Adjust "../../../Textures/" based on your actual project structure relative to executable
                string absolutePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Textures/", path));

                if (!File.Exists(absolutePath))
                {
                    Console.WriteLine($"Error: Texture file not found at resolved path: {absolutePath}");
                    GL.BindTexture(TextureTarget.Texture2D, 0); // Unbind
                    GL.DeleteTexture(textureID); // Delete the invalid texture handle
                    textureID = 0; // Set ID to 0
                    return;
                }


                using (Stream stream = File.OpenRead(absolutePath))
                {
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    if (image != null && image.Data != null)
                    {
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                        Console.WriteLine($"Texture loaded successfully: {path} ({image.Width}x{image.Height})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load texture image data: {path}");
                        GL.BindTexture(TextureTarget.Texture2D, 0); // Unbind
                        GL.DeleteTexture(textureID); // Delete the invalid texture handle
                        textureID = 0; // Set ID to 0
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load texture '{path}': {e.Message}");
                if (textureID != 0) GL.DeleteTexture(textureID); // Clean up if exception occurred after GenTexture
                textureID = 0;
            }
            finally // Ensure texture is unbound unless successfully loaded
            {
                if (textureID != 0) GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public void del_resources()
        {
            // Delete buffers, checking if they were created (handle != 0)
            if (VBO_Vertices != 0) GL.DeleteBuffer(VBO_Vertices);
            if (VBO_TexCoords != 0) GL.DeleteBuffer(VBO_TexCoords);
            if (VBO_Normals != 0) GL.DeleteBuffer(VBO_Normals); // Delete normal buffer
            if (EBO != 0) GL.DeleteBuffer(EBO);
            if (VAO != 0) GL.DeleteVertexArray(VAO);
            if (textureID != 0) GL.DeleteTexture(textureID);

            // Reset handles to 0 after deletion
            VAO = VBO_Vertices = VBO_TexCoords = VBO_Normals = EBO = textureID = 0;
            indicesCount = 0;
        }

        public void draw()
        {
            // Don't draw if VAO wasn't created or no indices
            if (VAO == 0 || indicesCount == 0) return;

            // Bind Texture (if it was loaded successfully)
            if (textureID != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureID);
            }

            // Bind VAO (this sets up all attribute pointers and the EBO binding)
            GL.BindVertexArray(VAO);

            // Draw command using the stored index count
            GL.DrawElements(PrimitiveType.Triangles, indicesCount, DrawElementsType.UnsignedInt, 0);

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
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenTK.Graphics.OpenGL4;
//using OpenTK.Mathematics;
//using OpenTK.Windowing.Common;
//using OpenTK.Windowing.Desktop;
//using OpenTK.Windowing.GraphicsLibraryFramework;
//using StbImageSharp;

//namespace OpenGL
//{
//    class GameObject
//    {
//        // --- OpenGL-related fields ---
//        private int VAO, VBO, EBO;
//        private int textureID, textureVBO;

//        // --- Geometry and Texture Data ---
//        private List<Vector3> vertices;
//        private uint[] indices;
//        List<Vector2> texCoords;
//        private string texturePath; // Store path for potential reloading/debugging

//        // --- Transformation Properties ---
//        public Vector3 Position { get; set; }
//        public Vector3 Rotation { get; set; } // Euler angles (Pitch, Yaw, Roll) in RADIANS
//        public Vector3 Scale { get; set; }

//        // --- Direction Vectors (calculated from Rotation) ---
//        public Vector3 Front { get; private set; }
//        public Vector3 Up { get; private set; }
//        public Vector3 Right { get; private set; }

//        // Constructor updated to accept initial transformation and texture path
//        public GameObject(List<Vector3> vert, uint[] ind, List<Vector2> crd, string path,
//                          Vector3 initialPosition = default, // Default to (0,0,0)
//                          Vector3 initialRotation = default, // Default to (0,0,0) radians
//                          Vector3 initialScale = default)    // Default to (1,1,1)
//        {
//            vertices = vert ?? new List<Vector3>(); // Use null-coalescing for safety
//            indices = ind ?? Array.Empty<uint>();
//            texCoords = crd ?? new List<Vector2>();
//            texturePath = path;

//            // --- Initialize Transformation ---
//            Position = initialPosition;
//            Rotation = initialRotation;
//            // Handle default scale: if Vector3.Zero is passed, set to (1,1,1)
//            Scale = (initialScale == Vector3.Zero) ? Vector3.One : initialScale;

//            // Calculate initial direction vectors based on initial rotation
//            UpdateVectors();

//            // --- Setup OpenGL resources ---
//            setup_buffers();
//            setup_texture(texturePath); // Pass the stored path
//        }

//        // Calculates and returns the Model matrix for this object
//        public Matrix4 GetModelMatrix()
//        {
//            // Order: Scale -> Rotate -> Translate
//            Matrix4 scaleMatrix = Matrix4.CreateScale(Scale);

//            // Create rotation matrix from Euler angles (remember Rotation is in radians)
//            // Order YXZ is common for FPS/Third-person views (Yaw, Pitch, Roll)
//            Matrix4 rotationMatrix = Matrix4.CreateRotationY(Rotation.Y) *
//                                     Matrix4.CreateRotationX(Rotation.X) *
//                                     Matrix4.CreateRotationZ(Rotation.Z);

//            Matrix4 translationMatrix = Matrix4.CreateTranslation(Position);

//            // Combine them: model = translation * rotation * scale
//            return scaleMatrix * rotationMatrix * translationMatrix;
//        }

//        // Updates the Front, Up, and Right vectors based on the current Rotation (Euler angles)
//        public void UpdateVectors()
//        {
//            // Calculate new Front vector using trigonometry based on Yaw and Pitch
//            // Note: We assume standard coordinate system (Y-up) and Yaw around Y, Pitch around X
//            float pitch = Rotation.X;
//            float yaw = Rotation.Y;
//            // Roll (Rotation.Z) doesn't affect the 'Front' direction in this common setup

//            Front = new Vector3(
//                MathF.Cos(pitch) * MathF.Sin(yaw), // X component
//                MathF.Sin(pitch),                  // Y component
//                MathF.Cos(pitch) * -MathF.Cos(yaw) // Z component (negative because +Z is often 'out of screen')
//            );
//            Front = Vector3.Normalize(Front);

//            // Recalculate Right vector as cross product of Front and World Up
//            // Using World Up (0,1,0) prevents unwanted rolling when looking straight up/down
//            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));

//            // Recalculate object's Up vector
//            Up = Vector3.Normalize(Vector3.Cross(Right, Front));

//            // TODO: If you need Roll (Rotation.Z) to visually affect the object's orientation,
//            // you would typically apply an additional rotation around the 'Front' axis
//            // *after* calculating Front, Right, Up based on Pitch and Yaw. This can be done
//            // by rotating the calculated 'Up' and 'Right' vectors around the 'Front' axis
//            // by the Roll angle. For basic movement, Pitch and Yaw are often sufficient.
//        }

//        // --- Existing OpenGL Setup Methods ---

//        private void setup_buffers()
//        {
//            if (vertices.Count == 0 || indices.Length == 0) return; // Don't create buffers if no data

//            VAO = GL.GenVertexArray();
//            GL.BindVertexArray(VAO);

//            // Vertex Buffer
//            VBO = GL.GenBuffer();
//            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
//            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);
//            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
//            GL.EnableVertexAttribArray(0); // Use direct state access if preferred (GL.EnableVertexArrayAttrib(VAO, 0);)

//            // Texture Coordinate Buffer (if texCoords exist)
//            if (texCoords.Count > 0)
//            {
//                textureVBO = GL.GenBuffer();
//                GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
//                GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
//                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
//                GL.EnableVertexAttribArray(1); // Use direct state access if preferred (GL.EnableVertexArrayAttrib(VAO, 1);)
//            }

//            // Element Buffer
//            EBO = GL.GenBuffer();
//            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
//            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

//            // Unbind buffers (optional but good practice)
//            GL.BindVertexArray(0);
//            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
//            // Do NOT unbind EBO while VAO is bound, VAO stores the EBO binding.
//            // GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // Remove this line
//        }

//        private void setup_texture(String path)
//        {
//            if (string.IsNullOrEmpty(path) || texCoords.Count == 0) return; // No texture if no path or coords

//            textureID = GL.GenTexture();
//            GL.ActiveTexture(TextureUnit.Texture0); // Activate unit 0 (can be changed)
//            GL.BindTexture(TextureTarget.Texture2D, textureID);

//            // Texture parameters
//            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
//            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
//            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Use mipmaps for better quality
//            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

//            // Load image using StbImageSharp
//            try
//            {
//                StbImage.stbi_set_flip_vertically_on_load(1); // Flip because OpenGL UV origin is bottom-left
//                using (Stream stream = File.OpenRead("../../../Textures/" + path))
//                {
//                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
//                    if (image != null && image.Data != null)
//                    {
//                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
//                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); // Generate mipmaps
//                    }
//                    else
//                    {
//                         Console.WriteLine($"Failed to load texture: {path}. Image data is null.");
//                         // Maybe load a default fallback texture here?
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine($"Failed to load texture '{path}': {e.Message}");
//                // Consider loading a default fallback texture here as well
//            }

//            // Unbind texture (optional)
//            GL.BindTexture(TextureTarget.Texture2D, 0);
//        }

//        public void del_resources()
//        {
//            GL.DeleteBuffer(VBO);
//            if (textureVBO != 0) GL.DeleteBuffer(textureVBO); // Delete tex coord buffer if it exists
//            GL.DeleteBuffer(EBO);
//            GL.DeleteVertexArray(VAO); // <<< --- IMPORTANT: Delete the VAO!
//            if (textureID != 0) GL.DeleteTexture(textureID); // Delete texture if it exists
//        }

//        public void draw()
//        {
//            if (VAO == 0 || indices.Length == 0) return; // Don't draw if not properly initialized

//            // Bind Texture (if it exists)
//            if (textureID != 0)
//            {
//                 GL.ActiveTexture(TextureUnit.Texture0); // Ensure correct texture unit is active
//                 GL.BindTexture(TextureTarget.Texture2D, textureID);
//            }

//            // Bind VAO (which includes VBO bindings, EBO binding, and attribute pointers)
//            GL.BindVertexArray(VAO);

//            // Draw command
//            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

//            // Unbind VAO (optional but good practice)
//            GL.BindVertexArray(0);

//            // Unbind Texture (optional)
//            if (textureID != 0)
//            {
//                GL.BindTexture(TextureTarget.Texture2D, 0);
//            }
//        }
//    }
//}
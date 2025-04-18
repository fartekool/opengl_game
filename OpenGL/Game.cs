using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL; // Consider removing if not used
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
// Removed: using static OpenTK.Graphics.OpenGL.GL; // Avoid static using for GL

namespace OpenGL
{
    internal class Game : GameWindow
    {
        Shader shaderProgram;
        Camera camera;

        // --- Game Objects ---
        GameObject Ship;
        // GameObject Saturn; // Removed if not used




        private List<GameObject> asteroids;
        private float spawnTimer = 0.0f;
        private float spawnInterval = 1.5f;
        private Random random;
        private float asteroidSpeed = 4.0f;
        private float spawnHeight = 25.0f;
        private float spawnRangeX = 15.0f;
        private float spawnRangeZ = 15.0f;
        private float despawnHeight = -10.0f;

        private float shipBoundingRadius = 0.8f; // Примерный радиус корабля (подберите по размеру куба/модели)
        private float asteroidBoundingRadius = 0.8f; // Примерный радиус астероида

        private bool isGameOver = false;
        private string asteroidTexturePath = "ast.jpg";







        // Walls (Consider a Skybox class later)
        GameObject Wall1;
        GameObject Wall2;
        GameObject Wall3;
        GameObject Wall4;
        GameObject Wall5;
        GameObject Wall6;

        // GameObject Box; // Removed if not used

        int width, height;

        // Ship movement parameters
        private float shipMoveSpeed = 5.0f;
        private float shipTurnSpeed = MathHelper.PiOver2; // Radians per second (90 degrees/sec)

        // --- Geometry Data (keep as before, maybe move to separate files later) ---
        private List<Vector3> BoxVertices = new List<Vector3>() { /* ... */ };
        private List<Vector2> BoxTexCoords = new List<Vector2>() { /* ... */ };

        private List<Vector3> PlaneVertices = new List<Vector3>() // Renamed from SaturnVertices for clarity
        {
            // Defined to be centered at origin on XY plane, facing +Z
            new Vector3(-0.5f, -0.5f, 0.0f), // Bottom-left
            new Vector3( 0.5f, -0.5f, 0.0f), // Bottom-right
            new Vector3( 0.5f,  0.5f, 0.0f), // Top-right
            new Vector3(-0.5f,  0.5f, 0.0f)  // Top-left
        };
        private uint[] PlaneIndices = // Renamed from SaturnIndices
        {
            0, 1, 2, // First triangle
            2, 3, 0  // Second triangle
        };
        private List<Vector2> PlaneTexCoords = new List<Vector2>() // Renamed from SaturnTexCoords
        {
            new Vector2(0f, 0f), // Corresponds to vertex 0
            new Vector2(1f, 0f), // Corresponds to vertex 1
            new Vector2(1f, 1f), // Corresponds to vertex 2
            new Vector2(0f, 1f)  // Corresponds to vertex 3
        };

        private List<Vector3> shipVertices = new List<Vector3>() // Keep your cube vertices
        {
            // Cube vertices centered around (0,0,0)
            new Vector3(-0.5f, -0.5f, -0.5f), // 0 Front-Bottom-Left
            new Vector3( 0.5f, -0.5f, -0.5f), // 1 Front-Bottom-Right
            new Vector3( 0.5f,  0.5f, -0.5f), // 2 Front-Top-Right
            new Vector3(-0.5f,  0.5f, -0.5f), // 3 Front-Top-Left
            new Vector3(-0.5f, -0.5f,  0.5f), // 4 Back-Bottom-Left
            new Vector3( 0.5f, -0.5f,  0.5f), // 5 Back-Bottom-Right
            new Vector3( 0.5f,  0.5f,  0.5f), // 6 Back-Top-Right
            new Vector3(-0.5f,  0.5f,  0.5f)  // 7 Back-Top-Left
        };

        private uint[] shipIndices = // Keep your cube indices
        {
            // Front face (using vertices 0, 1, 2, 3) - Assuming CCW winding
            0, 1, 2, 2, 3, 0,
            // Back face (using vertices 4, 5, 6, 7)
            5, 4, 7, 7, 6, 5, // Adjusted winding for consistency if needed
            // Left face (using vertices 4, 0, 3, 7)
            4, 0, 3, 3, 7, 4,
            // Right face (using vertices 1, 5, 6, 2)
            1, 5, 6, 6, 2, 1,
            // Top face (using vertices 3, 2, 6, 7)
            3, 2, 6, 6, 7, 3,
            // Bottom face (using vertices 4, 5, 1, 0)
            4, 5, 1, 1, 0, 4
        };

        // TODO: Create proper Texture Coordinates for the Cube!
        // This is just a placeholder and will likely look wrong.
        // You need 24 tex coords if each vertex is unique per face,
        // or adjust vertices/indices for shared tex coords.
        private List<Vector2> shipTexCoords = new List<Vector2>()
        {
            // Example for one face (repeat/adjust for others)
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            // Add coordinates for the other 16 vertices based on your UV map
             new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
             new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
        };


        public Game(int width, int height) : base
        (GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(width, height), Title = "OpenGL Third Person" }) // Set title
        {
            // No need to call CenterWindow here, it's handled by base constructor if Size is set
            this.width = width;
            this.height = height;
        }

        protected override void OnLoad()
        {
            base.OnLoad(); // Call base.OnLoad first

            GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f); // Darker background
            GL.Enable(EnableCap.DepthTest); // Enable depth testing once

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            // --- Create Shader ---
            shaderProgram = new Shader("shader.vert", "shader.frag"); // Assuming Shader constructor handles loading/linking

            // --- Create Ship ---
            Ship = new GameObject(
                shipVertices,
                shipIndices,
                shipTexCoords, // Use the cube tex coords (even if placeholder)
                "ship3.png",    // Texture file
                new Vector3(0, 0, 0), // Initial Position
                new Vector3(0, 0, 0), // Initial Rotation (radians)
                new Vector3(1, 1, 1)  // Initial Scale (adjusted cube size)
            );

            // --- Create Walls (Example for Skybox-like setup) ---
            // Use the Plane geometry for walls
            float skyboxSize = 100.0f; // How far away the walls are
            float halfSize = skyboxSize / 2.0f;

            // Positions for a cube centered at origin
            Vector3 frontPos = new Vector3(0, 0, -halfSize);
            Vector3 backPos = new Vector3(0, 0, halfSize);
            Vector3 leftPos = new Vector3(-halfSize, 0, 0);
            Vector3 rightPos = new Vector3(halfSize, 0, 0);
            Vector3 topPos = new Vector3(0, halfSize, 0);
            Vector3 bottomPos = new Vector3(0, -halfSize, 0);

            // Rotations to make planes face outwards/inwards (adjust as needed)
            Vector3 frontRot = new Vector3(0, 0, 0); // Faces -Z
            Vector3 backRot = new Vector3(0, MathHelper.DegreesToRadians(180), 0); // Faces +Z
            Vector3 leftRot = new Vector3(0, MathHelper.DegreesToRadians(90), 0); // Faces -X
            Vector3 rightRot = new Vector3(0, MathHelper.DegreesToRadians(-90), 0); // Faces +X
            Vector3 topRot = new Vector3(MathHelper.DegreesToRadians(-90), 0, 0); // Faces +Y
            Vector3 bottomRot = new Vector3(MathHelper.DegreesToRadians(90), 0, 0); // Faces -Y

            Vector3 wallScale = new Vector3(skyboxSize, skyboxSize, 1); // Scale the plane

            Wall1 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", frontPos, frontRot, wallScale); // Front
            Wall2 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", backPos, backRot, wallScale);   // Back
            Wall3 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", leftPos, leftRot, wallScale);   // Left
            Wall4 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", rightPos, rightRot, wallScale); // Right
            Wall5 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", topPos, topRot, wallScale);     // Top
            Wall6 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", bottomPos, bottomRot, wallScale); // Bottom


            // --- Create Camera ---
            // Initialize camera slightly behind the ship's starting position
            Vector3 initialCameraOffset = new Vector3(0, 2, 5); // Behind, slightly up
            camera = new Camera(width, height, Ship.Position - initialCameraOffset); // Start relative to ship
            // We'll make the camera follow the ship in OnUpdateFrame

            CursorState = CursorState.Grabbed; // Hide and lock cursor
        }

        protected override void OnUnload()
        {
            // Clean up resources
            Ship?.del_resources(); // Use null-conditional operator
            Wall1?.del_resources();
            Wall2?.del_resources();
            Wall3?.del_resources();
            Wall4?.del_resources();
            Wall5?.del_resources();
            Wall6?.del_resources();
            shaderProgram?.DeleteShader();

            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shaderProgram.UseShader();

            // --- Setup Camera Matrices (View and Projection) ---
            // Camera position and orientation will be updated in OnUpdateFrame
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection(); // Make sure aspect ratio is updated on resize

            // Send View and Projection matrices to the shader (ONCE per frame)
            // Assuming your shader uniform names are "view" and "projection"
            // Use transpose: false unless your shader expects row-major
            shaderProgram.SetMatrix4("view", view, true);
            shaderProgram.SetMatrix4("projection", projection, true);


            // --- Render Ship ---
            Matrix4 shipModelMatrix = Ship.GetModelMatrix();
            shaderProgram.SetMatrix4("model", shipModelMatrix, true); // Send ship's model matrix
            // Optional: Set texture uniform if your shader needs it (e.g., uniform sampler2D texture0;)
            // shaderProgram.SetInt("texture0", 0); // Assuming ship uses texture unit 0
            Ship.draw();

            // --- Render Walls/Skybox ---
            // Disable depth writing for skybox to ensure it's always behind everything
            // but keep depth testing enabled so closer objects obscure it.
            GL.DepthMask(false);

            shaderProgram.SetMatrix4("model", Wall1.GetModelMatrix(), true); Wall1.draw();
            shaderProgram.SetMatrix4("model", Wall2.GetModelMatrix(), true); Wall2.draw();
            shaderProgram.SetMatrix4("model", Wall3.GetModelMatrix(), true); Wall3.draw();
            shaderProgram.SetMatrix4("model", Wall4.GetModelMatrix(), true); Wall4.draw();
            shaderProgram.SetMatrix4("model", Wall5.GetModelMatrix(), true); Wall5.draw();
            shaderProgram.SetMatrix4("model", Wall6.GetModelMatrix(), true); Wall6.draw();

            GL.DepthMask(true); // Re-enable depth writing for other objects

            // --- Swap Buffers ---
            Context.SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (!IsFocused) // Optional: pause game logic when window loses focus
            {
                return;
            }

            float deltaTime = (float)args.Time; // Time since last frame

            // --- Input Handling ---
            KeyboardState input = KeyboardState;
            MouseState mouse = MouseState; // Get mouse state for camera control

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
                return; // Exit early
            }

            // --- Ship Movement ---
            bool shipMoved = false;
            bool shipRotated = false;

            // Forward/Backward movement along Ship.Front
            if (input.IsKeyDown(Keys.W))
            {
                Ship.Position -= Ship.Front * shipMoveSpeed * deltaTime;
                shipMoved = true;
            }
            if (input.IsKeyDown(Keys.S))
            {
                Ship.Position += Ship.Front * shipMoveSpeed * deltaTime;
                shipMoved = true;
            }

            // Turning (Yaw) - rotates the ship around its Up axis
            if (input.IsKeyDown(Keys.A))
            {
                // Negative rotation around Y for left turn
                //Ship.Rotation = new Vector3(Ship.Rotation.X, Ship.Rotation.Y + shipTurnSpeed * deltaTime, Ship.Rotation.Z);
                Ship.Position += Ship.Right * shipMoveSpeed * deltaTime;
                shipRotated = true;
            }
            if (input.IsKeyDown(Keys.D))
            {
                // Positive rotation around Y for right turn
                //Ship.Rotation = new Vector3(Ship.Rotation.X, Ship.Rotation.Y - shipTurnSpeed * deltaTime, Ship.Rotation.Z);
                Ship.Position -= Ship.Right * shipMoveSpeed * deltaTime;
                shipRotated = true;
            }

            // Optional: Strafing along Ship.Right
            /*
            if (input.IsKeyDown(Keys.Q))
            {
                Ship.Position -= Ship.Right * shipMoveSpeed * deltaTime;
                shipMoved = true;
            }
            if (input.IsKeyDown(Keys.E))
            {
                Ship.Position += Ship.Right * shipMoveSpeed * deltaTime;
                shipMoved = true;
            }
            */

            // Update ship's direction vectors ONLY if it rotated
            if (shipRotated)
            {
                Ship.UpdateVectors();
            }

            // --- Camera Update ---
            // Now, update the camera based on the *new* ship position and orientation
            // We need to add a method to Camera.cs to handle this logic.
            // For now, let's just pass the necessary info.
            camera.UpdateThirdPerson(Ship, mouse, deltaTime); // Assuming this method exists/will exist

            // --- End of Update ---
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;

            // Update camera's aspect ratio
            if (camera != null)
            {
                camera.AspectRatio = (float)width / height;
            }
        }

        // Helper method in Shader class (add this to your Shader.cs)
        /*
        public void SetMatrix4(string name, Matrix4 matrix, bool transpose = false)
        {
            int location = GL.GetUniformLocation(shaderHandle, name);
            if (location != -1)
            {
                GL.UniformMatrix4(location, transpose, ref matrix);
            }
            else
            {
                // Optional: Log warning if uniform not found
                // Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }
        public void SetInt(string name, int value) { ... }
        public void SetFloat(string name, float value) { ... }
        // etc. for other uniform types
        */
    }

    // --- Shader Class (Example with Constructor and SetMatrix4) ---
    public class Shader
    {
        public readonly int shaderHandle; // Make readonly after creation

        // Constructor that takes shader file paths
        public Shader(string vertexPath, string fragmentPath)
        {
            // 1. Load source code
            string vertexShaderSource = LoadShaderSource(vertexPath);
            string fragmentShaderSource = LoadShaderSource(fragmentPath);

            // 2. Create and Compile Vertex Shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            CompileShader(vertexShader, "Vertex");

            // 3. Create and Compile Fragment Shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            CompileShader(fragmentShader, "Fragment");

            // 4. Create Shader Program and Link
            shaderHandle = GL.CreateProgram();
            GL.AttachShader(shaderHandle, vertexShader);
            GL.AttachShader(shaderHandle, fragmentShader);
            LinkProgram(shaderHandle);

            // 5. Detach and Delete individual shaders (they are linked now)
            GL.DetachShader(shaderHandle, vertexShader);
            GL.DetachShader(shaderHandle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);
        }

        // --- Shader Loading/Compilation/Linking Helpers ---
        private static string LoadShaderSource(string filepath)
        {
            string shaderSource = "";
            try
            {
                // Adjust path as needed for your project structure
                using (StreamReader reader = new StreamReader(Path.Combine("../../../Shaders", filepath)))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load shader source file: {filepath} | {e.Message}");
            }
            return shaderSource;
        }

        private static void CompileShader(int shader, string type)
        {
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"ERROR::SHADER::COMPILATION_FAILED ({type})\n{infoLog}");
            }
        }

        private static void LinkProgram(int program)
        {
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");
            }
        }
        // --- End Helpers ---

        public void UseShader()
        {
            GL.UseProgram(shaderHandle);
        }

        public void DeleteShader()
        {
            GL.DeleteProgram(shaderHandle);
        }

        // --- Uniform Setting Methods ---
        public void SetMatrix4(string name, Matrix4 matrix, bool transpose = false)
        {
            // It's slightly more efficient to get location once and store it,
            // but getting it each time is fine for smaller projects.
            int location = GL.GetUniformLocation(shaderHandle, name);
            if (location != -1)
            {
                GL.UniformMatrix4(location, transpose, ref matrix);
            }
            else
            {
                Console.WriteLine($"Warning: Uniform '{name}' not found.");
            }
        }

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(shaderHandle, name);
            if (location != -1) GL.Uniform1(location, value);
            else Console.WriteLine($"Warning: Uniform '{name}' not found.");
        }

        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(shaderHandle, name);
            if (location != -1) GL.Uniform1(location, value);
            else Console.WriteLine($"Warning: Uniform '{name}' not found.");
        }

        public void SetVector3(string name, Vector3 value)
        {
            int location = GL.GetUniformLocation(shaderHandle, name);
            if (location != -1) GL.Uniform3(location, ref value);
            else Console.WriteLine($"Warning: Uniform '{name}' not found.");
        }
        // Add more setters as needed (SetVector2, SetVector4, etc.)
    }
}
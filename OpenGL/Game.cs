using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGL
{
    internal class Game : GameWindow
    {
        int width, height;

        private Shader shaderProgram;
        private Camera camera;

        private GameObject Ship;
        private float shipMoveSpeed = 25.0f;
        string shipTexturePath = "rus.png";
        private float shipBoundingRadius = 2f;



        private List<GameObject> asteroids;
        private float spawnTimer = 0.0f;
        private float spawnInterval = 0.01f / 50f;
        private Random random;
        private float asteroidSpeed;
        private float spawnHeight = 300.0f;
        private float spawnRangeX = 200.0f;
        private float spawnRangeZ = 200.0f;
        private float despawnHeight = -50.0f;
        private string asteroidTexturePath = "ast.jpg";
        private float asteroidBoundingRadius = 0.8f; 

        private bool isGameOver = false;
        

        GameObject Wall1;
        GameObject Wall2;
        GameObject Wall3;
        GameObject Wall4;
        GameObject Wall5;
        GameObject Wall6;

        private List<Vector3> PlaneVertices = new List<Vector3>()
        {
            // Defined to be centered at origin on XY plane, facing +Z
            new Vector3(-0.5f, -0.5f, 0.0f), // Bottom-left
            new Vector3( 0.5f, -0.5f, 0.0f), // Bottom-right
            new Vector3( 0.5f,  0.5f, 0.0f), // Top-right
            new Vector3(-0.5f,  0.5f, 0.0f)  // Top-left
        };
        private uint[] PlaneIndices = 
        {
            0, 1, 2, // First triangle
            2, 3, 0  // Second triangle
        };
        private List<Vector2> PlaneTexCoords = new List<Vector2>() 
        {
            new Vector2(0f, 0f), // Corresponds to vertex 0
            new Vector2(1f, 0f), // Corresponds to vertex 1
            new Vector2(1f, 1f), // Corresponds to vertex 2
            new Vector2(0f, 1f)  // Corresponds to vertex 3
        };


        private List<Vector3> asteroidVertices = new List<Vector3>()
        {
            // Face 1: Front (-Z)
            new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-Left
            new Vector3( 0.5f, -0.5f, -0.5f), // Bottom-Right
            new Vector3( 0.5f,  0.5f, -0.5f), // Top-Right
            new Vector3(-0.5f,  0.5f, -0.5f), // Top-Left

            // Face 2: Back (+Z)
            new Vector3(-0.5f, -0.5f,  0.5f), // Bottom-Left (of the back face)
            new Vector3( 0.5f, -0.5f,  0.5f), // Bottom-Right
            new Vector3( 0.5f,  0.5f,  0.5f), // Top-Right
            new Vector3(-0.5f,  0.5f,  0.5f), // Top-Left

            // Face 3: Left (-X)
            new Vector3(-0.5f, -0.5f,  0.5f), // Bottom-Left (of the left face)
            new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-Right
            new Vector3(-0.5f,  0.5f, -0.5f), // Top-Right
            new Vector3(-0.5f,  0.5f,  0.5f), // Top-Left

            // Face 4: Right (+X)
            new Vector3( 0.5f, -0.5f, -0.5f), // Bottom-Left (of the right face)
            new Vector3( 0.5f, -0.5f,  0.5f), // Bottom-Right
            new Vector3( 0.5f,  0.5f,  0.5f), // Top-Right
            new Vector3( 0.5f,  0.5f, -0.5f), // Top-Left

            // Face 5: Bottom (-Y)
            new Vector3(-0.5f, -0.5f,  0.5f), // Bottom-Left (of the bottom face)
            new Vector3( 0.5f, -0.5f,  0.5f), // Bottom-Right
            new Vector3( 0.5f, -0.5f, -0.5f), // Top-Right ---> Note: This is (X, -0.5, Z)
            new Vector3(-0.5f, -0.5f, -0.5f), // Top-Left  ---> Note: This is (X, -0.5, Z)

            // Face 6: Top (+Y)
            new Vector3(-0.5f,  0.5f, -0.5f), // Bottom-Left (of the top face)
            new Vector3( 0.5f,  0.5f, -0.5f), // Bottom-Right
            new Vector3( 0.5f,  0.5f,  0.5f), // Top-Right ---> Note: This is (X, 0.5, Z)
            new Vector3(-0.5f,  0.5f,  0.5f)  // Top-Left  ---> Note: This is (X, 0.5, Z)
        };
        private uint[] asteroidIndices =
        {
            // Face 1: Front (Vertices 0, 1, 2, 3)
            0, 1, 2,   2, 3, 0,

            // Face 2: Back (Vertices 4, 5, 6, 7) - Adjust winding if needed
            // Assuming standard CCW winding when viewed from outside
            // 4=BL, 5=BR, 6=TR, 7=TL
             4, 5, 6,   6, 7, 4, // Corrected based on standard CCW view from outside
            // If backface culling is on and it's invisible, try reversing: 6,5,4, 4,7,6

            // Face 3: Left (Vertices 8, 9, 10, 11)
            8, 9, 10,  10, 11, 8,

            // Face 4: Right (Vertices 12, 13, 14, 15)
            12, 13, 14, 14, 15, 12,

            // Face 5: Bottom (Vertices 16, 17, 18, 19)
            16, 17, 18, 18, 19, 16,

            // Face 6: Top (Vertices 20, 21, 22, 23)
            20, 21, 22, 22, 23, 20
        };
        private List<Vector2> asteroidTexCoords = new List<Vector2>()
        {
            // Face 1: Front
            new Vector2(0.0f, 0.0f), // Bottom-Left
            new Vector2(1.0f, 0.0f), // Bottom-Right
            new Vector2(1.0f, 1.0f), // Top-Right
            new Vector2(0.0f, 1.0f), // Top-Left

            // Face 2: Back
            new Vector2(1.0f, 0.0f), // Bottom-Left (Mirror horizontally if needed, depends on view) -> Let's assume standard UVs
            new Vector2(0.0f, 0.0f), // Bottom-Right -> Let's assume standard UVs
            new Vector2(0.0f, 1.0f), // Top-Right -> Let's assume standard UVs
            new Vector2(1.0f, 1.0f), // Top-Left -> Let's assume standard UVs
            // **Alternative for Back Face (Often preferred):** Use standard UVs (0,0 -> 1,1) and handle mirroring in texture or shader if necessary. Let's use standard.
            new Vector2(0.0f, 0.0f), // BL
            new Vector2(1.0f, 0.0f), // BR
            new Vector2(1.0f, 1.0f), // TR
            new Vector2(0.0f, 1.0f), // TL

            // Face 3: Left
            new Vector2(0.0f, 0.0f), // BL
            new Vector2(1.0f, 0.0f), // BR
            new Vector2(1.0f, 1.0f), // TR
            new Vector2(0.0f, 1.0f), // TL

            // Face 4: Right
            new Vector2(0.0f, 0.0f), // BL
            new Vector2(1.0f, 0.0f), // BR
            new Vector2(1.0f, 1.0f), // TR
            new Vector2(0.0f, 1.0f), // TL

            // Face 5: Bottom
            new Vector2(0.0f, 0.0f), // BL
            new Vector2(1.0f, 0.0f), // BR
            new Vector2(1.0f, 1.0f), // TR
            new Vector2(0.0f, 1.0f), // TL

            // Face 6: Top
            new Vector2(0.0f, 0.0f), // BL
            new Vector2(1.0f, 0.0f), // BR
            new Vector2(1.0f, 1.0f), // TR
            new Vector2(0.0f, 1.0f)  // TL
        };

        

        public Game(int width, int height) : base
        (GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(width, height), Title = "Звездный форсаж" })
        {
            this.width = width;
            this.height = height;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            asteroids = new List<GameObject>();
            random = new Random();

            shaderProgram = new Shader("shader.vert", "shader.frag");

            string shipModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Models/test.obj");

            ModelLoader.ModelData shipModelData = ModelLoader.LoadObj(shipModelPath);

            Ship = new GameObject(
                shipModelData,
                shipTexturePath,
                new Vector3(0, 0, 0),
                new Vector3(MathHelper.DegreesToRadians(90f), MathHelper.DegreesToRadians(180f), 0), 
                new Vector3(0.2f, 0.2f, 0.2f)
            );

            float skyboxSize = 1000.0f;
            float halfSize = skyboxSize / 2.0f;

            Vector3 frontPos = new Vector3(0, 0, -halfSize);
            Vector3 backPos = new Vector3(0, 0, halfSize);
            Vector3 leftPos = new Vector3(-halfSize, 0, 0);
            Vector3 rightPos = new Vector3(halfSize, 0, 0);
            Vector3 topPos = new Vector3(0, halfSize, 0);
            Vector3 bottomPos = new Vector3(0, -halfSize, 0);

            Vector3 frontRot = new Vector3(0, 0, 0); // Faces -Z
            Vector3 backRot = new Vector3(0, MathHelper.DegreesToRadians(180), 0); // Faces +Z
            Vector3 leftRot = new Vector3(0, MathHelper.DegreesToRadians(90), 0); // Faces -X
            Vector3 rightRot = new Vector3(0, MathHelper.DegreesToRadians(-90), 0); // Faces +X
            Vector3 topRot = new Vector3(MathHelper.DegreesToRadians(-90), 0, 0); // Faces +Y
            Vector3 bottomRot = new Vector3(MathHelper.DegreesToRadians(90), 0, 0); // Faces -Y

            Vector3 wallScale = new Vector3(skyboxSize, skyboxSize, 1);

            Wall1 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", frontPos, frontRot, wallScale); // Front
            Wall2 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", backPos, backRot, wallScale);   // Back
            Wall3 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", leftPos, leftRot, wallScale);   // Left
            Wall4 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", rightPos, rightRot, wallScale); // Right
            Wall5 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", topPos, topRot, wallScale);     // Top
            Wall6 = new GameObject(PlaneVertices, PlaneIndices, PlaneTexCoords, "space.jpg", bottomPos, bottomRot, wallScale); // Bottom


            Vector3 initialCameraOffset = new Vector3(0, 2, 5);
            camera = new Camera(width, height, Ship.Position - initialCameraOffset);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnUnload()
        {

            Ship?.del_resources();
            Wall1?.del_resources();
            Wall2?.del_resources();
            Wall3?.del_resources();
            Wall4?.del_resources();
            Wall5?.del_resources();
            Wall6?.del_resources();
            shaderProgram?.DeleteShader();

            if (asteroids != null)
            {
                foreach (var asteroid in asteroids)
                {
                    asteroid?.del_resources();
                }
                asteroids.Clear();
            }


            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (isGameOver)
            {
                GL.ClearColor(0.5f, 0.1f, 0.1f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                Context.SwapBuffers();
                return;
            }

            GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shaderProgram.UseShader();


            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection();


            shaderProgram.SetMatrix4("view", view, true);
            shaderProgram.SetMatrix4("projection", projection, true);


            Matrix4 shipModelMatrix = Ship.GetModelMatrix();
            shaderProgram.SetMatrix4("model", shipModelMatrix, true);
            Ship.draw();


            foreach (GameObject asteroid in asteroids)
            {
                Matrix4 asteroidModelMatrix = asteroid.GetModelMatrix();
                shaderProgram.SetMatrix4("model", asteroidModelMatrix, true);
                asteroid.draw();
            }

            GL.DepthMask(false);

            shaderProgram.SetMatrix4("model", Wall1.GetModelMatrix(), true); Wall1.draw();
            shaderProgram.SetMatrix4("model", Wall2.GetModelMatrix(), true); Wall2.draw();
            shaderProgram.SetMatrix4("model", Wall3.GetModelMatrix(), true); Wall3.draw();
            shaderProgram.SetMatrix4("model", Wall4.GetModelMatrix(), true); Wall4.draw();
            shaderProgram.SetMatrix4("model", Wall5.GetModelMatrix(), true); Wall5.draw();
            shaderProgram.SetMatrix4("model", Wall6.GetModelMatrix(), true); Wall6.draw();

            GL.DepthMask(true);

            Context.SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (!IsFocused)
            {
                return;
            }

            if (isGameOver)
            {
                Close();
                return;
            }


            float deltaTime = (float)args.Time;

            KeyboardState input = KeyboardState;
            MouseState mouse = MouseState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            if (input.IsKeyDown(Keys.W))
            {   
                if (Ship.Position.Z >= -100)
                    Ship.Position -= Ship.Up * shipMoveSpeed * deltaTime;
                
            }
            if (input.IsKeyDown(Keys.S))
            {
                if (Ship.Position.Z <= 100)
                    Ship.Position += Ship.Up * shipMoveSpeed * deltaTime;
                
            }
            if (input.IsKeyDown(Keys.A))
            {
                if (Ship.Position.X >= -100)
                    Ship.Position -= Ship.Right * shipMoveSpeed * deltaTime;
            }
            if (input.IsKeyDown(Keys.D))
            {
                if (Ship.Position.X <= 100)
                    Ship.Position += Ship.Right * shipMoveSpeed * deltaTime;
            }

            camera.UpdateThirdPerson(Ship, mouse, deltaTime);

            spawnTimer += deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                SpawnAsteroid();
            }


            float asteroidRotateSpeedX = MathHelper.DegreesToRadians(1500.0f);
            float asteroidRotateSpeedY = MathHelper.DegreesToRadians(2500.0f); 
            float asteroidRotateSpeedZ = MathHelper.DegreesToRadians(500.0f);
            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                GameObject asteroid = asteroids[i];

                asteroidSpeed = (random.NextSingle() + 0.5f) * 100;
                //asteroidSpeed = 10f;
                asteroid.Position -= new Vector3(0, asteroidSpeed * deltaTime, 0);
                asteroid.Rotation += new Vector3(
                    asteroidRotateSpeedX * deltaTime,
                    asteroidRotateSpeedY * deltaTime,
                    asteroidRotateSpeedZ * deltaTime
                );

                if (asteroid.Position.Y < despawnHeight)
                {
                    asteroid.del_resources();
                    asteroids.RemoveAt(i);
                    continue;
                }

                float distanceSq = Vector3.DistanceSquared(Ship.Position, asteroid.Position);
                float radiiSum = shipBoundingRadius + asteroidBoundingRadius;
                float radiiSumSq = radiiSum * radiiSum;

                if (distanceSq < radiiSumSq)
                {
                    isGameOver = true;
                    Console.WriteLine("Game Over! Collision detected."); // Выводим сообщение
                    break;
                }
            }
        }
        private void SpawnAsteroid()
        {
            float xPos = (float)(random.NextDouble() * 2.0 - 1.0) * spawnRangeX; 
            float zPos = (float)(random.NextDouble() * 2.0 - 1.0) * spawnRangeZ; 
            Vector3 initialPosition = new Vector3(xPos, spawnHeight, zPos);

            Vector3 initialRotation = new Vector3(
                (float)random.NextDouble() * MathHelper.TwoPi,
                (float)random.NextDouble() * MathHelper.TwoPi,
                (float)random.NextDouble() * MathHelper.TwoPi
            );

            float scaleFactor = random.NextSingle() * 3f + 1f;
            Vector3 initialScale = Vector3.One * scaleFactor;

            GameObject newAsteroid = new GameObject(
                asteroidVertices,
                asteroidIndices,
                asteroidTexCoords, 
                asteroidTexturePath,
                initialPosition,
                initialRotation,
                initialScale
            );
            asteroids.Add(newAsteroid); // Добавляем в список
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
            if (camera != null)
            {
                camera.AspectRatio = (float)width / height;
            }
        }
    }
}
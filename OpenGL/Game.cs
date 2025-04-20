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
        private float spawnInterval = 0.01f;
        private Random random;
        private float asteroidSpeed;
        private float spawnHeight = 200.0f;
        private float spawnRangeX = 150.0f;
        private float spawnRangeZ = 150.0f;
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
        private float shipMoveSpeed = 25.0f;
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
            //new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            // Add coordinates for the other 16 vertices based on your UV map
            // new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            //new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            // new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            //new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
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

            asteroids = new List<GameObject>();
            random = new Random();


            // --- Create Shader ---
            shaderProgram = new Shader("shader.vert", "shader.frag"); // Assuming Shader constructor handles loading/linking

            // --- Create Ship ---
            ////////////////////////////////Ship = new GameObject(
            ////////////////////////////////    shipVertices,
            ////////////////////////////////    shipIndices,
            ////////////////////////////////    shipTexCoords, // Use the cube tex coords (even if placeholder)
            ////////////////////////////////    "ship3.png",    // Texture file
            ////////////////////////////////    new Vector3(0, 0, 0), // Initial Position
            ////////////////////////////////    new Vector3(0, 0, 0), // Initial Rotation (radians)
            ////////////////////////////////    new Vector3(1, 1, 1)  // Initial Scale (adjusted cube size)
            ////////////////////////////////);
            ///

            Console.WriteLine("Loading ship model...");

            string shipModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Models/test.obj"); // <-- ИЗМЕНИТЕ НА ВАШ ПУТЬ к .obj

            string shipTexturePath = "ship3.png"; // <-- ИЗМЕНИТЕ НА ИМЯ ФАЙЛА ВАШЕЙ ТЕКСТУРЫ КОРАБЛЯ (которая лежит в /Textures/)

            ModelLoader.ModelData shipModelData = ModelLoader.LoadObj(shipModelPath);


            // Создаем GameObject корабля, используя новый конструктор и загруженные данные
            Ship = new GameObject(
                shipModelData,      // Передаем загруженные данные
                shipTexturePath,    // Указываем путь к текстуре корабля
                new Vector3(0, 0, 0), // Начальная позиция (можно настроить)
                new Vector3(MathHelper.DegreesToRadians(90f), MathHelper.DegreesToRadians(180f), 0), // Начальный поворот (возможно, модель смотрит не туда, разверните на 180 градусов по Y)
                new Vector3(0.2f, 0.2f, 0.2f)  // Начальный масштаб (модель может быть слишком большой/маленькой, подберите)
            );
            Console.WriteLine("Ship model loaded and GameObject created.");

            // Установите Bounding Radius для корабля (подберите по размеру модели)
            shipBoundingRadius = 1.5f;


            // --- Create Walls (Example for Skybox-like setup) ---
            // Use the Plane geometry for walls
            float skyboxSize = 1000.0f; // How far away the walls are
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
                // Опционально: Очистить экран другим цветом или нарисовать текст "Game Over"
                GL.ClearColor(0.5f, 0.1f, 0.1f, 1.0f); // Например, красный фон
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                // Здесь можно будет добавить рендеринг текста
                Context.SwapBuffers();
                return; // Не рендерим остальную сцену
            }

            GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
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


            foreach (GameObject asteroid in asteroids)
            {
                Matrix4 asteroidModelMatrix = asteroid.GetModelMatrix();
                shaderProgram.SetMatrix4("model", asteroidModelMatrix, true);
                asteroid.draw(); // Рисуем астероид
            }


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

            if (isGameOver) // Если игра окончена, ничего не обновляем
            {
                // Можно добавить логику ожидания нажатия клавиши для рестарта
                Close();
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
                Ship.Position += Ship.Up * shipMoveSpeed * deltaTime;
                shipMoved = true;
            }
            if (input.IsKeyDown(Keys.S))
            {
                Ship.Position -= Ship.Up * shipMoveSpeed * deltaTime;
                shipMoved = true;
            }

            // Turning (Yaw) - rotates the ship around its Up axis
            if (input.IsKeyDown(Keys.A))
            {
                // Negative rotation around Y for left turn
                //Ship.Rotation = new Vector3(Ship.Rotation.X, Ship.Rotation.Y + shipTurnSpeed * deltaTime, Ship.Rotation.Z);
                Ship.Position -= Ship.Right * shipMoveSpeed * deltaTime;
                shipRotated = true;
            }
            if (input.IsKeyDown(Keys.D))
            {
                // Positive rotation around Y for right turn
                //Ship.Rotation = new Vector3(Ship.Rotation.X, Ship.Rotation.Y - shipTurnSpeed * deltaTime, Ship.Rotation.Z);
                Ship.Position += Ship.Right * shipMoveSpeed * deltaTime;
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

            spawnTimer += deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval; // Сбросить таймер (вычитаем, чтобы не терять время)
                SpawnAsteroid();
            }

            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                GameObject asteroid = asteroids[i];

                // Двигаем астероид вниз
                asteroidSpeed = (random.NextSingle() + 0.5f) * 100; 
                asteroid.Position -= new Vector3(0, asteroidSpeed * deltaTime, 0);
                // asteroid.Rotation += new Vector3( ... ); // Можно добавить вращение

                // Проверка на выход за пределы экрана (деспавн)
                if (asteroid.Position.Y < despawnHeight)
                {
                    asteroid.del_resources(); // Освобождаем ресурсы GPU
                    asteroids.RemoveAt(i);    // Удаляем из списка
                    continue; // Переходим к следующему астероиду
                }

                // Проверка столкновения с кораблем (Bounding Sphere)
                float distanceSq = Vector3.DistanceSquared(Ship.Position, asteroid.Position); // Эффективнее проверять квадрат расстояния
                float radiiSum = shipBoundingRadius + asteroidBoundingRadius;
                float radiiSumSq = radiiSum * radiiSum;

                if (distanceSq < radiiSumSq)
                {
                    // Столкновение!
                    isGameOver = true;
                    Console.WriteLine("Game Over! Collision detected."); // Выводим сообщение
                                                                         // Close(); // Можно просто закрыть окно как самый простой вариант "конца игры"
                    break; // Выходим из цикла проверки столкновений
                }
            }

            // --- End of Update ---
        }
        private void SpawnAsteroid()
        {
            // Генерируем случайные координаты X и Z в заданном диапазоне
            float xPos = (float)(random.NextDouble() * 2.0 - 1.0) * spawnRangeX; // От -spawnRangeX до +spawnRangeX
            float zPos = (float)(random.NextDouble() * 2.0 - 1.0) * spawnRangeZ; // От -spawnRangeZ до +spawnRangeZ

            // Начальная позиция
            Vector3 initialPosition = new Vector3(xPos, spawnHeight, zPos);

            // Случайное начальное вращение (опционально)
            Vector3 initialRotation = new Vector3(
                (float)random.NextDouble() * MathHelper.TwoPi,
                (float)random.NextDouble() * MathHelper.TwoPi,
                (float)random.NextDouble() * MathHelper.TwoPi
            );

            // Случайный масштаб (опционально, но делает астероиды разнообразнее)
            float scaleFactor = random.NextSingle() * 3f + 1f; // Масштаб от 0.6 до 1.4
            Vector3 initialScale = Vector3.One * scaleFactor;

            // Создаем GameObject астероида
            // Убедитесь, что у вас есть текстура "rock.png" или укажите другую
            GameObject newAsteroid = new GameObject(
                asteroidVertices, // Используем ту же геометрию куба
                asteroidIndices,
                asteroidTexCoords, // TODO: Нужны UV для астероида, если текстура другая
                asteroidTexturePath,
                initialPosition,
                initialRotation,
                initialScale
            );

            // newAsteroid.BoundingRadius = asteroidBoundingRadius * scaleFactor; // Масштабируем радиус

            asteroids.Add(newAsteroid); // Добавляем в список
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
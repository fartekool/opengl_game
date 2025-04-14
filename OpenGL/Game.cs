using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using static OpenTK.Graphics.OpenGL.GL;

namespace OpenGL
{
    internal class Game : GameWindow
    {
        Shader shaderProgram;
        Camera camera;


        GameObject Ship;
        GameObject Saturn;

        GameObject Wall1;
        GameObject Wall2;
        GameObject Wall3;
        GameObject Wall4;
        GameObject Wall5;
        GameObject Wall6;

        int width, height;

        private List<Vector3> SaturnVertices = new List<Vector3>()
        {
            new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
        };
        private uint[] SaturnIndices =
        {
            0,2,1,
            1,3,2
        };
        private List<Vector2> SaturnTexCoords = new List<Vector2>()
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };


        private List<Vector3> shipVertices = new List<Vector3>()
        {
            // Куб
            new Vector3(-1f, -1f, -1f),
            new Vector3(1f, -1f, -1f),
            new Vector3(1f, 1f, -1f),
            new Vector3(-1f, 1f, -1f),
            new Vector3(-1f, -1f, 1f),
            new Vector3(1f, -1f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(-1f, 1f, 1f),
        };

        private uint[] shipIndices =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            4, 0, 1, 4, 1, 5,
            1, 2, 5, 2, 6, 5,
            2, 3, 6, 3, 7, 6,
            3, 0, 7, 0, 4, 7
        };

        private List<Vector2> shipTexCoords = new List<Vector2>()
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };


        public Game (int width, int height): base
        (GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.width = width;
            this.height = height;
        }

        protected override void OnLoad()
        {
            //Saturn = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "saturn.jpg");


            Wall1 = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "space.jpg");
            Wall2 = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "space.jpg");
            Wall3 = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "space.jpg");
            Wall4 = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "space.jpg");
            Wall5 = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "space.jpg");
            Wall6 = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "space.jpg");

            Ship = new GameObject(SaturnVertices, SaturnIndices, SaturnTexCoords, "ship.png");

            //Ship = new GameObject(shipVertices, shipIndices, shipTexCoords, "ship.png");


            // 5. Shader loading and linking
            shaderProgram = new Shader();
            shaderProgram.LoadShader();
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, Shader.LoadShaderSource("shader.vert"));
            GL.CompileShader(vertexShader);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, Shader.LoadShaderSource("shader.frag"));
            GL.CompileShader(fragmentShader);
            GL.AttachShader(shaderProgram.shaderHandle, vertexShader);
            GL.AttachShader(shaderProgram.shaderHandle, fragmentShader);
            GL.LinkProgram(shaderProgram.shaderHandle);

            



            // 7. Clean up shaders
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
            if (success1 == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine(infoLog);
            }
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int success2);
            if (success2 == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine(infoLog);
            }

            camera = new Camera(width, height, Vector3.Zero);
            CursorState = CursorState.Grabbed;
            base.OnLoad();
        }

        
        protected override void OnUnload()
        {

            //Saturn.del_resources();
            Ship.del_resources();
            Wall1.del_resources();
            Wall2.del_resources();
            Wall3.del_resources();
            Wall4.del_resources();
            Wall5.del_resources();
            Wall6.del_resources();
            shaderProgram.DeleteShader();
            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shaderProgram.UseShader();

            bool is_free_camera = true;
            if (is_free_camera)
            {
                Matrix4 view = camera.GetViewMatrix();
                Matrix4 projection = camera.GetProjection();
                int viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
                int projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");
                GL.UniformMatrix4(viewLocation, true, ref view);
                GL.UniformMatrix4(projectionLocation, true, ref projection);
            }


            //Matrix4 saturn_model = Matrix4.Identity;
            //Matrix4 saturn_translation = Matrix4.CreateTranslation(0f, -2f, -6f);
            //Matrix4 saturn_scale = Matrix4.CreateScale(5f, 8f, 1f);
            //saturn_model *= saturn_scale * saturn_translation;
            //int saturn_modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            //GL.UniformMatrix4(saturn_modelLocation, true, ref saturn_model);
            //Saturn.draw();


            Matrix4 wall1_model = Matrix4.Identity;
            Vector3 wall1_position = camera.position + camera.front * 10f;
            Matrix4 wall1_scale = Matrix4.CreateScale(5f, 8f, 1f);
            Matrix4 wall1_translation = Matrix4.CreateTranslation(wall1_position);
            wall1_model *= wall1_scale * wall1_translation;
            int wall1_modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            GL.UniformMatrix4(wall1_modelLocation, true, ref wall1_model);
            Wall1.draw();



            Matrix4 ship_model = Matrix4.Identity;
            Vector3 shipPosition = camera.position + camera.front * 10f;
            Matrix4 ship_translation = Matrix4.CreateTranslation(shipPosition);
            ship_model *= ship_translation;
            int ship_modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            GL.UniformMatrix4(ship_modelLocation, true, ref ship_model);
            Ship.draw();


            Context.SwapBuffers();
            GL.Enable(EnableCap.DepthTest);
            base.OnRenderFrame(args);

        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            base.OnUpdateFrame(args);
            camera.Update(input, mouse, args);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
        }
    }

    public class Shader
    {
        public int shaderHandle;

        public Shader() : base() { }  

        public void LoadShader()
        {
            shaderHandle = GL.CreateProgram();
        }

        public static string LoadShaderSource(string filepath)
        {
            string shaderSource = "";
            try
            {
                using (StreamReader reader = new
                StreamReader("../../../Shaders/" + filepath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file:" + e.Message);
            }
            return shaderSource;
        }

        public void UseShader()
        {
            GL.UseProgram(shaderHandle);
        }
        public void DeleteShader()
        {
            GL.DeleteProgram(shaderHandle);
        }

    }
}

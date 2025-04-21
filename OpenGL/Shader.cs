using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace OpenGL
{
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


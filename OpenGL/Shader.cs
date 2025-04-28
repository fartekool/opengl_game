using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace OpenGL
{
    public class Shader
    {
        public readonly int shaderHandle;

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = LoadShaderSource(vertexPath);
            string fragmentShaderSource = LoadShaderSource(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            CompileShader(fragmentShader);

            shaderHandle = GL.CreateProgram();
            GL.AttachShader(shaderHandle, vertexShader);
            GL.AttachShader(shaderHandle, fragmentShader);
            LinkProgram(shaderHandle);

            GL.DetachShader(shaderHandle, vertexShader);
            GL.DetachShader(shaderHandle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);
        }
        private static string LoadShaderSource(string filepath)
        {
            string shaderSource = "";
            using (StreamReader reader = new StreamReader(Path.Combine("../../../Shaders", filepath)))
            {
                shaderSource = reader.ReadToEnd();
            }
            return shaderSource;
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
            }
        }

        private static void LinkProgram(int program)
        {
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
            }
        }

        public void UseShader()
        {
            GL.UseProgram(shaderHandle);
        }

        public void DeleteShader()
        {
            GL.DeleteProgram(shaderHandle);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(shaderHandle, name);
            if (location != -1)
            {
                GL.UniformMatrix4(location, true, ref matrix);
            }
        }
    }
}


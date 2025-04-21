using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace OpenGL
{
    class GameObject
    {
        private int VAO;
        private int VBO_Vertices;
        private int VBO_TexCoords;
        private int EBO;
        private int textureID;

        private int indicesCount;
        private bool hasTexCoords = false;
        private string texturePath;

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Front { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 Right { get; private set; }

        public GameObject(ModelLoader.ModelData modelData, string textureFilePath,
                          Vector3 initialPosition = default,
                          Vector3 initialRotation = default,
                          Vector3 initialScale = default)
        {
            this.texturePath = textureFilePath;
            Position = initialPosition;
            Rotation = initialRotation;
            Scale = (initialScale == Vector3.Zero) ? Vector3.One : initialScale;
            UpdateVectors();
            setup_buffers(modelData);
            if (!string.IsNullOrEmpty(texturePath) && hasTexCoords)
            {
                setup_texture(texturePath);
            }
            else
            {
                
                textureID = 0;
            }
        }
        public GameObject(List<Vector3> vert, uint[] ind, List<Vector2> crd, string path,
                  Vector3 initialPosition = default, Vector3 initialRotation = default, Vector3 initialScale = default)
        {
            this.texturePath = path;
            Position = initialPosition;
            Rotation = initialRotation;
            Scale = (initialScale == Vector3.Zero) ? Vector3.One : initialScale;
            UpdateVectors();
            ModelLoader.ModelData simpleModelData = new ModelLoader.ModelData
            {
                Vertices = vert,
                TexCoords = crd,
                Indices = ind.ToList()
            };

            setup_buffers(simpleModelData);
            if (!string.IsNullOrEmpty(texturePath) && hasTexCoords)
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

        private void setup_buffers(ModelLoader.ModelData modelData)
        {
            if (modelData.Vertices == null || modelData.Vertices.Count == 0 ||
                modelData.Indices == null || modelData.Indices.Count == 0)
            {
                
                return;
            }

            indicesCount = modelData.Indices.Count; 
            hasTexCoords = true;

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // 1. VBO for Vertex Positions
            VBO_Vertices = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertices);
            GL.BufferData(BufferTarget.ArrayBuffer, modelData.Vertices.Count * Vector3.SizeInBytes,
                          modelData.Vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);

            // 2. VBO for Texture Coordinates
            if (hasTexCoords)
            {
                VBO_TexCoords = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_TexCoords);
                GL.BufferData(BufferTarget.ArrayBuffer, modelData.TexCoords.Count * Vector2.SizeInBytes,
                              modelData.TexCoords.ToArray(), BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
                GL.EnableVertexAttribArray(1);
            }
            else { VBO_TexCoords = 0; }



            // 3. EBO for Indices
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indicesCount * sizeof(uint),
                          modelData.Indices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            
        }

        private void setup_texture(String path)
        {
            if (string.IsNullOrEmpty(path))
            {
                
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
                string absolutePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Textures/", path));

                if (!File.Exists(absolutePath))
                {
                    
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                    GL.DeleteTexture(textureID);
                    textureID = 0;
                    return;
                }


                using (Stream stream = File.OpenRead(absolutePath))
                {
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    if (image != null && image.Data != null)
                    {
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                        
                    }
                    else
                    {
                        
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.DeleteTexture(textureID);
                        textureID = 0;
                    }
                }
            }
            catch (Exception e)
            {
                
                if (textureID != 0) GL.DeleteTexture(textureID);
                textureID = 0;
            }
            finally
            {
                if (textureID != 0) GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public void del_resources()
        {
            if (VBO_Vertices != 0) GL.DeleteBuffer(VBO_Vertices);
            if (VBO_TexCoords != 0) GL.DeleteBuffer(VBO_TexCoords);
            if (EBO != 0) GL.DeleteBuffer(EBO);
            if (VAO != 0) GL.DeleteVertexArray(VAO);
            if (textureID != 0) GL.DeleteTexture(textureID);
        }

        public void draw()
        {
            if (VAO == 0 || indicesCount == 0) return;

            if (textureID != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureID);
            }
            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, indicesCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            if (textureID != 0)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
    }
}

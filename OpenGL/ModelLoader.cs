using ObjLoader.Loader.Loaders;
using OpenTK.Mathematics;       

namespace OpenGL
{
    public static class ModelLoader
    {
        public struct ModelData
        {
            public List<Vector3> Vertices;
            public List<Vector2> TexCoords;
            public List<uint> Indices;
        }
        public static ModelData LoadObj(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new ModelData
                {
                    Vertices = new List<Vector3>(),
                    TexCoords = new List<Vector2>(),
                    Indices = new List<uint>()
                };
            }

            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();

            LoadResult result;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                result = objLoader.Load(fileStream);
            }

            var outVertices = new List<Vector3>();
            var outTexCoords = new List<Vector2>();
            var outIndices = new List<uint>();

            var uniqueVertexMap = new Dictionary<string, uint>();

            uint nextIndex = 0;

            foreach (var group in result.Groups)
            {
                foreach (var face in group.Faces)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var faceVertex = face[i];

                        int posIndex = faceVertex.VertexIndex - 1;
                        int uvIndex = faceVertex.TextureIndex - 1;

                        string vertexKey = $"{posIndex}/{uvIndex}";

                        if (uniqueVertexMap.TryGetValue(vertexKey, out uint existingIndex))
                        {
                            outIndices.Add(existingIndex);
                        }
                        else
                        {
                            if (posIndex < 0 || posIndex >= result.Vertices.Count) continue;
                            var vertexPos = result.Vertices[posIndex];
                            outVertices.Add(new Vector3(vertexPos.X, vertexPos.Y, vertexPos.Z));

                            if (uvIndex >= 0 && uvIndex < result.Textures.Count)
                            {
                                var vertexUV = result.Textures[uvIndex];
                                outTexCoords.Add(new Vector2(vertexUV.X, vertexUV.Y));
                            }
                            else
                            {
                                outTexCoords.Add(Vector2.Zero);
                            }

                            outIndices.Add(nextIndex);
                            uniqueVertexMap.Add(vertexKey, nextIndex);
                            nextIndex++;
                        }
                    }
                }
            }
            return new ModelData
            {
                Vertices = outVertices,
                TexCoords = outTexCoords,
                Indices = outIndices
            };
        }
    }
}
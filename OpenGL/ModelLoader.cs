using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ObjLoader.Loader.Loaders; // Добавляем using для библиотеки
using OpenTK.Mathematics;       // Добавляем using для Vector3, Vector2

namespace OpenGL // Убедитесь, что пространство имен совпадает с вашим проектом
{
    public static class ModelLoader
    {
        // Структура для хранения загруженных данных модели
        public struct ModelData
        {
            public List<Vector3> Vertices; // Позиции вершин
            public List<Vector2> TexCoords; // Текстурные координаты
            public List<Vector3> Normals;   // Нормали вершин (ВАЖНО для освещения!)
            public List<uint> Indices;     // Индексы для отрисовки
        }

        // Метод для загрузки OBJ модели
        public static ModelData LoadObj(string filePath)
        {
            // Проверяем, существует ли файл
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: Model file not found at {filePath}");
                // Возвращаем пустую структуру или выбрасываем исключение
                return new ModelData
                {
                    Vertices = new List<Vector3>(),
                    TexCoords = new List<Vector2>(),
                    Normals = new List<Vector3>(),
                    Indices = new List<uint>()
                };
            }

            // Создаем фабрику и загрузчик
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();

            // Загружаем файл
            LoadResult result;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                result = objLoader.Load(fileStream);
            }

            // --- Обработка загруженных данных ---
            // OBJ файлы часто имеют отдельные списки позиций, UV, нормалей,
            // а индексы в гранях ссылаются на эти разные списки.
            // OpenGL обычно требует один буфер индексов и буферы атрибутов,
            // где каждый индекс указывает на ПОЛНЫЙ набор атрибутов (поз+UV+нормаль).
            // Поэтому нам нужно "развернуть" данные: создать уникальные вершины
            // для каждой комбинации (позиция/UV/нормаль), встречающейся в гранях.

            var outVertices = new List<Vector3>();
            var outTexCoords = new List<Vector2>();
            var outNormals = new List<Vector3>();
            var outIndices = new List<uint>();

            // Словарь для отслеживания уже добавленных уникальных комбинаций вершин
            // Ключ: строка "posIndex/uvIndex/normalIndex"
            // Значение: индекс уже добавленной вершины в наших выходных списках (out*)
            var uniqueVertexMap = new Dictionary<string, uint>();

            uint nextIndex = 0;

            // Проходим по всем группам и их граням (Faces) в OBJ файле
            foreach (var group in result.Groups)
            {
                foreach (var face in group.Faces)
                {
                    // Мы предполагаем, что грани - это треугольники.
                    // Если модель содержит четырехугольники (face.Count == 4),
                    // их нужно триангулировать (разбить на 2 треугольника).
                    // ObjLoader.Loader может делать это сам, если настроено,
                    // но для простоты сейчас предположим, что все уже треугольники.
                    if (face.Count != 3)
                    {
                        Console.WriteLine($"Warning: Face with {face.Count} vertices found (expected 3). Skipping.");
                        continue; // Пропускаем не-треугольные грани
                    }

                    // Обрабатываем каждую вершину треугольника
                    for (int i = 0; i < 3; i++)
                    {
                        var faceVertex = face[i];

                        // Индексы из OBJ файла (начинаются с 1, поэтому вычитаем 1)
                        int posIndex = faceVertex.VertexIndex - 1;
                        int uvIndex = faceVertex.TextureIndex - 1;   // Может быть -1, если нет UV
                        int normalIndex = faceVertex.NormalIndex - 1; // Может быть -1, если нет нормалей

                        // Создаем уникальный ключ для этой комбинации индексов
                        string vertexKey = $"{posIndex}/{uvIndex}/{normalIndex}";

                        // Проверяем, обрабатывали ли мы уже такую комбинацию
                        if (uniqueVertexMap.TryGetValue(vertexKey, out uint existingIndex))
                        {
                            // Да, такая вершина уже есть, просто добавляем ее индекс
                            outIndices.Add(existingIndex);
                        }
                        else
                        {
                            // Нет, это новая уникальная вершина. Добавляем ее данные в списки.
                            // Позиция (обязательно должна быть)
                            if (posIndex < 0 || posIndex >= result.Vertices.Count) continue; // Защита
                            var vertexPos = result.Vertices[posIndex];
                            outVertices.Add(new Vector3(vertexPos.X, vertexPos.Y, vertexPos.Z));

                            // Текстурные координаты (если есть)
                            if (uvIndex >= 0 && uvIndex < result.Textures.Count)
                            {
                                var vertexUV = result.Textures[uvIndex];
                                outTexCoords.Add(new Vector2(vertexUV.X, vertexUV.Y));
                            }
                            else
                            {
                                outTexCoords.Add(Vector2.Zero); // Добавляем (0,0), если нет UV
                            }

                            // Нормали (если есть)
                            if (normalIndex >= 0 && normalIndex < result.Normals.Count)
                            {
                                var vertexNormal = result.Normals[normalIndex];
                                outNormals.Add(new Vector3(vertexNormal.X, vertexNormal.Y, vertexNormal.Z));
                            }
                            else
                            {
                                // Что делать, если нет нормалей? Можно вычислить их позже
                                // или добавить вектор по умолчанию (не очень хорошо).
                                // Пока добавим (0,1,0) как заглушку.
                                Console.WriteLine("Warning: Normal data missing in OBJ. Using default normal (0,1,0). Lighting may be incorrect.");
                                outNormals.Add(Vector3.UnitY);
                            }

                            // Добавляем индекс новой вершины в наш основной список индексов
                            outIndices.Add(nextIndex);
                            // Запоминаем эту комбинацию в словаре
                            uniqueVertexMap.Add(vertexKey, nextIndex);
                            // Увеличиваем счетчик для следующей уникальной вершины
                            nextIndex++;
                        }
                    }
                }
            }

            Console.WriteLine($"Model loaded: {outVertices.Count} vertices, {outIndices.Count / 3} triangles.");

            // Возвращаем структуру с обработанными данными
            return new ModelData
            {
                Vertices = outVertices,
                TexCoords = outTexCoords,
                Normals = outNormals,
                Indices = outIndices
            };
        }
    }
}
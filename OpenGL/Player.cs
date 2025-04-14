using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGL
{
    internal class Player
    {
        public Vector3 Position { get; private set; } = Vector3.Zero;
        public Vector3 Direction { get; private set; } = -Vector3.UnitZ;
        private float speed = 5f;
        private float rotationY = 0f;

        public void Update(KeyboardState input, FrameEventArgs e)
        {
            if (input.IsKeyDown(Keys.Left))
                rotationY += 90f * (float)e.Time;
            if (input.IsKeyDown(Keys.Right))
                rotationY -= 90f * (float)e.Time;

            float rad = MathHelper.DegreesToRadians(rotationY);
            Direction = new Vector3((float)Math.Sin(rad), 0f, (float)-Math.Cos(rad));

            if (input.IsKeyDown(Keys.Up))
                Position += Direction * speed * (float)e.Time;
        }

        public Matrix4 GetModelMatrix()
        {
            return Matrix4.CreateScale(0.5f) * // Уменьшаем размер корабля
                   Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationY)) *
                   Matrix4.CreateTranslation(Position);
        }
    }
}
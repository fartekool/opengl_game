using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGL
{
    class Camera
    {
        public Vector3 Position { get; private set; }
        public float AspectRatio { get; set; }

        private GameObject target;
        private float distance = 3.0f;
        private float heightOffset = 0.25f;
        private float orbitSpeed = 1.5f;
        private float pitchSensitivity = 0.005f;
        private float yawSensitivity = 0.005f;

        private float currentPitch = MathHelper.DegreesToRadians(-70f);
        private float currentYaw = 0f;

        private Vector2 lastMousePos;
        private bool firstMove = true;

        private Vector3 front = -Vector3.UnitZ;
        private Vector3 up = Vector3.UnitY;
        private Vector3 right = Vector3.UnitX;


        public Camera(int width, int height, Vector3 initialPosition)
        {
            AspectRatio = (float)width / height;
            Position = initialPosition;
        }
        public Matrix4 GetViewMatrix()
        {
            Vector3 lookAtPoint = target?.Position ?? Vector3.Zero;
            lookAtPoint.Y += heightOffset;

            CalculateCameraPosition(target?.Position ?? Vector3.Zero);

            return Matrix4.LookAt(Position, lookAtPoint, Vector3.UnitY);
        }
        public Matrix4 GetProjection()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(80f), 
                AspectRatio,
                0.1f,
                1000.0f
            );
        }
        public void UpdateThirdPerson(GameObject targetObject, MouseState mouse, float deltaTime)
        {
            if (targetObject == null) return;
            this.target = targetObject;

            if (firstMove)
            {
                lastMousePos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - lastMousePos.X;
                var deltaY = mouse.Y - lastMousePos.Y;
                lastMousePos = new Vector2(mouse.X, mouse.Y);

                currentYaw -= deltaX * yawSensitivity;
                currentPitch -= deltaY * pitchSensitivity;

                currentPitch = MathHelper.Clamp(currentPitch, MathHelper.DegreesToRadians(-85.0f), MathHelper.DegreesToRadians(85.0f));

            }
        }

        private void CalculateCameraPosition(Vector3 targetPosition)
        {

            float targetYaw = target?.Rotation.Y ?? 0.0f;

            float totalYaw = targetYaw + currentYaw;

            float horizontalDistance = distance * MathF.Cos(currentPitch);
            float verticalDistance = distance * MathF.Sin(currentPitch);

            float offsetX = horizontalDistance * MathF.Sin(totalYaw);
            float offsetZ = horizontalDistance * MathF.Cos(totalYaw); 

            Position = new Vector3(
                targetPosition.X - offsetX,
                targetPosition.Y + verticalDistance,
                targetPosition.Z - offsetZ
            );
        }
    }
}
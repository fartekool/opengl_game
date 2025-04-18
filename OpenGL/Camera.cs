using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace OpenGL
{
    class Camera
    {
        // --- Properties ---
        public Vector3 Position { get; private set; } // Camera position is now calculated
        public float AspectRatio { get; set; }        // Aspect ratio for projection matrix

        // --- Third-Person Specific Parameters ---
        private GameObject target;                     // The object the camera is following
        private float distance = 3.0f;                 // Distance from the target
        private float heightOffset = 0.25f;             // Height offset from the target's position to look at
        private float orbitSpeed = 1.5f;               // Speed of orbiting with the mouse (radians per pixel)
        private float pitchSensitivity = 0.005f;       // Sensitivity for vertical orbit
        private float yawSensitivity = 0.005f;         // Sensitivity for horizontal orbit

        // --- Orbit Angles ---
        private float currentPitch = MathHelper.DegreesToRadians(-50f); // Initial vertical angle
        private float currentYaw = 0f;                // Initial horizontal angle around the target

        // --- Internal State for Mouse Input ---
        private Vector2 lastMousePos;
        private bool firstMove = true;

        // --- Camera Vectors (calculated based on orbit) ---
        // We don't strictly need public front/right/up for third person
        // as LookAt handles it, but keep them if needed elsewhere.
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 up = Vector3.UnitY;
        private Vector3 right = Vector3.UnitX;


        // --- Constructor ---
        // Takes initial aspect ratio. Position is determined by the target later.
        public Camera(int width, int height, Vector3 initialPosition) // Keep initialPosition for potential use later, or remove
        {
            AspectRatio = (float)width / height;
            // Position will be set based on the target in UpdateThirdPerson
            Position = initialPosition; // Set an initial value, will be overwritten
        }

        // --- View Matrix Calculation ---
        public Matrix4 GetViewMatrix()
        {
            // Calculate the point the camera should look at
            Vector3 lookAtPoint = target?.Position ?? Vector3.Zero; // Look at target's position
            lookAtPoint.Y += heightOffset; // Add height offset

            // Calculate the camera's position based on orbit angles and distance
            CalculateCameraPosition(target?.Position ?? Vector3.Zero); // Ensure Position is up-to-date

            // Create the view matrix looking from the calculated Position towards the lookAtPoint
            return Matrix4.LookAt(Position, lookAtPoint, Vector3.UnitY); // Use world UP
        }

        // --- Projection Matrix Calculation ---
        public Matrix4 GetProjection()
        {
            // Field of View (FOV), Aspect Ratio, Near Plane, Far Plane
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f), // FOV
                AspectRatio,                      // Aspect Ratio
                0.1f,                             // Near clipping plane
                100.0f                            // Far clipping plane
            );
        }

        // --- Main Update Method for Third-Person Camera ---
        public void UpdateThirdPerson(GameObject targetObject, MouseState mouse, float deltaTime)
        {
            if (targetObject == null) return; // Need a target to follow
            this.target = targetObject;

            // --- Mouse Input for Orbiting ---
            //if (firstMove)
            //{
            //    lastMousePos = new Vector2(mouse.X, mouse.Y);
            //    firstMove = false;
            //}
            //else
            //{
            //    // Calculate the mouse delta since the last frame
            //    var deltaX = mouse.X - lastMousePos.X;
            //    var deltaY = mouse.Y - lastMousePos.Y;
            //    lastMousePos = new Vector2(mouse.X, mouse.Y);

            //    // Adjust orbit angles based on mouse movement
            //    // Invert yaw delta if needed based on preference
            //    currentYaw -= deltaX * yawSensitivity; // Yaw changes horizontal orbit
            //    currentPitch -= deltaY * pitchSensitivity; // Pitch changes vertical orbit

            //    // Clamp the pitch angle to prevent flipping upside down
            //    // Allow slightly more than 90 up/down if desired, but limit it
            //    currentPitch = MathHelper.Clamp(currentPitch, MathHelper.DegreesToRadians(-85.0f), MathHelper.DegreesToRadians(85.0f));

            //    // Optional: Wrap Yaw angle to keep it within 0-360 degrees (0-2PI radians)
            //    // currentYaw = MathHelper.WrapAngle(currentYaw);
            //}

            // --- Position Calculation (done in GetViewMatrix/CalculateCameraPosition) ---
            // CalculateCameraPosition(target.Position); // Called within GetViewMatrix now

            // --- Old FPS Input Controller (Keep commented or remove) ---
            /*
            if (input.IsKeyDown(Keys.W)) { position += front * SPEED * deltaTime; }
            if (input.IsKeyDown(Keys.A)) { position -= right * SPEED * deltaTime; }
            if (input.IsKeyDown(Keys.S)) { position -= front * SPEED * deltaTime; }
            if (input.IsKeyDown(Keys.D)) { position += right * SPEED * deltaTime; }
            */

            // --- Old FPS Vector Update (Not directly needed for LookAt) ---
            // UpdateVectors();
        }

        // --- Helper to calculate camera position based on target and orbit ---
        //private void CalculateCameraPosition(Vector3 targetPosition)
        //{
        //    // Calculate the offset from the target based on orbit angles and distance
        //    float horizontalDistance = distance * MathF.Cos(currentPitch);
        //    float verticalDistance = distance * MathF.Sin(currentPitch);

        //    float offsetX = horizontalDistance * MathF.Sin(currentYaw);
        //    float offsetZ = horizontalDistance * MathF.Cos(currentYaw);

        //    // Final camera position is target position + offset
        //    Position = new Vector3(
        //        targetPosition.X - offsetX,  // Subtract offset X
        //        targetPosition.Y + verticalDistance, // Add vertical offset
        //        targetPosition.Z - offsetZ   // Subtract offset Z
        //    );
        //}
        private void CalculateCameraPosition(Vector3 targetPosition)
        {
            // Получаем угол поворота корабля (Yaw)
            // Убедитесь, что target не null (хотя проверка есть в UpdateThirdPerson)
            float targetYaw = target?.Rotation.Y ?? 0.0f;

            // Общий горизонтальный угол = угол корабля + угол смещения камеры (от мыши)
            // Знак (+) или (-) здесь может зависеть от того, как вы определили оси и направления вращения.
            // Если камера вращается не в ту сторону при движении мыши или не следует за кораблем,
            // попробуйте поменять знак перед currentYaw или в расчете deltaX в UpdateThirdPerson.
            float totalYaw = targetYaw + currentYaw;

            // Рассчитываем смещение камеры на основе ОБЩЕГО угла поворота и угла тангажа (pitch)
            float horizontalDistance = distance * MathF.Cos(currentPitch);
            float verticalDistance = distance * MathF.Sin(currentPitch);

            // Рассчитываем смещение по осям X и Z в мировой системе координат
            // используя общий угол totalYaw
            float offsetX = horizontalDistance * MathF.Sin(totalYaw);
            float offsetZ = horizontalDistance * MathF.Cos(totalYaw); // Используем Cos для Z, если +Z "вперед" или "-вперед"

            // Финальная позиция камеры = позиция цели + смещение
            // Мы вычитаем смещение, так как хотим быть ПОЗАДИ цели (смещение указывает ОТ цели к камере)
            Position = new Vector3(
                targetPosition.X - offsetX,
                targetPosition.Y + verticalDistance, // Вертикальное смещение всегда добавляется к Y цели
                targetPosition.Z - offsetZ
            );
        }

        // --- Old FPS Vector Update (Keep for reference or remove) ---
        /*
        private void UpdateVectors()
        {
            if (pitch > 89.0f) pitch = 89.0f;
            if (pitch < -89.0f) pitch = -89.0f;

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }
        */
    }
}
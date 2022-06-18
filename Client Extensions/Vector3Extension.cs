using System;
using CitizenFX.Core;

namespace PoliceMP.Client.Extensions
{
    public static class Vector3Extension
    {
        public static float Distance(this Vector3 position, Vector3 targetPosition)
        {
            var diffX = position.X - targetPosition.X;
            var diffY = position.Y - targetPosition.Y;
            var diffZ = position.Z - targetPosition.Z;

            var sum = diffX * diffX + diffY * diffY + diffZ * diffZ;
            return (float)Math.Sqrt(sum);
        }
    }
}
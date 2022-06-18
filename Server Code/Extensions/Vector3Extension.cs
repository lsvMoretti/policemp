using CitizenFX.Core;
using System;

namespace PoliceMP.Server.Extensions
{
    public static class Vector3Extension
    {
        public static float Distance(this Vector3 position, Vector3 targetPosition, bool ignoreZ = false)
        {
            var diffX = position.X - targetPosition.X;
            var diffY = position.Y - targetPosition.Y;
            if (!ignoreZ)
            {
                var diffZ = position.Z - targetPosition.Z;
                var sum = diffX * diffX + diffY * diffY + diffZ * diffZ;
                return (float)Math.Sqrt(sum);
            }
            
            var sum3D = diffX * diffX + diffY * diffY;
            return (float)Math.Sqrt(sum3D);
        }

        public static Vector3 ConvertToCitizen(this Core.Shared.Models.Vector3 position)
        {
            return new(position.X, position.Y, position.Z);
        }

        public static Core.Shared.Models.Vector3 Around(this Core.Shared.Models.Vector3 position, float distance)
        {
            var pos = position.ConvertToCitizen();

            var returnPosCitizen = pos.Around(distance);

            return new Core.Shared.Models.Vector3(returnPosCitizen.X, returnPosCitizen.Y, returnPosCitizen.Z);
        }
        
        public static Vector3 Around(this Vector3 position, float distance)
        {
            return position + RandomXy() * distance;
        }
        
        private static Vector3 RandomXy()
        {
            var position = new Vector3();

            var random = new Random();

            var num = random.NextDouble() * 2.0 * Math.PI;

            position.X = (float) Math.Cos(num);
            position.Y = (float) Math.Sin(num);
            position.Normalize();
            return position;
        }
    }
}

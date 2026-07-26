using System;

namespace NebMainPluginLabApi.Systems.Database
{
    public class Levels
    {
        internal static void CheckLevel(PlayerData data)
        {
            int baseXP = 230;
            float curveFactor = 5f; // Adjust this for steeper or flatter growth

            while (data.XP >= data.RequiredXP)
            {
                data.XP -= data.RequiredXP;
                data.Level++;

                data.RequiredXP = baseXP + (int)(curveFactor * Math.Pow(data.Level, 2));
            }
        }

    }
}
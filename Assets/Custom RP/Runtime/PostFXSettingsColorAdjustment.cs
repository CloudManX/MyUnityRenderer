using System;
using UnityEngine;

public partial class PostFXSettings
{
    [Serializable]
    public struct ColorAdjustmentSettings 
    {
        public float postExposure;

        [Range(-100f, 100f)]
        public float constrast;

        [ColorUsage(false, true)]
        public Color colorFilter;

        [Range(-180f, 180f)]
        public float hueShift;

        [Range(-100f, 100f)]
        public float saturation;
    }

    [SerializeField]
    ColorAdjustmentSettings colorAdjustments =
        new ColorAdjustmentSettings
        {
            colorFilter = Color.white
        };

    public ColorAdjustmentSettings
        ColorAdjustments => colorAdjustments;
    
    [Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)]
        public float temperature, tint;
    }

    [SerializeField]
    WhiteBalanceSettings whiteBalance = default;

    public WhiteBalanceSettings WhiteBalance => whiteBalance;
}

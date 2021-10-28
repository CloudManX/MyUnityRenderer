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
    
    // 2.1 White Balancing
    [Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)]
        public float temperature, tint;
    }

    [SerializeField]
    WhiteBalanceSettings whiteBalance = default;

    public WhiteBalanceSettings WhiteBalance => whiteBalance;

    // 2.2 Split Toning
    [Serializable]
    public struct SplitToningSettings
    {
        [ColorUsage(false)]
        public Color shadows, highlights;

        [Range(-100f, 100f)]
        public float balance; // Balance => 100 : more darker areas are
                              // pushed to be affected by highlights tint
                              // Balance => 100 : more brighter areas are
                              // pushed to be affeted by shadows tint
    }

    [SerializeField]
    SplitToningSettings splitToning = new SplitToningSettings
    {
        shadows = Color.gray,
        highlights = Color.gray
    };

    public SplitToningSettings SplitToning => splitToning;

    // 2.3 Channel Mixer
    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }

    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward
    };

    public ChannelMixerSettings ChannelMixer => channelMixer;
}

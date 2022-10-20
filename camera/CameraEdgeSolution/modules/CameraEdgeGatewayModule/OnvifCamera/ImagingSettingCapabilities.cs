// Decompiled with JetBrains decompiler
// Type: OnvifCamera.ImagingSettingCapabilities
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct ImagingSettingCapabilities : IEquatable<ImagingSettingCapabilities>
    {
        public FloatRange Brightness { get; set; }

        public FloatRange ColorSaturation { get; set; }

        public FloatRange Contrast { get; set; }

        public ExposureCapabilities Exposure { get; set; }

        public FocusCapabilities Focus { get; set; }

        public FloatRange Sharpness { get; set; }

        public WideDynamicRangeCapabilities WideDynamicRange { get; set; }

        public WhiteBalanceCapabilities WhiteBalance { get; set; }

        public override int GetHashCode() => (this.Brightness, this.ColorSaturation, this.Contrast, this.Exposure, this.Sharpness, this.Focus, this.WideDynamicRange, this.WhiteBalance).GetHashCode();

        public override bool Equals(object obj) => obj is ImagingSettingCapabilities other && this.Equals(other);

        public bool Equals(ImagingSettingCapabilities other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(
          ImagingSettingCapabilities left,
          ImagingSettingCapabilities right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
          ImagingSettingCapabilities left,
          ImagingSettingCapabilities right)
        {
            return !left.Equals(right);
        }
    }
}

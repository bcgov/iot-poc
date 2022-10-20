// Decompiled with JetBrains decompiler
// Type: OnvifCamera.WideDynamicRangeCapabilities
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;

namespace OnvifCamera
{
    public struct WideDynamicRangeCapabilities : IEquatable<WideDynamicRangeCapabilities>
    {
        public IReadOnlyList<ControlMode> SupportedModes { get; set; }

        public FloatRange Level { get; set; }

        public override int GetHashCode() => (this.SupportedModes, this.Level).GetHashCode();

        public override bool Equals(object obj) => obj is WideDynamicRangeCapabilities other && this.Equals(other);

        public bool Equals(WideDynamicRangeCapabilities other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(
          WideDynamicRangeCapabilities left,
          WideDynamicRangeCapabilities right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
          WideDynamicRangeCapabilities left,
          WideDynamicRangeCapabilities right)
        {
            return !left.Equals(right);
        }
    }
}

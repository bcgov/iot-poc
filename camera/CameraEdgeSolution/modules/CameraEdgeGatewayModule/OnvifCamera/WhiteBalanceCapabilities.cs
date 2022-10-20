// Decompiled with JetBrains decompiler
// Type: OnvifCamera.WhiteBalanceCapabilities
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;

namespace OnvifCamera
{
    public struct WhiteBalanceCapabilities : IEquatable<WhiteBalanceCapabilities>
    {
        public IReadOnlyList<ControlMode> SupportedModes { get; set; }

        public FloatRange RGain { get; set; }

        public FloatRange BGain { get; set; }

        public override int GetHashCode() => (this.SupportedModes, this.RGain, this.BGain).GetHashCode();

        public override bool Equals(object obj) => obj is WhiteBalanceCapabilities other && this.Equals(other);

        public bool Equals(WhiteBalanceCapabilities other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(WhiteBalanceCapabilities left, WhiteBalanceCapabilities right) => left.Equals(right);

        public static bool operator !=(WhiteBalanceCapabilities left, WhiteBalanceCapabilities right) => !left.Equals(right);
    }
}

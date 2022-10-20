// Decompiled with JetBrains decompiler
// Type: OnvifCamera.FocusCapabilities
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;

namespace OnvifCamera
{
    public struct FocusCapabilities : IEquatable<FocusCapabilities>
    {
        public IReadOnlyList<ControlMode> SupportedModes { get; set; }

        public FloatRange DefaultSpeed { get; set; }

        public FloatRange NearLimit { get; set; }

        public FloatRange FarLimit { get; set; }

        public override int GetHashCode() => (this.SupportedModes, this.DefaultSpeed, this.NearLimit, this.FarLimit).GetHashCode();

        public override bool Equals(object obj) => obj is FocusCapabilities other && this.Equals(other);

        public bool Equals(FocusCapabilities other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(FocusCapabilities left, FocusCapabilities right) => left.Equals(right);

        public static bool operator !=(FocusCapabilities left, FocusCapabilities right) => !left.Equals(right);
    }
}

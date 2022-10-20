// Decompiled with JetBrains decompiler
// Type: OnvifCamera.FocusSettings
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct FocusSettings : IEquatable<FocusSettings>
    {
        public ControlMode AutoFocusMode { get; set; }

        public float? DefaultSpeed { get; set; }

        public float? NearLimit { get; set; }

        public float? FarLimit { get; set; }

        public override int GetHashCode() => (this.AutoFocusMode, this.DefaultSpeed, this.NearLimit, this.FarLimit).GetHashCode();

        public override bool Equals(object obj) => obj is FocusSettings other && this.Equals(other);

        public bool Equals(FocusSettings other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(FocusSettings left, FocusSettings right) => left.Equals(right);

        public static bool operator !=(FocusSettings left, FocusSettings right) => !left.Equals(right);
    }
}

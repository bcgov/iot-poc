// Decompiled with JetBrains decompiler
// Type: OnvifCamera.WhiteBalanceSettings
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct WhiteBalanceSettings : IEquatable<WhiteBalanceSettings>
    {
        public ControlMode Mode { get; set; }

        public float? RGain { get; set; }

        public float? BGain { get; set; }

        public override int GetHashCode() => (this.Mode, this.RGain, this.BGain).GetHashCode();

        public override bool Equals(object obj) => obj is WhiteBalanceSettings other && this.Equals(other);

        public bool Equals(WhiteBalanceSettings other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(WhiteBalanceSettings left, WhiteBalanceSettings right) => left.Equals(right);

        public static bool operator !=(WhiteBalanceSettings left, WhiteBalanceSettings right) => !left.Equals(right);
    }
}

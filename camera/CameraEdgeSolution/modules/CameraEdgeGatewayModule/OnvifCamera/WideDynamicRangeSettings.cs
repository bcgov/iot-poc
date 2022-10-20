﻿// Decompiled with JetBrains decompiler
// Type: OnvifCamera.WideDynamicRangeSettings
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct WideDynamicRangeSettings : IEquatable<WideDynamicRangeSettings>
    {
        public ControlMode Mode { get; set; }

        public float? Level { get; set; }

        public override int GetHashCode() => (this.Mode, this.Level).GetHashCode();

        public override bool Equals(object obj) => obj is WideDynamicRangeSettings other && this.Equals(other);

        public bool Equals(WideDynamicRangeSettings other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(WideDynamicRangeSettings left, WideDynamicRangeSettings right) => left.Equals(right);

        public static bool operator !=(WideDynamicRangeSettings left, WideDynamicRangeSettings right) => !left.Equals(right);
    }
}

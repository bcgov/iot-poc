// Decompiled with JetBrains decompiler
// Type: OnvifCamera.ExposureCapabilities
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;

namespace OnvifCamera
{
    public struct ExposureCapabilities : IEquatable<ExposureCapabilities>
    {
        public IReadOnlyList<ControlMode> SupportedModes { get; set; }

        public IReadOnlyList<ExposurePriority> Priority { get; set; }

        public FloatRange MinExposureTime { get; set; }

        public FloatRange MaxExposureTime { get; set; }

        public FloatRange MinGain { get; set; }

        public FloatRange MaxGain { get; set; }

        public FloatRange MinIris { get; set; }

        public FloatRange MaxIris { get; set; }

        public FloatRange ExposureTime { get; set; }

        public FloatRange Gain { get; set; }

        public FloatRange Iris { get; set; }

        public override int GetHashCode() => (this.SupportedModes, this.Priority, this.MinGain, this.MaxGain, this.MinIris, this.MaxIris, this.ExposureTime, this.Gain, this.Iris).GetHashCode();

        public override bool Equals(object obj) => obj is ExposureCapabilities other && this.Equals(other);

        public bool Equals(ExposureCapabilities other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(ExposureCapabilities left, ExposureCapabilities right) => left.Equals(right);

        public static bool operator !=(ExposureCapabilities left, ExposureCapabilities right) => !left.Equals(right);
    }
}

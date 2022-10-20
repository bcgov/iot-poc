// Decompiled with JetBrains decompiler
// Type: OnvifCamera.ExposureSettings
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct ExposureSettings : IEquatable<ExposureSettings>
    {
        public ControlMode Mode { get; set; }

        public ExposurePriority? Priority { get; set; }

        public Rectangle? Window { get; set; }

        public float? MinExposureTime { get; set; }

        public float? MaxExposureTime { get; set; }

        public float? MinGain { get; set; }

        public float? MaxGain { get; set; }

        public float? MinIris { get; set; }

        public float? MaxIris { get; set; }

        public float? ExposureTime { get; set; }

        public float? Gain { get; set; }

        public float? Iris { get; set; }

        public override int GetHashCode() => (this.Mode, this.Priority, this.Window, this.MinExposureTime, this.MaxExposureTime, this.MinIris, this.MaxIris, this.Iris, this.MinGain, this.MaxGain, this.Gain).GetHashCode();

        public override bool Equals(object obj) => obj is ExposureSettings other && this.Equals(other);

        public bool Equals(ExposureSettings other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(ExposureSettings left, ExposureSettings right) => left.Equals(right);

        public static bool operator !=(ExposureSettings left, ExposureSettings right) => !left.Equals(right);
    }
}

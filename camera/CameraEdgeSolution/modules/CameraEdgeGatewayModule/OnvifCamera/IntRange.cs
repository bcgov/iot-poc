// Decompiled with JetBrains decompiler
// Type: OnvifCamera.IntRange
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct IntRange : IEquatable<IntRange>
    {
        public int Min { get; set; }

        public int Max { get; set; }

        public override int GetHashCode() => (this.Min, this.Max).GetHashCode();

        public override bool Equals(object obj) => obj is IntRange other && this.Equals(other);

        public bool Equals(IntRange other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(IntRange left, IntRange right) => left.Equals(right);

        public static bool operator !=(IntRange left, IntRange right) => !left.Equals(right);
    }
}

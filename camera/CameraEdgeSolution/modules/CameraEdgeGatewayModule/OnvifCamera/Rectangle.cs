// Decompiled with JetBrains decompiler
// Type: OnvifCamera.Rectangle
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct Rectangle : IEquatable<Rectangle>
    {
        public float? Top { get; set; }

        public float? Bottom { get; set; }

        public float? Left { get; set; }

        public float? Right { get; set; }

        public override int GetHashCode() => (this.Top, this.Bottom, this.Left, this.Right).GetHashCode();

        public override bool Equals(object obj) => obj is Rectangle other && this.Equals(other);

        public bool Equals(Rectangle other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);

        public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);
    }
}

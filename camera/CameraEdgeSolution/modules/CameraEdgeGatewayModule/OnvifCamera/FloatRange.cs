// Decompiled with JetBrains decompiler
// Type: OnvifCamera.FloatRange
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public class FloatRange : IEquatable<FloatRange>
    {
        public float Min { get; set; }

        public float Max { get; set; }

        public FloatRange(float min, float max)
        {
            this.Min = min;
            this.Max = max;
        }

        public override int GetHashCode() => (this.Min, this.Max).GetHashCode();

        public override bool Equals(object obj) => obj != null && (object)(obj as FloatRange) != null && this.Equals((FloatRange)obj);

        public bool Equals(FloatRange other) => (object)other != null && this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(FloatRange left, FloatRange right)
        {
            if ((object)left == null && (object)right == null)
                return true;
            return (object)left != null && (object)right != null && (double)left.Min == (double)right.Min && (double)left.Max == (double)right.Max;
        }

        public static bool operator !=(FloatRange left, FloatRange right)
        {
            if ((object)left == null && (object)right == null)
                return false;
            return (object)left == null || (object)right == null || (double)left.Min != (double)right.Min || (double)left.Max != (double)right.Max;
        }
    }
}

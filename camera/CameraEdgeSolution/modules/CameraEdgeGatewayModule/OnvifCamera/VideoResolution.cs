// Decompiled with JetBrains decompiler
// Type: OnvifCamera.VideoResolution
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct VideoResolution : IEquatable<VideoResolution>
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public override int GetHashCode() => (this.Width, this.Height).GetHashCode();

        public override bool Equals(object obj) => obj is VideoResolution other && this.Equals(other);

        public bool Equals(VideoResolution other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(VideoResolution left, VideoResolution right) => left.Equals(right);

        public static bool operator !=(VideoResolution left, VideoResolution right) => !left.Equals(right);
    }
}

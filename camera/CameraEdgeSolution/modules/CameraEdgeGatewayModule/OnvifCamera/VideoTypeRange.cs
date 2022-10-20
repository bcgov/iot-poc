// Decompiled with JetBrains decompiler
// Type: OnvifCamera.VideoTypeRange
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;

namespace OnvifCamera
{
    public struct VideoTypeRange : IEquatable<VideoTypeRange>
    {
        public string EncoderToken { get; set; }

        public string SubType { get; set; }

        public IReadOnlyList<VideoResolution> Resolutions { get; set; }

        public IntRange FrameRateRange { get; set; }

        public bool GuaranteedFrameRateSupported { get; set; }

        public IntRange QualityRange { get; set; }

        public IntRange BitrateRange { get; set; }

        public IntRange GovLengthRange { get; set; }

        public IntRange EncodingIntervalRange { get; set; }

        public IReadOnlyList<string> EncoderProfiles { get; set; }

        public override int GetHashCode() => (this.EncoderToken, this.SubType, this.Resolutions, this.FrameRateRange, this.GuaranteedFrameRateSupported, this.QualityRange, this.BitrateRange, this.GovLengthRange, this.EncodingIntervalRange, this.EncoderProfiles).GetHashCode();

        public override bool Equals(object obj) => obj is VideoTypeRange other && this.Equals(other);

        public bool Equals(VideoTypeRange other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(VideoTypeRange left, VideoTypeRange right) => left.Equals(right);

        public static bool operator !=(VideoTypeRange left, VideoTypeRange right) => !left.Equals(right);
    }
}

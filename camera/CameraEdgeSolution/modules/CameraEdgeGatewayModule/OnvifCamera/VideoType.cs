// Decompiled with JetBrains decompiler
// Type: OnvifCamera.VideoType
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct VideoType : IEquatable<VideoType>
    {
        public VideoType(VideoTypeRange range)
        {
            this.EncoderToken = range.EncoderToken;
            this.SubType = range.SubType;
            this.Resolution = range.Resolutions[0];
            this.FrameRate = range.FrameRateRange.Max;
            this.GuaranteedFrameRate = false;
            IntRange qualityRange = range.QualityRange;
            int max1 = qualityRange.Max;
            qualityRange = range.QualityRange;
            int min1 = qualityRange.Min;
            this.Quality = (int)(((double)(max1 + min1) + 0.5) / 2.0);
            this.BitrateLimit = range.BitrateRange.Max;
            this.GovLength = this.FrameRate == 0 || this.FrameRate > range.GovLengthRange.Max || this.FrameRate < range.GovLengthRange.Min ? (int)(((double)(range.GovLengthRange.Max + range.GovLengthRange.Min) + 0.5) / 2.0) : this.FrameRate;
            int num;
            if (range.EncodingIntervalRange.Min != 1)
            {
                IntRange encodingIntervalRange = range.EncodingIntervalRange;
                int max2 = encodingIntervalRange.Max;
                encodingIntervalRange = range.EncodingIntervalRange;
                int min2 = encodingIntervalRange.Min;
                num = (int)(((double)(max2 + min2) + 0.5) / 2.0);
            }
            else
                num = 1;
            this.EncodingInterval = num;
            this.EncoderProfile = range.EncoderProfiles?[0];
        }

        public string EncoderToken { get; set; }

        public string SubType { get; set; }

        public VideoResolution Resolution { get; set; }

        public int FrameRate { get; set; }

        public bool GuaranteedFrameRate { get; set; }

        public int Quality { get; set; }

        public int BitrateLimit { get; set; }

        public int GovLength { get; set; }

        public int EncodingInterval { get; set; }

        public string EncoderProfile { get; set; }

        public override int GetHashCode() => (this.EncoderToken, this.SubType, this.Resolution, this.FrameRate, this.GuaranteedFrameRate, this.Quality, this.BitrateLimit, this.GovLength, this.EncodingInterval, this.EncoderProfile).GetHashCode();

        public override bool Equals(object obj) => obj is VideoType other && this.Equals(other);

        public bool Equals(VideoType other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(VideoType left, VideoType right) => left.Equals(right);

        public static bool operator !=(VideoType left, VideoType right) => !left.Equals(right);
    }
}

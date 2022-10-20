// Decompiled with JetBrains decompiler
// Type: OnvifCamera.ProfileInfo
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct ProfileInfo : IEquatable<ProfileInfo>
    {
        public string Name { get; set; }

        public string Token { get; set; }

        public bool IsDeletable { get; set; }

        public ProfileInfo(string name, string token, bool isDeletable)
        {
            this.Name = name;
            this.Token = token;
            this.IsDeletable = isDeletable;
        }

        public override int GetHashCode() => (this.Name, this.Token, this.IsDeletable).GetHashCode();

        public override bool Equals(object obj) => obj is ProfileInfo other && this.Equals(other);

        public bool Equals(ProfileInfo other) => this.GetHashCode() == other.GetHashCode();

        public static bool operator ==(ProfileInfo left, ProfileInfo right) => left.Equals(right);

        public static bool operator !=(ProfileInfo left, ProfileInfo right) => !left.Equals(right);
    }
}

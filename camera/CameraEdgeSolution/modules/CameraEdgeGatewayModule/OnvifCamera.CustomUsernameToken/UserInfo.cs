// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.UserInfo
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera.CustomUsernameToken
{
    public struct UserInfo : IEquatable<UserInfo>
    {
        private TimeOnDevice _timeOnDvc;

        public string Username { get; set; }

        public string Password { get; set; }

        public DateTime DateTime => this._timeOnDvc.DateTime;

        public UserInfo(string username, string password, DateTime timeOnDvc)
        {
            this.Username = username;
            this.Password = password;
            this._timeOnDvc = new TimeOnDevice(timeOnDvc);
        }

        public override bool Equals(object obj) => obj is UserInfo other && this.Equals(other);

        public override int GetHashCode() => (this.Username, this._timeOnDvc, this.Password).GetHashCode();

        public static bool operator ==(UserInfo left, UserInfo right) => left.Equals(right);

        public static bool operator !=(UserInfo left, UserInfo right) => !(left == right);

        public bool Equals(UserInfo other) => this.GetHashCode() == other.GetHashCode();
    }
}

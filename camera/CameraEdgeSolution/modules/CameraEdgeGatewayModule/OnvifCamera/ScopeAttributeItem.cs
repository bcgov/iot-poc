// Decompiled with JetBrains decompiler
// Type: OnvifCamera.ScopeAttributeItem
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;

namespace OnvifCamera
{
    public struct ScopeAttributeItem : IEquatable<ScopeAttributeItem>
    {
        public string Full { get; set; }

        public string Token { get; set; }

        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            int hashCode1 = this.GetHashCode();
            int? hashCode2 = obj?.GetHashCode();
            int valueOrDefault = hashCode2.GetValueOrDefault();
            return hashCode1 == valueOrDefault & hashCode2.HasValue;
        }

        public override int GetHashCode() => (this.Full + this.Token + this.Name).GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(ScopeAttributeItem left, ScopeAttributeItem right) => left.Equals(right);

        public static bool operator !=(ScopeAttributeItem left, ScopeAttributeItem right) => !(left == right);

        public bool Equals(ScopeAttributeItem other) => this.Equals((object)other);
    }
}

// Decompiled with JetBrains decompiler
// Type: OnvifCamera.DiscoveryResponse
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OnvifCamera
{
    public class DiscoveryResponse
    {
        private string[] xaddrField;
        private List<string> profileField;
        private List<string> locationField;
        private List<string> hardwareField;
        private List<string> nameField;
        private List<string> scopeuriField;
        private string uuidField;
        private string remoteAddress;

        public IReadOnlyCollection<string> Profile => this.profileField == null ? (IReadOnlyCollection<string>)null : (IReadOnlyCollection<string>)this.profileField.AsReadOnly();

        public IReadOnlyCollection<string> Location => this.locationField == null ? (IReadOnlyCollection<string>)null : (IReadOnlyCollection<string>)this.locationField.AsReadOnly();

        public IReadOnlyCollection<string> Hardware => this.hardwareField == null ? (IReadOnlyCollection<string>)null : (IReadOnlyCollection<string>)this.hardwareField.AsReadOnly();

        public IReadOnlyCollection<string> Name => this.nameField == null ? (IReadOnlyCollection<string>)null : (IReadOnlyCollection<string>)this.nameField.AsReadOnly();

        public IReadOnlyCollection<string> ScopeUris => this.scopeuriField == null ? (IReadOnlyCollection<string>)null : (IReadOnlyCollection<string>)this.scopeuriField.ToList<string>().AsReadOnly();

        public DiscoveryResponse(string remoteAddress, string[] Xaddrs)
        {
            this.xaddrField = Xaddrs ?? throw new ArgumentNullException();
            this.remoteAddress = remoteAddress;
        }

        public DiscoveryResponse(
          string remoteAddress,
          string[] Xaddrs,
          string Uuid,
          ScopeAttributeItem[] scopes)
        {
            this.xaddrField = Xaddrs ?? throw new ArgumentNullException();
            this.remoteAddress = remoteAddress ?? throw new ArgumentNullException();
            this.xaddrField = Xaddrs ?? throw new ArgumentNullException();
            this.uuidField = Uuid;
            if (scopes == null)
                return;
            this.ReArrangeScopes(scopes);
        }

        public ReadOnlyCollection<string> Xaddrs => ((IEnumerable<string>)this.xaddrField).ToList<string>().AsReadOnly();

        public string UUID => this.uuidField;

        public string RemoteAddress => this.remoteAddress;

        private void ReArrangeScopes(ScopeAttributeItem[] scopes)
        {
            foreach (ScopeAttributeItem scope in scopes)
            {
                if (this.scopeuriField == null)
                    this.scopeuriField = new List<string>();
                this.scopeuriField.Add(Uri.UnescapeDataString(scope.Full));
                if (scope.Token != null)
                {
                    if (scope.Token.Equals("profile", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.profileField == null)
                            this.profileField = new List<string>();
                        this.profileField.Add(Uri.UnescapeDataString(scope.Name));
                    }
                    else if (scope.Token.Equals("location", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.locationField == null)
                            this.locationField = new List<string>();
                        this.locationField.Add(Uri.UnescapeDataString(scope.Name));
                    }
                    else if (scope.Token.Equals("hardware", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.hardwareField == null)
                            this.hardwareField = new List<string>();
                        this.hardwareField.Add(Uri.UnescapeDataString(scope.Name));
                    }
                    else if (scope.Token.Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.nameField == null)
                            this.nameField = new List<string>();
                        this.nameField.Add(Uri.UnescapeDataString(scope.Name));
                    }
                }
            }
        }
    }
}

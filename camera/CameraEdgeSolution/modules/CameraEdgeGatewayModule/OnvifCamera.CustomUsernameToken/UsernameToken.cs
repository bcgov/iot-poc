// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.UsernameToken
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Xml.Serialization;

namespace OnvifCamera.CustomUsernameToken
{
    [XmlRoot(ElementName = "UsernameToken", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class UsernameToken
    {
        [XmlElement(ElementName = "Username")]
        public string Username { get; set; }

        [XmlElement(ElementName = "Password")]
        public Password Password { get; set; }

        [XmlElement(ElementName = "Nonce")]
        public Nonce Nonce { get; set; }

        [XmlElement(ElementName = "Created", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
        public string Created { get; set; }

        public UsernameToken()
        {
        }

        public UsernameToken(UsernameTokenInfo usernameTokenInfo)
        {
            this.Username = usernameTokenInfo != null ? usernameTokenInfo.GetUsername() : throw new ArgumentNullException(nameof(usernameTokenInfo));
            this.Password = new Password()
            {
                Value = usernameTokenInfo.GetPasswordDigestAsBase64(),
                Type = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"
            };
            this.Nonce = new Nonce()
            {
                Value = usernameTokenInfo.GetNonceAsBase64(),
                EncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"
            };
            this.Created = usernameTokenInfo.GetCreatedAsString();
        }
    }
}

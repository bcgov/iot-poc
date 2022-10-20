// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.UsernameTokenInfo
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace OnvifCamera.CustomUsernameToken
{
    public class UsernameTokenInfo
    {
        private UserInfo _usernameInfo;
        private DateTime _created = DateTime.Now;
        private byte[] _nonce = new byte[16];

        public UsernameTokenInfo(UserInfo userInfo)
        {
            this._usernameInfo = userInfo;
            if (!(userInfo.DateTime != new DateTime()))
                return;
            this._created = userInfo.DateTime;
        }

        public string GetUsername() => this._usernameInfo.Username;

        public string GetNonceAsBase64() => Convert.ToBase64String(this._nonce);

        public string GetCreatedAsString() => XmlConvert.ToString(this._created.ToUniversalTime(), "yyyy-MM-ddTHH:mm:ssZ");

        public string GetPasswordDigestAsBase64()
        {
            RandomNumberGenerator randomNumberGenerator = (RandomNumberGenerator)new RNGCryptoServiceProvider();
            randomNumberGenerator.GetBytes(this._nonce);
            byte[] bytes1 = Encoding.UTF8.GetBytes(this.GetCreatedAsString());
            byte[] bytes2 = Encoding.UTF8.GetBytes(this._usernameInfo.Password);
            byte[] numArray = new byte[this._nonce.Length + bytes1.Length + bytes2.Length];
            Array.Copy((Array)this._nonce, (Array)numArray, this._nonce.Length);
            Array.Copy((Array)bytes1, 0, (Array)numArray, this._nonce.Length, bytes1.Length);
            Array.Copy((Array)bytes2, 0, (Array)numArray, this._nonce.Length + bytes1.Length, bytes2.Length);
            using (SHA1 shA1 = SHA1.Create())
            {
                randomNumberGenerator.Dispose();
                return Convert.ToBase64String(shA1.ComputeHash(numArray));
            }
        }
    }
}

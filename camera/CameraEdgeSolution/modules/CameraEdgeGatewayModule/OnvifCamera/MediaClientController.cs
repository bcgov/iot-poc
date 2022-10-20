// Decompiled with JetBrains decompiler
// Type: OnvifCamera.MediaClientController
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using ServiceReference1;
using Microsoft.Extensions.Logging;
using OnvifCamera.CustomUsernameToken;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OnvifCamera
{
    public sealed class MediaClientController : IDisposable
    {
        private const string VideoTypeNotFound = "VideoType Not Found";
        private int _disposed;
        private readonly MediaClient _mediaClient;
        private readonly CameraLogger _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly NetworkCredential _creds;
        private readonly Dictionary<string, List<VideoTypeRange>> _videoTypeRangesByEncoderToken = new Dictionary<string, List<VideoTypeRange>>();

        private MediaClientController(
          Uri uri,
          string username,
          string password,
          bool fHttpDigestSupported,
          TimeOnDevice time)
        {
            this._logger = CameraLoggerConfig.CreateCameraLoggerConfig().CreateLogger((object)this, typeof(MediaClientController).ToString());
            this._mediaClient = OnvifFactory.CreateOnvifObj<MediaClient>(uri);
            OnvifFactory.SetCredentials(username, password, this._mediaClient.ClientCredentials);
            if (!fHttpDigestSupported)
                this._mediaClient.Endpoint.EndpointBehaviors.Add((IEndpointBehavior)new WsSecurityEndpointBehavior(username, password, time));
            this._creds = new NetworkCredential(username, password);
        }

        private async Task<List<VideoTypeRange>> GetTypesInVideoEncoderToken(
          Profile profile)
        {
            List<VideoTypeRange> videoTypes = new List<VideoTypeRange>();
            VideoEncoderConfiguration[] configurations;
            try
            {
                if (profile != null)
                    configurations = (await this._mediaClient.GetCompatibleVideoEncoderConfigurationsAsync(profile.token).ConfigureAwait(false)).Configurations;
                else
                    configurations = (await this._mediaClient.GetVideoEncoderConfigurationsAsync().ConfigureAwait(false)).Configurations;
                this._logger.LogObjectState(LogLevel.Debug, "GetTypesInVideoEncoderToken - GetCompatibleVideoEncoderConfigurationsAsync/GetVideoEncoderConfigurationsAsync", (object)profile, (object)configurations);
            }
            catch (Exception ex) when (ex is FaultException || ex is ProtocolException)
            {
                this._logger.LogObjectState(LogLevel.Warning, "GetTypesInVideoEncoderToken - GetCompatibleVideoEncoderConfigurationsAsync", (object)profile, (object)CameraLogger.GetExceptionString(ex));
                return videoTypes;
            }
            foreach (VideoEncoderConfiguration encoderConfiguration in configurations)
            {
                if (this._videoTypeRangesByEncoderToken.ContainsKey(encoderConfiguration.token))
                {
                    foreach (VideoTypeRange videoTypeRange in this._videoTypeRangesByEncoderToken[encoderConfiguration.token])
                        videoTypes.Add(videoTypeRange);
                }
                else
                {
                    List<string> stringList = new List<string>();
                    // OnvifCamera.IntRange intRange = new OnvifCamera.IntRange();
                    List<VideoTypeRange> videoTypeRangeList = new List<VideoTypeRange>();
                    this._videoTypeRangesByEncoderToken.Add(encoderConfiguration.token, videoTypeRangeList);
                    VideoEncoderConfigurationOptions result = this._mediaClient.GetVideoEncoderConfigurationOptionsAsync(encoderConfiguration.token, (string)null).Result;
                    this._logger.LogObjectState(LogLevel.Debug, "GetTypesInVideoEncoderToken - GetVideoEncoderConfigurationOptionsAsync", (object)encoderConfiguration.token, (object)result);
                    VideoTypeRange videoTypeRange1;
                    if (result.H264 != null)
                    {
                        
                        foreach (H264Profile h264Profile in result.H264.H264ProfilesSupported)
                            stringList.Add(h264Profile.ToString());
                        videoTypeRange1 = new VideoTypeRange();
                        videoTypeRange1.EncoderToken = encoderConfiguration.token;
                        videoTypeRange1.SubType = "H264";
                        videoTypeRange1.EncoderProfiles = (IReadOnlyList<string>)stringList.ToArray();
                        VideoTypeRange videoTypeRange2 = videoTypeRange1;
                        videoTypes.Add(videoTypeRange2);
                        videoTypeRangeList.Add(videoTypeRange2);
                    }
                    if (result.JPEG != null)
                    {
                        
                        videoTypeRange1 = new VideoTypeRange();
                        videoTypeRange1.EncoderToken = encoderConfiguration.token;
                        videoTypeRange1.SubType = "JPEG";
                        videoTypeRange1.EncoderProfiles = (IReadOnlyList<string>)null;
                        VideoTypeRange videoTypeRange3 = videoTypeRange1;
                        videoTypes.Add(videoTypeRange3);
                        videoTypeRangeList.Add(videoTypeRange3);
                    }
                    if (result.MPEG4 != null)
                    {
                        
                    }
                }
            }
            return videoTypes;
        }

        private VideoType FindType(List<VideoTypeRange> encoderTypes, VideoType videoType)
        {
            bool flag1 = false;
            foreach (VideoTypeRange encoderType in encoderTypes)
            {
                if (videoType.EncoderProfile != null && string.Compare(videoType.SubType, "JPEG", StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    bool flag2 = false;
                    if (encoderType.EncoderProfiles != null)
                    {
                        foreach (string encoderProfile in (IEnumerable<string>)encoderType.EncoderProfiles)
                        {
                            if (string.Compare(encoderProfile, videoType.EncoderProfile, StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                    }
                    if (!flag2)
                        continue;
                }
                if (string.Compare(videoType.EncoderToken, encoderType.EncoderToken, StringComparison.InvariantCultureIgnoreCase) == 0 && string.Compare(videoType.SubType, encoderType.SubType, StringComparison.InvariantCultureIgnoreCase) == 0 && MediaClientController.ValueInRange(videoType.FrameRate, encoderType.FrameRateRange) && (!videoType.GuaranteedFrameRate || encoderType.GuaranteedFrameRateSupported) && MediaClientController.ValueInRange(videoType.Quality, encoderType.QualityRange) && MediaClientController.ValueInRange(videoType.BitrateLimit, encoderType.BitrateRange) && MediaClientController.ValueInRange(videoType.GovLength, encoderType.GovLengthRange) && MediaClientController.ValueInRange(videoType.EncodingInterval, encoderType.EncodingIntervalRange))
                {
                    foreach (VideoResolution resolution in (IEnumerable<VideoResolution>)encoderType.Resolutions)
                    {
                        if ((videoType.Resolution.Height == -1 || videoType.Resolution.Height == resolution.Height) && (videoType.Resolution.Width == -1 || videoType.Resolution.Width == resolution.Width))
                        {
                            videoType = MediaClientController.CreateDefaults(videoType, encoderType);
                            flag1 = true;
                            break;
                        }
                    }
                    if (flag1)
                        break;
                }
            }
            if (!flag1)
                throw new ArgumentException("VideoType Not Found");
            return videoType;
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref this._disposed, 1) != 0 || !disposing)
                return;
            this._mediaClient.Close();
            this._lock?.Dispose();
            this._logger?.Dispose();
        }

        private static bool ValueInRange(int i, IntRange range)
        {
            if (i == 0 || range.Min == 0 && range.Max == 0)
                return true;
            return i >= range.Min && i <= range.Max;
        }

        private static VideoType CreateDefaults(
          VideoType videoType,
          VideoTypeRange videoTypeRange)
        {
            if (videoType.EncoderToken == null)
                videoType.EncoderToken = videoTypeRange.EncoderToken;
            if (videoType.SubType == null)
                videoType.SubType = videoTypeRange.SubType;
            if (videoType.FrameRate <= 0)
                videoType.FrameRate = videoTypeRange.FrameRateRange.Max;
            IntRange intRange;
            if (videoType.Quality <= 0)
            {
                ref VideoType local = ref videoType;
                int max = videoTypeRange.QualityRange.Max;
                intRange = videoTypeRange.QualityRange;
                int min = intRange.Min;
                int num = (int)(((double)(max + min) + 0.5) / 2.0);
                local.Quality = num;
            }
            if (videoType.BitrateLimit <= 0)
            {
                ref VideoType local = ref videoType;
                intRange = videoTypeRange.BitrateRange;
                int max = intRange.Max;
                local.BitrateLimit = max;
            }
            if (videoType.EncodingInterval <= 0)
            {
                ref VideoType local = ref videoType;
                intRange = videoTypeRange.EncodingIntervalRange;
                int num;
                if (intRange.Min != 1)
                {
                    intRange = videoTypeRange.EncodingIntervalRange;
                    int max = intRange.Max;
                    intRange = videoTypeRange.EncodingIntervalRange;
                    int min = intRange.Min;
                    num = (int)(((double)(max + min) + 0.5) / 2.0);
                }
                else
                    num = 1;
                local.EncodingInterval = num;
            }
            if (string.Compare(videoType.SubType, "JPEG", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                if (videoType.GovLength <= 0)
                {
                    if (videoType.FrameRate != 0)
                    {
                        int frameRate1 = videoType.FrameRate;
                        intRange = videoTypeRange.GovLengthRange;
                        int max = intRange.Max;
                        if (frameRate1 <= max)
                        {
                            int frameRate2 = videoType.FrameRate;
                            intRange = videoTypeRange.GovLengthRange;
                            int min = intRange.Min;
                            if (frameRate2 >= min)
                            {
                                videoType.GovLength = videoType.FrameRate;
                                goto label_22;
                            }
                        }
                    }
                    ref VideoType local = ref videoType;
                    intRange = videoTypeRange.GovLengthRange;
                    int max1 = intRange.Max;
                    intRange = videoTypeRange.GovLengthRange;
                    int min1 = intRange.Min;
                    int num = (int)(((double)(max1 + min1) + 0.5) / 2.0);
                    local.GovLength = num;
                }
            label_22:
                if (videoType.EncoderProfile == null)
                    videoType.EncoderProfile = videoTypeRange.EncoderProfiles?[0] ?? (string)null;
            }
            if (videoType.Resolution.Width == -1 && videoType.Resolution.Height == -1)
            {
                ref VideoType local1 = ref videoType;
                VideoResolution videoResolution1 = new VideoResolution();
                ref VideoResolution local2 = ref videoResolution1;
                VideoResolution resolution = videoTypeRange.Resolutions[0];
                int width = resolution.Width;
                local2.Width = width;
                ref VideoResolution local3 = ref videoResolution1;
                resolution = videoTypeRange.Resolutions[0];
                int height = resolution.Height;
                local3.Height = height;
                VideoResolution videoResolution2 = videoResolution1;
                local1.Resolution = videoResolution2;
            }
            return videoType;
        }

        internal static MediaClientController CreateMediaClientController(
          Uri uri,
          string username,
          string password,
          bool fHttpDigestSupported,
          TimeOnDevice time)
        {
            return new MediaClientController(uri, username, password, fHttpDigestSupported, time);
        }

        ~MediaClientController() => this.Dispose(false);

        public async Task<ProfileInfo[]> GetCameraProfilesAsync()
        {
            List<ProfileInfo> profileInfoList = (List<ProfileInfo>)null;
            await this._lock.WaitAsync().ConfigureAwait(false);
            try
            {
                profileInfoList = new List<ProfileInfo>();
                foreach (Profile profile in (await this._mediaClient.GetProfilesAsync().ConfigureAwait(false)).Profiles)
                    profileInfoList.Add(new ProfileInfo(profile.Name, profile.token, !profile.fixedSpecified || !profile.@fixed));
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetCameraProfilesAsync), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            finally
            {
                this._lock.Release();
            }
            ProfileInfo[] cameraProfilesAsync = profileInfoList?.ToArray() ?? (ProfileInfo[])null;
            profileInfoList = (List<ProfileInfo>)null;
            return cameraProfilesAsync;
        }

        public async Task<SourceConfig[]> GetSourceConfigsAsync(string profileToken = null)
        {
            VideoSourceConfiguration[] configurations;
            try
            {
                if (profileToken != null)
                    configurations = (await this._mediaClient.GetCompatibleVideoSourceConfigurationsAsync(profileToken).ConfigureAwait(false)).Configurations;
                else
                    configurations = (await this._mediaClient.GetVideoSourceConfigurationsAsync().ConfigureAwait(false)).Configurations;
                this._logger.LogObjectState(LogLevel.Debug, "GetSourceConfigsAsync - GetCompatibleVideoSourceConfigurationsAsync/GetVideoSourceConfigurationsAsync", (object)profileToken, (object)configurations);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetSourceConfigsAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            
            return null;// ((IEnumerable<VideoSourceConfiguration>)configurations).Select<VideoSourceConfiguration, SourceConfig>((Func<VideoSourceConfiguration, SourceConfig>)(x => (SourceConfig)x)).ToArray<SourceConfig>();
        }

        public async Task<string> GetProfileSourceConfigTokenAsync(string profileToken)
        {
            string configTokenAsync;
            try
            {
                Profile output = await this._mediaClient.GetProfileAsync(profileToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetProfileSourceConfigTokenAsync - GetProfileAsync", (object)profileToken, (object)output);
                configTokenAsync = output.VideoSourceConfiguration == null ? (string)null : output.VideoSourceConfiguration.token;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetProfileSourceConfigTokenAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return configTokenAsync;
        }

        public async Task<SourceConfig> GetSourceConfigAsync(string sourceConfigToken)
        {
            VideoSourceConfiguration output;
            try
            {
                output = await this._mediaClient.GetVideoSourceConfigurationAsync(sourceConfigToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetSourceConfigAsync - GetVideoSourceConfigurationAsync", (object)sourceConfigToken, (object)output);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetSourceConfigAsync), (object)sourceConfigToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            
            return new SourceConfig();// (SourceConfig)output;
        }

        public async Task SetSourceConfigAsync(string profileToken, string videoSourceConfigToken)
        {
            try
            {
                await this._mediaClient.AddVideoSourceConfigurationAsync(profileToken, videoSourceConfigToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetSourceConfigAsync), (object)(profileToken, videoSourceConfigToken), (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<string> TryCreateProfileAsync(string profileName, string profileToken = null)
        {
            bool flag = false;
            await this._lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (profileToken != null)
                {
                    Regex regex = new Regex("^[A-Za-z0-9\\-\\.]+$");
                    if (profileToken.Length > 12 || !regex.IsMatch(profileToken))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid profileToken: {0}. ProfileToken has length at most 12 and its characters are contained in the set '\"A - Z\" | \"a - z\" | \"0 - 9\" | \" -.\"'", (object)profileToken));
                }
                Profile profile = await this._mediaClient.CreateProfileAsync(profileName, profileToken).ConfigureAwait(false);
                if (profileToken == null || string.Compare(profile.token, profileToken, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    this._logger.LogObjectState(LogLevel.Debug, "TryCreateProfileAsync - CreateProfileAsync: Device alters token", (object)profileToken, (object)profile.token);
                    profileToken = profile.token;
                }
                this._logger.LogObjectState(LogLevel.Debug, "TryCreateProfileAsync - CreateProfileAsync Success", (object)(profileName, profileToken), (object)true);
                flag = true;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(TryCreateProfileAsync), (object)(profileName, profileToken), (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            finally
            {
                this._lock.Release();
            }
            return flag ? profileToken : (string)null;
        }

        public async Task<bool> TryDeleteProfileAsync(string profileToken)
        {
            bool found = false;
            await this._lock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (Profile profile in (await this._mediaClient.GetProfilesAsync().ConfigureAwait(false)).Profiles)
                {
                    if (string.Compare(profile.token, profileToken, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        await this._mediaClient.DeleteProfileAsync(profileToken).ConfigureAwait(false);
                        this._logger.LogObjectState(LogLevel.Debug, "TryDeleteProfileAsync - DeleteProfileAsync", (object)profileToken, (object)true);
                        found = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(TryDeleteProfileAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            finally
            {
                this._lock.Release();
            }
            return found;
        }

        public async Task<VideoTypeRange[]> GetVideoTypeRangesAsync(
          string profileToken)
        {
            await this._lock.WaitAsync().ConfigureAwait(false);
            HashSet<VideoTypeRange> types;
            try
            {
                types = new HashSet<VideoTypeRange>();
                if (string.IsNullOrEmpty(profileToken))
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid null or empty profile token value: {0}", (object)profileToken));
                foreach (VideoTypeRange videoTypeRange in await this.GetTypesInVideoEncoderToken(await this._mediaClient.GetProfileAsync(profileToken).ConfigureAwait(false)).ConfigureAwait(false))
                    types.Add(videoTypeRange);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetVideoTypeRangesAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            finally
            {
                this._lock.Release();
            }
            HashSet<VideoTypeRange> source = types;
            VideoTypeRange[] videoTypeRangesAsync = source != null ? source.ToArray<VideoTypeRange>() : (VideoTypeRange[])null;
            types = (HashSet<VideoTypeRange>)null;
            return videoTypeRangesAsync;
        }

        public async Task SetVideoTypeAsync(string profileToken, VideoType videoType)
        {
            await this._lock.WaitAsync().ConfigureAwait(false);
            try
            {
                Profile profile = await this._mediaClient.GetProfileAsync(profileToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "FindType - GetProfileAsync", (object)profileToken, (object)profile);
                VideoType foundVideoType = this.FindType(await this.GetTypesInVideoEncoderToken(profile).ConfigureAwait(false), videoType);
                VideoEncoderConfiguration output = await this._mediaClient.GetVideoEncoderConfigurationAsync(videoType.EncoderToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "SetVideoTypeAsync - GetVideoEncoderConfigurationAsync", (object)videoType.EncoderToken, (object)output);
                VideoEncoderConfiguration encoderConfiguration = new VideoEncoderConfiguration();
                encoderConfiguration.token = foundVideoType.EncoderToken;
                encoderConfiguration.Name = output.Name;
                encoderConfiguration.UseCount = output.UseCount;
                encoderConfiguration.GuaranteedFrameRate = foundVideoType.GuaranteedFrameRate;
                encoderConfiguration.GuaranteedFrameRateSpecified = output.GuaranteedFrameRateSpecified;
                encoderConfiguration.Encoding = Enum.Parse<VideoEncoding>(foundVideoType.SubType, true);
                encoderConfiguration.RateControl = output.RateControl;
                encoderConfiguration.Multicast = output.Multicast;
                encoderConfiguration.SessionTimeout = output.SessionTimeout;
                encoderConfiguration.Quality = output.Quality;
                VideoEncoderConfiguration videoEncoderConfig = encoderConfiguration;
                if (videoType.Quality > 0)
                    videoEncoderConfig.Quality = (float)videoType.Quality;
                if (output.token == foundVideoType.EncoderToken && output.Encoding == Enum.Parse<VideoEncoding>(foundVideoType.SubType, true) && videoType.Resolution.Width == -1 && videoType.Resolution.Height == -1)
                    videoEncoderConfig.Resolution = output.Resolution;
                if (output.RateControl == null && (foundVideoType.FrameRate != 0 || foundVideoType.BitrateLimit != 0 || foundVideoType.EncodingInterval != 0))
                    videoEncoderConfig.RateControl = new VideoRateControl()
                    {
                        FrameRateLimit = foundVideoType.FrameRate,
                        BitrateLimit = foundVideoType.BitrateLimit,
                        EncodingInterval = foundVideoType.EncodingInterval
                    };
                if (videoType.FrameRate > 0)
                    videoEncoderConfig.RateControl.FrameRateLimit = videoType.FrameRate;
                if (videoType.BitrateLimit > 0)
                    videoEncoderConfig.RateControl.BitrateLimit = videoType.BitrateLimit;
                if (videoType.EncodingInterval > 0)
                    videoEncoderConfig.RateControl.EncodingInterval = videoType.EncodingInterval;
                if (string.Compare(videoType.SubType, "JPEG", StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    if (videoEncoderConfig.Encoding == VideoEncoding.H264)
                    {
                        videoEncoderConfig.H264 = new H264Configuration();
                        videoEncoderConfig.H264.H264Profile = string.IsNullOrEmpty(videoType.EncoderProfile) ? (output.Encoding != VideoEncoding.H264 || output.H264 == null ? Enum.Parse<H264Profile>(foundVideoType.EncoderProfile, true) : Enum.Parse<H264Profile>(output.H264.H264Profile.ToString(), true)) : Enum.Parse<H264Profile>(videoType.EncoderProfile, true);
                        videoEncoderConfig.H264.GovLength = videoType.GovLength == 0 ? (output.Encoding != VideoEncoding.H264 || output.H264 == null ? foundVideoType.GovLength : output.H264.GovLength) : videoType.GovLength;
                    }
                    else if (videoEncoderConfig.Encoding == VideoEncoding.MPEG4)
                    {
                        videoEncoderConfig.MPEG4 = new Mpeg4Configuration();
                        videoEncoderConfig.MPEG4.Mpeg4Profile = string.IsNullOrEmpty(videoType.EncoderProfile) ? (output.Encoding != VideoEncoding.MPEG4 || output.MPEG4 == null ? Enum.Parse<Mpeg4Profile>(foundVideoType.EncoderProfile, true) : Enum.Parse<Mpeg4Profile>(output.MPEG4.Mpeg4Profile.ToString(), true)) : Enum.Parse<Mpeg4Profile>(videoType.EncoderProfile, true);
                        videoEncoderConfig.MPEG4.GovLength = videoType.GovLength == 0 ? (output.Encoding != VideoEncoding.MPEG4 || output.MPEG4 == null ? foundVideoType.GovLength : output.MPEG4.GovLength) : videoType.GovLength;
                    }
                }
                await this._mediaClient.SetVideoEncoderConfigurationAsync(videoEncoderConfig, true).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "SetVideoTypeAsync - SetVideoEncoderConfigurationAsync", (object)(videoEncoderConfig, true), (object)null);
                await this._mediaClient.AddVideoEncoderConfigurationAsync(profileToken, videoEncoderConfig.token).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "SetVideoTypeAsync - AddVideoEncoderConfigurationAsync", (object)(profileToken, videoEncoderConfig.token), (object)null);
                foundVideoType = new VideoType();
                videoEncoderConfig = (VideoEncoderConfiguration)null;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetVideoTypeAsync), (object)(profileToken, videoType), (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            finally
            {
                this._lock.Release();
            }
        }

        public async Task<VideoType> GetVideoTypeAsync(string profileToken)
        {
            VideoType videoType = new VideoType();
            try
            {
                Profile profile = await this._mediaClient.GetProfileAsync(profileToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetVideoTypeAsync - GetProfileAsync", (object)profileToken, (object)profile);
                if (profile.VideoEncoderConfiguration == null)
                    throw new UnknownVideoTypeStateException("Either videotype is not set or type is not supported");
                VideoEncoderConfiguration output = await this._mediaClient.GetVideoEncoderConfigurationAsync(profile.VideoEncoderConfiguration.token).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetVideoTypeAsync - GetVideoEncoderConfigurationAsync", (object)profile.VideoEncoderConfiguration.token, (object)output);
                VideoType local1 = videoType;
                VideoRateControl rateControl = output.RateControl;
                int num1 = rateControl != null ? rateControl.BitrateLimit : 0;
                local1.BitrateLimit = num1;
                videoType.EncoderToken = output.token;
                videoType.EncodingInterval = output != null ? output.RateControl.EncodingInterval : 1;
                videoType.FrameRate = output != null ? output.RateControl.FrameRateLimit : 0;
                videoType.GuaranteedFrameRate = output.GuaranteedFrameRateSpecified && output.GuaranteedFrameRate;
                videoType.Quality = (int)output.Quality;
                //videoType.Resolution = (VideoResolution)output.Resolution;
                videoType.SubType = output.Encoding.ToString();
                if (output.Encoding == VideoEncoding.H264 && output.H264 != null)
                {
                    VideoType local2 = videoType;
                    H264Configuration h264 = output.H264;
                    int num2 = h264 != null ? h264.GovLength : 0;
                    local2.GovLength = num2;
                    videoType.EncoderProfile = output.H264?.H264Profile.ToString() ?? (string)null;
                }
                else if (output.Encoding == VideoEncoding.MPEG4 && output.MPEG4 != null)
                {
                    VideoType local3 = videoType;
                    Mpeg4Configuration mpeG4 = output.MPEG4;
                    int num3 = mpeG4 != null ? mpeG4.GovLength : 0;
                    local3.GovLength = num3;
                    videoType.EncoderProfile = output.MPEG4?.Mpeg4Profile.ToString() ?? (string)null;
                }
                else if (output.Encoding == VideoEncoding.JPEG)
                {
                    videoType.GovLength = 0;
                    videoType.EncoderProfile = (string)null;
                }
                profile = (Profile)null;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetVideoTypeAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            VideoType videoTypeAsync = videoType;
            videoType = new VideoType();
            return videoTypeAsync;
        }

        public async Task<string> GetStreamUriAsync(
          string profileToken,
          StreamType streamType = StreamType.RTPUnicast,
          TransportProtocol transport = TransportProtocol.RTSP)
        {
            MediaUri output;
            try
            {
                StreamSetup streamSetup = new StreamSetup()
                {
                    Stream = (ServiceReference1.StreamType)streamType,
                    Transport = new Transport()
                    {
                        Protocol = (ServiceReference1.TransportProtocol)transport
                    }
                };
                output = await this._mediaClient.GetStreamUriAsync(streamSetup, profileToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetVideoTypeAsync - GetStreamUriAsync", (object)(streamSetup, profileToken), (object)output);
                streamSetup = (StreamSetup)null;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetStreamUriAsync), (object)(profileToken, streamType, transport), (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return output?.Uri;
        }

        public async Task<byte[]> GetSnapshotAsync(string profileToken, int msTimeout = -1)
        {
            HttpClientHandler httpHandler = new HttpClientHandler()
            {
                Credentials = (ICredentials)this._creds
            };
            HttpClient httpClient = new HttpClient((HttpMessageHandler)httpHandler);
            byte[] numArray;
            try
            {
                MediaUri output = await this._mediaClient.GetSnapshotUriAsync(profileToken).ConfigureAwait(false);
                Uri requestUri = new Uri(output.Uri);
                this._logger.LogObjectState(LogLevel.Debug, "GetSnapshotAsync - GetSnapshotUriAsync", (object)profileToken, (object)output);
                if (msTimeout != -1)
                    httpClient.Timeout = new TimeSpan(0, 0, 0, 0, msTimeout);
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(requestUri).ConfigureAwait(false);
                httpResponseMessage.EnsureSuccessStatusCode();
                numArray = await httpResponseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetSnapshotAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            finally
            {
                httpClient.Dispose();
                httpHandler.Dispose();
            }
            byte[] snapshotAsync = numArray;
            httpHandler = (HttpClientHandler)null;
            httpClient = (HttpClient)null;
            return snapshotAsync;
        }

        public async Task<string> GetSnapshotURIAsync(string profileToken)
        {
            string uri;
            try
            {
                MediaUri output = await this._mediaClient.GetSnapshotUriAsync(profileToken).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetSnapshotURIAsync - GetSnapshotUriAsync", (object)profileToken, (object)output);
                uri = output.Uri;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetSnapshotURIAsync), (object)profileToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return uri;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }
    }
}

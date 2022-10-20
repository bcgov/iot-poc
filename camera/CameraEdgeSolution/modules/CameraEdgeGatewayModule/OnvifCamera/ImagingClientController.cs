// Decompiled with JetBrains decompiler
// Type: OnvifCamera.ImagingClientController
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll
using ServiceReference4;//image
using Microsoft.Extensions.Logging;
using OnvifCamera.CustomUsernameToken;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace OnvifCamera
{
    public sealed class ImagingClientController : IDisposable
    {
        private readonly ImagingPortClient _imagingClient;
        private readonly CameraLogger _logger;
        private readonly string _videosourcetoken;
        private ImagingSettingCapabilities _imagingCapabilities;
        private bool _disposed;

        private async Task GetImagingOptionsAsync(string strVideoSourceToken)
        {
            try
            {
                ImagingOptions20 imagingOptions20 = await this._imagingClient.GetOptionsAsync(strVideoSourceToken).ConfigureAwait(false);
                ImagingSettingCapabilities settingCapabilities = new ImagingSettingCapabilities();
                settingCapabilities.Brightness = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Brightness);
                settingCapabilities.ColorSaturation = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.ColorSaturation);
                settingCapabilities.Contrast = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Contrast);
                if (imagingOptions20.Exposure != null)
                {
                    ExposureCapabilities exposureCapabilities = new ExposureCapabilities();
                    if (imagingOptions20.Exposure.Mode.Length != 0)
                        exposureCapabilities.SupportedModes = this.ConvertOnvifModesToControlMode<ExposureMode>(imagingOptions20.Exposure.Mode);
                    exposureCapabilities.MinGain = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.MinGain);
                    exposureCapabilities.MaxGain = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.MaxGain);
                    exposureCapabilities.ExposureTime = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.ExposureTime);
                    exposureCapabilities.MinIris = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.MinIris);
                    exposureCapabilities.MaxIris = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.MaxIris);
                    exposureCapabilities.Gain = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.Gain);
                    exposureCapabilities.Iris = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Exposure.Iris);
                    settingCapabilities.Exposure = exposureCapabilities;
                }
                if (imagingOptions20.Focus != null)
                {
                    FocusCapabilities focusCapabilities = new FocusCapabilities();
                    if (imagingOptions20.Focus.AutoFocusModes.Length != 0)
                        focusCapabilities.SupportedModes = this.ConvertOnvifModesToControlMode<AutoFocusMode>(imagingOptions20.Focus.AutoFocusModes);
                    focusCapabilities.DefaultSpeed = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Focus.DefaultSpeed);
                    focusCapabilities.NearLimit = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Focus.NearLimit);
                    focusCapabilities.FarLimit = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Focus.FarLimit);
                    settingCapabilities.Focus = focusCapabilities;
                }
                if (imagingOptions20.WideDynamicRange != null)
                {
                    WideDynamicRangeCapabilities rangeCapabilities = new WideDynamicRangeCapabilities();
                    if (imagingOptions20.WideDynamicRange.Mode.Length != 0)
                        rangeCapabilities.SupportedModes = this.ConvertOnvifModesToControlMode<WideDynamicMode>(imagingOptions20.WideDynamicRange.Mode);
                    rangeCapabilities.Level = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.WideDynamicRange.Level);
                    settingCapabilities.WideDynamicRange = rangeCapabilities;
                }
                if (imagingOptions20.WhiteBalance != null)
                {
                    WhiteBalanceCapabilities balanceCapabilities = new WhiteBalanceCapabilities();
                    if (imagingOptions20.WhiteBalance.Mode.Length != 0)
                        balanceCapabilities.SupportedModes = this.ConvertOnvifModesToControlMode<WhiteBalanceMode>(imagingOptions20.WhiteBalance.Mode);
                    balanceCapabilities.RGain = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.WhiteBalance.YrGain);
                    balanceCapabilities.BGain = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.WhiteBalance.YbGain);
                    settingCapabilities.WhiteBalance = balanceCapabilities;
                }
                settingCapabilities.Sharpness = ImagingClientController.ConvertOnvifFloatRange(imagingOptions20.Sharpness);
                this._imagingCapabilities = settingCapabilities;
                this._logger.LogObjectState(LogLevel.Debug, "GetImagingOptionsAsync - ", (object)this._videosourcetoken, (object)this._imagingCapabilities);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetImagingOptionsAsync), (object)strVideoSourceToken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        private void Dispose(bool disposing)
        {
            if (this._disposed)
                return;
            this._disposed = true;
            if (!disposing)
                return;
            this._logger?.Dispose();
            this._imagingClient?.Close();
        }

        private ImagingClientController(
          Uri uri,
          string username,
          string password,
          string videosourcetoken,
          bool fHttpDigestSupported,
          TimeOnDevice time)
        {
            this._logger = CameraLoggerConfig.CreateCameraLoggerConfig().CreateLogger((object)this, typeof(MediaClientController).ToString());
            this._imagingClient = OnvifFactory.CreateOnvifObj<ImagingPortClient>(uri);
            OnvifFactory.SetCredentials(username, password, this._imagingClient.ClientCredentials);
            if (!fHttpDigestSupported)
                this._imagingClient.Endpoint.EndpointBehaviors.Add((IEndpointBehavior)new WsSecurityEndpointBehavior(username, password, time));
            this._videosourcetoken = videosourcetoken;
        }

        internal static async Task<ImagingClientController> CreateImagingClientController(
          Uri uri,
          string username,
          string password,
          string videosourcetoken,
          bool fHttpDigestSupported,
          TimeOnDevice time)
        {
            ImagingClientController ic = new ImagingClientController(uri, username, password, videosourcetoken, fHttpDigestSupported, time);
            await ic.GetImagingOptionsAsync(videosourcetoken).ConfigureAwait(false);
            ImagingClientController clientController = ic;
            ic = (ImagingClientController)null;
            return clientController;
        }

        ~ImagingClientController() => this.Dispose(false);

        public ImagingSettingCapabilities GetImagingCapabilities() => this._imagingCapabilities;

        public async Task<float> GetBrightnessAsync()
        {
            float brightness;
            try
            {
                if (!(this._imagingCapabilities.Brightness != (FloatRange)null))
                    throw new InvalidOperationException();
                brightness = (await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false)).Brightness;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetBrightnessAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return brightness;
        }

        public async Task SetBrightnessAsync(float value)
        {
            try
            {
                if (!(this._imagingCapabilities.Brightness != (FloatRange)null))
                    throw new InvalidOperationException();
                if ((double)value < (double)this._imagingCapabilities.Brightness.Min || (double)value > (double)this._imagingCapabilities.Brightness.Max)
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Brightness value: {0}", (object)value));
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, new ImagingSettings20()
                {
                    Brightness = value,
                    BrightnessSpecified = true
                }, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetBrightnessAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<float> GetContrastAsync()
        {
            float contrast;
            try
            {
                if (!(this._imagingCapabilities.Contrast != (FloatRange)null))
                    throw new InvalidOperationException();
                contrast = (await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false)).Contrast;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetContrastAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return contrast;
        }

        public async Task SetContrastAsync(float value)
        {
            try
            {
                if (!(this._imagingCapabilities.Contrast != (FloatRange)null))
                    throw new InvalidOperationException();
                if ((double)value < (double)this._imagingCapabilities.Contrast.Min || (double)value > (double)this._imagingCapabilities.Contrast.Max)
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Contrast value: {0}", (object)value));
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, new ImagingSettings20()
                {
                    Contrast = value,
                    ContrastSpecified = true
                }, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetContrastAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<float> GetSharpnessAsync()
        {
            float sharpness;
            try
            {
                if (!(this._imagingCapabilities.Sharpness != (FloatRange)null))
                    throw new InvalidOperationException();
                sharpness = (await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false)).Sharpness;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetSharpnessAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return sharpness;
        }

        public async Task SetSharpnessAsync(float value)
        {
            try
            {
                if (!(this._imagingCapabilities.Sharpness != (FloatRange)null))
                    throw new InvalidOperationException();
                if ((double)value < (double)this._imagingCapabilities.Sharpness.Min || (double)value > (double)this._imagingCapabilities.Sharpness.Max)
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Sharpness value: {0}", (object)value));
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, new ImagingSettings20()
                {
                    Sharpness = value,
                    SharpnessSpecified = true
                }, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetSharpnessAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<float> GetColorSaturationAsync()
        {
            float colorSaturation;
            try
            {
                if (!(this._imagingCapabilities.ColorSaturation != (FloatRange)null))
                    throw new InvalidOperationException();
                colorSaturation = (await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false)).ColorSaturation;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetColorSaturationAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return colorSaturation;
        }

        public async Task SetColorSaturationAsync(float value)
        {
            try
            {
                if (!(this._imagingCapabilities.ColorSaturation != (FloatRange)null))
                    throw new InvalidOperationException();
                if ((double)value < (double)this._imagingCapabilities.ColorSaturation.Min || (double)value > (double)this._imagingCapabilities.ColorSaturation.Max)
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid ColorSaturation value: {0}", (object)value));
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, new ImagingSettings20()
                {
                    ColorSaturation = value,
                    ColorSaturationSpecified = true
                }, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetColorSaturationAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<ExposureSettings> GetExposureSettingsAsync()
        {
            ExposureSettings exposureSettingsAsync;
            try
            {
                ExposureCapabilities exposure = this._imagingCapabilities.Exposure;
                ImagingSettings20 imagingSettings20 = await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false);
                ExposureSettings exposureSettings = new ExposureSettings();
                if (imagingSettings20.Exposure == null)
                    throw new InvalidOperationException();
                exposureSettings.Mode = imagingSettings20.Exposure.Mode == ExposureMode.AUTO ? ControlMode.Auto : ControlMode.Manual;
                if (imagingSettings20.Exposure.Window != null && (imagingSettings20.Exposure.Window.topSpecified || imagingSettings20.Exposure.Window.bottomSpecified || imagingSettings20.Exposure.Window.leftSpecified || imagingSettings20.Exposure.Window.rightSpecified))
                {
                    Rectangle rectangle = new Rectangle();
                    if (imagingSettings20.Exposure.Window.topSpecified)
                        rectangle.Top = new float?(imagingSettings20.Exposure.Window.top);
                    if (imagingSettings20.Exposure.Window.bottomSpecified)
                        rectangle.Bottom = new float?(imagingSettings20.Exposure.Window.bottom);
                    if (imagingSettings20.Exposure.Window.leftSpecified)
                        rectangle.Left = new float?(imagingSettings20.Exposure.Window.bottom);
                    if (imagingSettings20.Exposure.Window.rightSpecified)
                        rectangle.Right = new float?(imagingSettings20.Exposure.Window.right);
                    exposureSettings.Window = new Rectangle?(rectangle);
                }
                else
                    exposureSettings.Window = new Rectangle?();
                if (imagingSettings20.Exposure.MinExposureTimeSpecified)
                    exposureSettings.MinExposureTime = new float?(imagingSettings20.Exposure.MinExposureTime);
                if (imagingSettings20.Exposure.MaxExposureTimeSpecified)
                    exposureSettings.MaxExposureTime = new float?(imagingSettings20.Exposure.MaxExposureTime);
                if (imagingSettings20.Exposure.MinGainSpecified)
                    exposureSettings.MinGain = new float?(imagingSettings20.Exposure.MinGain);
                if (imagingSettings20.Exposure.MaxGainSpecified)
                    exposureSettings.MaxGain = new float?(imagingSettings20.Exposure.MaxGain);
                if (imagingSettings20.Exposure.MinIrisSpecified)
                    exposureSettings.MinIris = new float?(imagingSettings20.Exposure.MinIris);
                if (imagingSettings20.Exposure.MaxIrisSpecified)
                    exposureSettings.MaxIris = new float?(imagingSettings20.Exposure.MaxIris);
                if (imagingSettings20.Exposure.ExposureTimeSpecified)
                    exposureSettings.ExposureTime = new float?(imagingSettings20.Exposure.ExposureTime);
                if (imagingSettings20.Exposure.GainSpecified)
                    exposureSettings.Gain = new float?(imagingSettings20.Exposure.Gain);
                if (imagingSettings20.Exposure.IrisSpecified)
                    exposureSettings.Iris = new float?(imagingSettings20.Exposure.Iris);
                exposureSettingsAsync = exposureSettings;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetExposureSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return exposureSettingsAsync;
        }

        public async Task SetExposureSettingsAsync(ExposureSettings exposure)
        {
            try
            {
                ExposureCapabilities exposure1 = this._imagingCapabilities.Exposure;
                ImagingSettings20 ImagingSettings = new ImagingSettings20();
                Exposure20 exposure20_1 = new Exposure20();
                if (exposure.Mode != ControlMode.Auto && exposure.Mode != ControlMode.Manual || !this._imagingCapabilities.Exposure.SupportedModes.Contains<ControlMode>(exposure.Mode))
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.Mode: {0}", (object)exposure.Mode));
                exposure20_1.Mode = exposure.Mode == ControlMode.Auto ? ExposureMode.AUTO : ExposureMode.MANUAL;
                float? nullable;
                if (exposure.Window.HasValue)
                {
                    Rectangle rectangle1 = exposure.Window.Value;
                    nullable = rectangle1.Top;
                    if (!nullable.HasValue)
                    {
                        rectangle1 = exposure.Window.Value;
                        nullable = rectangle1.Bottom;
                        if (!nullable.HasValue)
                        {
                            rectangle1 = exposure.Window.Value;
                            nullable = rectangle1.Left;
                            if (!nullable.HasValue)
                            {
                                rectangle1 = exposure.Window.Value;
                                nullable = rectangle1.Right;
                                if (!nullable.HasValue)
                                    goto label_17;
                            }
                        }
                    }
                    ServiceReference4.Rectangle rectangle2 = new ServiceReference4.Rectangle();
                    rectangle1 = exposure.Window.Value;
                    nullable = rectangle1.Top;
                    Rectangle? window;
                    if (nullable.HasValue)
                    {
                        ServiceReference4.Rectangle rectangle3 = rectangle2;
                        window = exposure.Window;
                        rectangle1 = window.Value;
                        nullable = rectangle1.Top;
                        double num = (double)nullable.Value;
                        rectangle3.top = (float)num;
                        rectangle2.topSpecified = true;
                    }
                    window = exposure.Window;
                    rectangle1 = window.Value;
                    nullable = rectangle1.Bottom;
                    if (nullable.HasValue)
                    {
                        ServiceReference4.Rectangle rectangle4 = rectangle2;
                        window = exposure.Window;
                        rectangle1 = window.Value;
                        nullable = rectangle1.Bottom;
                        double num = (double)nullable.Value;
                        rectangle4.bottom = (float)num;
                        rectangle2.bottomSpecified = true;
                    }
                    window = exposure.Window;
                    rectangle1 = window.Value;
                    nullable = rectangle1.Left;
                    if (nullable.HasValue)
                    {
                        ServiceReference4.Rectangle rectangle5 = rectangle2;
                        window = exposure.Window;
                        rectangle1 = window.Value;
                        nullable = rectangle1.Left;
                        double num = (double)nullable.Value;
                        rectangle5.left = (float)num;
                        rectangle2.leftSpecified = true;
                    }
                    window = exposure.Window;
                    rectangle1 = window.Value;
                    nullable = rectangle1.Right;
                    if (nullable.HasValue)
                    {
                        ServiceReference4.Rectangle rectangle6 = rectangle2;
                        window = exposure.Window;
                        rectangle1 = window.Value;
                        nullable = rectangle1.Right;
                        double num = (double)nullable.Value;
                        rectangle6.right = (float)num;
                        rectangle2.rightSpecified = true;
                    }
                    exposure20_1.Window = rectangle2;
                }
            label_17:
                nullable = exposure.MinExposureTime;
                ExposureCapabilities exposure2;
                if (nullable.HasValue)
                {
                    nullable = exposure.MinExposureTime;
                    double num1 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange minExposureTime = exposure2.MinExposureTime;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num1, minExposureTime))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.MinExposureTime: {0}", (object)exposure.MinExposureTime));
                    Exposure20 exposure20_2 = exposure20_1;
                    nullable = exposure.MinExposureTime;
                    double num2 = (double)nullable.Value;
                    exposure20_2.MinExposureTime = (float)num2;
                    exposure20_1.MinExposureTimeSpecified = true;
                }
                else
                    exposure20_1.MinExposureTimeSpecified = false;
                nullable = exposure.MinExposureTime;
                if (nullable.HasValue)
                {
                    nullable = exposure.MaxExposureTime;
                    double num3 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange maxExposureTime = exposure2.MaxExposureTime;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num3, maxExposureTime))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.MaxExposureTime: {0}", (object)exposure.MaxExposureTime));
                    Exposure20 exposure20_3 = exposure20_1;
                    nullable = exposure.MaxExposureTime;
                    double num4 = (double)nullable.Value;
                    exposure20_3.MaxExposureTime = (float)num4;
                    exposure20_1.MaxExposureTimeSpecified = true;
                }
                else
                    exposure20_1.MaxExposureTimeSpecified = false;
                nullable = exposure.MinGain;
                if (nullable.HasValue)
                {
                    nullable = exposure.MinGain;
                    double num5 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange minGain = exposure2.MinGain;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num5, minGain))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.MinGain: {0}", (object)exposure.MinGain));
                    Exposure20 exposure20_4 = exposure20_1;
                    nullable = exposure.MinGain;
                    double num6 = (double)nullable.Value;
                    exposure20_4.MinGain = (float)num6;
                    exposure20_1.MinGainSpecified = true;
                }
                else
                    exposure20_1.MinGainSpecified = false;
                nullable = exposure.MaxGain;
                if (nullable.HasValue)
                {
                    nullable = exposure.MaxGain;
                    double num7 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange maxGain = exposure2.MaxGain;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num7, maxGain))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.MaxGain: {0}", (object)exposure.MaxGain));
                    Exposure20 exposure20_5 = exposure20_1;
                    nullable = exposure.MaxGain;
                    double num8 = (double)nullable.Value;
                    exposure20_5.MaxGain = (float)num8;
                    exposure20_1.MaxGainSpecified = true;
                }
                else
                    exposure20_1.MaxGainSpecified = false;
                nullable = exposure.MinIris;
                if (nullable.HasValue)
                {
                    nullable = exposure.MinIris;
                    double num9 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange minIris = exposure2.MinIris;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num9, minIris))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.MinIris: {0}", (object)exposure.MinIris));
                    Exposure20 exposure20_6 = exposure20_1;
                    nullable = exposure.MinIris;
                    double num10 = (double)nullable.Value;
                    exposure20_6.MinIris = (float)num10;
                    exposure20_1.MinIrisSpecified = true;
                }
                else
                    exposure20_1.MinIrisSpecified = false;
                nullable = exposure.MaxIris;
                if (nullable.HasValue)
                {
                    nullable = exposure.MaxIris;
                    double num11 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange maxIris = exposure2.MaxIris;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num11, maxIris))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.MaxIris: {0}", (object)exposure.MaxIris));
                    Exposure20 exposure20_7 = exposure20_1;
                    nullable = exposure.MaxIris;
                    double num12 = (double)nullable.Value;
                    exposure20_7.MaxIris = (float)num12;
                    exposure20_1.MaxIrisSpecified = true;
                }
                else
                    exposure20_1.MaxIrisSpecified = false;
                nullable = exposure.ExposureTime;
                if (nullable.HasValue)
                {
                    nullable = exposure.ExposureTime;
                    double num13 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange exposureTime = exposure2.ExposureTime;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num13, exposureTime))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.ExposureTime: {0}", (object)exposure.ExposureTime));
                    Exposure20 exposure20_8 = exposure20_1;
                    nullable = exposure.ExposureTime;
                    double num14 = (double)nullable.Value;
                    exposure20_8.ExposureTime = (float)num14;
                    exposure20_1.ExposureTimeSpecified = true;
                }
                else
                    exposure20_1.ExposureTimeSpecified = false;
                nullable = exposure.Iris;
                if (nullable.HasValue)
                {
                    nullable = exposure.Iris;
                    double num15 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange iris = exposure2.Iris;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num15, iris))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.Iris: {0}", (object)exposure.Iris));
                    Exposure20 exposure20_9 = exposure20_1;
                    nullable = exposure.Iris;
                    double num16 = (double)nullable.Value;
                    exposure20_9.Iris = (float)num16;
                    exposure20_1.IrisSpecified = true;
                }
                else
                    exposure20_1.IrisSpecified = false;
                nullable = exposure.Gain;
                if (nullable.HasValue)
                {
                    nullable = exposure.Gain;
                    double num17 = (double)nullable.Value;
                    exposure2 = this._imagingCapabilities.Exposure;
                    FloatRange gain = exposure2.Gain;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num17, gain))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Exposure.Gain: {0}", (object)exposure.Gain));
                    Exposure20 exposure20_10 = exposure20_1;
                    nullable = exposure.Gain;
                    double num18 = (double)nullable.Value;
                    exposure20_10.Gain = (float)num18;
                    exposure20_1.GainSpecified = true;
                }
                else
                    exposure20_1.GainSpecified = false;
                ImagingSettings.Exposure = exposure20_1;
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, ImagingSettings, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetExposureSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<WideDynamicRangeSettings> GetWideDynamicRangeSettingsAsync()
        {
            WideDynamicRangeSettings rangeSettingsAsync;
            try
            {
                WideDynamicRangeCapabilities wideDynamicRange = this._imagingCapabilities.WideDynamicRange;
                ImagingSettings20 imagingSettings20 = await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false);
                WideDynamicRangeSettings dynamicRangeSettings = new WideDynamicRangeSettings();
                if (imagingSettings20.WideDynamicRange == null)
                    throw new InvalidOperationException();
                ControlMode result = ControlMode.Auto;
                if (!Enum.TryParse<ControlMode>(imagingSettings20.WideDynamicRange.Mode.ToString(), true, out result))
                    throw new InvalidOperationException("Unable to parse device WideDynamicRange mode");
                dynamicRangeSettings.Mode = result;
                if (imagingSettings20.WideDynamicRange.LevelSpecified)
                    dynamicRangeSettings.Level = new float?(imagingSettings20.WideDynamicRange.Level);
                rangeSettingsAsync = dynamicRangeSettings;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetWideDynamicRangeSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return rangeSettingsAsync;
        }

        public async Task SetWideDynamicRangeSettingsAsync(WideDynamicRangeSettings wdrSettings)
        {
            try
            {
                WideDynamicRangeCapabilities wideDynamicRange = this._imagingCapabilities.WideDynamicRange;
                ImagingSettings20 ImagingSettings = new ImagingSettings20();
                WideDynamicRange20 wideDynamicRange20 = new WideDynamicRange20();
                if (wdrSettings.Mode != ControlMode.On && wdrSettings.Mode != ControlMode.Off || !this._imagingCapabilities.WideDynamicRange.SupportedModes.Contains<ControlMode>(wdrSettings.Mode))
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid WideDynamicRange.Mode: {0}", (object)wdrSettings.Mode));
                wideDynamicRange20.Mode = wdrSettings.Mode == ControlMode.On ? WideDynamicMode.ON : WideDynamicMode.OFF;
                if (wdrSettings.Level.HasValue)
                {
                    if (!ImagingClientController.CheckValueInFloatRange(wdrSettings.Level.Value, this._imagingCapabilities.WideDynamicRange.Level))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid WideDynamicRange.Level: {0}", (object)wdrSettings.Level));
                    wideDynamicRange20.Level = wdrSettings.Level.Value;
                    wideDynamicRange20.LevelSpecified = true;
                }
                else
                    wideDynamicRange20.LevelSpecified = false;
                ImagingSettings.WideDynamicRange = wideDynamicRange20;
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, ImagingSettings, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetWideDynamicRangeSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<FocusSettings> GetFocusSettingsAsync()
        {
            FocusSettings focusSettingsAsync;
            try
            {
                FocusCapabilities focus = this._imagingCapabilities.Focus;
                ImagingSettings20 imagingSettings20 = await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false);
                FocusSettings focusSettings = new FocusSettings();
                if (imagingSettings20.Focus == null)
                    throw new InvalidOperationException();
                ControlMode result = ControlMode.Auto;
                if (!Enum.TryParse<ControlMode>(imagingSettings20.Focus.AutoFocusMode.ToString(), true, out result))
                    throw new InvalidOperationException("Unable to parse device Focus mode");
                focusSettings.AutoFocusMode = result;
                if (imagingSettings20.Focus.DefaultSpeedSpecified)
                    focusSettings.DefaultSpeed = new float?(imagingSettings20.Focus.DefaultSpeed);
                if (imagingSettings20.Focus.NearLimitSpecified)
                    focusSettings.NearLimit = new float?(imagingSettings20.Focus.NearLimit);
                if (imagingSettings20.Focus.FarLimitSpecified)
                    focusSettings.FarLimit = new float?(imagingSettings20.Focus.FarLimit);
                focusSettingsAsync = focusSettings;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetFocusSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return focusSettingsAsync;
        }

        public async Task SetFocusSettingsAsync(FocusSettings focus)
        {
            try
            {
                FocusCapabilities focus1 = this._imagingCapabilities.Focus;
                ImagingSettings20 ImagingSettings = new ImagingSettings20();
                FocusConfiguration20 focusConfiguration20_1 = new FocusConfiguration20();
                if (focus.AutoFocusMode != ControlMode.Auto && focus.AutoFocusMode != ControlMode.Manual || !this._imagingCapabilities.Exposure.SupportedModes.Contains<ControlMode>(focus.AutoFocusMode))
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Focus.AutoFocusMode: {0}", (object)focus.AutoFocusMode));
                focusConfiguration20_1.AutoFocusMode = focus.AutoFocusMode == ControlMode.Auto ? AutoFocusMode.AUTO : AutoFocusMode.MANUAL;
                FocusCapabilities focus2;
                float? nullable;
                if (focus.DefaultSpeed.HasValue)
                {
                    double num1 = (double)focus.DefaultSpeed.Value;
                    focus2 = this._imagingCapabilities.Focus;
                    FloatRange defaultSpeed = focus2.DefaultSpeed;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num1, defaultSpeed))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Focus.DefaultSpeed: {0}", (object)focus.DefaultSpeed));
                    FocusConfiguration20 focusConfiguration20_2 = focusConfiguration20_1;
                    nullable = focus.DefaultSpeed;
                    double num2 = (double)nullable.Value;
                    focusConfiguration20_2.DefaultSpeed = (float)num2;
                    focusConfiguration20_1.DefaultSpeedSpecified = true;
                }
                else
                    focusConfiguration20_1.DefaultSpeedSpecified = false;
                nullable = focus.NearLimit;
                if (nullable.HasValue)
                {
                    nullable = focus.NearLimit;
                    double num3 = (double)nullable.Value;
                    focus2 = this._imagingCapabilities.Focus;
                    FloatRange nearLimit = focus2.NearLimit;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num3, nearLimit))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Focus.NearLimit: {0}", (object)focus.NearLimit));
                    FocusConfiguration20 focusConfiguration20_3 = focusConfiguration20_1;
                    nullable = focus.NearLimit;
                    double num4 = (double)nullable.Value;
                    focusConfiguration20_3.NearLimit = (float)num4;
                    focusConfiguration20_1.NearLimitSpecified = true;
                }
                else
                    focusConfiguration20_1.NearLimitSpecified = false;
                nullable = focus.FarLimit;
                if (nullable.HasValue)
                {
                    nullable = focus.FarLimit;
                    double num5 = (double)nullable.Value;
                    focus2 = this._imagingCapabilities.Focus;
                    FloatRange farLimit = focus2.FarLimit;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num5, farLimit))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid Focus.FarLimit: {0}", (object)focus.FarLimit));
                    FocusConfiguration20 focusConfiguration20_4 = focusConfiguration20_1;
                    nullable = focus.FarLimit;
                    double num6 = (double)nullable.Value;
                    focusConfiguration20_4.FarLimit = (float)num6;
                    focusConfiguration20_1.FarLimitSpecified = true;
                }
                else
                    focusConfiguration20_1.FarLimitSpecified = false;
                ImagingSettings.Focus = focusConfiguration20_1;
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, ImagingSettings, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetFocusSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public async Task<WhiteBalanceSettings> GetWhiteBalanceSettingsAsync()
        {
            WhiteBalanceSettings balanceSettingsAsync;
            try
            {
                WhiteBalanceCapabilities whiteBalance = this._imagingCapabilities.WhiteBalance;
                ImagingSettings20 imagingSettings20 = await this._imagingClient.GetImagingSettingsAsync(this._videosourcetoken).ConfigureAwait(false);
                WhiteBalanceSettings whiteBalanceSettings = new WhiteBalanceSettings();
                if (imagingSettings20.WhiteBalance == null)
                    throw new InvalidOperationException();
                ControlMode result = ControlMode.Auto;
                if (!Enum.TryParse<ControlMode>(imagingSettings20.WhiteBalance.Mode.ToString(), true, out result))
                    throw new InvalidOperationException("Unable to parse device WhiteBalance mode");
                whiteBalanceSettings.Mode = result;
                if (imagingSettings20.WhiteBalance.CrGainSpecified)
                    whiteBalanceSettings.RGain = new float?(imagingSettings20.WhiteBalance.CrGain);
                if (imagingSettings20.WhiteBalance.CbGainSpecified)
                    whiteBalanceSettings.BGain = new float?(imagingSettings20.WhiteBalance.CbGain);
                balanceSettingsAsync = whiteBalanceSettings;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetWhiteBalanceSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return balanceSettingsAsync;
        }

        public async Task SetWhiteBalanceSettingsAsync(WhiteBalanceSettings whitebalance)
        {
            try
            {
                WhiteBalanceCapabilities whiteBalance1 = this._imagingCapabilities.WhiteBalance;
                ImagingSettings20 ImagingSettings = new ImagingSettings20();
                WhiteBalance20 whiteBalance20_1 = new WhiteBalance20();
                if (whitebalance.Mode != ControlMode.Auto && whitebalance.Mode != ControlMode.Manual || !this._imagingCapabilities.WhiteBalance.SupportedModes.Contains<ControlMode>(whitebalance.Mode))
                    throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid WhiteBalance.Mode: {0}", (object)whitebalance.Mode));
                whiteBalance20_1.Mode = whitebalance.Mode == ControlMode.Auto ? WhiteBalanceMode.AUTO : WhiteBalanceMode.MANUAL;
                WhiteBalanceCapabilities whiteBalance2;
                float? nullable;
                if (whitebalance.RGain.HasValue)
                {
                    double num1 = (double)whitebalance.RGain.Value;
                    whiteBalance2 = this._imagingCapabilities.WhiteBalance;
                    FloatRange rgain = whiteBalance2.RGain;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num1, rgain))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid WhiteBalance.RGain: {0}", (object)whitebalance.RGain));
                    WhiteBalance20 whiteBalance20_2 = whiteBalance20_1;
                    nullable = whitebalance.RGain;
                    double num2 = (double)nullable.Value;
                    whiteBalance20_2.CrGain = (float)num2;
                    whiteBalance20_1.CrGainSpecified = true;
                }
                else
                    whiteBalance20_1.CrGainSpecified = false;
                nullable = whitebalance.BGain;
                if (nullable.HasValue)
                {
                    nullable = whitebalance.BGain;
                    double num3 = (double)nullable.Value;
                    whiteBalance2 = this._imagingCapabilities.WhiteBalance;
                    FloatRange rgain = whiteBalance2.RGain;
                    if (!ImagingClientController.CheckValueInFloatRange((float)num3, rgain))
                        throw new ArgumentException(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Invalid WhiteBalance.BGain: {0}", (object)whitebalance.BGain));
                    WhiteBalance20 whiteBalance20_3 = whiteBalance20_1;
                    nullable = whitebalance.BGain;
                    double num4 = (double)nullable.Value;
                    whiteBalance20_3.CbGain = (float)num4;
                    whiteBalance20_1.CbGainSpecified = true;
                }
                else
                    whiteBalance20_1.CbGainSpecified = false;
                ImagingSettings.WhiteBalance = whiteBalance20_1;
                await this._imagingClient.SetImagingSettingsAsync(this._videosourcetoken, ImagingSettings, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetWhiteBalanceSettingsAsync), (object)this._videosourcetoken, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        private IReadOnlyList<ControlMode> ConvertOnvifModesToControlMode<T>(
          T[] onvifModes)
        {
            ControlMode[] source = new ControlMode[onvifModes.Length];
            for (int index = 0; index < onvifModes.Length; ++index)
            {
                T onvifMode = onvifModes[index];
                ControlMode result = ControlMode.Auto;
                if (Enum.TryParse<ControlMode>(onvifMode.ToString(), true, out result))
                    source[index] = result;
            }
            return (IReadOnlyList<ControlMode>)((IEnumerable<ControlMode>)source).ToList<ControlMode>();
        }

        private static FloatRange ConvertOnvifFloatRange(ServiceReference4.FloatRange range) => range != null ? new FloatRange(range.Min, range.Max) : (FloatRange)null;

        private static bool CheckValueInFloatRange(float value, FloatRange range) => !(range == (FloatRange)null) && (double)value >= (double)range.Min && (double)value <= (double)range.Max;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }
    }
}

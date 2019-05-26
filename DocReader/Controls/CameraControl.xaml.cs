using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using DocReader.EventHandlers;
using DocReader.Helpers;
using DocReader.Services;
using DocReader.Views;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace DocReader.Controls
{
    public sealed partial class CameraControl
    {
        public event EventHandler<CameraControlEventArgs> PhotoTaken;

        private bool isAutoFocusCapable;
        private bool isAutoShot;

        private SetupService _setup;

        public static readonly DependencyProperty CanSwitchProperty =
            DependencyProperty.Register("CanSwitch", typeof(bool), typeof(CameraControl), new PropertyMetadata(false));

        public static readonly DependencyProperty PanelProperty =
            DependencyProperty.Register("Panel", typeof(Windows.Devices.Enumeration.Panel), typeof(CameraControl), new PropertyMetadata(Windows.Devices.Enumeration.Panel.Front, OnPanelChanged));

        public static readonly DependencyProperty IsInitializedProperty =
            DependencyProperty.Register("IsInitialized", typeof(bool), typeof(CameraControl), new PropertyMetadata(false));

        public static readonly DependencyProperty CameraButtonStyleProperty =
            DependencyProperty.Register("CameraButtonStyle", typeof(Style), typeof(CameraControl), new PropertyMetadata(null));

        public static readonly DependencyProperty SwitchCameraButtonStyleProperty =
            DependencyProperty.Register("SwitchCameraButtonStyle", typeof(Style), typeof(CameraControl), new PropertyMetadata(null));

        // Rotation metadata to apply to the preview stream and recorded videos (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        private readonly Guid _rotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private MediaCapture _mediaCapture;
        private bool _isPreviewing;
        private bool _mirroringPreview;
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private DeviceInformationCollection _cameraDevices;
        private bool _capturing;

        private List<WordOverlay> wordBoxes = new List<WordOverlay>();

        private ThreadPoolTimer _frameProcessingTimer;

        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private VideoEncodingProperties vep;

        public bool CanSwitch
        {
            get { return (bool)GetValue(CanSwitchProperty); }
            set { SetValue(CanSwitchProperty, value); }
        }

        public Windows.Devices.Enumeration.Panel Panel
        {
            get { return (Windows.Devices.Enumeration.Panel)GetValue(PanelProperty); }
            set { SetValue(PanelProperty, value); }
        }

        public bool IsInitialized
        {
            get { return (bool)GetValue(IsInitializedProperty); }
            private set { SetValue(IsInitializedProperty, value); }
        }

        public Style CameraButtonStyle
        {
            get { return (Style)GetValue(CameraButtonStyleProperty); }
            set { SetValue(CameraButtonStyleProperty, value); }
        }

        public Style SwitchCameraButtonStyle
        {
            get { return (Style)GetValue(SwitchCameraButtonStyleProperty); }
            set { SetValue(SwitchCameraButtonStyleProperty, value); }
        }

        public CameraControl()
        {
            InitializeComponent();
            _setup = new SetupService();

            CameraButtonStyle = Resources["CameraButtonStyle"] as Style;
            SwitchCameraButtonStyle = Resources["SwitchCameraButtonStyle"] as Style;
        }

        public async Task InitializeCameraAsync()
        {
            try
            {
                if(_setup==null)
                {
                    _setup = new SetupService();
                }
                isAutoShot = await _setup.GetAutomode();

                if (_mediaCapture == null)
                {
                    _mediaCapture = new MediaCapture();
                    _mediaCapture.Failed += MediaCapture_Failed;

                    _cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                    if (_cameraDevices == null || !_cameraDevices.Any())
                    {
                        throw new NotSupportedException();
                    }
                    DeviceInformation device;
                    if (_cameraDevices.Count > 1)
                    {
                        device = _cameraDevices.FirstOrDefault(camera => camera.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Back);
                    }
                    else
                    {
                        device = _cameraDevices.FirstOrDefault(camera => camera.EnclosureLocation?.Panel == Panel);

                    }

                    var cameraId = device?.Id ?? _cameraDevices.First().Id;
                                     

                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = cameraId });
                                        
                    if(_mediaCapture.VideoDeviceController.FocusControl.Supported)
                    {
                        isAutoFocusCapable = true;
                        errorMessage.Text = "VIZZoneInFront".GetLocalized();
                    }
                    else
                    {
                        isAutoFocusCapable = false;
                        errorMessage.Text = "NoFocusCamera".GetLocalized();
                    }

                    IMediaEncodingProperties IProps = this._mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
                    vep = (VideoEncodingProperties)IProps;

                    DrawLineOnCanvas(vep.Width, vep.Height);
                    

                    if (Panel == Windows.Devices.Enumeration.Panel.Back)
                    {
                        //_mediaCapture.SetRecordRotation(VideoRotation.Clockwise90Degrees);
                        //_mediaCapture.SetPreviewRotation(VideoRotation.Clockwise90Degrees);
                        _mirroringPreview = false;
                    }
                    else
                    {
                        _mirroringPreview = false;
                    }

                    IsInitialized = true;
                    
                    CanSwitch = _cameraDevices?.Count > 1;
                    RegisterOrientationEventHandlers();
                    await StartPreviewAsync();
                }
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage.Text = "Camera_Exception_UnauthorizedAccess".GetLocalized();
            }
            catch (NotSupportedException)
            {
                errorMessage.Text = "Camera_Exception_NotSupported".GetLocalized();
            }
            catch (TaskCanceledException)
            {
                errorMessage.Text = "Camera_Exception_InitializationCanceled".GetLocalized();
            }
            catch (Exception)
            {
                errorMessage.Text = "Camera_Exception_InitializationError".GetLocalized();
            }
        }

        public async Task CleanupCameraAsync()
        {
            if (IsInitialized)
            {
                if (_isPreviewing)
                {
                    await StopPreviewAsync();
                }

                UnregisterOrientationEventHandlers();
                IsInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private void DrawLineOnCanvas(double width,double height)
        {
            if (width > 0 && height > 0)
            {
                float widthScale = (float)PreviewControl.ActualWidth / (float)width;
                float heightScale = (float)PreviewControl.ActualHeight / (float)height;

                float scale = Math.Min(widthScale, heightScale);

                Rect r = new Rect(0, 0, width * scale, height * scale);

                Line line = new Line();
                line.Stroke = new SolidColorBrush(Colors.Yellow);
                line.StrokeThickness = 3;

                line.Y1 = 0.3 * (r.Height / 2) + PreviewControl.ActualHeight / 2;
                line.X1 = 0;
                line.X2 = PreviewControl.ActualWidth;
                line.Y2 = 0.3 * (r.Height / 2) + PreviewControl.ActualHeight / 2;

                VisualizationCanvas.FlowDirection = _mirroringPreview ? Windows.UI.Xaml.FlowDirection.RightToLeft : Windows.UI.Xaml.FlowDirection.LeftToRight;

                if (VisualizationCanvas.Children.Count >= 1)
                {
                    VisualizationCanvas.Children.RemoveAt(0);
                }
                VisualizationCanvas.Children.Add(line);
            }
        }

        public async Task<string> TakePhoto()
        {
            if (_capturing)
            {
                return "";
            }

            _capturing = true;

            if (_mediaCapture != null)
            {
                if (isAutoFocusCapable)
                {
                    await _mediaCapture.VideoDeviceController.FocusControl.FocusAsync();
                }

                using (var stream = new InMemoryRandomAccessStream())
                {
                    //await _mediaCapture.VideoDeviceController.FocusControl.FocusAsync();
                    await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

                    var photoOrientation = _displayInformation
                        .ToSimpleOrientation(_deviceOrientation, _mirroringPreview)
                        .ToPhotoOrientation(_mirroringPreview);

                    var photo = await ReencodeAndSavePhotoAsync(stream, photoOrientation);
                    PhotoTaken?.Invoke(this, new CameraControlEventArgs(photo));
                    _capturing = false;
                    return photo;
                }
            }
            else
            {
                return "";
            }

        }

        public void SwitchPanel()
        {
            Panel = (Panel == Windows.Devices.Enumeration.Panel.Front) ? Windows.Devices.Enumeration.Panel.Back : Windows.Devices.Enumeration.Panel.Front;
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            string strPhoto = await TakePhoto();
            if (!string.IsNullOrEmpty(strPhoto))
            {
                NavigationService.Navigate(typeof(TabbedDetailsPage),strPhoto);
            }
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchPanel();
        }

        private async void CleanAndInitialize()
        {
            await Task.Run(async () => await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await CleanupCameraAsync();
                await InitializeCameraAsync();
            }));
        }

        private async Task SetupFocus()
        {
            if(_mediaCapture!=null && _isPreviewing)
            {
                var focusControl = _mediaCapture.VideoDeviceController.FocusControl;
                await focusControl.UnlockAsync();
                var settings = new FocusSettings { Mode = FocusMode.Auto };
                focusControl.Configure(settings);
                
                await Task.CompletedTask;
            }
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Task.Run(async () => await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await CleanupCameraAsync();
                if (this._frameProcessingTimer != null)
                {
                    this._frameProcessingTimer.Cancel();
                }
            }));
        }

        private async Task StartPreviewAsync()
        {
            PreviewControl.Source = _mediaCapture;
            PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            if (_mediaCapture != null)
            {
                await _mediaCapture.StartPreviewAsync();
                
                await SetPreviewRotationAsync();

                

                if (isAutoShot)
                {
                    TimeSpan timerInterval = TimeSpan.FromMilliseconds(500);
                    this._frameProcessingTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(ProcessFrame), timerInterval, new TimerDestroyedHandler(Zakonczono));
                }

                _isPreviewing = true;

                if(isAutoFocusCapable)
                {
                    await SetupFocus();
                }
                
                
            }
        }

        private async void ProcessFrame(ThreadPoolTimer timer)
        {
            if (!semaphore.Wait(0))
            {
                return;
            }
            else
            {
                if (_mediaCapture != null)
                {
                    if (_mediaCapture.CameraStreamState == CameraStreamState.Streaming)
                    {
                        var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                        int videoFrameWidth = (int)previewProperties.Width;
                        int videoFrameHeight = (int)previewProperties.Height;

                        // In portrait modes, the width and height must be swapped for the VideoFrame to have the correct aspect ratio and avoid letterboxing / black bars.
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            if ((_displayInformation.CurrentOrientation == DisplayOrientations.Portrait || _displayInformation.CurrentOrientation == DisplayOrientations.PortraitFlipped))
                            {
                                videoFrameWidth = (int)previewProperties.Height;
                                videoFrameHeight = (int)previewProperties.Width;
                            }
                        });

                        if (_mediaCapture != null)
                        {
                            // Create the video frame to request a SoftwareBitmap preview frame.
                            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, videoFrameWidth, videoFrameHeight);
                            if (_mediaCapture.CameraStreamState == CameraStreamState.Streaming)
                            {
                                using (VideoFrame vf = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
                                {
                                    SoftwareBitmap bitmap = vf.SoftwareBitmap;

                                    OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));

                                    if (ocrEngine != null)
                                    {
                                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                        {
                                            var imgSource = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                                            bitmap.CopyToBuffer(imgSource.PixelBuffer);
                                            PreviewImage.Source = imgSource;
                                            await Task.CompletedTask;
                                        });
                                        bool success = false;
                                        var ocrResult = await ocrEngine.RecognizeAsync(bitmap);
                                        // Used for text overlay.
                                        // Prepare scale transform for words since image is not displayed in original format.
                                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                        {
                                            var scaleTrasform = new ScaleTransform
                                            {
                                                CenterX = 0,
                                                CenterY = 0,
                                                ScaleX = PreviewControl.ActualWidth / bitmap.PixelWidth,
                                                ScaleY = PreviewControl.ActualHeight / bitmap.PixelHeight
                                            };

                                            if (ocrResult.TextAngle != null)
                                            {

                                            // If text is detected under some angle in this sample scenario we want to
                                            // overlay word boxes over original image, so we rotate overlay boxes.
                                            TextOverlay.RenderTransform = new RotateTransform
                                                {
                                                    Angle = (double)ocrResult.TextAngle,
                                                    CenterX = PreviewImage.ActualWidth / 2,
                                                    CenterY = PreviewImage.ActualHeight / 2
                                                };
                                            }

                                            if (ocrResult.Lines != null && ocrResult.Lines.Count >= 1)
                                            {

                                                List<int> lstWordCount = new List<int>();
                                            // Iterate over recognized lines of text.
                                            foreach (var line in ocrResult.Lines)
                                                {
                                                    lstWordCount.Add(line.Words.Count);
                                                // Iterate over words in line.
                                                foreach (var word in line.Words)
                                                    {
                                                    // Define the TextBlock.
                                                    var wordTextBlock = new TextBlock()
                                                        {
                                                            Text = word.Text,
                                                            Style = (Style)this.Resources["ExtractedWordTextStyle"]
                                                        };

                                                        WordOverlay wordBoxOverlay = new WordOverlay(word);

                                                    // Keep references to word boxes.
                                                    wordBoxes.Add(wordBoxOverlay);

                                                    // Define position, background, etc.
                                                    var overlay = new Border()
                                                        {
                                                            Child = wordTextBlock,
                                                            Style = (Style)this.Resources["HighlightedWordBoxHorizontalLine"]
                                                        };

                                                    // Bind word boxes to UI.
                                                    overlay.SetBinding(Border.MarginProperty, wordBoxOverlay.CreateWordPositionBinding());
                                                        overlay.SetBinding(Border.WidthProperty, wordBoxOverlay.CreateWordWidthBinding());
                                                        overlay.SetBinding(Border.HeightProperty, wordBoxOverlay.CreateWordHeightBinding());

                                                    // Put the filled textblock in the results grid.
                                                    TextOverlay.Children.Add(overlay);
                                                    }
                                                }

                                                if (isAutoFocusCapable && isAutoShot && lstWordCount.Max() >= 2)
                                                {
                                                    success = true;
                                                    await Task.CompletedTask;
                                                }
                                                else
                                                {
                                                    success = false;
                                                    await Task.CompletedTask;
                                                }
                                            }
                                            else
                                            {
                                                errorMessage.Text = "VIZZoneInFront".GetLocalized();
                                                await Task.CompletedTask;
                                            }
                                        });

                                        if(success)
                                        {
                                            string s = await Dispatcher.RunTaskAsync<string>(MakePhoto);
                                            await Task.CompletedTask;
                                        }
                                    }
                                    else
                                    {
                                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                        {
                                            errorMessage.Text = "OCRLanguage".GetLocalized();
                                            await Task.CompletedTask;
                                        });
                                        await Task.CompletedTask;
                                    }
                                }
                            }
                        }

                    }
                }
                semaphore.Release();
            }
            
        }

        private async Task<string> MakePhoto()
        {
            string strPhoto = await TakePhoto();
            await CleanupCameraAsync();
            if (!string.IsNullOrEmpty(strPhoto))
            {
                NavigationService.Navigate(typeof(TabbedDetailsPage), strPhoto);
            }
            return strPhoto;

        }
        private void UpdateWordBoxTransform()
        {
            WriteableBitmap bitmap = PreviewImage.Source as WriteableBitmap;

            if (bitmap != null)
            {
                // Used for text overlay.
                // Prepare scale transform for words since image is not displayed in original size.
                ScaleTransform scaleTrasform = new ScaleTransform
                {
                    CenterX = 0,
                    CenterY = 0,
                    ScaleX = PreviewImage.ActualWidth / bitmap.PixelWidth,
                    ScaleY = PreviewImage.ActualHeight / bitmap.PixelHeight
                };

                foreach (var item in wordBoxes)
                {
                    item.Transform(scaleTrasform);
                }
            }
        }

        /// <summary>
        /// Occures when displayed image size changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWordBoxTransform();

            // Update image rotation center.
            var rotate = TextOverlay.RenderTransform as RotateTransform;
            if (rotate != null)
            {
                rotate.CenterX = PreviewImage.ActualWidth / 2;
                rotate.CenterY = PreviewImage.ActualHeight / 2;
            }
            if (vep != null)
            {
                DrawLineOnCanvas(vep.Width, vep.Height);
            }
        }

        private async void Zakonczono(ThreadPoolTimer timer)
        {
            await Task.CompletedTask;
        }

            private async Task SetPreviewRotationAsync()
        {
            _displayOrientation = _displayInformation.CurrentOrientation;
            int rotationDegrees = _displayOrientation.ToDegrees();

            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            if (_mediaCapture != null)
            {
                var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
                props.Properties.Add(_rotationKey, rotationDegrees);
                await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
            }
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            if (_mediaCapture != null)
            {
                await _mediaCapture.StopPreviewAsync();
            }
                PreviewControl.Source = null;
            if (this._frameProcessingTimer != null)
            {
                this._frameProcessingTimer.Cancel();
            }
        }

        private async Task<string> ReencodeAndSavePhotoAsync(IRandomAccessStream stream, PhotoOrientation photoOrientation)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);

                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("photo.jpeg", CreationCollisionOption.GenerateUniqueName);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);

                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(photoOrientation, PropertyType.UInt16) } };

                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }

                return file.Path;
            }
        }

        private void RegisterOrientationEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            _displayOrientation = _displayInformation.CurrentOrientation;
        }

        private void UnregisterOrientationEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
            }
        }

        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;
            await SetPreviewRotationAsync();
        }

        private static void OnPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (CameraControl)d;

            if (ctrl.IsInitialized)
            {
                ctrl.CleanAndInitialize();
            }
        }
    }
}

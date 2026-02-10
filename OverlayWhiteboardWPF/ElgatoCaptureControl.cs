using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace ElgatoCaptureControl
{
    [TemplatePart(Name = "PART_MediaElement", Type = typeof(System.Windows.Controls.MediaElement))]
    public class ElgatoCaptureControl : Control, INotifyPropertyChanged
    {
        private MediaCapture _mediaCapture;
        private WriteableBitmap _bitmap;
        private bool _isInitialized;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private System.Windows.Controls.MediaElement _mediaElement;

        static ElgatoCaptureControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ElgatoCaptureControl), 
                new FrameworkPropertyMetadata(typeof(ElgatoCaptureControl)));
        }

        public ElgatoCaptureControl()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Debug.WriteLine("ElgatoCaptureControl: Constructor called");
        }

        #region Properties

        public static readonly DependencyProperty DeviceIdProperty =
            DependencyProperty.Register("DeviceId", typeof(string), typeof(ElgatoCaptureControl),
                new PropertyMetadata(null, OnDeviceIdChanged));

        public string DeviceId
        {
            get => (string)GetValue(DeviceIdProperty);
            set => SetValue(DeviceIdProperty, value);
        }

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register("IsRunning", typeof(bool), typeof(ElgatoCaptureControl),
                new PropertyMetadata(false, OnIsRunningChanged));

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
        }

        public static readonly DependencyProperty VideoWidthProperty =
            DependencyProperty.Register("VideoWidth", typeof(uint), typeof(ElgatoCaptureControl),
                new PropertyMetadata(1920u, OnVideoSizeChanged));

        public uint VideoWidth
        {
            get => (uint)GetValue(VideoWidthProperty);
            set => SetValue(VideoWidthProperty, value);
        }

        public static readonly DependencyProperty VideoHeightProperty =
            DependencyProperty.Register("VideoHeight", typeof(uint), typeof(ElgatoCaptureControl),
                new PropertyMetadata(1080u, OnVideoSizeChanged));

        public uint VideoHeight
        {
            get => (uint)GetValue(VideoHeightProperty);
            set => SetValue(VideoHeightProperty, value);
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        private static void OnDeviceIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ElgatoCaptureControl)d;
            Debug.WriteLine($"ElgatoCaptureControl: DeviceId changed to {e.NewValue}");
            control.InitializeAsync();
        }

        private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ElgatoCaptureControl)d;
            Debug.WriteLine($"ElgatoCaptureControl: IsRunning changed to {e.NewValue}");
            if ((bool)e.NewValue)
                control.StartAsync();
            else
                control.StopAsync();
        }

        private static void OnVideoSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ElgatoCaptureControl)d;
            Debug.WriteLine($"ElgatoCaptureControl: Video size changed to {control.VideoWidth}x{control.VideoHeight}");
            control.UpdateBitmapSize();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ElgatoCaptureControl: Loaded event triggered");
            InitializeAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ElgatoCaptureControl: Unloaded event triggered");
            StopAsync();
            CleanupAsync();
        }

        private async void InitializeAsync()
        {
            Debug.WriteLine($"ElgatoCaptureControl: InitializeAsync called, DeviceId: {DeviceId}, IsInitialized: {_isInitialized}");
            if (_isInitialized) 
            {
                Debug.WriteLine("ElgatoCaptureControl: Already initialized");
                return;
            }
            if (string.IsNullOrEmpty(DeviceId)) 
            {
                Debug.WriteLine("ElgatoCaptureControl: DeviceId is null or empty");
                return;
            }

            try
            {
                Debug.WriteLine("ElgatoCaptureControl: Creating MediaCapture instance");
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MediaCategory = MediaCategory.Media,
                    AudioDeviceId = "",
                    VideoDeviceId = DeviceId
                };

                Debug.WriteLine("ElgatoCaptureControl: Initializing MediaCapture");
                await _mediaCapture.InitializeAsync(settings);
                _isInitialized = true;
                Debug.WriteLine("ElgatoCaptureControl: MediaCapture initialized successfully");
                UpdateBitmapSize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ElgatoCaptureControl: InitializeAsync failed - {ex.Message}");
                Debug.WriteLine($"ElgatoCaptureControl: StackTrace - {ex.StackTrace}");
            }
        }

        private async void StartAsync()
        {
            Debug.WriteLine($"ElgatoCaptureControl: StartAsync called, IsInitialized: {_isInitialized}, IsRunning: {_isRunning}");
            if (!_isInitialized || _isRunning) 
            {
                Debug.WriteLine("ElgatoCaptureControl: Cannot start - not initialized or already running");
                return;
            }

            try
            {
                Debug.WriteLine($"ElgatoCaptureControl: Starting preview with {VideoWidth}x{VideoHeight}");
                var videoEncodingProperties = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, VideoWidth, VideoHeight);
                Debug.WriteLine($"ElgatoCaptureControl: VideoEncodingProperties created: {videoEncodingProperties.Width}x{videoEncodingProperties.Height}");
                
                await _mediaCapture.StartPreviewAsync();
                _isRunning = true;
                Debug.WriteLine("ElgatoCaptureControl: Preview started successfully");
                OnPropertyChanged(nameof(IsRunning));
                
                // Force a refresh
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ElgatoCaptureControl: StartAsync failed - {ex.Message}");
                Debug.WriteLine($"ElgatoCaptureControl: StackTrace - {ex.StackTrace}");
            }
        }

        private async void StopAsync()
        {
            Debug.WriteLine("ElgatoCaptureControl: StopAsync called");
            if (!_isRunning) 
            {
                Debug.WriteLine("ElgatoCaptureControl: Not running");
                return;
            }

            try
            {
                await _mediaCapture.StopPreviewAsync();
                _isRunning = false;
                Debug.WriteLine("ElgatoCaptureControl: Preview stopped successfully");
                OnPropertyChanged(nameof(IsRunning));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ElgatoCaptureControl: StopAsync failed - {ex.Message}");
                Debug.WriteLine($"ElgatoCaptureControl: StackTrace - {ex.StackTrace}");
            }
        }

        private async void CleanupAsync()
        {
            Debug.WriteLine("ElgatoCaptureControl: CleanupAsync called");
            if (_mediaCapture != null)
            {
                try
                {
                    await _mediaCapture.StopPreviewAsync();
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                    _isInitialized = false;
                    _isRunning = false;
                    Debug.WriteLine("ElgatoCaptureControl: Cleanup completed successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ElgatoCaptureControl: Cleanup failed - {ex.Message}");
                }
            }
        }

        private void UpdateBitmapSize()
        {
            Debug.WriteLine($"ElgatoCaptureControl: UpdateBitmapSize called - {VideoWidth}x{VideoHeight}");
            try
            {
                if (_bitmap == null || _bitmap.PixelWidth != VideoWidth || _bitmap.PixelHeight != VideoHeight)
                {
                    Debug.WriteLine($"ElgatoCaptureControl: Creating new bitmap {VideoWidth}x{VideoHeight}");
                    _bitmap = new WriteableBitmap(
                        (int)VideoWidth, (int)VideoHeight, 
                        96, 96, 
                        PixelFormats.Bgra32, 
                        null);
                    Debug.WriteLine("ElgatoCaptureControl: Bitmap created successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ElgatoCaptureControl: UpdateBitmapSize failed - {ex.Message}");
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Debug.WriteLine("ElgatoCaptureControl: OnRender called");
            base.OnRender(drawingContext);
            
            if (_bitmap != null)
            {
                Debug.WriteLine($"ElgatoCaptureControl: Drawing bitmap - {VideoWidth}x{VideoHeight}");
                try
                {
                    drawingContext.DrawImage(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
                    Debug.WriteLine("ElgatoCaptureControl: Drawing completed successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ElgatoCaptureControl: OnRender drawing failed - {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("ElgatoCaptureControl: Bitmap is null, nothing to draw");
                // Draw a simple rectangle to show the control exists
                drawingContext.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 1), new Rect(0, 0, ActualWidth, ActualHeight));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            Debug.WriteLine($"ElgatoCaptureControl: OnRenderSizeChanged - {sizeInfo.NewSize.Width}x{sizeInfo.NewSize.Height}");
            base.OnRenderSizeChanged(sizeInfo);
        }

        public override void OnApplyTemplate()
        {
            Debug.WriteLine("ElgatoCaptureControl: OnApplyTemplate called");
            base.OnApplyTemplate();
            _mediaElement = GetTemplateChild("PART_MediaElement") as System.Windows.Controls.MediaElement;
            Debug.WriteLine($"ElgatoCaptureControl: MediaElement found: {_mediaElement != null}");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            Debug.WriteLine($"ElgatoCaptureControl: OnPropertyChanged - {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

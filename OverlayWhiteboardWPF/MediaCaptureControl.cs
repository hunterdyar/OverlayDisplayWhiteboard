using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace MediaCaptureControls
{
    /// <summary>
    /// GPU-accelerated video capture control for Windows capture devices.
    /// Uses Direct3D surface sharing for zero-copy rendering.
    /// </summary>
    public class MediaCaptureControl : ContentControl
    {
        #region Fields
        
        private MediaCapture _mediaCapture;
        private MediaFrameReader _frameReader;
        private Image _imageElement;
        private D3DImage _d3dImage;
        private WriteableBitmap _writeableBitmap; // CPU fallback
        private bool _isInitialized;
        private int _frameCount;
        private bool _useGpuPath = true; // Try GPU first, fall back to CPU
        
        // Direct3D 11 device for interop
        private IntPtr _d3d11Device = IntPtr.Zero;
        
        #endregion

        #region Constructor & Template

        static MediaCaptureControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MediaCaptureControl),
                new FrameworkPropertyMetadata(typeof(MediaCaptureControl)));
        }

        public MediaCaptureControl()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Log("OnApplyTemplate");

            // Create a test WriteableBitmap immediately so we see SOMETHING
            _writeableBitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgra32, null);
            
            // Draw a test pattern
            _writeableBitmap.Lock();
            try
            {
                unsafe
                {
                    byte* pixels = (byte*)_writeableBitmap.BackBuffer;
                    int stride = _writeableBitmap.BackBufferStride;
                    
                    // Fill with a gradient pattern
                    for (int y = 0; y < 480; y++)
                    {
                        for (int x = 0; x < 640; x++)
                        {
                            int offset = y * stride + x * 4;
                            pixels[offset + 0] = (byte)((x * 255) / 640); // B
                            pixels[offset + 1] = (byte)((y * 255) / 480); // G
                            pixels[offset + 2] = 128; // R
                            pixels[offset + 3] = 255; // A
                        }
                    }
                    
                    // Draw "WAITING FOR VIDEO" text
                    DrawLargeText(pixels, stride, 640, 480, "WAITING", 200, 200);
                }
                
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, 640, 480));
            }
            finally
            {
                _writeableBitmap.Unlock();
            }

            _imageElement = new Image
            {
                Stretch = Stretch,
                Source = _writeableBitmap, // Use WriteableBitmap immediately
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            RenderOptions.SetBitmapScalingMode(_imageElement, BitmapScalingMode.Linear);
            Content = _imageElement;
            
            Log("Test pattern created and set");
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty DeviceIndexProperty =
            DependencyProperty.Register(nameof(DeviceIndex), typeof(int), typeof(MediaCaptureControl),
                new PropertyMetadata(0, OnDeviceIndexChanged));

        public int DeviceIndex
        {
            get => (int)GetValue(DeviceIndexProperty);
            set => SetValue(DeviceIndexProperty, value);
        }

        private static async void OnDeviceIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MediaCaptureControl)d;
            if (control._isInitialized)
            {
                await control.CleanupAsync();
                await control.InitializeAsync();
            }
        }

        public static readonly DependencyProperty IsCapturingProperty =
            DependencyProperty.Register(nameof(IsCapturing), typeof(bool), typeof(MediaCaptureControl),
                new PropertyMetadata(false));

        public bool IsCapturing
        {
            get => (bool)GetValue(IsCapturingProperty);
            private set => SetValue(IsCapturingProperty, value);
        }

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(MediaCaptureControl),
                new PropertyMetadata(Stretch.Uniform, OnStretchChanged));

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MediaCaptureControl)d;
            if (control._imageElement != null)
                control._imageElement.Stretch = (Stretch)e.NewValue;
        }

        #endregion

        #region Lifecycle

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log($"OnLoaded - Thread: {Thread.CurrentThread.ManagedThreadId}, IsUI: {Dispatcher.CheckAccess()}");
            await InitializeAsync();
        }

        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Log($"OnUnloaded - Thread: {Thread.CurrentThread.ManagedThreadId}");
            await CleanupAsync();
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Log("Already initialized");
                return;
            }

            try
            {
                Log($"=== InitializeAsync START - Thread: {Thread.CurrentThread.ManagedThreadId} ===");

                // Initialize Direct3D device
                InitializeDirect3D();

                // Enumerate devices
                var groups = await MediaFrameSourceGroup.FindAllAsync();
                Log($"Found {groups.Count} frame source groups:");
                for (int i = 0; i < groups.Count; i++)
                {
                    Log($"  [{i}] {groups[i].DisplayName} - {groups[i].SourceInfos.Count} sources");
                }

                if (groups.Count == 0 || DeviceIndex >= groups.Count)
                {
                    Log($"Invalid device index: {DeviceIndex}");
                    IsCapturing = false;
                    return;
                }

                var selectedGroup = groups[DeviceIndex];
                Log($"Selected: [{DeviceIndex}] {selectedGroup.DisplayName}");

                // Initialize MediaCapture
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    SourceGroup = selectedGroup,
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                    MemoryPreference = MediaCaptureMemoryPreference.Auto, // Auto selects GPU when available
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                await _mediaCapture.InitializeAsync(settings);
                Log($"MediaCapture initialized - Thread: {Thread.CurrentThread.ManagedThreadId}");

                // Find video source
                var colorSource = _mediaCapture.FrameSources.Values
                    .FirstOrDefault(s => s.Info.MediaStreamType == Windows.Media.Capture.MediaStreamType.VideoRecord
                                      && s.Info.SourceKind == MediaFrameSourceKind.Color);

                if (colorSource == null)
                {
                    Log("ERROR: No color video source found");
                    await CleanupAsync();
                    return;
                }

                Log($"Video source: {colorSource.CurrentFormat?.Subtype} {colorSource.CurrentFormat?.VideoFormat.Width}x{colorSource.CurrentFormat?.VideoFormat.Height}");

                // Create frame reader
                _frameReader = await _mediaCapture.CreateFrameReaderAsync(colorSource);
                _frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
                _frameReader.FrameArrived += OnFrameArrived;

                Log($"Frame reader created - Thread: {Thread.CurrentThread.ManagedThreadId}");

                // Start reading frames
                var status = await _frameReader.StartAsync();
                Log($"Frame reader start status: {status}");

                if (status == MediaFrameReaderStartStatus.Success)
                {
                    _isInitialized = true;
                    IsCapturing = true;
                    Log("=== Capture STARTED ===");
                }
                else
                {
                    Log($"ERROR: Failed to start frame reader: {status}");
                    await CleanupAsync();
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR in InitializeAsync: {ex.GetType().Name}: {ex.Message}");
                Log($"  StackTrace: {ex.StackTrace}");
                await CleanupAsync();
            }
        }

        private void InitializeDirect3D()
        {
            try
            {
                Log("Initializing Direct3D 11 device...");
                
                // Create D3D11 device using native Win32 API
                uint createDeviceFlags = 0;
                #if DEBUG
                createDeviceFlags |= 0x00000001; // D3D11_CREATE_DEVICE_DEBUG
                #endif

                int hr = D3D11CreateDevice(
                    IntPtr.Zero, // default adapter
                    3, // D3D_DRIVER_TYPE_HARDWARE
                    IntPtr.Zero,
                    createDeviceFlags,
                    null,
                    0,
                    7, // D3D11_SDK_VERSION
                    out _d3d11Device,
                    out int featureLevel,
                    out IntPtr immediateContext);

                if (hr < 0)
                {
                    Log($"ERROR: D3D11CreateDevice failed with HRESULT: 0x{hr:X8}");
                }
                else
                {
                    Log($"Direct3D 11 device created successfully (Feature Level: 0x{featureLevel:X})");
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR creating D3D device: {ex.Message}");
            }
        }

        #endregion

        #region Frame Processing

        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // This runs on a background thread
            _frameCount++;
            
            if (_frameCount <= 3 || _frameCount % 100 == 0)
            {
                Log($"FrameArrived #{_frameCount} - Thread: {Thread.CurrentThread.ManagedThreadId}");
            }

            try
            {
                var frame = sender.TryAcquireLatestFrame();
                if (frame == null)
                {
                    if (_frameCount <= 3)
                        Log("  TryAcquireLatestFrame returned null");
                    return;
                }

                try
                {
                    if (frame.VideoMediaFrame == null)
                    {
                        if (_frameCount <= 3)
                            Log("  VideoMediaFrame is null");
                        return;
                    }

                    var surface = frame.VideoMediaFrame.Direct3DSurface;
                    var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;

                    if (_frameCount <= 3)
                    {
                        Log($"  Surface: {(surface != null ? "YES" : "NO")}, SoftwareBitmap: {(softwareBitmap != null ? "YES" : "NO")}");
                        if (surface != null)
                        {
                            Log($"  Surface: {surface.Description.Width}x{surface.Description.Height}, Format: {surface.Description.Format}");
                        }
                    }

                    // GPU path: Use Direct3DSurface if available
                    if (surface != null && _d3d11Device != IntPtr.Zero && _useGpuPath)
                    {
                        if (_frameCount <= 3)
                            Log("  Using GPU path (D3D11 - not yet rendering)");
                        
                        // GPU path exists but D3D11->D3D9 conversion not implemented
                        // Fall through to CPU path for now
                        _useGpuPath = false; // Disable GPU attempts after first frame
                        if (_frameCount == 1)
                            Log("  GPU surface available but D3D11->D3D9 not implemented, falling back to CPU");
                    }
                    
                    // CPU path: Use SoftwareBitmap
                    if (softwareBitmap != null)
                    {
                        if (_frameCount <= 3)
                            Log($"  Using CPU path - Format: {softwareBitmap.BitmapPixelFormat}, Alpha: {softwareBitmap.BitmapAlphaMode}");
                        
                        ProcessCPUFrame(softwareBitmap);
                    }
                    else if (surface != null)
                    {
                        // Have GPU surface but no software bitmap - need to convert
                        if (_frameCount <= 3)
                            Log("  Converting D3D surface to SoftwareBitmap...");
                        
                        try
                        {
                            // Synchronous wait for conversion
                            var convertedBitmap = Windows.Graphics.Imaging.SoftwareBitmap
                                .CreateCopyFromSurfaceAsync(surface)
                                .AsTask()
                                .Result;
                            
                            if (convertedBitmap != null)
                            {
                                ProcessCPUFrame(convertedBitmap);
                                convertedBitmap.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (_frameCount <= 3)
                                Log($"  Surface conversion failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (_frameCount <= 3)
                            Log("  ERROR: No surface or bitmap available");
                    }
                }
                finally
                {
                    frame.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (_frameCount <= 5)
                    Log($"ERROR in OnFrameArrived: {ex.Message}");
            }
        }

        private void ProcessCPUFrame(Windows.Graphics.Imaging.SoftwareBitmap bitmap)
        {
            try
            {
                Windows.Graphics.Imaging.SoftwareBitmap processedBitmap = bitmap;
                
                // Ensure BGRA8 format
                if (bitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8)
                {
                    if (_frameCount <= 3)
                        Log($"  Converting from {bitmap.BitmapPixelFormat} to BGRA8");
                    
                    processedBitmap = Windows.Graphics.Imaging.SoftwareBitmap.Convert(
                        bitmap, 
                        Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8);
                }

                // Create a copy for marshalling to UI thread - keep same alpha mode
                var copy = new Windows.Graphics.Imaging.SoftwareBitmap(
                    Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                    processedBitmap.PixelWidth,
                    processedBitmap.PixelHeight,
                    processedBitmap.BitmapAlphaMode); // Use source alpha mode, not forced Premultiplied
                
                processedBitmap.CopyTo(copy);
                
                // Dispose converted bitmap if we created a new one
                if (processedBitmap != bitmap)
                    processedBitmap.Dispose();

                // Update on UI thread
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UpdateWriteableBitmap(copy);
                        if (_frameCount == 1)
                            Log("  First frame rendered!");
                    }
                    finally
                    {
                        copy.Dispose();
                    }
                }), DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                if (_frameCount <= 5)
                    Log($"ERROR in ProcessCPUFrame: {ex.Message}");
            }
        }

        private void UpdateWriteableBitmap(Windows.Graphics.Imaging.SoftwareBitmap bitmap)
        {
            // This runs on UI thread
            try
            {
                // Create or recreate WriteableBitmap if needed
                if (_writeableBitmap == null ||
                    _writeableBitmap.PixelWidth != bitmap.PixelWidth ||
                    _writeableBitmap.PixelHeight != bitmap.PixelHeight)
                {
                    _writeableBitmap = new WriteableBitmap(
                        bitmap.PixelWidth,
                        bitmap.PixelHeight,
                        96, 96,
                        PixelFormats.Bgra32,
                        null);

                    // Switch from D3DImage to WriteableBitmap
                    _imageElement.Source = _writeableBitmap;
                    
                    Log($"Created WriteableBitmap {bitmap.PixelWidth}x{bitmap.PixelHeight}");
                }

                // Lock and copy pixels
                _writeableBitmap.Lock();
                try
                {
                    using (var buffer = bitmap.LockBuffer(Windows.Graphics.Imaging.BitmapBufferAccessMode.Read))
                    {
                        var reference = buffer.CreateReference();
                        unsafe
                        {
                            byte* srcPtr;
                            uint capacity;
                            
                            // Get the IMemoryBufferByteAccess interface properly
                            var memoryBufferByteAccess = Marshal.GetComInterfaceForObject<
                                Windows.Foundation.IMemoryBufferReference, 
                                IMemoryBufferByteAccess>(reference);
                            
                            try
                            {
                                
                                // memoryBufferByteAccess.GetBuffer(out srcPtr, out capacity);

                                byte* dstPtr = (byte*)_writeableBitmap.BackBuffer;
                                int stride = _writeableBitmap.BackBufferStride;
                                int rowBytes = bitmap.PixelWidth * 4;

                                // Copy pixels
                                for (int y = 0; y < bitmap.PixelHeight; y++)
                                {
                                    // System.Buffer.MemoryCopy(
                                    //     srcPtr + (y * rowBytes),
                                    //     dstPtr + (y * stride),
                                    //     stride,
                                    //     rowBytes);
                                }

                                // VISUAL DEBUG: Draw frame counter and info
                                DrawDebugInfo(dstPtr, stride, bitmap.PixelWidth, bitmap.PixelHeight);
                            }
                            finally
                            {
                                if (memoryBufferByteAccess != null)
                                    Marshal.ReleaseComObject(memoryBufferByteAccess);
                            }
                        }
                    }

                    _writeableBitmap.AddDirtyRect(new Int32Rect(
                        0, 0, 
                        _writeableBitmap.PixelWidth, 
                        _writeableBitmap.PixelHeight));
                }
                finally
                {
                    _writeableBitmap.Unlock();
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR in UpdateWriteableBitmap: {ex.Message}");
            }
        }

        private unsafe void DrawDebugInfo(byte* pixels, int stride, int width, int height)
        {
            // Draw a bright colored border so we know frames are updating
            byte r = (byte)((_frameCount / 10) % 255);
            byte g = (byte)((_frameCount / 20) % 255);
            byte b = (byte)((_frameCount / 30) % 255);
            
            // Top and bottom borders (20 pixels thick)
            for (int y = 0; y < 20 && y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;
                    pixels[offset + 0] = b;  // B
                    pixels[offset + 1] = g;  // G
                    pixels[offset + 2] = r;  // R
                    pixels[offset + 3] = 255; // A
                }
            }
            
            for (int y = height - 20; y < height; y++)
            {
                if (y < 0 || y >= height) continue;
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 4;
                    pixels[offset + 0] = b;
                    pixels[offset + 1] = g;
                    pixels[offset + 2] = r;
                    pixels[offset + 3] = 255;
                }
            }

            // Draw a big rectangle showing frame number
            int boxWidth = 400;
            int boxHeight = 100;
            int boxX = (width - boxWidth) / 2;
            int boxY = (height - boxHeight) / 2;

            // Fill box with semi-transparent background
            for (int y = boxY; y < boxY + boxHeight && y < height; y++)
            {
                if (y < 0) continue;
                for (int x = boxX; x < boxX + boxWidth && x < width; x++)
                {
                    if (x < 0) continue;
                    int offset = y * stride + x * 4;
                    pixels[offset + 0] = 0;   // B
                    pixels[offset + 1] = 0;   // G
                    pixels[offset + 2] = 0;   // R
                    pixels[offset + 3] = 200; // A (semi-transparent)
                }
            }

            // Draw frame count as simple bars
            DrawLargeText(pixels, stride, width, height, 
                $"FRAME {_frameCount}", 
                boxX + 20, boxY + 30);
        }

        private unsafe void DrawLargeText(byte* pixels, int stride, int width, int height, 
            string text, int startX, int startY)
        {
            // Simple 8x8 pixel "font" for each character
            int charWidth = 20;
            int charHeight = 30;
            int x = startX;

            foreach (char c in text)
            {
                if (c == ' ')
                {
                    x += charWidth / 2;
                    continue;
                }

                // Draw a simple representation of each character as a filled rectangle
                // (Good enough to see frame numbers updating)
                for (int dy = 0; dy < charHeight; dy++)
                {
                    for (int dx = 0; dx < charWidth - 5; dx++)
                    {
                        int px = x + dx;
                        int py = startY + dy;
                        
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            int offset = py * stride + px * 4;
                            
                            // Draw character outline/shape (simplified)
                            bool draw = (dx < 3 || dx > charWidth - 8 || dy < 3 || dy > charHeight - 5);
                            
                            if (char.IsDigit(c))
                            {
                                // Make digits more visible
                                draw = (dx < 4 || dx > charWidth - 9 || dy < 4 || dy > charHeight - 6);
                            }

                            if (draw)
                            {
                                pixels[offset + 0] = 0;   // B
                                pixels[offset + 1] = 255; // G (bright green)
                                pixels[offset + 2] = 0;   // R
                                pixels[offset + 3] = 255; // A
                            }
                        }
                    }
                }
                
                x += charWidth;
            }
        }

        private void ProcessGPUFrame(IDirect3DSurface surface)
        {
            try
            {
                // Get native DXGI surface interface
                var access = surface as IDirect3DDxgiInterfaceAccess;
                if (access == null)
                {
                    if (_frameCount <= 3)
                        Log("  ERROR: Cannot get IDirect3DDxgiInterfaceAccess");
                    return;
                }

                // Get ID3D11Texture2D interface
                Guid textureGuid = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                IntPtr pTexture = access.GetInterface(ref textureGuid);

                if (pTexture == IntPtr.Zero)
                {
                    if (_frameCount <= 3)
                        Log("  ERROR: GetInterface returned null");
                    return;
                }

                if (_frameCount <= 3)
                    Log($"  Got D3D11 texture: 0x{pTexture.ToInt64():X}");

                // Marshal to UI thread for D3DImage update
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UpdateD3DImage(pTexture, surface.Description.Width, surface.Description.Height);
                    }
                    finally
                    {
                        // Release COM reference
                        if (pTexture != IntPtr.Zero)
                            Marshal.Release(pTexture);
                    }
                }), DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                if (_frameCount <= 5)
                    Log($"ERROR in ProcessGPUFrame: {ex.Message}");
            }
        }

        private void UpdateD3DImage(IntPtr pTexture, int width, int height)
        {
            // This runs on UI thread
            if (_d3dImage == null)
            {
                Log("ERROR: D3DImage is null");
                return;
            }

            try
            {
                _d3dImage.Lock();

                try
                {
                    // TODO: Convert D3D11 texture to D3D9 surface for D3DImage
                    // This is the complex part - D3DImage requires D3D9, but MediaCapture provides D3D11
                    // For now, we'll log success and note this limitation
                    
                    if (_frameCount == 1)
                    {
                        Log($"UpdateD3DImage called on UI thread: {Thread.CurrentThread.ManagedThreadId}");
                        Log($"  Texture: 0x{pTexture.ToInt64():X}, Size: {width}x{height}");
                        Log("  NOTE: D3D11->D3D9 interop needed for D3DImage (not yet implemented)");
                    }

                    // SetBackBuffer would go here once we have D3D9 surface
                    // _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pD3D9Surface);
                    // _d3dImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
                }
                finally
                {
                    _d3dImage.Unlock();
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR in UpdateD3DImage: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        private async Task CleanupAsync()
        {
            Log("CleanupAsync START");
            
            _isInitialized = false;
            IsCapturing = false;

            if (_frameReader != null)
            {
                try
                {
                    _frameReader.FrameArrived -= OnFrameArrived;
                    await _frameReader.StopAsync();
                    _frameReader.Dispose();
                    Log("Frame reader stopped");
                }
                catch (Exception ex)
                {
                    Log($"Error stopping frame reader: {ex.Message}");
                }
                _frameReader = null;
            }

            if (_mediaCapture != null)
            {
                try
                {
                    _mediaCapture.Dispose();
                    Log("MediaCapture disposed");
                }
                catch (Exception ex)
                {
                    Log($"Error disposing MediaCapture: {ex.Message}");
                }
                _mediaCapture = null;
            }

            if (_d3d11Device != IntPtr.Zero)
            {
                try
                {
                    Marshal.Release(_d3d11Device);
                    Log("D3D11 device released");
                }
                catch (Exception ex)
                {
                    Log($"Error releasing D3D device: {ex.Message}");
                }
                _d3d11Device = IntPtr.Zero;
            }

            if (_d3dImage != null && Dispatcher.CheckAccess())
            {
                try
                {
                    _d3dImage.Lock();
                    _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                    _d3dImage.Unlock();
                }
                catch (Exception ex)
                {
                    Log($"Error clearing D3DImage: {ex.Message}");
                }
            }

            if (_imageElement != null && Dispatcher.CheckAccess())
            {
                _imageElement.Source = null;
            }

            Log("CleanupAsync COMPLETE");
        }

        #endregion

        #region Diagnostics

        private void Log(string message)
        {
            Debug.WriteLine($"[MediaCaptureControl] {message}");
        }

        #endregion

        #region Native Interop

        [DllImport("d3d11.dll", SetLastError = true)]
        private static extern int D3D11CreateDevice(
            IntPtr pAdapter,
            int driverType,
            IntPtr software,
            uint flags,
            int[] pFeatureLevels,
            uint featureLevels,
            uint sdkVersion,
            out IntPtr ppDevice,
            out int pFeatureLevel,
            out IntPtr ppImmediateContext);

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid guid);
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        #endregion
    }
}

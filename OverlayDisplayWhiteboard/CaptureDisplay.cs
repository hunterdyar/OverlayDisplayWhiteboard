using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
using Raylib_cs;
using System.Runtime.InteropServices.WindowsRuntime;
using OverlayDisplayWhiteboard.Utility;

public class CaptureDisplay
{
    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    public Texture2D Texture => _texture;
    private Texture2D _texture;
    private byte[]? _frameBuffer;
    private int _width;
    private int _height;
    private readonly Lock _frameLock = new();
    private bool _hasNewFrame;

    public async Task InitializeAsync()
    {
        _mediaCapture = new MediaCapture();
        
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        MediaFrameSourceGroup? selectedGroup = null;
        MediaFrameSourceInfo? colorSourceInfo = null;

        // Find a color video source
        foreach (var sourceGroup in frameSourceGroups)
        {
            if (!sourceGroup.DisplayName.Contains(Program.DeviceName))
            {
                continue;
            }
            foreach (var sourceInfo in sourceGroup.SourceInfos)
            {
                if (sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                {
                    selectedGroup = sourceGroup;
                    colorSourceInfo = sourceInfo;
                    break;
                }
            }

            if (selectedGroup != null)
            {
                break;
            }
        }

        if (selectedGroup == null)
        {
            Program.SetNoDeviceFound();
            return;
        }

        var settings = new MediaCaptureInitializationSettings
        {
            SourceGroup = selectedGroup,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            StreamingCaptureMode = StreamingCaptureMode.Video,
        };
        await _mediaCapture.InitializeAsync(settings);

        // Query all properties of the specified stream type 
        IEnumerable<StreamPropertiesHelper> allStreamProperties =_mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord).Select(x => new StreamPropertiesHelper(x));
        // Order them by resolution then frame rate
        allStreamProperties = allStreamProperties.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);

        var properties = allStreamProperties.First();
        //set chosen steam property.

        await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, properties.EncodingProperties);

        Console.WriteLine($"Set Video to: {properties.Width}x{properties.Height}, {properties.FrameRate}");
       

        // Get the color frame source
        var colorFrameSource = _mediaCapture.FrameSources[colorSourceInfo?.Id];
        
        // Create frame reader
        _frameReader = await _mediaCapture.CreateFrameReaderAsync(colorFrameSource);
        _frameReader.FrameArrived += OnFrameArrived;

        // Start reading frames
        await _frameReader.StartAsync();


        
        Program.SetDeviceFound();
    }

    private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame == null)
        {
            return;
        }

        var videoFrame = frame.VideoMediaFrame;
        var softwareBitmap = videoFrame.SoftwareBitmap;

        if (softwareBitmap == null)
        {
            if (videoFrame.Direct3DSurface != null)
            {
                softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface);
            }
            else
            {
                return;
            }
        }

        // Convert to BGRA8 format
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        }

        lock (_frameLock)
        {
            _width = softwareBitmap.PixelWidth;
            _height = softwareBitmap.PixelHeight;

            if (_frameBuffer == null || _frameBuffer.Length != _width * _height * 4)
            {
                _frameBuffer = new byte[_width * _height * 4];
            }

            // Copy bitmap data to buffer
            softwareBitmap.CopyToBuffer(_frameBuffer.AsBuffer());
                
            // Convert BGRA to RGBA for RayLib
            for (var i = 0; i < _frameBuffer.Length; i += 4)
            {
                byte b = _frameBuffer[i];
                byte r = _frameBuffer[i + 2];
                _frameBuffer[i] = r;
                _frameBuffer[i + 2] = b;
            }

            _hasNewFrame = true;
        }

        softwareBitmap.Dispose();
    }

    public void UpdateTexture()
    {
        lock (_frameLock)
        {
            if (!_hasNewFrame)
            {
                return;
            }
            
            if (_texture.Id == 0)
            {
                unsafe
                {
                    fixed (byte* ptr = _frameBuffer)
                    {
                        var image = new Image
                        {
                            Data = ptr,
                            Width = _width,
                            Height = _height,
                            Mipmaps = 1,
                            Format = PixelFormat.UncompressedR8G8B8A8
                        };
                        _texture = Raylib.LoadTextureFromImage(image);
                    }
                }
            }
            else
            {
                unsafe
                {
                    fixed (byte* ptr = _frameBuffer)
                    {
                        Raylib.UpdateTexture(_texture, ptr);
                    }
                }
            }

            _hasNewFrame = false;
        }
    }


    public void Dispose()
    {
        _frameReader?.StopAsync();//.Wait() was freezing things. so that's probably a bug....
        _mediaCapture?.Dispose(); 
        _frameReader?.Dispose();
        
        if (_texture.Id != 0)
        {
            Raylib.UnloadTexture(_texture);
        }
    }
}
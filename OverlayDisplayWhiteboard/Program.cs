using Windows.Media.Capture;
using Raylib_cs;
using static Raylib_cs.Raylib;
using OverlayDisplayWhiteboard;
using Raylib_cs;

public enum ProgramState
{
	Uninitialized,
	NoDeviceFound,
	DeviceFoundNoTexture,
	OverlayWhiteboard,
	CleanWhiteboard,
}
public static class Program
{
	static CaptureDisplay _capture;
	private static Whiteboard _whiteboard;
	private static List<IInputHandler> _inputHandlers = new List<IInputHandler>();
	public static string DeviceName;
	public static ProgramState ProgramState => _programState;
	private static ProgramState _programState = ProgramState.Uninitialized;
	private static double _errorTime = 0;
	public static async Task Main(string[] args)
	{
		if ((args.Length > 1))
		{
			DeviceName = args[1];
		}
		else
		{
			DeviceName = "Elgato 4K S";
		}

		await Run();
	}

	private static async Task Run()
	{

		_capture = new CaptureDisplay();
		await _capture.InitializeAsync();

		Raylib.InitWindow(1920, 1080, "Whiteboard");

		//Raylib.SetTargetFPS(60);
		SetConfigFlags(ConfigFlags.BorderlessWindowMode);

		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.White);
		Raylib.DrawText("Loading Images", 10, 10, 20, Color.Black);
		Raylib.EndDrawing();

		_whiteboard = new Whiteboard();
		var toolUI = new ChooseToolUI(_whiteboard);
		var colorUI = new ChooseColorUI(_whiteboard);

		//this list is ordered "top to button" intercepting mouse clicks.
		_inputHandlers.Add(toolUI);
		_inputHandlers.Add(colorUI);
		_inputHandlers.Add(_whiteboard);

		if (_programState == ProgramState.Uninitialized)
		{
			//_capture should set this to either nodevice or overlayWhiteboard. whats going oon?
			Console.WriteLine("Something isn't right.... marching forward anyway!");
		}

		while (!Raylib.WindowShouldClose())
		{
			//the list is ord
			if (_programState != ProgramState.NoDeviceFound && _programState != ProgramState.CleanWhiteboard)
			{
				_capture.UpdateTexture();
			}

			TickInput();
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.White);

			//draw window
			if (_programState == ProgramState.OverlayWhiteboard)
			{
				var texture = _capture.Texture;
				if (texture.Id != 0)
				{
					Raylib.DrawTexture(texture, 0, 0, Color.White);

				}
			}
			else if (_programState == ProgramState.NoDeviceFound)
			{
				Raylib.DrawText($"Capture Device not found. Trying again in {_errorTime.ToString("N0")}", 100, 150,
					35, Color.DarkGreen);
				_errorTime -= Raylib.GetFrameTime();
				if (_errorTime >= 0)
				{
					await _capture.InitializeAsync();
					_errorTime = 10;
				}
			}

				//draw program
				_whiteboard.Draw();
				toolUI.Draw();
				colorUI.Draw();

				Raylib.DrawFPS(10, 10);
				//offset layout
				Raylib.EndDrawing();
		}

		_capture.Dispose();
		Raylib.CloseWindow();
		}

	private static void TickInput()
	{
		foreach (var handler in _inputHandlers)
		{
			if (handler.Tick())
			{
				break;
			}
		}
	}

	public static void SetNoDeviceFound()
	{
		_programState = ProgramState.NoDeviceFound;
	}

	public static void SetDeviceFound()
	{
		_programState = ProgramState.OverlayWhiteboard;
	}
}
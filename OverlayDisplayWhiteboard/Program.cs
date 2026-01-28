using Windows.Media.Capture;
using Raylib_cs;
using static Raylib_cs.Raylib;
using OverlayDisplayWhiteboard;
using Raylib_cs;

public static class Program
{
	static CaptureDisplay _capture;
	private static Whiteboard _whiteboard;
	private static List<IInputHandler> _inputHandlers = new List<IInputHandler>();
	public static string DeviceName;

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

		while (!Raylib.WindowShouldClose())
		{
			//the list is ord
		
			_capture.UpdateTexture();
			TickInput();
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.White);

			//draw window
			var texture = _capture.Texture;
			if (texture.Id != 0)
			{
				Raylib.DrawTexture(texture, 0, 0, Color.White);
			}
			else
			{
				Raylib.DrawText("Capture Device Loading or not found or idk", 100,150,35,Color.DarkGreen);	
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
}
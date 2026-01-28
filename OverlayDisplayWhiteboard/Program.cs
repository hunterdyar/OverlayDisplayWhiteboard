using Windows.Media.Capture;
using Raylib_cs;
using static Raylib_cs.Raylib;
using OverlayDisplayWhiteboard;
using Raylib_cs;

public static class Program
{
	static CaptureDisplay _capture;
	private static Whiteboard _whiteboard;
	private static List<IInputHandler> _inputHandlers = new List<IInputHandler>(

	
	public static async Task Main(string[] args)
	{
		Run();
	}

	private static void Run()
	{
		_capture = new CaptureDisplay();
		await _capture.InitializeAsync();

		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(1920, 1080, "Grid Viewer");
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
			TickInput();
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.White);
			
			//draw window
			var texture = _capture.Texture;
			if (texture.Id != 0)
			{
				Raylib.DrawTexture(texture, 0, 0, Color.White);
			}

			//draw program
			_whiteboard.Draw();
			toolUI.Draw();
			colorUI.Draw();
			
			Raylib.DrawFPS(10,10);
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
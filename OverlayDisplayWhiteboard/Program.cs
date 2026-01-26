// See https://aka.ms/new-console-template for more information

using OverlayDisplayWhiteboard;
using Raylib_cs;

public static class Program
{
	private static Whiteboard _whiteboard;
	private static List<IInputHandler> _inputHandlers = new List<IInputHandler>();
	public static async Task Main(string[] args)
	{
		Run();
	}

	private static void Run()
	{
		Raylib.SetTargetFPS(60);
		Raylib.InitWindow(1920, 1080, "Grid Viewer");
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.White);
		Raylib.DrawText("Loading Images", 10, 10, 20, Color.Black);
		Raylib.EndDrawing();

		_whiteboard = new Whiteboard();
		_inputHandlers.Add(_whiteboard);
		while (!Raylib.WindowShouldClose())
		{
			//the list is ord
			TickInput();
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.White);
			_whiteboard.Draw();
			Raylib.DrawFPS(10,10);
			//offset layout
			Raylib.EndDrawing();
		}

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
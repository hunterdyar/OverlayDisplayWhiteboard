using Windows.Media.Capture;
using Raylib_cs;
using static Raylib_cs.Raylib;

public static class Program
{
	static CaptureDisplay _capture;

	public static async Task Main()
	{
		_capture = new CaptureDisplay();
		await _capture.InitializeAsync();
		
		InitWindow(1280, 720, "Screen Whiteboard");
		Raylib.SetTargetFPS(60);
		SetConfigFlags(ConfigFlags.BorderlessWindowMode);
		
		while (!WindowShouldClose()){
                // Draw
                _capture.UpdateTexture();
                BeginDrawing(); 
				ClearBackground(Color.Black);
				
				//draw window
				var texture = _capture.Texture;
				if (texture.Id != 0)
				{
					Raylib.DrawTexture(texture, 0, 0, Color.White);
				}

				Raylib.DrawFPS(10, 10);
				
                EndDrawing();
		}

		_capture.Dispose();
		CloseWindow();
	}

	// Create and initialze the MediaCapture object.
	
}
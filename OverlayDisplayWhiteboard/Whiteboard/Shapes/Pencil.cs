using System.Diagnostics;
using System.Numerics;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class Pencil : Shape
{
	public const float MouseMoveThreshold = 0.95f;

	private Vector2[] Points = new Vector2[512];
	private int pointCount = 0;
	
	private void AddPoint(Vector2 point)
	{
		if (Points.Length == pointCount)
		{
			//reallocate more memory
			var p = new Vector2[Points.Length * 2];
			Array.Copy(Points, p, Points.Length);
			Points = p;
		}

		_lastAddedPos = point;
		Points[pointCount] = point;
		pointCount++;
	}

	private Vector2 _lastAddedPos;
	public override void TickMouseMove(Vector2 mousePos)
	{
		var currentPosition = Raylib.GetMousePosition();
		if ((currentPosition - _lastAddedPos).LengthSquared() < MouseMoveThreshold)
		{
			return;
		}
		AddPoint(mousePos);
		Console.WriteLine(pointCount);
	}
	public override void DrawInProgress()
	{
		Draw();
		//add a line from the last point to the current position of the mouse.
	}

	public override void Draw()
	{
		Raylib.DrawLineStrip(Points, pointCount, Color);
	}

	public override void Start(Vector2 pos)
	{
		AddPoint(pos);
	}

	public override void Complete(Vector2 pos)
	{
		AddPoint(pos);
	}
}
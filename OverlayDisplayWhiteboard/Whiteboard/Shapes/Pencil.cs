using System.Diagnostics;
using System.Numerics;
using OverlayDisplayWhiteboard.Utility;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class Pencil : Shape
{
	private const int SmoothWindowSize = 4;
	private const float MouseMoveThreshold = 0.95f;
	private Vector2[] _points = new Vector2[512];
	private int _pointCount = 0;
	
	private void AddPoint(Vector2 point)
	{
		if (_points.Length == _pointCount)
		{
			//reallocate more memory
			Array.Resize(ref _points, _points.Length + 512);
		}

		_lastAddedPos = point;
		_points[_pointCount] = point;
		_pointCount++;
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
		Console.WriteLine(_pointCount);
	}
	public override void DrawInProgress()
	{
		Draw();
		//add a line from the last point to the current position of the mouse.
	}

	public override void Draw()
	{
		Raylib.DrawLineStrip(_points, _pointCount, Color);
	}

	public override void Start(Vector2 pos)
	{
		AddPoint(pos);
	}

	public override void Complete(Vector2 pos)
	{
		_points = PointUtility.Smooth(_points, _pointCount, SmoothWindowSize);
		AddPoint(pos);
	}
}
using System.Numerics;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class MTWhiteboard : IInputHandler
{
	private const int MaxTouchCount = 10;
	private List<Shape> _shapes = new List<Shape>();
	private Shape?[] _inProgressShapes = new Shape?[MaxTouchCount];
	private Func<Shape> _shapeFactory = () => new Pencil();
	private Color ActiveColor = Color.Black;
	
	public void Draw()
	{
		foreach (var shape in _shapes)
		{
			shape.Draw();
		}

		foreach (var shape in _inProgressShapes)
		{
			if (shape != null)
			{
				shape.DrawInProgress();
			}
		}
	}

	private Vector2[] _touchPositions = new Vector2[MaxTouchCount];
	public bool Tick()
	{ var touchCount = Raylib.GetTouchPointCount();
		if (touchCount > MaxTouchCount)
		{
			touchCount = MaxTouchCount;
		}
		// Get touch points positions
		for (int i = 0; i < touchCount; i++)
		{
			_touchPositions[i] = Raylib.GetTouchPosition(i);
		}
		for (int i = 0; i < touchCount; i++)
		{
			// Make sure point is not (0, 0) as this means there is no touch for it
			if ((_touchPositions[i].X > 0) && (_touchPositions[i].Y > 0))
			{
				// Draw circle and touch index number
				Raylib.DrawCircle((int)_touchPositions[i].X, (int)_touchPositions[i].Y,34, Color.Orange);
				Raylib.DrawText(i.ToString(), (int)_touchPositions[i].X - 10, (int)_touchPositions[i].Y - 70, 40, Color.Black);
			}
		}

		return touchCount > 0;
	}
}
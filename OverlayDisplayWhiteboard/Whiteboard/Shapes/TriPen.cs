using System.Numerics;
using OverlayDisplayWhiteboard.Utility;

namespace OverlayDisplayWhiteboard;

public class TriPen : Shape
{
	private Vector2[] _points = new Vector2[512];
	private int _pointCount = 0;
	private Vector2 _lastMouse;
	private Vector2 _drawLineA;
	private Vector2 _drawLineB;
	private Vector2 _lastDir;
	private float _thickness = 10;
	public override void DrawInProgress()
	{
		Draw();
	}

	private void AddPointToTriangleStrip(Vector2 point)
	{
		if (_points.Length == _pointCount)
		{
			if (_points.Length == _pointCount)
			{
				Array.Resize(ref _points, _points.Length+512);
			}
		}

		_points[_pointCount] = point;
		_pointCount++;
	}

	
	public override void TickMouseMove(Vector2 pos)
	{
		var dir = (pos-_lastMouse);
		var perp = Vector2.Normalize(new Vector2(dir.Y, -dir.X));
		var thickMod = 1-Vector2.Dot(dir, _lastDir);
		thickMod = thickMod / 2;
		var thick = _thickness * thickMod;
		thick = float.Clamp(thick, _thickness*.5f, _thickness);
		_drawLineA = pos + perp* thick /2;
		_drawLineB = pos - perp * thick /2;

		
		if (pos.X > _lastMouse.X)
		{
			if (pos.Y > _lastMouse.Y)
			{
				AddPointToTriangleStrip(_drawLineB);
				AddPointToTriangleStrip(_drawLineA);
			}
			else
			{
				AddPointToTriangleStrip(_drawLineB);
				AddPointToTriangleStrip(_drawLineA);
			}
		}
		else
		{
			AddPointToTriangleStrip(_drawLineB);
			AddPointToTriangleStrip(_drawLineA);
		}


		_lastDir = dir;
		_lastMouse = pos;
	}

	public override void Start(Vector2 pos)
	{
		AddPointToTriangleStrip(pos);
		_lastMouse = pos;
	}

	public override void Complete(Vector2 pos)
	{
		//wait, duh, the points are the triangle strip. we can't smooth those that just collapses the triangles. haha oops.
		//_points = PointUtility.Smooth(_points, _pointCount, 10);
		AddPointToTriangleStrip(pos);
	}

	public override void Draw()
	{
		if (_pointCount > 3)
		{
			Raylib_cs.Raylib.DrawTriangleStrip(_points, _pointCount, Color);
		}
		else
		{
			//draw a circle
		}
	}
}
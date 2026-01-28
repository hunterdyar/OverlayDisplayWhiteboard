using System.Numerics;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

//this whole file is just a reimplementation of this jsfiddle from this stack overflow answer
//https://jsfiddle.net/95tft/
//https://stackoverflow.com/questions/10567287/implementing-smooth-sketching-and-drawing-on-the-canvas-element/10661872#10661872

//but i'm going to throw it away and do something smarter because of the different rendering system.
//(this is immediate mode, that hit a canvas)

//instead of a bunch of lines in parallel that draw (current), we should have a flat line that varies it's width.
//then, it trails a triangle strip.


public class PenPainter()
{
	public float dx;
	public float dy;
	public float ax;
	public float ay;
	public float div;
	public float ease;
	
	private Color _color;
	private List<(int fromX, int fromY, int toX, int toY)> _lines = new List<(int, int, int, int)>();
	public static PenPainter GetPainter(Color color)
	{
		return new PenPainter()
		{
			dx = Raylib.GetScreenWidth() / 2f,
			dy = Raylib.GetScreenHeight() / 2f,
			ax = 0,
			ay = 0,
			div = 0.2f,
			ease = Random.Shared.NextSingle() * 0.05f + 0.5f,
			_color = color
		};
	}

	public void Update(Vector2 mouse)
	{
		var fromx = dx;
		var fromy = dy;

		dx -= ax =
			(ax + (dx - mouse.X) * div) * ease;
		dy -= ay =
			(ay + (dy - mouse.Y) * div) * ease;

		var toX = (int)dx;
		var toY = (int)dy;
		AddStroke(fromx, fromy, toX, toY);
	}
	private void AddStroke(float fromX, float fromY, float toX, float toY)
	{
		float l = MathF.Abs(fromX - fromY) + MathF.Abs(toX - toY);
		if (l < 0.5f)
		{
			Console.WriteLine("don't pen");
			return;
		}
		_lines.Add(((int)fromX, (int)fromY, (int)toX,(int)toY));
	}
	public void Draw()
	{
		foreach (var stroke in _lines)
		{
			Raylib.DrawLine(stroke.fromX, stroke.fromY, stroke.toX, stroke.toY, _color);
			Raylib.DrawLineEx(new Vector2(stroke.fromX, stroke.fromY), new Vector2(stroke.toX, stroke.toY), 2, _color);
		}
		
	}
}
public class Pen : Shape
{
	private Vector2 mouse;
	private int UpdatesPerSecond = 240;
	private int MaxUpdatesPerTick = 1000;

	private double lastUpdate;
	private PenPainter[] _painters;
	
	public Pen()
	{
		int painterCount = 25;
		_painters = new PenPainter[50];
		for (int i = 0; i < 50; i++)
		{
			_painters[i] = PenPainter.GetPainter(Color);
		}
	}
	public override void DrawInProgress()
	{
		Draw();
	}

	public override void Draw()
	{
		foreach (var painter in _painters)
		{
			painter.Draw();
		}
	}

	public override void Tick()
	{
		Update();
	}

	private void DoUpdates()
	{
		if (lastUpdate == 0)
		{
			Update();
			lastUpdate = Raylib.GetTime();
			return;
		}
		
		var elapsed = Raylib.GetTime() - lastUpdate;
		var needed = (int)double.Floor(elapsed * UpdatesPerSecond);
		//clamp to max.
		needed = needed > MaxUpdatesPerTick ? MaxUpdatesPerTick : needed;
		for (int i = 0; i < needed; i++)
		{
			Update();
		}

		lastUpdate = Raylib.GetTime();
	}


	void Update()
	{
		//create lines to draw? or uh?
		for (int i = 0; i < _painters.Length; i++)
		{
			_painters[i].Update(mouse);
		}
	}

	public override void TickMouseMove(Vector2 pos)
	{
		if ((pos - mouse).Length() < 0.95f)
		{
			return;
		}
		mouse = pos;
	}

	public override void Start(Vector2 pos)
	{
		mouse = pos;
		for (var i = 0; i < _painters.Length; i++)
		{
			_painters[i].dx = mouse.X;
			_painters[i].dy = mouse.Y;
		}
	}
	
}
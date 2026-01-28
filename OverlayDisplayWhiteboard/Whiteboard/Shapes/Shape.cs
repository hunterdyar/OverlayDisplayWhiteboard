using System.Numerics;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public abstract class Shape
{
	public Color Color = Raylib_cs.Color.Black;
	public abstract void DrawInProgress();
	public abstract void Draw();
	
	//handle input
	public virtual void Start(Vector2 pos)
	{
		
	}

	public virtual void Tick()
	{
		
	}
	public virtual void TickMouseMove(Vector2 pos)
	{
		
	}
	public virtual void Complete(Vector2 pos)
	{
	}
}
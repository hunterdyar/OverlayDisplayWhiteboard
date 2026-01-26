using System.Numerics;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class Whiteboard : IInputHandler
{
	//i thiiiink we're assuming screen space and 1unit->1px scaling?

	public bool ClearBackground;
	public Color ClearColor = Color.White;
	
	//a whiteboard is a list of shapes and an in-progress shape.
	private List<Shape> _shapes = new List<Shape>();
	public Shape? _inProgressShape;
	
	public void Draw()
	{
		if (ClearBackground)
		{
			Raylib.ClearBackground(ClearColor);
		}

		foreach (var shape in _shapes)
		{
			shape.Draw();
		}

		if (_inProgressShape != null)
		{
			_inProgressShape.DrawInProgress();
		}
	}
	
	//whiteboard utilities
	public void Clear(bool clearInProgress = false)
	{
		_shapes.Clear();
		if (clearInProgress)
		{
			_inProgressShape?.Complete(Raylib.GetMousePosition());
			_inProgressShape = null;
		}
	}
	
	//input start.
	//input continue.
	//input end.
	private enum MouseState
	{
		Up,
		Down
	}
	//just a helper
	private static MouseState GetLeftMouseButtonState() => Raylib.IsMouseButtonDown(MouseButton.Left) ? MouseState.Down : MouseState.Up;
	
	private MouseState _mouseState = GetLeftMouseButtonState();
	public bool Tick()
	{
		//handle initial press/release
		bool didSomething = false;
		var currentMouseState = GetLeftMouseButtonState();
		if (currentMouseState != _mouseState)
		{
			if (currentMouseState == MouseState.Down)
			{
				//just pressed
				StartNewShape();
				didSomething = true;
			}else if (currentMouseState == MouseState.Up)
			{
				//just released
				CompleteShape();
				didSomething = true;
			}
		}
		//update
		_mouseState = currentMouseState;
		
		//handle movement
		if (_mouseState == MouseState.Down)
		{
			if (_inProgressShape == null)
			{
				//not drawing. maybe clicking on UI or something and the event didn't get handled?
				return didSomething;
			}
			_inProgressShape.TickMouseMove(Raylib.GetMousePosition());
		}
		else
		{
			return didSomething;
		}

		//should this be below?
		if (_inProgressShape != null)
		{
			_inProgressShape.Tick();
		}
		
		return true;
	}


	private void CompleteShape()
	{
		if (_inProgressShape == null)
		{
			//invalid state.
			Console.WriteLine("can't finish shape. no in-progress shape exists. resetting.");
			return;
		}

		_inProgressShape.Complete(Raylib.GetMousePosition());
		_shapes.Add(_inProgressShape);
		_inProgressShape = null;
	}

	private void StartNewShape()
	{
		if (_inProgressShape != null)
		{
			Console.WriteLine("can't start shape. previous shape not finished. Force completing now, this is probably wrong.");
			CompleteShape();
		}

		//get active tool/selected shape.
		var shape = new Pen();
		shape.Start(Raylib.GetMousePosition());
		_inProgressShape = shape;
	}
}
using System.Numerics;
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public enum ButtonShape
{
	Rect,
	Circle,
	Icon
}
public class Button
{
	public string Name;
	public Color Color;
	public Action PressAction;
	private bool _hovering;

	private ButtonShape _buttonShape = ButtonShape.Rect;
	private int _x;
	private int _y;
	private int _width;
	private int _height;

	public Button(string name, Color color, int x, int y, int width, int height)
	{
		Name = name;
		Color = color;
		_x = x;
		_y = y;
		_width = width;
		_height = height;
	}

	public void SetHovering(bool hovering)
	{
		_hovering = hovering;
	}
	
	public bool Overlapping(Vector2 pos)
	{
		switch (_buttonShape)
		{
			case ButtonShape.Rect:
				return pos.X >= _x && pos.X <= _x + _width && pos.Y >= _y && pos.Y <= _y + _height; 
			case ButtonShape.Circle:
				return (pos - new Vector2(_x, _y)).Length() < _width / 2f;
			default:
				return false;
		}
	}

	public void Draw()
	{
		//todo: cache tint color
		var c = !_hovering ? Color : Color.Lerp(Color, Color.Black, 0.2f);
		var textWidth = Raylib.MeasureText(Name, 14);

		switch (_buttonShape)
		{
			case ButtonShape.Rect:
				Raylib.DrawRectangle(_x, _y, _width, _height, c);
				Raylib.DrawRectangleLines(_x, _y, _width, _height, Color.Black);
				Raylib.DrawText(Name, _x + _width / 2 - textWidth / 2, _y + _height / 2 - 7, 14, Color.Black);
				return;
			case ButtonShape.Circle:
				Raylib.DrawCircle(_x, _y, _width/2f, c);
				Raylib.DrawCircleLines(_x, _y, _width/2f, Color.Black);
				Raylib.DrawText(Name, _x - textWidth / 2, _y - 7, 14, Color.Black);
				return;
			case ButtonShape.Icon:
				//todo
				return;
		}
	}

	public void SetShape(ButtonShape buttonShape)
	{
		_buttonShape = buttonShape;
	}
}
using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class ButtonUI : IInputHandler
{
	private List<Button> _buttons = new List<Button>();

	protected void AddButton(Button button)
	{
		_buttons.Add(button);
	}

	public void Draw()
	{
		foreach (var button in _buttons)
		{
			button.Draw();
		}
	}
	public bool Tick()
	{
		var p = Raylib.GetMousePosition();
		var click = Raylib.IsMouseButtonPressed(0);
		foreach (var button in _buttons)
		{
			button.SetHovering(button.Overlapping(p));
			if (click)
			{
				if (button.Overlapping(p))
				{
					Console.WriteLine("button pressed!");
					button.PressAction?.Invoke();
					return true;
				}
			}
		}

		return false;
	}
}
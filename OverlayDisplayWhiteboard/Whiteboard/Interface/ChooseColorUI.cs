using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class ChooseColorUI : ButtonUI
{
	private Whiteboard _whiteboard;
	private Color[] _palette = Palettes.CyberGum;
	public ChooseColorUI(Whiteboard whiteboard)
	{
		_whiteboard = whiteboard;
		
		int xpos = 40;
		int size = 50;
		int gutter = 10;
		var screenHeight = Raylib.GetScreenHeight();
		int totalHeight = _palette.Length*size+(_palette.Length-1)*gutter;
		int ystart = screenHeight / 2 - totalHeight / 2;
		for (var i = 0; i < _palette.Length; i++)
		{
			var color = _palette[i];
			int y = (size * i) + (i > 0 ? (gutter * i) : 0);
			var button = new Button("", color, xpos, ystart+y , size, size);
			button.SetShape(ButtonShape.Circle);
			button.PressAction += () => { _whiteboard.SetColor(color); };
			AddButton(button);
		}
	}
}
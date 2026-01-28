using Raylib_cs;

namespace OverlayDisplayWhiteboard;

public class ChooseToolUI : ButtonUI
{
	private Whiteboard _whiteboard;
	public ChooseToolUI(Whiteboard whiteboard)
	{
		_whiteboard = whiteboard;
		int ypos = 40;
		int size = 50;
		int gutter = 10;
		var pencilButton = new Button("pencil", Color.Brown, gutter + (gutter + size) * 1, ypos, size, size);
		pencilButton.PressAction += ()=> { _whiteboard.SetShapeFactory(() => new Pencil()); };
		AddButton(pencilButton);

		var pen = new Button("inky", Color.Blue, gutter+(gutter+size)*2, ypos, size, size);
		pen.PressAction += () => { _whiteboard.SetShapeFactory(() => new TriPen()); };
		AddButton(pen);
	}
	
}
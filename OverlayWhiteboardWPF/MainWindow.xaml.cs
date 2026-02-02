using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OverlayWhiteboardWPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		// TheInkCanvas.SetEnabledGestures();
		this.IsManipulationEnabled = true;
		this.WindowState = WindowState.Maximized;
		this.WindowStyle = WindowStyle.None;
		this.Topmost = true;
	}
	// Set the EditingMode to ink input.
	private void Ink(object sender, RoutedEventArgs e)
	{
		TheInkCanvas.EditingMode = InkCanvasEditingMode.Ink;

		// Set the DefaultDrawingAttributes for a red pen.
		TheInkCanvas.DefaultDrawingAttributes.Color = Colors.Red;
		TheInkCanvas.DefaultDrawingAttributes.IsHighlighter = false;
		TheInkCanvas.DefaultDrawingAttributes.Height = 2;
		TheInkCanvas.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
	}

// Set the EditingMode to highlighter input.
	private void Highlight(object sender, RoutedEventArgs e)
	{
		TheInkCanvas.EditingMode = InkCanvasEditingMode.Ink;

		// Set the DefaultDrawingAttributes for a highlighter pen.
		TheInkCanvas.DefaultDrawingAttributes.Color = Colors.Yellow;
		TheInkCanvas.DefaultDrawingAttributes.IsHighlighter = true;
		TheInkCanvas.DefaultDrawingAttributes.Height = 25;
		TheInkCanvas.DefaultDrawingAttributes.StylusTip = StylusTip.Rectangle;

	}

	private void EraseStroke(object sender, RoutedEventArgs e)
	{
		TheInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
	}

	private void Select(object sender, RoutedEventArgs e)
	{
		TheInkCanvas.EditingMode = InkCanvasEditingMode.Select;
	}

	private void Clear(object sender, RoutedEventArgs e)
	{
		TheInkCanvas.Strokes.Clear();
	}
}
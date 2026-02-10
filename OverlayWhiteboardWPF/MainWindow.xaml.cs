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
		this.WindowState = WindowState.Maximized;
		this.WindowStyle = WindowStyle.None;
		this.Topmost = true;
		CaptureControl.Loaded += (s,e) => { UpdateStatus(); };
		// Update status when capturing changes
		var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MediaCaptureControls.MediaCaptureControl.IsCapturingProperty, typeof(MediaCaptureControls.MediaCaptureControl));

		descriptor?.AddValueChanged(CaptureControl, (s, e) => { UpdateStatus(); });
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

	private void DeviceIndexCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		// if (CaptureControl != null && DeviceIndexCombo.SelectedIndex >= 0)
		// {
		// 	System.Diagnostics.Debug.WriteLine($"Now switching to capture control device index {DeviceIndexCombo.SelectedIndex}");
		// 	CaptureControl.DeviceIndex = DeviceIndexCombo.SelectedIndex;
		// }
	}

	// private void StretchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	// {
	// 	if (CaptureControl != null && StretchCombo.SelectedItem is ComboBoxItem item)
	// 	{
	// 		CaptureControl.Stretch = item.Content.ToString() switch
	// 		{
	// 			"Uniform" => Stretch.Uniform,
	// 			"Fill" => Stretch.Fill,
	// 			"UniformToFill" => Stretch.UniformToFill,
	// 			"None" => Stretch.None,
	// 			_ => Stretch.Uniform
	// 		};
	// 	}
	// }

	private void UpdateStatus()
	{
		if (StatusText != null)
		{
			// StatusText.Text = CaptureControl.IsCapturing
			// 	? $"Status: Capturing ({CaptureControl.DeviceIndex})"
			// 	: "Status: No Signal";
			//
			// StatusText.Foreground = CaptureControl.IsCapturing
			// 	? new SolidColorBrush(Colors.Green)
			// 	: new SolidColorBrush(Colors.Red);
		}
	}
}
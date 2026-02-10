using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OverlayWhiteboardWPF;

public class TouchButton : Button
{
	public TouchButton()
	{
		TouchDown += OnTouchDown;
		TouchUp += OnTouchUp;
		TouchEnter += OnTouchEnter;
		TouchLeave += OnTouchLeave;
	}

	private void OnTouchLeave(object? sender, TouchEventArgs e)
	{
		
	}

	private void OnTouchEnter(object? sender, TouchEventArgs e)
	{
	}

	private void OnTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
	{
		RaiseEvent(new RoutedEventArgs(ClickEvent));
	}

	private void OnTouchUp(object? sender, TouchEventArgs e)
	{
	}

}
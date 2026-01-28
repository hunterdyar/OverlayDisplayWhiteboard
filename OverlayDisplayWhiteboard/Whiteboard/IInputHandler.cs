namespace OverlayDisplayWhiteboard;

public interface IInputHandler
{
	/// <summary>
	/// Return true when input has been handled and should not be handled by any 'lower' handlers.
	/// </summary>
	public abstract bool Tick();
}
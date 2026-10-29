namespace ClassSystems.Infra.Display;

/// <summary>
/// An object that knows how to turn itself into text for the user.
/// </summary>
/// <remarks>
/// Usually it is a DTO built from a model: the model keeps only data, and the DTO decides how the data looks.
/// </remarks>
public interface IDisplayable
{
    /// <summary>
    /// Builds the text representation of the object (it may span several lines).
    /// </summary>
    string Render();
}

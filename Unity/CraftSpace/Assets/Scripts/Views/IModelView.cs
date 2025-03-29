/// <summary>
/// Interface for views that display model data
/// </summary>
/// <typeparam name="T">The type of model this view displays</typeparam>
public interface IModelView<T> where T : class
{
    /// <summary>
    /// Gets the current model being displayed
    /// </summary>
    T Model { get; }
    
    /// <summary>
    /// Sets the model to be displayed
    /// </summary>
    void SetModel(T model);
    
    /// <summary>
    /// Called when the model is updated and the view should refresh
    /// </summary>
    void HandleModelUpdated();
} 
// #define VERBOSE_Bindable

namespace Lyt.Avalonia.Mvvm;

/// <summary> View Model base class, derived from ObservableObject from the MSFT CTK.  </summary>
public class ViewModel : ObservableObject, ISupportBehaviors, IBindable
{
    private static ILocalizer? StaticLocalizer;

    private static IFocuser? StaticFocuser;

#pragma warning disable CS8618 
    // Non-nullable field must contain a non-null value when exiting constructor.
    // Consider adding the 'required' modifier or declaring as nullable.

    private static IHost StaticHost;

    private static ILogger StaticLogger;

    private static IDispatch StaticDispatcher;

    // private static IMessenger StaticMessenger;

    private static IProfiler StaticProfiler;

#pragma warning restore CS8618 

    public static void TypeInitialize(IHost host)
    {
        StaticHost = host;
        var services = host.Services;
        if (services is null)
        {
            Debug.WriteLine("No services \n");
            throw new Exception("No services.");
        }

        try
        {
            StaticDispatcher = services.GetRequiredService<IDispatch>();
            StaticLogger = services.GetRequiredService<ILogger>();
            StaticProfiler = services.GetRequiredService<IProfiler>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Missing some essential services \n" + ex.ToString());
            throw;
        }

        try
        {
            StaticLocalizer = services.GetService<ILocalizer>();
            if (StaticLocalizer is null)
            {
                StaticLogger.Error("Missing localizer service, will not be able to localize. \n");
            }

            StaticFocuser = services.GetService<IFocuser>();
            if (StaticFocuser is null)
            {
                StaticLogger.Error("Missing focuser service, will not be able to set focus on fields. \n");
            }
        }
        catch (Exception ex)
        {
            StaticLogger.Error("Exception when trying to retrieve localizer service. \n" + ex.ToString());
        }
    }

    public ViewModel() { }

#pragma warning disable IDE0079
#pragma warning disable CA1822 // Mark members as static

    public IHost Host => StaticHost;

    public IDispatch Dispatcher => StaticDispatcher;

    public IProfiler Profiler => StaticProfiler;

#pragma warning disable NX0001 // Nullable

    public ILocalizer Localizer =>
        // ! Verified by CanLocalize
        this.CanLocalize ? StaticLocalizer! : throw new Exception("Should have checked CanLocalize property.");

    public IFocuser Focuser =>
        // ! Verified by CanFocus
        this.CanFocus ? StaticFocuser! : throw new Exception("Should have checked CanFocus property.");

#pragma warning restore NX0001 // Nullable
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079

    /// <summary> The View, its Data Context is this instance. </summary>
    /// <remarks> Aka, the "Control" </remarks>
    public IView? ViewBase { get; private set; }

    public List<object> Behaviors { get; private set; } = [];

    /// <summary> Binds a view and setup callbacks. </summary>
    public void BindOnDataContextChanged(IView view)
    {
        this.ViewBase = view;
        this.OnDataBinding();
    }

    /// <summary> Binds a view and setup callbacks. </summary>
    public void Bind(IView view)
    {
        this.ViewBase = view;
        this.Unbind();
        try
        {
            this.ViewBase.DataContext = this;
        }
        catch (InvalidCastException ex)
        {
            // Crash here ? This should never happen, ever. 
            // Major issue when defining the view, usually conflicting DaataContext by inheritance
            if (Debugger.IsAttached) { Debugger.Break(); }
            Debug.WriteLine(this.GetType().FullName);
            Debug.WriteLine(this.ViewBase.GetType().FullName);
            Debug.WriteLine(this.ViewBase.DataContext?.GetType().FullName);
            Debug.WriteLine(ex);
        }

        this.OnDataBinding();
    }

    /// <summary> Unbinds this view model. </summary>
    public void Unbind()
    {
        if (this.ViewBase is not null)
        {
            if (this.ViewBase.DataContext != null)
            {
                this.ViewBase.DataContext = null;
            }
        }
    }

    /// <summary> Unbinds the provided view. </summary>
    public static void Unbind(IView view)
    {
        if (view is not null)
        {
            if (view.DataContext is ViewModel viewModel)
            {
                viewModel.ViewBase = null;
                view.DataContext = null;
            }
        }
    }

    /// <summary> Invoked when this view model is bound </summary>
    protected virtual void OnDataBinding() { }

    /// <summary> Invoked when this view model's control is loaded. </summary>
    public virtual void OnViewLoaded() { }

    public bool IsActivated { get; private set; }

    /// <summary> Usually invoked when this view model is about to be shown, but could be used for other purposes. </summary>
    public virtual void Activate(object? activationParameters)
    {
        this.LogActivation(activationParameters);
        this.IsActivated = true;
    }

    /// <summary> Usually invoked when this view model is about to be hidden, and same as above. </summary>
    public virtual void Deactivate()
    {
        this.LogDeactivation();
        this.IsActivated = false;
    }

    public virtual bool CanEscape { get; set; } = true;

    public virtual bool CanEnter { get; set; } = true;

    public virtual bool Validate() => true;

    public virtual bool TrySaveAndClose() => true;

    public virtual void CancelViewModel() { }

    public virtual void Cancel() { }

    public virtual void Initialize() { }

    #region IBindable implementation 

    public void Set<T>(string propertyName, T value)
    {
        try
        {
            // this.InvokeSetProperty(propertyName, value);
        }
        catch (Exception ex)
        {
            this.Logger.Warning("Property " + propertyName + " not set: " + ex.Message);
            Debug.WriteLine(ex);
        }
    }

    public T? Get<T>(string propertyName)
    {
        try
        {
            //object? maybeT = this.InvokeGetProperty(propertyName);
            //if (maybeT is T realT)
            //{
            //    return realT;
            //}

            //if (typeof(T) == typeof(string))
            //{
            //    return default;
            //}
            //else
            //{
            //    maybeT = Convert.ChangeType(maybeT, typeof(T), CultureInfo.InvariantCulture);
            //    if (maybeT is T convertedT)
            //    {
            //        return convertedT;
            //    }

            throw new Exception("Incompatible types");
            //}
        }
        catch (Exception ex)
        {
            this.Logger.Warning("Failed to get property " + propertyName + " : " + ex.Message);
            Debug.WriteLine(ex);
            throw;
        }
    }

    public ILogger Logger => StaticLogger;

    public bool CanLocalize => StaticLocalizer is not null;

    public string Localize(string message, bool failSilently = false)
    {
        if (this.CanLocalize)
        {
            return this.Localizer.Lookup(message, failSilently);
        }

        return message;
    }

    public bool CanFocus => StaticFocuser is not null;

    public bool TryFocusField(string focusFieldName)
    {
        if (this.ViewBase is null || this.Focuser is null)
        {
            string msg = "TryFocusField: no view or no Focus service: " + focusFieldName;
            Debug.WriteLine(msg);
            this.Logger.Warning(msg);
            return false;
        }

        return this.Focuser.SetFocus(this.ViewBase, focusFieldName);
    }

    #endregion IBindable implementation 

    #region Debug Utilities 

    /// <summary> Logs that a view model is being deactivated. </summary>
    [Conditional("DEBUG")]
    private void LogDeactivation()
    {
        string typeName = this.GetType().Name;
        string message = string.Format("Deactivating {0}", typeName);
        this.Logger.Info(message);
    }

    /// <summary> Logs that a view model is being activated. </summary>
    [Conditional("DEBUG")]
    private void LogActivation(object? parameter)
    {
        string parameterString =
            parameter is null ? "<null>" : parameter.GetType().Name + " - " + parameter.ToString();
        string typeName = this.GetType().Name;
        string message = string.Format("Activating {0} with {1}", typeName, parameterString);
        this.Logger.Info(message);
    }


    #endregion Debug Utilities 
}

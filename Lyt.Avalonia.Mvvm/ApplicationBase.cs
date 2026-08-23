namespace Lyt.Avalonia.Mvvm;

#pragma warning disable CS8618 
// Non-nullable field must contain a non-null value when exiting constructor.
// Consider declaring as nullable.

public class ApplicationBase : Application, IApplicationBase
{
    public static Window MainWindow { get; private set; }

    // The host cannot be null or else there is no app...
    public static IHost AppHost { get; private set; }

    // Logger will never be null or else the app did not take off
    public ILogger Logger { get; private set; }

    // Can be null ! 
    private Window? splashWindow;

    // LATER, maybe, using Fluent theme for now
    // public StyleManager StyleManager { get; private set; }

    // To enforce single instance 
    private static FileStream? LockFile;

    private readonly string organizationKey;
    private readonly string applicationKey;
    private readonly string uriString;
    private readonly bool isSingleInstanceRequested;
    private readonly Uri? splashImageUri;
    private readonly Window? appSplashWindow;

    private IClassicDesktopStyleApplicationLifetime? desktop;

    private readonly Func<IHost>? initializeHosting;
    private readonly Func<List<Type>> getModelTypes;

    public ApplicationBase(
        string organizationKey,
        string applicationKey,
        string uriString,
        Func<IHost> initializeHosting,
        Func<List<Type>> getModelTypes,
        bool singleInstanceRequested = false,
        Uri? splashImageUri = null,
        Window? appSplashWindow = null)
    {
        this.organizationKey = organizationKey;
        this.applicationKey = applicationKey;
        this.uriString = uriString;
        this.initializeHosting = initializeHosting;
        this.getModelTypes = getModelTypes;
        this.isSingleInstanceRequested = singleInstanceRequested;
        this.splashImageUri = splashImageUri;
        this.appSplashWindow = appSplashWindow;
    }

    public static Tuple<Type, Type> Service<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
        => new(typeof(TInterface), typeof(TImplementation));

    public static T GetRequiredService<T>() where T : notnull
        => ApplicationBase.AppHost.Services.GetRequiredService<T>();

    public static object GetRequiredService(Type type)
        => ApplicationBase.AppHost.Services.GetRequiredService(type);

    public static T? GetOptionalService<T>() where T : notnull
        => ApplicationBase.AppHost.Services.GetService<T>();

    public static object? GetOptionalService(Type type)
        => ApplicationBase.AppHost.Services.GetService(type);

    public List<IModel> GetModels()
    {
        List<IModel> models = [];
        foreach (var modelType in this.getModelTypes())
        {
            object? model = ApplicationBase.GetRequiredService(modelType);
            if (model is not IModel iModel)
            {
                continue;
            }

            models.Add(iModel); 
            this.Logger.Info(" Found Model: " + model.ToString());
        }

        return models; 
    }

    public static TModel GetModel<TModel>() where TModel : notnull
    {
        TModel? model = ApplicationBase.GetRequiredService<TModel>() ??
            throw new ApplicationException("No model of type " + typeof(TModel).FullName);
        bool isModel = typeof(IModel).IsAssignableFrom(typeof(TModel));
        if (!isModel)
        {
            throw new ApplicationException(typeof(TModel).FullName + "  is not a IModel");
        }

        return model;
    }

    public async Task Shutdown()
    {
        this.Logger.Info("***   Shutdown   ***");
        await this.OnShutdownBegin();

        //startupWindow.Closing += (_, _) => { this.logViewer?.Close(); };
        IApplicationModel applicationModel = ApplicationBase.GetRequiredService<IApplicationModel>();
        await applicationModel.Shutdown();
        await ApplicationBase.AppHost.StopAsync();
        await this.OnShutdownComplete();

        this.ForceShutdown();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Try to catch all exceptions, missing the ones on the main thread at this time 
        TaskScheduler.UnobservedTaskException += this.OnTaskSchedulerUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += this.OnCurrentDomainUnhandledException;
        Dispatcher.UIThread.ShutdownStarted += this.OnDispatcherShutdownStarted;

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            this.desktop =
                lifetime ??
                throw new InvalidOperationException("Desktop should not be null.");

            // Enforce single instance if requested 
            if (this.isSingleInstanceRequested && this.IsAlreadyRunning())
            {
                this.ForceShutdown();
                return;
            }

            if ((this.splashImageUri is not null) && (this.appSplashWindow is not null))
            {
                throw new InvalidOperationException("Cannot have two splash windows.");
            }

            if (this.splashImageUri is not null)
            {
                // Show default splash window
                this.splashWindow = new ImageSplashWindow(this.splashImageUri);
                this.desktop.MainWindow = this.splashWindow;
            }

            if (this.appSplashWindow is not null)
            {
                // Show app provided splash screen window
                this.splashWindow = this.appSplashWindow;
                this.desktop.MainWindow = this.splashWindow;
            }
        }

        // Let Avalonia complete its own startup and show us the splash.
        // Note: Base class doing nothing, but keep: may change in the future 
        base.OnFrameworkInitializationCompleted();

        // Launch the actual init of the app, delay just a bit to ensure the splash shows up
        Schedule.OnUiThread(50, this.InitializeApplication, DispatcherPriority.ApplicationIdle);
    }

    protected virtual Task OnStartupBegin() => Task.CompletedTask;

    protected virtual Task OnStartupComplete() => Task.CompletedTask;

    protected virtual Task OnShutdownBegin() => Task.CompletedTask;

    protected virtual Task OnShutdownComplete() => Task.CompletedTask;

    private async void InitializeApplication()
    {
        if (this.initializeHosting is Func<IHost> hostingCallback)
        {
            ApplicationBase.AppHost = hostingCallback();
        }
        else
        {
            throw new Exception("Invalid Configuration");
        }

        if (Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        // 
        // BindingPlugins.DataValidators.RemoveAt(0);

        if (this.desktop is not null)
        {
            var startupWindow = ApplicationBase.GetRequiredService<Window>();
            if (startupWindow is Window window)
            {
                // Create the main window without showing it 
                ApplicationBase.MainWindow = window;

                // LATER, maybe, using Fluent theme for now
                // this.StyleManager = new StyleManager(window);

                // Start and wait for startup to complete
                await this.Startup();

                // Show the main window once init is fully complete 
                this.desktop.MainWindow = ApplicationBase.MainWindow;
                ApplicationBase.MainWindow.Show();

                // Close the splash screen if any was created 
                this.splashWindow?.Close();
            }
            else
            {
                throw new NotImplementedException("Failed to create MainWindow");
            }
        }
        else
        {
            // Still in designer mode ? 
            throw new InvalidOperationException("Desktop should not be null.");
        }
    }

    private async Task Startup()
    {
        await ApplicationBase.AppHost.StartAsync();
        ViewModel.TypeInitialize(ApplicationBase.AppHost);
        await this.OnStartupBegin();

        var logger = ApplicationBase.GetRequiredService<ILogger>();
        this.Logger = logger;

        if (/* Debugger.IsAttached && */ this.Logger is LogViewerWindow logViewer)
        {
            try
            {
                logViewer.Show();
            }
            catch (Exception) { /* swallow */ }
        }

        this.Logger.Info("***   Startup   ***");

        // Warming up the models: 
        // This ensures that the Application Model and all listed models are constructed.
        foreach (var modelType in this.getModelTypes())
        {
            var model = ApplicationBase.GetRequiredService(modelType); 
            if (model  is not IModel)
            {
                throw new ApplicationException("Failed to warmup model: " + model.ToString());
            }

            this.Logger.Info(" Found Model: " + model.ToString());            
        }

        IApplicationModel applicationModel = ApplicationBase.GetRequiredService<IApplicationModel>();
        await applicationModel.Initialize();
        await this.OnStartupComplete();
    }

    private void ForceShutdown()
    {
        if (this.desktop is not null)
        {
            this.desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            this.desktop.Shutdown();
        }
    }

    private bool IsAlreadyRunning()
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            // No multiple instances on Mac 
            return false;
        }
        else
        {
            // Windows or Unix
            try
            {
                string directory =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), this.organizationKey);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string filePath = Path.Combine(directory, string.Concat(this.applicationKey, ".lock"));
                ApplicationBase.LockFile = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                if (ApplicationBase.LockFile is not null)
                {
                    ApplicationBase.LockFile.Lock(0, 0);
                    return false;
                }
            }
            catch
            {
                // Swallow and assume we are permitted to run 
                return false;
            }

            return true;
        }
    }

    private void OnDispatcherShutdownStarted(object? sender, EventArgs e)
    {
        if (Debugger.IsAttached)
        {
            // Use this break to debug issues at startup, if needed 
            // Debugger.Break();
        }

        this.Logger.Info("***   Shutdown Started   ***");
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        => this.GlobalExceptionHandler(e.ExceptionObject as Exception);

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        => this.GlobalExceptionHandler(e.Exception);

    private void GlobalExceptionHandler(Exception? exception)
    {
        if ((this.Logger is not null) && (exception is not null))
        {
            this.Logger.Error(exception.ToString());
        }

        if (Debugger.IsAttached) { Debugger.Break(); }

        // ??? 
        // What can we do here ? 
    }
}

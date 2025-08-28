namespace Lyt.Avalonia.TestApp.Shell;

using CommunityToolkit.Mvvm.Messaging;

public sealed partial class ShellViewModel : ViewModel<ShellView>, IRecipient<ModelUpdateMessage>
{
    private readonly TimingModel timingModel;

    [ObservableProperty]
    public string? buttonText;

    [ObservableProperty]
    public string? tickCount;

    [ObservableProperty]
    public string? isTicking;

    public ShellViewModel()
    {
        this.timingModel = App.GetRequiredService<TimingModel>();  
        this.Subscribe<ModelUpdateMessage>();
        this.TickCount = "Hello Avalonia!";
        this.IsTicking = string.Empty;
    }

    public WorkflowManager<WorkflowState, WorkflowTrigger>? Workflow { get; private set; }

    [RelayCommand]
    public void OnStartStop()
    {
        if (this.timingModel.IsTicking)
        {
            this.timingModel.Stop();
        }
        else
        {
            this.timingModel.Start();
        }

        // Not good 
        // This can only be caught in the main top level try catch 
        //
        // throw new NotImplementedException("wtf");
    }

    [RelayCommand]
    public void OnSvg()
    {
        string source = this.View!.callIcon.Source;
        this.View.callIcon.Source = source == "call" ? "call_end" : "call";
        //var rootFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        //var folder = "assets";
        //var svgFolderPath = Path.Combine(rootFolder, folder);
        //var targetFolder = "ExtractedSvg";
        //var targetFolderPath = Path.Combine(rootFolder, targetFolder);
        //if (Directory.Exists(svgFolderPath))
        //{
        //    // Enumerates files 
        //    var enumerationOptions = new EnumerationOptions()
        //    {
        //        IgnoreInaccessible = true,
        //        RecurseSubdirectories = true,
        //        MatchType = MatchType.Simple,
        //        MaxRecursionDepth = 2,
        //    };
        //    var files = Directory.EnumerateFiles(svgFolderPath, "*.svg", enumerationOptions);
        //    foreach (string file in files)
        //    {
        //        if ( !file.Contains("regular"))
        //        {
        //            continue;
        //        }

        //        if (!file.Contains("24"))
        //        {
        //            continue;
        //        }

        //        FileInfo fi = new FileInfo(file);
        //        var target = Path.Combine(targetFolderPath, fi.Name);
        //        target = target.Replace("ic_fluent_", "");
        //        target = target.Replace("_24", "");
        //        target = target.Replace("_regular", "");

        //        File.Copy(file, target, true);
        //    }
        //}
    }

    public override async void OnViewLoaded()
    {
        base.OnViewLoaded();

        //this.timingModel.Stop();

        this.timingModel.Start();
        this.SetupWorkflow();
        if (this.Workflow is not null)
        {
            await this.Workflow.Initialize();
            _ = this.Workflow.Start();
        }
    }

    public void Receive(ModelUpdateMessage _)
    {
        Dispatch.OnUiThread(() =>
        {
            int ticks = this.timingModel.TickCount;
            this.TickCount = string.Format("Ticks: {0}", ticks);
            bool modelIsTicking = this.timingModel.IsTicking;
            this.IsTicking = modelIsTicking ? "Ticking" : "Stopped";
            this.ButtonText = modelIsTicking ? "Stop" : "Start";
            var profiler = App.GetRequiredService<IProfiler>();
            profiler.MemorySnapshot();
            if (this.Workflow is not null)
            {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                var fireAndForget = this.Workflow.Next();
#pragma warning restore IDE0059
            }
        });
    }

    private void SetupWorkflow()
    {
        StateDefinition<WorkflowState, WorkflowTrigger, ViewModel> Create<TViewModel, TView>(
            WorkflowState state, WorkflowTrigger trigger, WorkflowState target)
            where TViewModel : ViewModel, new()
            where TView : Control, IView, new()
        {
            var vm = App.GetRequiredService<TViewModel>();
            vm.Bind(new TView());
            if (vm is WorkflowPage<WorkflowState, WorkflowTrigger> page)
            {
                page.State = state;
                page.Title = state.ToString();
                return
                    new StateDefinition<WorkflowState, WorkflowTrigger, ViewModel>(
                        state, page, null, null, null, null,
                        [
                            new TriggerDefinition<WorkflowState, WorkflowTrigger> ( trigger, target , null )
                        ]);
            }
            else
            {
                string msg = "View is not a Workflow Page";
                this.Logger.Error(msg);
                throw new Exception(msg);
            }
        }

        var startup = Create<StartupViewModel, StartupView>(WorkflowState.Startup, WorkflowTrigger.Ready, WorkflowState.Login);
        var login = Create<LoginViewModel, LoginView>(WorkflowState.Login, WorkflowTrigger.LoggedIn, WorkflowState.Select);
        var select = Create<SelectViewModel, SelectView>(WorkflowState.Select, WorkflowTrigger.Selected, WorkflowState.Process);
        var process = Create<ProcessViewModel, ProcessView>(WorkflowState.Process, WorkflowTrigger.Complete, WorkflowState.Login);
        var stateMachineDefinition =
            new StateMachineDefinition<WorkflowState, WorkflowTrigger, ViewModel>(
                WorkflowState.Startup, // Initial state
                [ 
                    // List of state definitions
                    startup, login , select, process,
                ]);

        var hostControl = 
            this.View!.WorkflowContent ?? throw new Exception("Workflow Host control is null");
        this.Workflow =
            new WorkflowManager<WorkflowState, WorkflowTrigger>(this.Logger, hostControl, stateMachineDefinition);
    }
}

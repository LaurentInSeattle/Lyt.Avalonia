﻿namespace Lyt.Avalonia.TestApp.Workflow;

public enum WorkflowState
{
    Startup, 
    Login, 
    Select,
    Process,
}

public enum WorkflowTrigger
{
    Ready, 
    LoggedIn, 
    Selected, 
    Complete, 
}

global using System;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;
global using System.Windows.Input;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;

global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Data;
global using Avalonia.Data.Core.Plugins;
global using Avalonia.Markup.Xaml;
global using Avalonia.Media;
global using Avalonia.Media.Immutable;
global using Avalonia.Threading;

global using Lyt.Framework.Interfaces.Binding;
global using Lyt.Framework.Interfaces.Dispatching;
global using Lyt.Framework.Interfaces.Messaging;
global using Lyt.Framework.Interfaces.Modeling;
global using Lyt.Framework.Interfaces.Logging;
global using Lyt.Framework.Interfaces.Profiling;

global using Lyt.Avalonia.Mvvm;
global using Lyt.Avalonia.Mvvm.Logging;
global using Lyt.Avalonia.Mvvm.Utilities;
global using Lyt.Avalonia.Controls;
global using Lyt.Avalonia.Controls.BadgeControl;
global using Lyt.Persistence;
global using Lyt.Avalonia.Themes;

global using Lyt.Mvvm;
global using Lyt.Model;
global using Lyt.Orchestrator;
global using Lyt.Utilities;
global using Lyt.Utilities.Profiling;
global using Lyt.StateMachine;
global using Lyt.UserAdministration;

global using Lyt.Avalonia.TestApp; 
global using Lyt.Avalonia.TestApp.Shell;
global using Lyt.Avalonia.TestApp.Models;
global using Lyt.Avalonia.TestApp.Workflow;
global using Lyt.Avalonia.TestApp.Workflow.Login;
global using Lyt.Avalonia.TestApp.Workflow.Process;
global using Lyt.Avalonia.TestApp.Workflow.Select;
global using Lyt.Avalonia.TestApp.Workflow.Startup;
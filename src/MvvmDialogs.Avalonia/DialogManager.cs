﻿using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using Avalonia.Media;

namespace HanumanInstitute.MvvmDialogs.Avalonia;

/// <summary>
/// DialogManager for Avalonia.
/// </summary>
public class DialogManager : DialogManagerBase<ContentControl>
{
    private readonly NavigationManager _navigationManager;
    private readonly IDispatcher _dispatcher;
    private readonly bool _singlePageApp;

    /// <inheritdoc />
    public DialogManager(
        IViewLocator? viewLocator = null,
        IDialogFactory? dialogFactory = null,
        ILogger<DialogManager>? logger = null,
        IDispatcher? dispatcher = null,
        NavigationManager? navigationManager = null)
        :
        base(
            viewLocator ?? new ViewLocatorBase(),
            dialogFactory ?? new DialogFactory(),
            logger)
    {
        _dispatcher = dispatcher ?? Dispatcher.UIThread;
        _navigationManager = navigationManager ?? new NavigationManager();
        _singlePageApp = Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime;
    }

    /// <inheritdoc />
    protected override IView CreateWrapper(INotifyPropertyChanged viewModel, Type viewType)
    {
        var wrapper = _singlePageApp ?
            (IView)new ViewNavigationWrapper().SetNavigation(_navigationManager) :
            new ViewWrapper();
        wrapper.Initialize(viewModel, viewType);
        return wrapper;
    }

    private static IEnumerable<Window> Windows =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows ?? Array.Empty<Window>();

    /// <inheritdoc />
    public override IView? FindViewByViewModel(INotifyPropertyChanged viewModel)
    {
        if (_singlePageApp)
        {
            return _navigationManager.GetViewForViewModel(viewModel).AsWrapper(_navigationManager);
        }
        else
        {
            return Windows.FirstOrDefault(x => ReferenceEquals(viewModel, x.DataContext)).AsWrapper();
        }
    }

    /// <inheritdoc />
    public override IView? GetMainWindow()
    {
        if (_singlePageApp)
        {
            return null;
        }

        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow.AsWrapper();
    }

    /// <inheritdoc />
    public override IView? GetDummyWindow()
    {
        if (_singlePageApp)
        {
            return null;
        }

        var parent = new Window()
        {
            Height = 1,
            Width = 1,
            SystemDecorations = SystemDecorations.None,
            ShowInTaskbar = false,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = Brushes.Transparent
        };
        parent.Show();
        return parent.AsWrapper();
    }

    /// <inheritdoc />
    protected override void Dispatch(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Post(action, DispatcherPriority.Render);
        }
    }

    /// <inheritdoc />
    protected override Task<T> DispatchAsync<T>(Func<T> action) =>
        //_dispatcher.CheckAccess() ? Task.FromResult(action()) : _dispatcher.InvokeAsync(action, DispatcherPriority.Render);
        _dispatcher.CheckAccess() ? Task.FromResult(action()) : DispatchWithResult(action);

    /// <summary>
    /// Work-around for missing interface member in Avalonia v11-preview1.
    /// </summary>
    private Task<T> DispatchWithResult<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>();
        _ = _dispatcher.InvokeAsync(
            () =>
            {
                tcs.SetResult(action());
            },
            DispatcherPriority.Render);
        return tcs.Task;
    }
}

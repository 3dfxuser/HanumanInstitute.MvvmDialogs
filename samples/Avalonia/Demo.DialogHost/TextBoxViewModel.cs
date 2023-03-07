﻿using ReactiveUI.Fody.Helpers;

namespace Demo.Avalonia.DialogHost;

public class TextBoxViewModel : ViewModelBase
{
    [Reactive]
    public string Title { get; set; } = "Title";

    [Reactive]
    public string Text { get; set; } = string.Empty;
}

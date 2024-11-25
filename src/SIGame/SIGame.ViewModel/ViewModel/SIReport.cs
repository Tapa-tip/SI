﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SIGame.ViewModel;

/// <summary>
/// Defines a user provided report.
/// </summary>
public sealed class SIReport : INotifyPropertyChanged
{
    private string _report = "";

    public string Report
    {
        get => _report;
        set { _report = value; OnPropertyChanged(); }
    }

    private string _comment = "";

    public string Comment
    {
        get => _comment;
        set { _comment = value; OnPropertyChanged(); }
    }

    public string Title { get; set; }

    public string Subtitle { get; set; }

    public ICommand SendReport { get; set; }

    public ICommand SendNoReport { get; set; }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

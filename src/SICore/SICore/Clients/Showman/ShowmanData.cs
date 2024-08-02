﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore;

/// <summary>
/// Defines a showman data.
/// </summary>
public sealed class ShowmanData : INotifyPropertyChanged
{
    /// <summary>
    /// Послать сообщение об изменении суммы
    /// </summary>
    public ICommand ChangeSums2 { get; set; }

    private CustomCommand? _manageTable;

    /// <summary>
    /// Manage game table command.
    /// </summary>
    public CustomCommand? ManageTable
    {
        get => _manageTable;
        set
        {
            if (_manageTable != value)
            {
                _manageTable = value;
                OnPropertyChanged();
            }
        }
    }

    private Pair _selectedPlayer = null;

    /// <summary>
    /// Выбранный игрок
    /// </summary>
    public Pair SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (_selectedPlayer != value)
            {
                _selectedPlayer = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

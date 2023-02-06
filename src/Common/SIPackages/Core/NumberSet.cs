﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using SIPackages.TypeConverters;

namespace SIPackages.Core;

// TODO: sealed

/// <summary>
/// Defines a set of numbers.
/// </summary>
[TypeConverter(typeof(NumberSetTypeConverter))]
public class NumberSet : IEquatable<NumberSet>, INotifyPropertyChanged
{
    private int _minimum = 0;

    /// <summary>
    /// Minimum value.
    /// </summary>
    [DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set { if (_minimum != value) { _minimum = value; OnPropertyChanged(); } }
    }

    private int _maximum = 0;

    /// <summary>
    /// Maximum value.
    /// </summary>
    [DefaultValue(0)]
    public int Maximum
    {
        get => _maximum;
        set { if (_maximum != value) { _maximum = value; OnPropertyChanged(); } }
    }

    private int _step = 0;

    /// <summary>
    /// Step (a minimum distance between two possible nubmbers) value.
    /// </summary>
    [DefaultValue(0)]
    public int Step
    {
        get => _step;
        set { if (_step != value) { _step = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NumberSet" /> class.
    /// </summary>
    public NumberSet() { }

    /// <summary>
    /// Initializes a new instance of <see cref="NumberSet" /> class.
    /// </summary>
    /// <param name="value">Single value to include in the set.</param>
    public NumberSet(int value)
    {
        _minimum = value;
        _maximum = value;
    }

    /// <inheritdoc />
    public bool Equals(NumberSet? other) =>
        other is not null
        && Minimum.Equals(other.Minimum)
        && Maximum.Equals(other.Maximum)
        && Step.Equals(other.Step);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as NumberSet);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Minimum, Maximum, Step);

    /// <summary>
    /// Raises object property change event.
    /// </summary>
    /// <param name="name">Name of the property wchich value has been changed.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}

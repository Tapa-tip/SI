﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using Utils.Timers;

namespace Utils.Wpf;

/// <inheritdoc cref="IAnimatableTimer" />.
public sealed class AnimatableTimer : Animatable, IAnimatableTimer
{
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(nameof(Time), typeof(double), typeof(AnimatableTimer), new PropertyMetadata(0.0));

    public static readonly DependencyPropertyDescriptor TimeDescriptor = DependencyPropertyDescriptor.FromProperty(TimeProperty, typeof(AnimatableTimer));

    /// <summary>
    /// Current timer time, animated from 0.0 to 100.0.
    /// </summary>
    public double Time
    {
        get => (double)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    private bool _isUserPaused;

    private bool _isSystemPaused;

    /// <summary>
    /// Maximum running time, 0.1 s.
    /// </summary>
    public int MaxTime { get; set; }

    public bool KeepFinalValue { get; set; }

    /// <summary>
    /// Current timer state.
    /// </summary>
    public TimerState State { get; private set; } = TimerState.Stopped;

    /// <summary>
    /// Time changed event.
    /// </summary>
    public event Action<IAnimatableTimer>? TimeChanged;

    public AnimatableTimer() => TimeDescriptor.AddValueChanged(this, OnTimeChanged);

    private void OnTimeChanged(object? sender, EventArgs e) => TimeChanged?.Invoke(this);

    public void Run(int maxTime, bool byUser, double? fromValue = null)
    {
        if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
        {
            Dispatcher.Invoke(Run, maxTime, byUser, fromValue);
            return;
        }
        
        if (maxTime > -1)
        {
            MaxTime = maxTime;
        }

        if (byUser)
        {
            _isUserPaused = false;

            if (State != TimerState.Paused)
            {
                return;
            }
        }
        else
        {
            _isSystemPaused = false;
        }

        if (_isUserPaused || _isSystemPaused)
        {
            return;
        }

        var animationTime = State == TimerState.Paused ? MaxTime * (1.0 - (fromValue ?? Time) / 100) : MaxTime;

        if (animationTime < double.Epsilon)
        {
            return;
        }

        var duration = new Duration(TimeSpan.FromMilliseconds(animationTime * 100));

        State = TimerState.Running;

        var fillBehavior = KeepFinalValue ? FillBehavior.HoldEnd : FillBehavior.Stop;

        var animation = fromValue.HasValue
            ? new DoubleAnimation(fromValue.Value, 100.0, duration) { FillBehavior = fillBehavior }
            : new DoubleAnimation(100.0, duration) { FillBehavior = fillBehavior };

        BeginAnimation(TimeProperty, animation);
    }

    protected override Freezable CreateInstanceCore() => new AnimatableTimer();

    public void Stop()
    {
        if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
        {
            Dispatcher.Invoke(Stop);
            return;
        }

        State = TimerState.Stopped;
        
        BeginAnimation(TimeProperty, null);

        Time = 0.0;
    }

    /// <summary>
    /// Pauses timer and adjustes its current time.
    /// </summary>
    /// <param name="currentTime">Current time value, 0.1 s.</param>
    /// <param name="byUser">Has pause been caused by user (not by system).</param>
    public void Pause(int currentTime, bool byUser)
    {
        if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
        {
            Dispatcher.Invoke(Pause, currentTime, byUser);
            return;
        }

        if (byUser)
        {
            _isUserPaused = true;
        }
        else
        {
            _isSystemPaused = true;
        }
        
        if (State == TimerState.Running)
        {
            State = TimerState.Paused;

            if (MaxTime > 0)
            {
                var animation = new DoubleAnimation(
                    Math.Min(100.0, currentTime * 100 / MaxTime),
                    new Duration(TimeSpan.FromMilliseconds(300)))
                {
                    FillBehavior = FillBehavior.HoldEnd
                };

                BeginAnimation(TimeProperty, animation);
            }
        }
    }

    public void Dispose() => TimeDescriptor.RemoveValueChanged(this, OnTimeChanged);
}

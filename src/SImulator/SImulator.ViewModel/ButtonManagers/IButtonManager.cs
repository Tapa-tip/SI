﻿using SImulator.ViewModel.Contracts;

namespace SImulator.ViewModel.ButtonManagers;

/// <summary>
/// Supports players buttons.
/// </summary>
public interface IButtonManager : IAsyncDisposable
{
    /// <summary>
    /// Does button manager manage player connections.
    /// </summary>
    bool ArePlayersManaged();

    /// <summary>
    /// Handles player added event.
    /// </summary>
    void OnPlayersChanged();

    /// <summary>
    /// Removes player by identifier.
    /// </summary>
    /// <param name="id">Player identifier.</param>
    /// <param name="name">Player name.</param>
    void DisconnectPlayerById(string id, string name);

    /// <summary>
    /// Enables players buttons.
    /// </summary>
    /// <returns>Has the start been successfull.</returns>
    bool Start();

    /// <summary>
    /// Disables players buttons.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets command executor for current manager.
    /// </summary>
    ICommandExecutor? TryGetCommandExecutor();
}

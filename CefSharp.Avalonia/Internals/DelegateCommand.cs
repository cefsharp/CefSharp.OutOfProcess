// Copyright © 2019 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Windows.Input;

namespace CefSharp.Avalonia.Internals;

/// <summary>
/// DelegateCommand
/// </summary>
/// <seealso cref="System.Windows.Input.ICommand" />
internal class DelegateCommand : ICommand
{
    /// <summary>
    /// The command handler
    /// </summary>
    private readonly Action _commandHandler;
    /// <summary>
    /// The can execute handler
    /// </summary>
    private readonly Func<bool> _canExecuteHandler;

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
    /// </summary>
    /// <param name="commandHandler">The command handler.</param>
    /// <param name="canExecuteHandler">The can execute handler.</param>
    public DelegateCommand(Action commandHandler, Func<bool> canExecuteHandler = null)
    {
        _commandHandler = commandHandler;
        _canExecuteHandler = canExecuteHandler;
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
    public void Execute(object parameter)
    {
        _commandHandler();
    }

    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
    /// <returns>true if this command can be executed; otherwise, false.</returns>
    public bool CanExecute(object parameter)
    {
        return _canExecuteHandler == null || _canExecuteHandler();
    }

    /// <summary>
    /// Raises the can execute changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

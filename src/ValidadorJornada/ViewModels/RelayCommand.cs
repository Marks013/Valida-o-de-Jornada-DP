using System;
using System.Windows.Input;

namespace ValidadorJornada.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

    /// <summary>
    /// RelayCommand com suporte a parâmetro
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
                return true;
            
            if (parameter is T typedParam)
                return _canExecute(typedParam);
            
            if (parameter == null && default(T) == null)
                return _canExecute(default);
            
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParam)
            {
                _execute(typedParam);
            }
            else if (parameter == null && default(T) == null)
            {
                _execute(default);
            }
        }
    }
}

using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MPDCtrlX.Common
{
    /// <summary>
    /// GenericRelayCommand
    /// </summary>
    public class GenericRelayCommand<T> : IRelayCommand
    {
        private readonly Action<T> execute;

        public GenericRelayCommand(Action<T> execute) : this(execute, p => true)
        {
        }

        public GenericRelayCommand(Action<T> execute, Predicate<T> canExecuteFunc)
        {
            this.execute = execute;
            this.CanExecuteFunc = canExecuteFunc;
        }

        public event EventHandler? CanExecuteChanged; /* CanExecuteChanged
        {
            add
            {
                // TODO:
                //CommandManager.RequerySuggested += value;
            }

            remove
            {
                // TODO:
                //CommandManager.RequerySuggested -= value;
            }
        }
            */

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public Predicate<T> CanExecuteFunc { get; private set; }

        public bool CanExecute(object? parameter)
        {
            if (parameter is not null)
            {
                var canExecute = this.CanExecuteFunc((T)parameter);
                return canExecute;
            }
            else
            {
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            if (parameter is null)
            {
                return;
            }
            this.execute((T)parameter);
        }
    }

}

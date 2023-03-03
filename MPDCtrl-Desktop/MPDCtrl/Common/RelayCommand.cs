using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MPDCtrl.Common
{
    /// <summary>
    /// GenericRelayCommand
    /// </summary>
    public class GenericRelayCommand<T> : ICommand
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

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public Predicate<T> CanExecuteFunc { get; private set; }

        public bool CanExecute(object? parameter)
        {
            if (parameter != null)
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
            if (parameter == null)
            {
                return;
            }
            this.execute((T)parameter);
        }
    }

    /// <summary>
    /// RelayCommand
    /// </summary>
    public class RelayCommand : ICommand
    {
        private Action methodToExecute;
        private Func<bool> canExecuteEvaluator;

        public RelayCommand(Action methodToExecute, Func<bool> canExecuteEvaluator)
        {
            this.methodToExecute = methodToExecute;
            this.canExecuteEvaluator = canExecuteEvaluator;
        }

        public RelayCommand(Action methodToExecute)
            : this(methodToExecute, null)
        {
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (this.canExecuteEvaluator == null)
            {
                return true;
            }
            else
            {
                bool result = this.canExecuteEvaluator.Invoke();
                return result;
            }
        }

        public void Execute(object? parameter)
        {
            this.methodToExecute.Invoke();
        }
    }


}

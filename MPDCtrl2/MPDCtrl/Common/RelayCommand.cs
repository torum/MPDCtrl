using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        public event EventHandler CanExecuteChanged
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

        public bool CanExecute(object parameter)
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

        public void Execute(object parameter)
        {
            this.execute((T)parameter);
        }
    }

    /// <summary>
    /// RelayCommandWithParam
    /// </summary>
    public class RelayCommandWithParam<T> : ICommand
    {
        #region Fields

        private readonly Action<T> execute = null;
        private readonly Predicate<T> canExecute = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommandWithParam(Action<T> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command with conditional execution.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommandWithParam(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        #endregion

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }

            remove
            {
                if (this.canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null ? true : this.canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            this.execute((T)parameter);
        }

        #endregion
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

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
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

        public void Execute(object parameter)
        {
            this.methodToExecute.Invoke();
        }
    }


}

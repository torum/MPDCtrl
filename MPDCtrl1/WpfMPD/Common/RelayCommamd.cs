using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfMPD.Common
{
    // http://stackoverflow.com/questions/21821762/relaycommand-wont-execute-on-button-click
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

    // http://www.kellydun.com/wpf-relaycommand-with-parameter/
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

    //// http://sourcechord.hatenablog.com/entry/2014/01/13/200039

    //// http://docs.telerik.com/data-access/quick-start-scenarios/wpf/quickstart-wpf-viewmodelbase-and-relaycommand
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


    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public Predicate<object> CanExecuteDelegate { get; set; }

        public Action<object> ExecuteDelegate { get; set; }

        public bool CanExecute(object parameter)
        {
            if (this.CanExecuteDelegate != null)
            {
                return this.CanExecuteDelegate(parameter);
            }
            else
            {
                return true; // if there is no can execute default to true
            }
        }

        public void Execute(object parameter)
        {
            if (this.ExecuteDelegate != null)
            {
                this.ExecuteDelegate(parameter);
            }
        }
    }
}

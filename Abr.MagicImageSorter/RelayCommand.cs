using System;
using System.Windows.Input;

namespace MagicImageSorter
{
    /// <summary>
    /// RelayCommand
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Events

        /// <inheritdoc/>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion

        #region Fields

        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes new Instance of <see cref="RelayCommand"/>
        /// </summary>
        /// <param name="execute"><inheritdoc/></param>
        /// <param name="canExecute"><inheritdoc/></param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        /// <inheritdoc/>
        public void Execute(object parameter) => _execute(parameter);

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Countries.ViewModel
{
    public class RelayCommand : ICommand
    {
        private Action _execute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
            //return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute.Invoke();
        }
    }
}

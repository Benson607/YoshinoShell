using System.Windows;

namespace YoshinoShell
{
    internal class ShellHistory
    {
        public string command = "";

        public string result = "";

        public bool Looking = false;

        private Action ResultUpdate = () =>
        {
        };

        private object _lock = new object();

        public ShellHistory(Action update_func)
        {
            ResultUpdate = update_func;
        }

        public ShellHistory(Action update_func, string command, string result)
        {
            ResultUpdate = update_func;

            this.command = command;
            this.result = result;
        }

        public void AppendResult(string result)
        {
            lock (_lock)
            {
                this.result += result;
            }

            if (Looking)
            {
                Application.Current.Dispatcher.Invoke(ResultUpdate);
            }
        }
    }
}

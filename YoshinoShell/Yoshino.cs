using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Windows.Controls;

namespace YoshinoShell
{
    internal class Yoshino
    {
        private TextBox CurrentDir = new TextBox();

        public List<ShellHistory> Histories = new List<ShellHistory>();

        public int LookingIndex { get; private set; } = -1;

        private object _lock = new object();

        private Action ResultUpdate = () => { };

        private readonly Runspace _shared_runspace;

        public Yoshino(Action update_func)
        {
            ResultUpdate = update_func;

            _shared_runspace = RunspaceFactory.CreateRunspace();
            _shared_runspace.Open();
        }

        public Yoshino(Action update_func, TextBox current_dir_box)
        {
            ResultUpdate = update_func;

            _shared_runspace = RunspaceFactory.CreateRunspace();
            _shared_runspace.Open();

            CurrentDir = current_dir_box;
        }

        public string GetCurrentCommand()
        {
            lock (_lock)
            {
                return Histories[LookingIndex].command;
            }
        }

        public string GetCurrentResult()
        {
            lock (_lock)
            {
                return Histories[LookingIndex].result;
            }
        }

        public void HistoryForward()
        {
            if (LookingIndex == Histories.Count - 1)
            {
                return;
            }

            SwitchLooking(LookingIndex + 1);
        }

        public void HistoryBackward()
        {
            if (LookingIndex <= 0)
            {
                return;
            }

            SwitchLooking(LookingIndex - 1);
        }

        public void SwitchLooking(int index)
        {
            if (index >= Histories.Count)
            {
                return;
            }
            lock (_lock)
            {
                if (LookingIndex != -1)
                {
                    if (Histories.Count > LookingIndex)
                    {
                        Histories[LookingIndex].Looking = false;
                    }
                }

                Histories[index].Looking = true;
            }

            LookingIndex = index;
        }

        public async void Run(String command, bool Looking = true)
        {
            if (command == "")
            {
                return;
            }

            await ExecutePowerShellAsync(command, Looking);
        }

        private async Task ExecutePowerShellAsync(string command, bool Looking)
        {
            await Task.Run(() =>
            {
                using (var power_shell = PowerShell.Create())
                {
                    power_shell.Runspace = _shared_runspace;
                    power_shell.AddScript(command);

                    ShellHistory history = AddNewHistory(command);
                    
                    if (Looking)
                    {
                        SwitchLooking(Histories.Count - 1);
                    }

                    power_shell.Streams.Error.DataAdded += (s, e) =>
                    {
                        if (s is PSDataCollection<ErrorRecord> errors && e.Index < errors.Count)
                        {
                            var err = ((PSDataCollection<ErrorRecord>)s)[e.Index];
                            history.AppendResult($"[ERROR] {err}\n");
                        }
                    };

                    power_shell.Streams.Information.DataAdded += (s, e) =>
                    {
                        if (s is PSDataCollection<InformationRecord> infos && e.Index < infos.Count)
                        {
                            var info = ((PSDataCollection<InformationRecord>)s)[e.Index];
                            history.AppendResult($"[INFO] {info}\n");
                        }
                    };

                    try
                    {
                        Collection<PSObject> results = power_shell.Invoke();

                        StringBuilder string_builder = new();
                        foreach (var item in results)
                        {
                            string_builder.AppendLine(item.ToString());
                        }

                        history.AppendResult(string_builder.ToString());
                    }
                    catch (Exception ex)
                    {
                        history.AppendResult($"[EXCEPTION] {ex.Message}\n");
                    }

                    try
                    {
                        power_shell.Commands.Clear();
                        var result = power_shell.AddScript("Get-Location").Invoke();
                        string current_dir = result[0].ToString();

                        CurrentDir.Dispatcher.Invoke(() => CurrentDir.Text = current_dir);
                    }
                    catch
                    {
                        Debug.WriteLine("can not get current dir");
                    }
                }
            });
        }

        private ShellHistory AddNewHistory(string command)
        {
            ShellHistory history = new ShellHistory(ResultUpdate, command, "");

            lock (_lock)
            {
                Histories.Add(history);
            }

            return history;
        }

        public void Release()
        {
            try
            {
                if (_shared_runspace != null)
                {
                    if (_shared_runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                    {
                        _shared_runspace.Close();
                        _shared_runspace.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing runspace: {ex.Message}");
            }

            ResultUpdate = () => { };
            Histories.Clear();
        }
    } 
}

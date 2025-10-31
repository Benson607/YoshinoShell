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

        private PowerShell? CurrentPowershell;

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

            UpdatePWD();
        }

        public string GetCurrentCommand()
        {
            lock (_lock)
            {
                if (LookingIndex < 0 || LookingIndex >= Histories.Count)
                {
                    return "";
                }

                return Histories[LookingIndex].command;
            }
        }

        public string GetCurrentResult()
        {
            lock (_lock)
            {
                if (LookingIndex < 0 || LookingIndex >= Histories.Count)
                {
                    return "";
                }

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

        public void Execute(String command)
        {
            try
            {
                using (var power_shell = PowerShell.Create())
                {
                    CurrentPowershell = power_shell;
                    power_shell.Runspace = _shared_runspace;
                    var result = power_shell.AddScript(command).Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.Write($"[EXCEPTION] {e.Message}\n");
            }
            finally
            {
                CurrentPowershell = null;
            }

            UpdatePWD();
        }

        public void Interrupt()
        {
            if (CurrentPowershell != null && CurrentPowershell.InvocationStateInfo.State == PSInvocationState.Running)
            {
                try
                {
                    CurrentPowershell.Stop();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("error when interrupt: " + e.Message);
                }
            }
        }

        private async Task ExecutePowerShellAsync(string command, bool Looking)
        {
            await Task.Run(() =>
            {
                using (var power_shell = PowerShell.Create())
                {
                    CurrentPowershell = power_shell;
                    power_shell.Runspace = _shared_runspace;
                    power_shell.AddScript($"{command} 2>&1");

                    ShellHistory history = AddNewHistory(command);
                    
                    if (Looking)
                    {
                        SwitchLooking(Histories.Count - 1);
                    }

                    var output = new PSDataCollection<PSObject>();
                    output.DataAdded += (s, e) =>
                    {
                        if (s is PSDataCollection<PSObject> outputs && e.Index < outputs.Count)
                        {
                            var text = outputs[e.Index]?.ToString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                history.AppendResult(text + "\n");
                            }
                        }
                    };

                    power_shell.Streams.Error.DataAdded += (s, e) =>
                    {
                        if (s is PSDataCollection<ErrorRecord> errors && e.Index < errors.Count)
                        {
                            var err = errors[e.Index];
                            history.AppendResult($"[ERROR] {err}\n");
                        }
                    };

                    try
                    {
                        var asyncResult = power_shell.BeginInvoke<PSObject, PSObject>(null, output);
                        power_shell.EndInvoke(asyncResult);
                    }
                    catch (Exception e)
                    {
                        history.AppendResult($"[EXCEPTION] {e.Message}\n");
                    }
                    finally
                    {
                        CurrentPowershell = null;
                    }

                    UpdatePWD();
                }
            });
        }

        private void UpdatePWD()
        {
            try
            {
                using (var power_shell = PowerShell.Create())
                {
                    CurrentPowershell = power_shell;
                    power_shell.Runspace = _shared_runspace;
                    var result = power_shell.AddScript("Get-Location").Invoke();
                    string current_pwd = result[0].ToString();

                    CurrentDir.Dispatcher.Invoke(() => CurrentDir.Text = current_pwd);
                }
            }
            catch (Exception e)
            {
                Debug.Write($"[EXCEPTION] {e.Message}\n");
            }
            finally
            {
                CurrentPowershell = null;
            }
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

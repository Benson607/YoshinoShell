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
        public TextBox CommandLineTextBox = new TextBox();
        public TextBox PWDTextBox = new TextBox();
        public TextBox UIBox = new TextBox();

        public ListBox HistoryBox = new ListBox();

        public List<ShellHistory> Histories = new List<ShellHistory>();

        private bool executing = false;

        public int LookingIndex { get; private set; } = -1;

        private object _lock = new object();

        private readonly Runspace _shared_runspace;

        private PowerShell? CurrentPowershell;

        public Yoshino(TextBox command_line_text_box, TextBox ui_box, TextBox pwd_text_box, ListBox history_box)
        {
            CommandLineTextBox = command_line_text_box;
            UIBox = ui_box;
            PWDTextBox = pwd_text_box;
            HistoryBox = history_box;

            var ui = new YoshinoUI
            (
                s => UIBox.Dispatcher.Invoke(() => AppendUIText("[UI data] " + s + "\n")),
                    () =>
                    {
                        return WaitInput();
                    }
            );

            var host = new YoshinoHost(ui);
            _shared_runspace = RunspaceFactory.CreateRunspace(host);
            _shared_runspace.Open();

            using (var power_shell = PowerShell.Create())
            {
                power_shell.Runspace = _shared_runspace;
                power_shell.AddScript(@"
                    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
                    if (Test-Path $PROFILE.CurrentUserAllHosts) { . $PROFILE.CurrentUserAllHosts }
                ").Invoke();

                if (power_shell.HadErrors)
                {
                    foreach (var err in power_shell.Streams.Error)
                    {
                        Debug.WriteLine("[PROFILE INIT ERROR] " + err.ToString());
                    }
                }
            }

            UpdatePWDText();
        }

        public string WaitInput()
        {
            return "";
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

            UpdateCommandLineText();
            UpdateUIText();
        }

        public void HistoryBackward()
        {
            if (LookingIndex <= 0)
            {
                return;
            }

            SwitchLooking(LookingIndex - 1);

            UpdateCommandLineText();
            UpdateUIText();
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
        public void UpdateCommandLineText()
        {
            CommandLineTextBox.Text = GetCurrentCommand();
        }

        public void UpdatePWDText()
        {
            try
            {
                using (var power_shell = PowerShell.Create())
                {
                    CurrentPowershell = power_shell;
                    power_shell.Runspace = _shared_runspace;
                    var result = power_shell.AddScript("Get-Location").Invoke();
                    string current_pwd = result[0].ToString();

                    PWDTextBox.Dispatcher.Invoke(() => PWDTextBox.Text = current_pwd);
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

        public void UpdateUIText()
        {
            UIBox.Text = GetCurrentResult();
        }

        public void SetUIText(string text)
        {
            UIBox.Text = text;
        }

        public void AppendUIText(string text)
        {
            UIBox.AppendText(text);
        }

        public void ClearUIText()
        {
            UIBox.Text = "";
        }

        public async void Enter(String command, bool Looking = true)
        {
            if (command == "")
            {
                return;
            }

            if (executing)
            {

            }
            else
            {
                await ExecutePowerShellAsync(command, Looking);
            }
        }

        public void Execute(String command)
        {
            executing = true;

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

            UpdatePWDText();

            executing = false;
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
            executing = true;

            await Task.Run(() =>
            {
                using (var power_shell = PowerShell.Create())
                {
                    CurrentPowershell = power_shell;
                    power_shell.Runspace = _shared_runspace;
                    power_shell.AddScript($"{command}");

                    ShellHistory history = AddNewHistory(command);

                    if (Looking)
                    {
                        SwitchLooking(Histories.Count - 1);
                    }

                    var output = BindPowerShellDataAddedEvent(power_shell, history);

                    BindPowerShellStreamEvent(power_shell, history);

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

                    UpdatePWDText();
                }
            });

            executing = false;
        }

        private PSDataCollection<PSObject> BindPowerShellDataAddedEvent(PowerShell power_shell, ShellHistory history)
        {
            var output = new PSDataCollection<PSObject>();
            output.DataAdded += (s, e) =>
            {
                if (s is PSDataCollection<PSObject> outputs && e.Index < outputs.Count)
                {
                    var text = outputs[e.Index]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        history.AppendResult("[DataAdded] " + text + "\n");
                    }
                }
            };

            return output;
        }

        private void BindPowerShellStreamEvent(PowerShell power_shell, ShellHistory history)
        {
            power_shell.Streams.Error.DataAdded += (s, e) =>
            {
                if (s is PSDataCollection<ErrorRecord> errors && e.Index < errors.Count)
                {
                    var err = errors[e.Index];
                    history.AppendResult($"[ERROR] {err}\n");
                }
            };

            power_shell.Streams.Warning.DataAdded += (s, e) =>
            {
                if (s is PSDataCollection<WarningRecord> warnings && e.Index < warnings.Count)
                {
                    var warn = warnings[e.Index];
                    history.AppendResult($"[WARNING] {warn.Message}\n");
                }
            };

            power_shell.Streams.Verbose.DataAdded += (s, e) =>
            {
                if (s is PSDataCollection<VerboseRecord> verbose && e.Index < verbose.Count)
                {
                    var msg = verbose[e.Index];
                    history.AppendResult($"[VERBOSE] {msg.Message}\n");
                }
            };

            power_shell.Streams.Debug.DataAdded += (s, e) =>
            {
                if (s is PSDataCollection<DebugRecord> debug && e.Index < debug.Count)
                {
                    var msg = debug[e.Index];
                    history.AppendResult($"[DEBUG] {msg.Message}\n");
                }
            };

            power_shell.Streams.Information.DataAdded += (s, e) =>
            {
                if (s is PSDataCollection<InformationRecord> infos && e.Index < infos.Count)
                {
                    var msg = infos[e.Index];
                    history.AppendResult($"[INFO] {msg.MessageData}\n");
                }
            };
        }

        private ShellHistory AddNewHistory(string command)
        {
            ShellHistory history = new ShellHistory(UpdateUIText, command, "");

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

            Histories.Clear();
        }
    }
}

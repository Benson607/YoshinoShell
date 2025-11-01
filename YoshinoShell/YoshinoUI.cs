using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;

namespace YoshinoShell
{
    internal class YoshinoUI : PSHostUserInterface
    {
        private readonly Action<string> _outputHandler;
        private readonly Func<string> _inputProvider;

        public YoshinoUI(Action<string> outputHandler, Func<string> inputProvider)
        {
            _outputHandler = outputHandler;
            _inputProvider = inputProvider;
        }

        // normal output
        //public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) => _outputHandler(value);
        //public override void Write(string value) => _outputHandler(value);
        //public override void WriteLine(string value) => _outputHandler(value + "\n");
        //public override void WriteErrorLine(string value) => _outputHandler("[ERROR] " + value + "\n");
        //public override void WriteDebugLine(string message) => _outputHandler("[DEBUG] " + message + "\n");
        //public override void WriteVerboseLine(string message) => _outputHandler("[VERBOSE] " + message + "\n");
        //public override void WriteWarningLine(string message) => _outputHandler("[WARN] " + message + "\n");
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) { }
        public override void Write(string value) { }
        public override void WriteLine(string value) { }
        public override void WriteErrorLine(string value) { }
        public override void WriteDebugLine(string message) { }
        public override void WriteVerboseLine(string message) { }
        public override void WriteWarningLine(string message) { }

        // input
        public override string ReadLine()
        {
            try { return _inputProvider.Invoke(); }
            catch { return ""; }
        }

        public override SecureString ReadLineAsSecureString()
        {
            var input = ReadLine();
            var secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        // Prompt 系列
        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            var result = new Dictionary<string, PSObject>();
            foreach (var d in descriptions)
            {
                _outputHandler($"{caption}: {d.Name} => ");
                result[d.Name] = new PSObject(_inputProvider.Invoke());
            }
            return result;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            _outputHandler($"{caption}\n{message}\n");
            for (int i = 0; i < choices.Count; i++)
            {
                _outputHandler($"[{i}] {choices[i].Label}\n");
            }
            _outputHandler($"Enter choice (default {defaultChoice}): ");
            string input = _inputProvider.Invoke();
            return int.TryParse(input, out int idx) ? idx : defaultChoice;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            _outputHandler($"{caption}\n{message}\nUsername: ");
            string user = _inputProvider.Invoke();
            _outputHandler("Password: ");
            string pass = _inputProvider.Invoke();
            var secure = new SecureString();
            foreach (char c in pass)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return new PSCredential(user, secure);
        }

        public override PSCredential PromptForCredential(
            string caption, string message, string userName, string targetName,
            PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
            => PromptForCredential(caption, message, userName, targetName);

        public override void WriteProgress(long sourceId, ProgressRecord record)
            => _outputHandler($"[PROGRESS] {record.PercentComplete}% - {record.StatusDescription}\n");

        public override PSHostRawUserInterface RawUI { get; } = new YoshinoRawUI();
    }
}

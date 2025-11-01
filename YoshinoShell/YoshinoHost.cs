using System;
using System.Globalization;
using System.Management.Automation.Host;

namespace YoshinoShell
{
    internal class YoshinoHost : PSHost
    {
        private readonly Guid _instanceId = Guid.NewGuid();
        private readonly YoshinoUI _ui;

        public YoshinoHost(YoshinoUI ui) => _ui = ui;

        public override Guid InstanceId => _instanceId;
        public override string Name => "YoshinoHost";
        public override Version Version => new Version(1, 0);
        public override PSHostUserInterface UI => _ui;
        public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
        public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

        public override void EnterNestedPrompt() { }
        public override void ExitNestedPrompt() { }
        public override void NotifyBeginApplication() { }
        public override void NotifyEndApplication() { }
        public override void SetShouldExit(int exitCode) { }
    }
}

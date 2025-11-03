using System.Configuration;
using System.Data;
using System.Windows;

namespace YoshinoShell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] args = { };
        protected override void OnStartup(StartupEventArgs e)
        {
            args = e.Args;
            base.OnStartup(e);
        }
    }

}

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YoshinoShell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Yoshino yoshino;
        private SoundLoader sound_loader;
        private MediaPlayer BGMPlayer;

        public MainWindow()
        {
            InitializeComponent();

            yoshino = new Yoshino(CommandLine, ResultBox, CurrentPWD);

            ReadArgs();

            this.KeyDown += MainWindowKeyDown;
            this.Focus();
            
            sound_loader = new SoundLoader();

            //sound_loader.Load("ciallo", "Sounds/ciallo.wav");
            //sound_loader.Play("ciallo");

            BGMPlayer = new MediaPlayer();
            //BGMPlayer.Open(new Uri("Sounds/koihikoifuen.wav", UriKind.RelativeOrAbsolute));
            //BGMPlayer.Volume = 0.2;
            //BGMPlayer.Play();
        }

        private void ReadArgs()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-p")
                {
                    if (i + 1 < args.Length)
                    {
                        yoshino.Execute($"Set-Location {args[++i]}");
                    }
                }
            }
        }

        private void CommandLineBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                yoshino.HistoryBackward();
                CommandLine.CaretIndex = CommandLine.Text.Length;
            }
            else if (e.Key == Key.Down)
            {
                yoshino.HistoryForward();
                CommandLine.CaretIndex = CommandLine.Text.Length;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                yoshino.Interrupt();
            }
        }

        private void MainWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EnterButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else if (e.Key == Key.Tab)
            {
                if (yoshino.Histories.Count > 0)
                {
                    ResultBox.Text = yoshino.Histories[yoshino.Histories.Count - 1].result;
                }
            }
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandLine.Text;
            
            if (command == "exit")
            {
                Shutdown();
                return;
            }

            CommandLine.Text = "";
            ResultBox.Text = "";
            yoshino.Run(command);
        }

        private void Shutdown()
        {
            yoshino.Release();
            Application.Current.Shutdown();
        }

        private void CloseWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Shutdown();
        }
    }
}

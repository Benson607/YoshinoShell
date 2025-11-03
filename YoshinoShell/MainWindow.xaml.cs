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

            yoshino = new Yoshino(CommandLine, ResultBox, CurrentPWD, HistoryBox);

            ReadArgs();

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
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i] == "-p")
                {
                    if (i + 1 < App.args.Length)
                    {
                        yoshino.Execute($"Set-Location \"{App.args[++i]}\"");
                    }
                }
            }
        }

        private void CommandLineBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                yoshino.HistoryBackward();
                CommandLine.CaretIndex = CommandLine.Text.Length;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                yoshino.HistoryForward();
                CommandLine.CaretIndex = CommandLine.Text.Length;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                yoshino.Interrupt();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                e.Handled = true;

                int origin_caret = CommandLine.CaretIndex;

                CommandLine.Text = CommandLine.Text.Insert(origin_caret, Environment.NewLine);
                CommandLine.CaretIndex = origin_caret + Environment.NewLine.Length;
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;

                EnterCommand();
            }
        }

        private void CommandLineBoxTextChanged(object sender, EventArgs e)
        {
            CommandLine.UpdateLayout();
            CommandLine.Height = Math.Min(Math.Max(CommandLine.ExtentHeight + 6, CommandLine.MinHeight), 250);
        }

        private void EnterCommand()
        {
            string command = CommandLine.Text;
            
            if (command == "exit")
            {
                Shutdown();
                return;
            }

            CommandLine.Text = "";
            ResultBox.Text = "";
            yoshino.Enter(command);
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

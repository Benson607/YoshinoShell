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

            yoshino = new Yoshino(ResultUpdate, CurrentPWD);

            this.KeyDown += MainWindowKeyDown;
            this.Focus();
            
            yoshino.Run("pwd");

            sound_loader = new SoundLoader();

            sound_loader.Load("ciallo", "Sounds/ciallo.wav");
            sound_loader.Load("koihikoifuen", "Sounds/koihikoifuen.wav");

            sound_loader.Play("ciallo");

            BGMPlayer = new MediaPlayer();
            BGMPlayer.Open(new Uri("Sounds/koihikoifuen.wav", UriKind.RelativeOrAbsolute));
            BGMPlayer.Volume = 0.2;
            BGMPlayer.Play();
        }

        private void CommandLineBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                yoshino.HistoryBackward();
                CommandLineUpdate();
                ResultUpdate();
            }
            else if (e.Key == Key.Down)
            {
                yoshino.HistoryForward();
                CommandLineUpdate();
                ResultUpdate();
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
            else if (e.Key == Key.Up)
            {
                Debug.WriteLine("up");
            }
            else if (e.Key == Key.Down)
            {
                Debug.WriteLine("down");
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
            yoshino.Run(command);
        }

        private void CommandLineUpdate()
        {
            CommandLine.Text = yoshino.GetCurrentCommand();
        }

        private void ResultUpdate()
        {
            ResultBox.Text = yoshino.GetCurrentResult();
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
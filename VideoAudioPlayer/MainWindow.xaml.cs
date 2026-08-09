using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                    lblStatus.Content = String.Format("{0} / {1}", mediaElement.Position.ToString(@"mm\:ss"), mediaElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
            }
            else
                lblStatus.Content = "No file selected...";
        }



        private void openBtnMediaFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            // Set the filter to show only video files
            openFileDialog.Filter = "Video files (*.mp4;*.mkv;*.avi;*mp3)|*.mp4;*.mkv;*.avi;*jpg|All files (*.*)|*.*";

            // Show the dialog and get the result
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                mediaElement.Source = new Uri(fileName);


            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Pause();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
            mediaElement.Position = TimeSpan.Zero;
        }





    }
}

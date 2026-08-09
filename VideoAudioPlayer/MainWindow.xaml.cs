using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using MediaPlayer.AudioAnalysis;

namespace MediaPlayer
{
    public partial class MainWindow : Window
    {
        private enum PlaybackState
        {
            Idle,
            Loaded,
            Playing,
            Paused,
            Stopped,
            Failed
        }

        private readonly DispatcherTimer _progressTimer;
        private readonly DispatcherTimer _visualizerTimer;
        private readonly AudioSpectrumAnalyzer _spectrumAnalyzer = new();
        private readonly List<Uri> _playlist = new();
        private PlaybackState _playbackState = PlaybackState.Idle;
        private int _currentIndex = -1;
        private bool _isSeeking;
        private bool _isSynchronizingPlaylistSelection;
        private bool _isCurrentItemAudio;
        private int _spectrumRequestInFlight;
        private int _analysisGeneration;

        public MainWindow()
        {
            InitializeComponent();

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _progressTimer.Tick += timer_Tick;
            _visualizerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _visualizerTimer.Tick += visualizerTimer_Tick;

            mediaElement.MediaOpened += mediaElement_MediaOpened;
            mediaElement.MediaEnded += mediaElement_MediaEnded;
            mediaElement.MediaFailed += mediaElement_MediaFailed;
            mediaElement.Volume = sliderVolume.Value;
            Closed += MainWindow_Closed;

            UpdateControls();
        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            if (_playbackState == PlaybackState.Playing)
            {
                UpdateProgressStatus();
            }
        }

        private void visualizerTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isCurrentItemAudio || _playbackState != PlaybackState.Playing || !_spectrumAnalyzer.IsLoaded || Interlocked.CompareExchange(ref _spectrumRequestInFlight, 1, 0) != 0)
            {
                return;
            }

            TimeSpan position = mediaElement.Position;
            int generation = _analysisGeneration;
            _ = Task.Run(() => _spectrumAnalyzer.AnalyzeAt(position, 32)).ContinueWith(task => Dispatcher.BeginInvoke(new Action(() =>
            {
                Interlocked.Exchange(ref _spectrumRequestInFlight, 0);
                if (generation == _analysisGeneration && task.Status == TaskStatus.RanToCompletion && task.Result is not null)
                {
                    audioVisualPlaceholder.UpdateSpectrum(task.Result);
                }
            })));
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _visualizerTimer.Stop();
            _spectrumAnalyzer.Dispose();
        }

        private void openBtnMediaFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Media files (*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.mp3;*.wav;*.wma;*.m4a;*.aac)|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.mp3;*.wav;*.wma;*.m4a;*.aac|Video files (*.mp4;*.mkv;*.avi;*.mov;*.wmv)|*.mp4;*.mkv;*.avi;*.mov;*.wmv|Audio files (*.mp3;*.wav;*.wma;*.m4a;*.aac)|*.mp3;*.wav;*.wma;*.m4a;*.aac|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            _playlist.Clear();
            foreach (string fileName in openFileDialog.FileNames)
            {
                _playlist.Add(new Uri(fileName));
            }

            _currentIndex = 0;
            RefreshPlaylistDisplay();
            LoadCurrentItem();
        }

        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            _playbackState = PlaybackState.Loaded;
            ConfigureProgressSlider();
            UpdateTimeDisplay();
            emptyMediaMessage.Visibility = Visibility.Collapsed;
            if (_isCurrentItemAudio)
            {
                audioVisualPlaceholder.SetTrack(CurrentItemTitle, "Ready to play");
            }
            lblStatus.Content = $"Loaded - {FormatPositionAndDuration()}";
            UpdateControls();
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            StopProgressTimer();
            _playbackState = PlaybackState.Stopped;
            UpdateTimeDisplay();
            StopAudioVisual("Playback finished");
            audioVisualPlaceholder.ResetSpectrum();
            _analysisGeneration++;
            lblStatus.Content = "Playback finished";
            UpdateControls();
        }

        private void mediaElement_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            StopProgressTimer();
            _playbackState = PlaybackState.Failed;
            StopAudioVisual("Playback unavailable");
            audioVisualPlaceholder.ResetSpectrum();
            _analysisGeneration++;
            if (_isCurrentItemAudio)
            {
                audioVisualPlaceholder.ResetSpectrum();
                audioVisualPlaceholder.SetTrack(CurrentItemTitle, "Playback unavailable");
                audioVisualPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                emptyMediaMessage.Visibility = Visibility.Visible;
            }
            lblStatus.Content = $"Unable to play this file: {e.ErrorException.Message}";
            UpdateControls();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!CanPlay())
            {
                return;
            }

            mediaElement.Play();
            _playbackState = PlaybackState.Playing;
            if (_isCurrentItemAudio)
            {
                audioVisualPlaceholder.SetTrack(CurrentItemTitle, "Playing");
                audioVisualPlaceholder.StartAmbientAnimation();
                StartVisualizerTimer();
            }
            StartProgressTimer();
            UpdateProgressStatus();
            UpdateControls();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackState != PlaybackState.Playing)
            {
                return;
            }

            mediaElement.Pause();
            StopProgressTimer();
            _playbackState = PlaybackState.Paused;
            StopAudioVisual("Paused");
            lblStatus.Content = $"Paused - {FormatPositionAndDuration()}";
            UpdateControls();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackState is not (PlaybackState.Playing or PlaybackState.Paused))
            {
                return;
            }

            mediaElement.Stop();
            StopProgressTimer();
            _playbackState = PlaybackState.Stopped;
            UpdateTimeDisplay();
            StopAudioVisual("Stopped");
            audioVisualPlaceholder.ResetSpectrum();
            _analysisGeneration++;
            lblStatus.Content = $"Stopped - {FormatPositionAndDuration()}";
            UpdateControls();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (!HasLoadedMedia())
            {
                return;
            }

            mediaElement.Stop();
            mediaElement.Position = TimeSpan.Zero;
            StopProgressTimer();
            _playbackState = PlaybackState.Stopped;
            UpdateTimeDisplay();
            StopAudioVisual("Ready to restart");
            audioVisualPlaceholder.ResetSpectrum();
            _analysisGeneration++;
            lblStatus.Content = $"Reset - {FormatPositionAndDuration()}";
            UpdateControls();
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex <= 0)
            {
                return;
            }

            _currentIndex--;
            LoadCurrentItem();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _playlist.Count - 1)
            {
                return;
            }

            _currentIndex++;
            LoadCurrentItem();
        }

        private void playlistList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isSynchronizingPlaylistSelection || playlistList.SelectedIndex < 0 || playlistList.SelectedIndex == _currentIndex)
            {
                return;
            }

            _currentIndex = playlistList.SelectedIndex;
            LoadCurrentItem();
        }

        private void sliderProgress_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSeeking = HasLoadedMedia();
        }

        private void sliderProgress_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isSeeking)
            {
                return;
            }

            mediaElement.Position = TimeSpan.FromSeconds(sliderProgress.Value);
            _isSeeking = false;
            UpdateTimeDisplay();
        }

        private void sliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSeeking)
            {
                lblElapsedTime.Content = TimeSpan.FromSeconds(e.NewValue).ToString(@"mm\:ss");
            }
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement.Volume = e.NewValue;
        }

        private void LoadCurrentItem()
        {
            StopProgressTimer();
            _playbackState = PlaybackState.Idle;
            audioVisualPlaceholder.StopAmbientAnimation();
            audioVisualPlaceholder.ResetSpectrum();
            _visualizerTimer.Stop();
            _analysisGeneration++;
            _spectrumAnalyzer.Dispose();
            mediaElement.Stop();
            mediaElement.Source = _playlist[_currentIndex];
            _isCurrentItemAudio = IsAudioFile(_playlist[_currentIndex]);
            if (_isCurrentItemAudio)
            {
                try
                {
                    _spectrumAnalyzer.Load(_playlist[_currentIndex].LocalPath);
                }
                catch
                {
                    // Playback remains owned by MediaElement when this optional analysis reader cannot open the file.
                }
            }
            lblCurrentItem.Content = $"{_currentIndex + 1} of {_playlist.Count}: {CurrentItemTitle}";
            lblStatus.Content = "Loading media...";
            ConfigureMediaViewport();
            sliderProgress.IsEnabled = false;
            sliderProgress.Value = 0;
            sliderProgress.Maximum = 1;
            lblElapsedTime.Content = "00:00";
            lblDuration.Content = "--:--";
            SynchronizePlaylistSelection();
            UpdateControls();
        }

        private string CurrentItemTitle => _currentIndex >= 0 && _currentIndex < _playlist.Count
            ? Path.GetFileName(_playlist[_currentIndex].LocalPath)
            : "Audio ready";

        private static bool IsAudioFile(Uri item)
        {
            string extension = Path.GetExtension(item.LocalPath);
            return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".wma", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".aac", StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigureMediaViewport()
        {
            StopAudioVisual("Ready to play");
            if (_isCurrentItemAudio)
            {
                mediaElement.Visibility = Visibility.Collapsed;
                audioVisualPlaceholder.Visibility = Visibility.Visible;
                audioVisualPlaceholder.SetTrack(CurrentItemTitle, "Loading audio...");
                emptyMediaMessage.Visibility = Visibility.Collapsed;
                return;
            }

            audioVisualPlaceholder.Visibility = Visibility.Collapsed;
            mediaElement.Visibility = Visibility.Visible;
            emptyMediaMessage.Visibility = Visibility.Visible;
        }

        private void StopAudioVisual(string state)
        {
            if (_isCurrentItemAudio)
            {
                audioVisualPlaceholder.StopAmbientAnimation();
                _visualizerTimer.Stop();
                audioVisualPlaceholder.SetTrack(CurrentItemTitle, state);
            }
        }

        private void StartVisualizerTimer()
        {
            if (!_visualizerTimer.IsEnabled && _spectrumAnalyzer.IsLoaded)
            {
                _visualizerTimer.Start();
            }
        }

        private void UpdateProgressStatus()
        {
            UpdateTimeDisplay();
            lblStatus.Content = $"Playing - {FormatPositionAndDuration()}";
        }

        private void ConfigureProgressSlider()
        {
            if (!mediaElement.NaturalDuration.HasTimeSpan)
            {
                sliderProgress.IsEnabled = false;
                lblDuration.Content = "--:--";
                return;
            }

            sliderProgress.Maximum = Math.Max(1, mediaElement.NaturalDuration.TimeSpan.TotalSeconds);
            sliderProgress.IsEnabled = true;
            lblDuration.Content = mediaElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
        }

        private void UpdateTimeDisplay()
        {
            lblElapsedTime.Content = mediaElement.Position.ToString(@"mm\:ss");
            if (!_isSeeking && sliderProgress.IsEnabled)
            {
                sliderProgress.Value = Math.Min(sliderProgress.Maximum, mediaElement.Position.TotalSeconds);
            }
        }

        private void RefreshPlaylistDisplay()
        {
            _isSynchronizingPlaylistSelection = true;
            playlistList.Items.Clear();
            foreach (Uri item in _playlist)
            {
                playlistList.Items.Add(Path.GetFileName(item.LocalPath));
            }

            _isSynchronizingPlaylistSelection = false;
        }

        private void SynchronizePlaylistSelection()
        {
            _isSynchronizingPlaylistSelection = true;
            playlistList.SelectedIndex = _currentIndex;
            playlistList.ScrollIntoView(playlistList.SelectedItem);
            _isSynchronizingPlaylistSelection = false;
        }

        private string FormatPositionAndDuration()
        {
            if (!mediaElement.NaturalDuration.HasTimeSpan)
            {
                return mediaElement.Position.ToString(@"mm\:ss");
            }

            return $"{mediaElement.Position:mm\\:ss} / {mediaElement.NaturalDuration.TimeSpan:mm\\:ss}";
        }

        private bool HasLoadedMedia() => _playbackState is PlaybackState.Loaded or PlaybackState.Playing or PlaybackState.Paused or PlaybackState.Stopped;

        private bool CanPlay() => _playbackState is PlaybackState.Loaded or PlaybackState.Paused or PlaybackState.Stopped;

        private void StartProgressTimer()
        {
            if (!_progressTimer.IsEnabled)
            {
                _progressTimer.Start();
            }
        }

        private void StopProgressTimer()
        {
            if (_progressTimer.IsEnabled)
            {
                _progressTimer.Stop();
            }
        }

        private void UpdateControls()
        {
            btnPlay.IsEnabled = CanPlay();
            btnPause.IsEnabled = _playbackState == PlaybackState.Playing;
            btnStop.IsEnabled = _playbackState is PlaybackState.Playing or PlaybackState.Paused;
            btnReset.IsEnabled = HasLoadedMedia();
            btnPrevious.IsEnabled = _currentIndex > 0;
            btnNext.IsEnabled = _currentIndex >= 0 && _currentIndex < _playlist.Count - 1;
        }
    }
}

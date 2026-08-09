using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Windows.Media;
using MediaPlayer.AudioAnalysis;

namespace MediaPlayer.Controls
{
    public partial class AudioVisualPlaceholder : UserControl
    {
        private bool _isAnimating;
        private readonly List<Border> _bars = new();
        private readonly float[] _displayBands = new float[32];

        public AudioVisualPlaceholder()
        {
            InitializeComponent();
            for (int index = 0; index < _displayBands.Length; index++)
            {
                var accent = index % 7 == 0 ? (SolidColorBrush)FindResource("VioletBrush") : (SolidColorBrush)FindResource("CyanBrush");
                var bar = new Border
                {
                    Background = new LinearGradientBrush(
                        Color.FromArgb(105, accent.Color.R, accent.Color.G, accent.Color.B),
                        accent.Color,
                        new Point(0.5, 0),
                        new Point(0.5, 1)),
                    CornerRadius = new CornerRadius(3, 3, 1, 1),
                    Height = 4,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(3, 0, 3, 0),
                    Opacity = 0.88,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = accent.Color,
                        BlurRadius = 7,
                        ShadowDepth = 0,
                        Opacity = 0.22
                    }
                };
                _bars.Add(bar);
                spectrumBars.Children.Add(bar);
            }
        }

        public void SetTrack(string title, string state)
        {
            trackTitle.Text = title;
            stateLabel.Text = state;
        }

        public void StartAmbientAnimation()
        {
            if (_isAnimating)
            {
                return;
            }

            ((Storyboard)FindResource("AmbientStoryboard")).Begin(this, true);
            _isAnimating = true;
            reactiveCanvas.SetActive(true);
        }

        public void StopAmbientAnimation()
        {
            if (!_isAnimating)
            {
                return;
            }

            ((Storyboard)FindResource("AmbientStoryboard")).Stop(this);
            _isAnimating = false;
            reactiveCanvas.SetActive(false);
        }

        public void UpdateSpectrum(float[] bands)
        {
            for (int index = 0; index < _bars.Count && index < bands.Length; index++)
            {
                float target = bands[index];
                _displayBands[index] = target > _displayBands[index]
                    ? _displayBands[index] + (target - _displayBands[index]) * 0.65f
                    : _displayBands[index] + (target - _displayBands[index]) * 0.18f;
                _bars[index].Height = 4 + _displayBands[index] * 140;
            }
        }

        internal void UpdateAudioFrame(AudioAnalysisFrame frame)
        {
            UpdateSpectrum(frame.Bands);
            reactiveCanvas.UpdateFrame(frame);
        }

        public void ResetSpectrum()
        {
            for (int index = 0; index < _bars.Count; index++)
            {
                _displayBands[index] = 0;
                _bars[index].Height = 4;
            }
            reactiveCanvas.Reset();
        }
    }
}

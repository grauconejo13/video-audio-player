using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Windows.Media;

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
                var bar = new Border
                {
                    Background = index % 7 == 0 ? (Brush)FindResource("VioletBrush") : (Brush)FindResource("CyanBrush"),
                    CornerRadius = new CornerRadius(2),
                    Height = 4,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(2, 0, 2, 0),
                    Opacity = 0.82
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
        }

        public void StopAmbientAnimation()
        {
            if (!_isAnimating)
            {
                return;
            }

            ((Storyboard)FindResource("AmbientStoryboard")).Stop(this);
            _isAnimating = false;
        }

        public void UpdateSpectrum(float[] bands)
        {
            for (int index = 0; index < _bars.Count && index < bands.Length; index++)
            {
                float target = bands[index];
                _displayBands[index] = target > _displayBands[index]
                    ? _displayBands[index] + (target - _displayBands[index]) * 0.65f
                    : _displayBands[index] + (target - _displayBands[index]) * 0.18f;
                _bars[index].Height = 4 + _displayBands[index] * 108;
            }
        }

        public void ResetSpectrum()
        {
            for (int index = 0; index < _bars.Count; index++)
            {
                _displayBands[index] = 0;
                _bars[index].Height = 4;
            }
        }
    }
}

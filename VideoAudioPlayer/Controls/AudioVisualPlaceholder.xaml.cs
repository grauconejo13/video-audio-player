using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MediaPlayer.Controls
{
    public partial class AudioVisualPlaceholder : UserControl
    {
        private bool _isAnimating;

        public AudioVisualPlaceholder()
        {
            InitializeComponent();
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
    }
}

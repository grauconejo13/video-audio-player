using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using MediaPlayer.AudioAnalysis;

namespace MediaPlayer.Controls
{
    internal sealed class AudioReactiveCanvas : FrameworkElement
    {
        private const int MaximumRipples = 6;
        private readonly List<Ripple> _ripples = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Pen _wavePen = new(new LinearGradientBrush(Color.FromArgb(190, 164, 147, 255), Color.FromArgb(230, 111, 227, 237), 0), 1.7);
        private readonly Brush _cyan = new SolidColorBrush(Color.FromArgb(128, 111, 227, 237));
        private readonly Brush _violet = new SolidColorBrush(Color.FromArgb(105, 164, 147, 255));
        private float[] _waveform = Array.Empty<float>();
        private TimeSpan _lastRender;
        private TimeSpan _lastRipple;
        private bool _active;

        public AudioReactiveCanvas()
        {
            _wavePen.Freeze();
            ((Freezable)_cyan).Freeze();
            ((Freezable)_violet).Freeze();
            CompositionTarget.Rendering += (_, _) =>
            {
                if (_active && _clock.Elapsed - _lastRender >= TimeSpan.FromMilliseconds(50))
                {
                    _lastRender = _clock.Elapsed;
                    InvalidateVisual();
                }
            };
        }

        public void UpdateFrame(AudioAnalysisFrame frame)
        {
            _waveform = frame.Waveform;
            if (_active && frame.BassEnergy > 0.30f && _clock.Elapsed - _lastRipple > TimeSpan.FromMilliseconds(380))
            {
                AddRipple(frame.BassEnergy, _cyan);
            }
            else if (_active && frame.HighEnergy > 0.42f && _clock.Elapsed - _lastRipple > TimeSpan.FromMilliseconds(520))
            {
                AddRipple(frame.HighEnergy, _violet);
            }
            InvalidateVisual();
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (active) _lastRender = _clock.Elapsed;
        }

        public void Reset()
        {
            _waveform = Array.Empty<float>();
            _ripples.Clear();
            InvalidateVisual();
        }

        private void AddRipple(float energy, Brush brush)
        {
            if (_ripples.Count == MaximumRipples) _ripples.RemoveAt(0);
            _ripples.Add(new Ripple(_clock.Elapsed, 36 + energy * 36, brush));
            _lastRipple = _clock.Elapsed;
        }

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);
            double width = ActualWidth;
            double height = ActualHeight;
            TimeSpan now = _clock.Elapsed;

            for (int index = _ripples.Count - 1; index >= 0; index--)
            {
                Ripple ripple = _ripples[index];
                double age = (now - ripple.Start).TotalSeconds;
                if (age > 2.8)
                {
                    _ripples.RemoveAt(index);
                    continue;
                }
                double fade = 1 - age / 2.8;
                var baseColor = ((SolidColorBrush)ripple.Brush).Color;
                var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(61 * fade), baseColor.R, baseColor.G, baseColor.B)), 1.0);
                context.DrawEllipse(null, pen, new Point(width / 2, height / 2), ripple.InitialRadius + age * 72, ripple.InitialRadius + age * 72);
            }

            if (_waveform.Length < 2 || width <= 0 || height <= 0) return;
            var geometry = new StreamGeometry();
            using (StreamGeometryContext wave = geometry.Open())
            {
                double baseline = height * 0.27;
                wave.BeginFigure(new Point(0, baseline), false, false);
                for (int index = 0; index < _waveform.Length; index++)
                {
                    double x = width * index / (_waveform.Length - 1);
                    double y = baseline - Math.Clamp(_waveform[index], -1, 1) * height * 0.11;
                    wave.LineTo(new Point(x, y), true, false);
                }
            }
            geometry.Freeze();
            context.DrawGeometry(null, _wavePen, geometry);
        }

        private sealed record Ripple(TimeSpan Start, double InitialRadius, Brush Brush);
    }
}

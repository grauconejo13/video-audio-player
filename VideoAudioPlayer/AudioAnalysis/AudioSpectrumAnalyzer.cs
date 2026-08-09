using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace MediaPlayer.AudioAnalysis
{
    internal sealed class AudioSpectrumAnalyzer : IDisposable
    {
        private const int FftLength = 2048;
        private readonly object _syncRoot = new();
        private readonly Complex[] _fftBuffer = new Complex[FftLength];
        private AudioFileReader? _reader;
        private float[]? _sampleBuffer;
        private int _channels;
        private int _sampleRate;

        public bool IsLoaded { get { lock (_syncRoot) return _reader is not null; } }

        public void Load(string path)
        {
            lock (_syncRoot)
            {
                DisposeReader();
                _reader = new AudioFileReader(path);
                _channels = Math.Max(1, _reader.WaveFormat.Channels);
                _sampleRate = _reader.WaveFormat.SampleRate;
                _sampleBuffer = new float[FftLength * _channels];
            }
        }

        public float[]? AnalyzeAt(TimeSpan position, int bandCount)
        {
            lock (_syncRoot)
            {
                if (_reader is null || _sampleBuffer is null)
                {
                    return null;
                }

                try
                {
                    if (Math.Abs((_reader.CurrentTime - position).TotalMilliseconds) > 120)
                    {
                        _reader.CurrentTime = position;
                    }

                    int read = _reader.Read(_sampleBuffer, 0, _sampleBuffer.Length);
                    for (int frame = 0; frame < FftLength; frame++)
                    {
                        float mono = 0;
                        int offset = frame * _channels;
                        if (offset + _channels <= read)
                        {
                            for (int channel = 0; channel < _channels; channel++)
                            {
                                mono += _sampleBuffer[offset + channel];
                            }
                            mono /= _channels;
                        }

                        float window = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * frame / (FftLength - 1))));
                        _fftBuffer[frame].X = mono * window;
                        _fftBuffer[frame].Y = 0;
                    }

                    FastFourierTransform.FFT(true, (int)Math.Log2(FftLength), _fftBuffer);
                    return CreateBands(bandCount);
                }
                catch
                {
                    return null;
                }
            }
        }

        private float[] CreateBands(int bandCount)
        {
            var bands = new float[bandCount];
            double maxFrequency = Math.Min(18000, _sampleRate / 2.0);
            const double minFrequency = 35;

            for (int band = 0; band < bandCount; band++)
            {
                double startFrequency = minFrequency * Math.Pow(maxFrequency / minFrequency, band / (double)bandCount);
                double endFrequency = minFrequency * Math.Pow(maxFrequency / minFrequency, (band + 1) / (double)bandCount);
                int startBin = Math.Max(1, (int)(startFrequency * FftLength / _sampleRate));
                int endBin = Math.Min(FftLength / 2 - 1, Math.Max(startBin + 1, (int)(endFrequency * FftLength / _sampleRate)));
                double magnitude = 0;

                for (int bin = startBin; bin <= endBin; bin++)
                {
                    magnitude = Math.Max(magnitude, Math.Sqrt(_fftBuffer[bin].X * _fftBuffer[bin].X + _fftBuffer[bin].Y * _fftBuffer[bin].Y));
                }

                bands[band] = (float)Math.Clamp(Math.Log10(1 + magnitude * 90) / Math.Log10(91), 0, 1);
            }

            return bands;
        }

        public void Dispose()
        {
            lock (_syncRoot) DisposeReader();
        }

        private void DisposeReader()
        {
            _reader?.Dispose();
            _reader = null;
            _sampleBuffer = null;
        }
    }
}

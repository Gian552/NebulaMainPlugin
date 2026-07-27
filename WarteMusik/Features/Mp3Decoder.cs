using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NLayer;

namespace WarteMusik.Features
{

    public static class Mp3Decoder
    {
        public const int TargetSampleRate = 48000;

        public static float[] Decode(string path, int maxSeconds)
        {
            using (MpegFile file = new MpegFile(path))
            {
                int channels = Math.Max(1, file.Channels);
                int sourceRate = file.SampleRate;

                if (sourceRate <= 0)
                    throw new InvalidOperationException("MP3 reports a sample rate of " + sourceRate + ".");

                float[] mono = ReadMono(file, channels, sourceRate, maxSeconds);
                return Resample(mono, sourceRate, TargetSampleRate);
            }
        }


        private static float[] ReadMono(MpegFile file, int channels, int sourceRate, int maxSeconds)
        {
            int limit = maxSeconds > 0 ? maxSeconds * sourceRate : int.MaxValue;

            int estimate = (int)Math.Min(limit, Math.Max(0, file.Duration.TotalSeconds * sourceRate));
            List<float> mono = new List<float>(estimate > 0 ? estimate : 1 << 16);

            float[] buffer = new float[channels * 4096];
            int read;

            while ((read = file.ReadSamples(buffer, 0, buffer.Length)) > 0)
            {
                int frames = read / channels;

                for (int frame = 0; frame < frames; frame++)
                {
                    float sum = 0f;
                    int offset = frame * channels;

                    for (int channel = 0; channel < channels; channel++)
                        sum += buffer[offset + channel];

                    mono.Add(sum / channels);
                }

                if (mono.Count >= limit)
                    break;
            }

            if (mono.Count > limit)
                mono.RemoveRange(limit, mono.Count - limit);

            return mono.ToArray();
        }


        private static float[] Resample(float[] input, int inRate, int outRate)
        {
            if (input.Length == 0 || inRate == outRate)
                return input;

            long outLength = (long)input.Length * outRate / inRate;
            if (outLength <= 1)
                return new float[0];

            float[] output = new float[outLength];
            double step = (double)(input.Length - 1) / (outLength - 1);

            for (long i = 0; i < outLength; i++)
            {
                double position = i * step;
                int index = (int)position;
                double fraction = position - index;

                float a = input[index];
                float b = index + 1 < input.Length ? input[index + 1] : a;
                output[i] = (float)(a + (b - a) * fraction);
            }

            return output;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetDecoderName()
        {
            return typeof(MpegFile).Assembly.GetName().Name;
        }
    }
}

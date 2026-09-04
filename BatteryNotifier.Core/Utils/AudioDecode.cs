using NLayer;

namespace BatteryNotifier.Core.Utils;

/// <summary>
/// Decodes an audio file to normalized mono float samples (-1..1) plus its sample rate, for
/// analysis (e.g. loudness envelopes) rather than playback. WAV is parsed directly; MP3 via
/// NLayer — both pure-managed, no external tool or COM dependency, so they work on every OS and
/// under any publish configuration (unlike NAudio's MediaFoundationReader, which MP3 playback
/// deliberately avoids for the same reason — see SoundManager.CreateReaderStream).
/// </summary>
public static class AudioDecode
{
    /// <summary>Reads a 16-bit PCM WAV file. Returns an empty array if the file can't be parsed.</summary>
    public static (float[] Samples, int SampleRate) ReadWavMono(string path)
    {
        using var fs = File.OpenRead(path);
        using var reader = new BinaryReader(fs);

        if (new string(reader.ReadChars(4)) != "RIFF") return ([], 0);
        reader.ReadInt32();                                   // overall size
        if (new string(reader.ReadChars(4)) != "WAVE") return ([], 0);

        short audioFormat = 0, channels = 0, bitsPerSample = 0;
        int sampleRate = 0;
        byte[]? data = null;

        while (fs.Position + 8 <= fs.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();
            if (chunkSize < 0 || fs.Position + chunkSize > fs.Length + 1) break;

            if (chunkId == "fmt ")
            {
                audioFormat = reader.ReadInt16();
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32();                           // byte rate
                reader.ReadInt16();                           // block align
                bitsPerSample = reader.ReadInt16();
                if (chunkSize > 16) reader.ReadBytes(chunkSize - 16); // fmt extension
            }
            else if (chunkId == "data")
            {
                data = reader.ReadBytes(chunkSize);
            }
            else
            {
                reader.ReadBytes(chunkSize);                  // skip unknown chunk
            }

            if ((chunkSize & 1) == 1 && fs.Position < fs.Length)
                reader.ReadByte();                            // chunks are word-aligned
        }

        if (data == null || sampleRate <= 0 || channels <= 0 || audioFormat != 1 || bitsPerSample != 16)
            return ([], 0);

        int frameCount = data.Length / (2 * channels);
        var mono = new float[frameCount];
        int idx = 0;
        for (int f = 0; f < frameCount; f++)
        {
            int sum = 0;
            for (int c = 0; c < channels; c++)
            {
                sum += (short)(data[idx] | (data[idx + 1] << 8));
                idx += 2;
            }
            mono[f] = sum / channels / 32768f;
        }
        return (mono, sampleRate);
    }

    /// <summary>
    /// Decodes an MP3 file via NLayer. Returns an empty array if the file can't be parsed.
    /// <paramref name="maxDurationMs"/> bounds how much is decoded (0 = unlimited), so a caller
    /// with a length budget doesn't pay to decode audio it will just discard.
    /// </summary>
    public static (float[] Samples, int SampleRate) ReadMp3Mono(string path, int maxDurationMs = 0)
    {
        using var mpegFile = new MpegFile(path);
        var channels = mpegFile.Channels;
        var sampleRate = mpegFile.SampleRate;
        if (channels <= 0 || sampleRate <= 0) return ([], 0);

        var maxInterleaved = maxDurationMs > 0
            ? (long)sampleRate * channels * maxDurationMs / 1000
            : long.MaxValue;

        var buffer = new float[8192];
        var interleaved = new List<float>();
        int read;
        while (interleaved.Count < maxInterleaved &&
               (read = mpegFile.ReadSamples(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                interleaved.Add(buffer[i]);
        }

        if (interleaved.Count == 0) return ([], 0);

        int frameCount = interleaved.Count / channels;
        var mono = new float[frameCount];
        for (int f = 0; f < frameCount; f++)
        {
            float sum = 0;
            for (int c = 0; c < channels; c++)
                sum += interleaved[f * channels + c];
            mono[f] = sum / channels;
        }
        return (mono, sampleRate);
    }
}

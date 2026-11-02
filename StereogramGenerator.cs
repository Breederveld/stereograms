using BitMiracle.LibJpeg;

namespace Stereograms
{
    internal static class StereogramGenerator
    {
        public static int Generate(string patternFile, string templateFile, string outputFile)
        {
            var offsetConversion = 15;

            using (var patternStream = File.OpenRead(patternFile))
            using (var patternImage = new JpegImage(patternStream))
            using (var templateStream = File.OpenRead(templateFile))
            using (var templateImage = new JpegImage(templateStream))
            {
                var height = patternImage.Height;
                var outputRows = new Sample[height][];

                // Copy pattern once.
                for (var y = 0; y < height; y++)
                {
                    var row = patternImage.GetRow(y);
                    outputRows[y] = Enumerable.Range(0, row.Length).Select(i => row[i])
                        .Concat(Enumerable.Range(0, templateImage.Width).Select(_ => row[0]))
                        .ToArray();
                }

                // Repeat output until template is completed.
                for (var y = 0; y < height; y++)
                {
                    var outputRow = outputRows[y];
                    int[] elevations;
                    if (y < templateImage.Height)
                    {
                        var templateRow = templateImage.GetRow(y);
                        elevations = Enumerable.Range(0, templateRow.Length)
                            .Select(i => i == 0 ? 0 : templateRow[i].GetComponent(0))
                            .Select(v => v / offsetConversion)
                            .ToArray();
                    }
                    else
                    {
                        elevations = Enumerable.Range(0, templateImage.Width)
                            .Select(_ => 0)
                            .ToArray();
                    }
                    var width = templateImage.Width + patternImage.Width;
                    for (var x = 0; x < elevations.Length; x++)
                    {
                        outputRow[x + patternImage.Width] = outputRow[x - elevations[x]];
                    }
                }

                // Build the image from the generated rows.
                var image = new JpegImage(outputRows
                    .Select(samples =>
                    {
                        var bits = new List<bool>();
                        foreach (var sample in samples)
                        {
                            for (var i = 0; i < sample.ComponentCount; i++)
                            {
                                var cmp = sample.GetComponent(i);
                                bits.AddRange(Enumerable.Range(0, sample.BitsPerComponent).Select(i => (cmp & (1 << i)) != 0));
                            }
                        }
                        // Pad bits.
                        while (bits.Count % 8 != 0)
                        {
                            bits.Add(false);
                        }
                        var bytes = Enumerable.Range(0, bits.Count / 8)
                            .Select(i => (byte)Enumerable.Range(0, 8).Aggregate(0, (acc, next) => acc * 2 + (bits[i * 8 + 7 - next] ? 1 : 0)))
                            .ToArray();
                        var row = new SampleRow(bytes, samples.Length, templateImage.BitsPerComponent, templateImage.ComponentsPerSample);
                        return row;
                    })
                    .ToArray(), templateImage.Colorspace);

                // Write the image to the output file.
                using (var outputStream = File.OpenWrite(outputFile))
                {
                    image.WriteJpeg(outputStream, new CompressionParameters { Quality = 100 });
                }
                return patternImage.Width;
            }
        }
    }
}
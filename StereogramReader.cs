using BitMiracle.LibJpeg;

namespace Stereograms
{
    internal static class StereogramReader
    {
        public static void Parse(string streogramFile, int patternWidth, string outputFile)
        {
            var scanWidth = 30;
            using (var streogramStream = File.OpenRead(streogramFile))
            using (var stereogramImage = new JpegImage(streogramStream))
            {
                var height = stereogramImage.Height;
                var outputRows = new byte[height][];

                // Try to match pixels to their expected pattern location.
                for (var y = 0; y < height; y++)
                {
                    var imageRow = stereogramImage.GetRow(y);
                    outputRows[y] = Enumerable.Range(patternWidth, imageRow.Length - patternWidth)
                        .Select(x =>
                        {
                            var offset = Enumerable.Range(-scanWidth, scanWidth * 2)
                                .Select(offset => (offset, cmp: x - patternWidth + offset))
                                .Select(t => (t.offset, dist: t.cmp < 0 || t.cmp >= imageRow.Length ? 255 : Distance(imageRow[x], imageRow[t.cmp])))
                                .OrderBy(t => t.dist / 50.0 + Math.Abs(t.offset) * 1.0 / scanWidth)
                                .First().offset;
                            return (byte)(offset * 256 / scanWidth);
                        })
                        .ToArray();
                }

                // Build the image from the generated rows.
                var image = new JpegImage(outputRows
                    .Select(bytes =>
                    {
                        var row = new SampleRow(bytes, bytes.Length, 8, 1);
                        return row;
                    })
                    .ToArray(), Colorspace.Grayscale);

                // Write the image to the output file.
                using (var outputStream = File.OpenWrite(outputFile))
                {
                    image.WriteJpeg(outputStream, new CompressionParameters { Quality = 100 });
                }
            }
        }

        private static int Distance(Sample left, Sample right)
        {
            return left.ComponentCount != right.ComponentCount
                ? 255
                : (Enumerable.Range(0, left.ComponentCount).Sum(i => Math.Abs(left.GetComponent(i) - right.GetComponent(i))) / left.ComponentCount);
        }
    }
}
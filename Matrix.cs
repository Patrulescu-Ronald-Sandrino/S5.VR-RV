using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace rt
{
    public class Matrix
    {
        public int length { get; }
        public int width { get; }
        public int height { get; }
        public int[,,] data { get; }

        public double[] subBox { get; }

        private Matrix(int length, int width, int height, IEnumerable<object> subBox)
        {
            this.length = length;
            this.width = width;
            this.height = height;
            data = new int[length, width, height];
            this.subBox = subBox.Select(o => (double)o).ToArray();
        }

        public static Matrix Create(int length, int width, int height, IEnumerable<object> subBox, string filepath)
        {
            var matrix = new Matrix(length, width, height, subBox);

            using var reader = new BinaryReader(File.Open(filepath, FileMode.Open));
            for (var i = 0; i < length; i++)
            {
                for (var j = 0; j < width; j++)
                {
                    for (var k = 0; k < height; k++)
                    {
                        // Console.WriteLine($"Reading {i} {j} {k}");
                        // Reading 90 108 90
                        // Unhandled exception. System.IO.EndOfStreamException: Unable to read beyond the end of the stream.
                
                        matrix.data[i, j, k] = reader.ReadByte();
                        if (matrix.data[i, j, k] < 0 || matrix.data[i, j, k] > 255)
                        {
                            Console.WriteLine($"Reading {i} {j} {k}: {matrix.data[i, j, k]}");
                        }
                    }
                }
            }

            return matrix;
        }
    }
}
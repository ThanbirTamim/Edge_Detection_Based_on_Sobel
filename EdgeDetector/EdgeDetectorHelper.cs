// ***********************************************************************
// Assembly         : EdgeDetector
// Created          : 12-22-2021
//
// Last Modified By : Sheikh Thanbir Alam
// Last Modified On : 12-22-2021
// ***********************************************************************
// <copyright file="EdgeDetectorHelper.cs" company="">
//     Copyright ©  2021
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeDetector
{
    /// <summary>
    /// Class EdgeDetectorHelper.
    /// </summary>
    public class EdgeDetectorHelper
    {
        /// <summary>
        /// Enum FilterType
        /// </summary>
        public enum FilterType { NoEdgeDetection, SobelFilter, PrewittFilter, CannyFilter, CustomConvMatrix };
        /// <summary>
        /// The conv matrix gx
        /// </summary>
        double[,] ConvMatrix_Gx;
        /// <summary>
        /// The conv matrix gy
        /// </summary>
        double[,] ConvMatrix_Gy;
        /// <summary>
        /// The gaussian
        /// </summary>
        double[,] Gaussian;
        /// <summary>
        /// The m filter type
        /// </summary>
        FilterType mFilterType;
        /// <summary>
        /// The BMP
        /// </summary>
        public Bitmap Bmp;
        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        /// <value>The threshold.</value>
        public double Threshold { get; set; }
        /// <summary>
        /// Gets or sets the low threshold.
        /// </summary>
        /// <value>The low threshold.</value>
        public double LowThreshold { get; set; }
        /// <summary>
        /// Gets or sets the sigma.
        /// </summary>
        /// <value>The sigma.</value>
        public double Sigma { get; set; }
        /// <summary>
        /// The filter size
        /// </summary>
        int FilterSize = 3;
        /// <summary>
        /// The bytes per pixel
        /// </summary>
        int BytesPerPixel = 0;
        /// <summary>
        /// The BMP rect
        /// </summary>
        Rectangle BmpRect;
        /// <summary>
        /// The BMP data
        /// </summary>
        BitmapData bmpData;
        /// <summary>
        /// The RGB values
        /// </summary>
        byte[] rgbValues;
        /// <summary>
        /// The DST values
        /// </summary>
        byte[] dstValues;
        /// <summary>
        /// The gradients
        /// </summary>
        double[,,] Gradients;
        /// <summary>
        /// The orientations
        /// </summary>
        byte[,,] Orientations;
        /// <summary>
        /// The maximum gradient
        /// </summary>
        double MaxGradient;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EdgeDetector.EdgeDetectorHelper" /> class.
        /// </summary>
        /// <param name="filterType">Type of the filter.</param>
        /// <param name="bmp">The BMP.</param>
        /// <exception cref="Exception">Unsupported format</exception>
        public EdgeDetectorHelper(FilterType filterType, Bitmap bmp)
        {
            Threshold = 100;
            LowThreshold = 20;
            Sigma = 1.4;
            mFilterType = filterType;
            Bmp = bmp;

            switch (filterType)
            {
                case FilterType.CannyFilter:
                case FilterType.SobelFilter:
                    FilterSize = 3;
                    ConvMatrix_Gx = new double[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
                    ConvMatrix_Gy = new double[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
                    break;
                case FilterType.PrewittFilter:
                    FilterSize = 3;
                    ConvMatrix_Gx = new double[,] { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };
                    ConvMatrix_Gy = new double[,] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };
                    break;
            }

            BmpRect = new Rectangle(0, 0, Bmp.Width, Bmp.Height);

            try
            {
                switch (Bmp.PixelFormat)
                {
                    case PixelFormat.Format32bppArgb:
                        BytesPerPixel = 4;
                        break;
                    case PixelFormat.Format24bppRgb:
                        BytesPerPixel = 3;
                        break;
                    case PixelFormat.Format8bppIndexed:
                        BytesPerPixel = 1;
                        break;
                    default:
                        throw new Exception("Unsupported format");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Locks the bitmap.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool LockBitmap()
        {
            if (Bmp == null)
                return false;
            bmpData = Bmp.LockBits(BmpRect, ImageLockMode.ReadWrite, Bmp.PixelFormat);
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * Bmp.Height;
            rgbValues = new byte[bytes];
            // Copy the RGB values into the array. (actually, BGR)
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            return true;
        }

        /// <summary>
        /// Unlocks the bitmap.
        /// </summary>
        /// <param name="destination">The destination.</param>
        private void UnlockBitmap(ref byte[] destination)
        {
            IntPtr ptr = bmpData.Scan0;
            int bytes = bmpData.Stride * Bmp.Height;
            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(destination, 0, ptr, bytes);
            Bmp.UnlockBits(bmpData);
        }

        /// <summary>
        /// Applies the canny.
        /// </summary>
        public void ApplyCanny()
        {
            if (!LockBitmap())
                return;

            // 1st Step - Blur and sharpen image
            MakeGaussianBlurMatrix();
            GaussianInternal();
            rgbValues = (byte[])dstValues.Clone(); // Update source
            MakeGaussianSharpenMatrix();
            GaussianInternal();
            rgbValues = (byte[])dstValues.Clone(); // Update source

            // 2nd Step - Apply Sobel filter
            ApplyInternal();
            // 3rd Step - Supress non maxima
            SupressNonMaxima();
            rgbValues = (byte[])dstValues.Clone(); // Update source
                                                   // 4st Step - Hysteresis
            Hysteresis();
            rgbValues = (byte[])dstValues.Clone(); // Update source
            InvertInternal();

            UnlockBitmap(ref dstValues);
        }

        /// <summary>
        /// Inverts this instance.
        /// </summary>
        public void Invert()
        {
            if (!LockBitmap())
                return;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * Bmp.Height;
            dstValues = new byte[bytes];
            dstValues = rgbValues;

            InvertInternal();

            UnlockBitmap(ref dstValues);
        }

        /// <summary>
        /// Inverts the internal.
        /// </summary>
        private void InvertInternal()
        {
            int StartX = BmpRect.Left + 1;
            int StartY = BmpRect.Top + 1;
            int EndX = BmpRect.Left + BmpRect.Width - 1;
            int EndY = BmpRect.Top + BmpRect.Height - 1;

            for (int Y = StartY; Y < EndY; Y++)
                for (int X = StartX; X < EndX; X++)
                {
                    int ByteStart = Y * bmpData.Stride + X * BytesPerPixel;

                    for (int p = 0; p < BytesPerPixel; p++)
                    {
                        int ByteLoc = ByteStart + p;
                        dstValues[ByteLoc] = (byte)(255 - dstValues[ByteLoc]);
                    }
                }
        }

        /// <summary>
        /// Hysteresises this instance.
        /// </summary>
        private void Hysteresis()
        {
            int StartX = BmpRect.Left + 1;
            int StartY = BmpRect.Top + 1;
            int EndX = BmpRect.Left + BmpRect.Width - 1;
            int EndY = BmpRect.Top + BmpRect.Height - 1;

            for (int Y = StartY; Y < EndY; Y++)
                for (int X = StartX; X < EndX; X++)
                {
                    int ByteStart = Y * bmpData.Stride + X * BytesPerPixel;

                    for (int p = 0; p < BytesPerPixel; p++)
                    {
                        int ByteLoc = ByteStart + p;
                        if (rgbValues[ByteLoc] < Threshold)
                        {
                            if (rgbValues[ByteLoc] < LowThreshold)
                                rgbValues[ByteLoc] = dstValues[ByteLoc] = 0;
                            else // if between thresholds, check neighbours
                            {
                                bool foundHighThresNeighbour = false;
                                for (int i = 0; i < 3; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        if (i == 1 && j == 1)
                                            continue; // don't check against self
                                        int NeighByte = ByteLoc + bmpData.Stride * (i - 1)
                                                                + BytesPerPixel * (j - 1);
                                        if (rgbValues[NeighByte] > Threshold)
                                        {
                                            foundHighThresNeighbour = true;
                                            break;
                                        }
                                    }
                                    if (foundHighThresNeighbour)
                                        break;
                                }
                                if (!foundHighThresNeighbour)
                                    rgbValues[ByteLoc] = dstValues[ByteLoc] = 0;
                                else
                                    rgbValues[ByteLoc] = dstValues[ByteLoc] = 255;
                            }
                        }
                        else
                            dstValues[ByteLoc] = 255;
                    }
                }
        }

        /// <summary>
        /// Gaussians the function.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        private double GaussianFunc(double x, double y)
        {
            return Math.Exp(-(x * x + y * y) / (2 * Sigma * Sigma)) / Math.Sqrt(2 * Math.PI * Sigma * Sigma);
        }

        /// <summary>
        /// Makes the gaussian blur matrix.
        /// </summary>
        public void MakeGaussianBlurMatrix()
        {
            Sigma = Math.Max(0.1, Math.Min(20.0, Sigma));
            Gaussian = new double[5, 5];
            double Weight = 0;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    Gaussian[i, j] = GaussianFunc(i - 2, j - 2);
                    Weight += Gaussian[i, j];
                }
            // Normalize
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    Gaussian[i, j] /= Weight;
        }

        /// <summary>
        /// Makes the gaussian sharpen matrix.
        /// </summary>
        public void MakeGaussianSharpenMatrix()
        {
            //Sigma = Math.Max(0.1, Math.Min(20.0, Sigma));
            Gaussian = new double[3, 3] {{0, -1.0/5.0, 0},
                                    {-1.0/5.0, 1+4.0/5.0, -1.0/5.0},
                                    {0, -1.0/5.0, 0}};
        }

        /// <summary>
        /// Gaussians the internal.
        /// </summary>
        private void GaussianInternal()
        {
            int GaussSize = Gaussian.GetLength(0);
            int ConvMatrixSide = GaussSize / 2;
            int StartX = BmpRect.Left + ConvMatrixSide;
            int StartY = BmpRect.Top + ConvMatrixSide;
            int EndX = BmpRect.Left + BmpRect.Width - ConvMatrixSide;
            int EndY = BmpRect.Top + BmpRect.Height - ConvMatrixSide;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * Bmp.Height;
            dstValues = new byte[bytes];
            dstValues = rgbValues;

            double G = 0.0;
            for (int Y = StartY; Y < EndY; Y++)
                for (int X = StartX; X < EndX; X++)
                {
                    int ByteStart = Y * bmpData.Stride + X * BytesPerPixel;

                    for (int p = 0; p < BytesPerPixel; p++)
                    {
                        int ByteLoc = ByteStart + p;
                        for (int i = 0; i < GaussSize; i++)
                            for (int j = 0; j < GaussSize; j++)
                            {
                                //int NeighByte = (Y-ConvMatrixSide+i)*bmpData.Stride + X*BytesPerPixel +
                                //               p - ConvMatrixSide + j;
                                int NeighByte = ByteLoc + bmpData.Stride * (i - ConvMatrixSide)
                                                        + BytesPerPixel * (j - ConvMatrixSide);
                                G += Gaussian[i, j] * rgbValues[NeighByte];
                            }
                        G = Math.Max(Math.Min(255, G), 0.0);
                        dstValues[ByteLoc] = (byte)(G);
                        G = 0;
                    }
                }
        }

        /// <summary>
        /// Applies the gaussian.
        /// </summary>
        public void ApplyGaussian()
        {
            if (!LockBitmap())
                return;

            GaussianInternal();

            UnlockBitmap(ref dstValues);
        }

        /// <summary>
        /// Converts to gray scale.
        /// </summary>
        public void ConvertToGrayScale()
        {
            if (!LockBitmap())
                return;
            try
            {
                if (Bmp.PixelFormat == PixelFormat.Format24bppRgb || Bmp.PixelFormat == PixelFormat.Format32bppArgb)
                {
                    for (int Y = 0; Y < Bmp.Height; Y++)
                        for (int X = 0; X < Bmp.Width; X++)
                        {
                            int ByteStart = Y * bmpData.Stride + X * BytesPerPixel;
                            byte blue = rgbValues[ByteStart];
                            byte green = rgbValues[ByteStart + 1];
                            byte red = rgbValues[ByteStart + 2];
                            byte GrayVal = (byte)(.299 * red + .587 * green + .114 * blue);
                            rgbValues[ByteStart] = rgbValues[ByteStart + 1] = rgbValues[ByteStart + 2] = GrayVal;
                        }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
            finally
            {
                UnlockBitmap(ref rgbValues);
            }
        }

        /// <summary>
        /// Applies the internal.
        /// </summary>
        private void ApplyInternal()
        {
            int ConvMatrixSide = (int)FilterSize / 2;
            int StartX = BmpRect.Left + ConvMatrixSide;
            int StartY = BmpRect.Top + ConvMatrixSide;
            int EndX = BmpRect.Left + BmpRect.Width - ConvMatrixSide;
            int EndY = BmpRect.Top + BmpRect.Height - ConvMatrixSide;

            MaxGradient = double.NegativeInfinity;

            try
            {
                // Declare an array to hold the bytes of the bitmap.
                int bytes = bmpData.Stride * Bmp.Height;
                dstValues = new byte[bytes];

                if (mFilterType == FilterType.CannyFilter)
                {
                    Gradients = new double[BmpRect.Width, BmpRect.Height, BytesPerPixel];
                    Orientations = new byte[BmpRect.Width, BmpRect.Height, BytesPerPixel];
                }

                double G = 0.0, GradX = 0.0, GradY = 0, Orientation = 0;
                double Rad2Deg = 180.0 / System.Math.PI;

                for (int Y = StartY; Y < EndY; Y++)
                    for (int X = StartX; X < EndX; X++)
                    {
                        int ByteStart = Y * bmpData.Stride + X * BytesPerPixel;

                        for (int p = 0; p < BytesPerPixel; p++)
                        {
                            int ByteLoc = ByteStart + p;
                            for (int i = 0; i < FilterSize; i++)
                                for (int j = 0; j < FilterSize; j++)
                                {
                                    //int NeighByte = (Y-ConvMatrixSide+i)*bmpData.Stride + X*BytesPerPixel +
                                    //               p - ConvMatrixSide + j;
                                    int NeighByte = ByteLoc + bmpData.Stride * (i - ConvMatrixSide)
                                                            + BytesPerPixel * (j - ConvMatrixSide);
                                    GradX += ConvMatrix_Gx[i, j] * rgbValues[NeighByte];
                                    GradY += ConvMatrix_Gy[i, j] * rgbValues[NeighByte];
                                }

                            if (mFilterType == FilterType.CannyFilter)
                            {
                                //G = Math.Min(255, Math.Abs(GradX) + Math.Abs(GradY));
                                G = Math.Abs(GradX) + Math.Abs(GradY);
                                Gradients[X, Y, p] = G;
                                if (MaxGradient < Gradients[X, Y, p])
                                    MaxGradient = Gradients[X, Y, p];
                                if (GradX == 0)
                                    Orientation = (GradY == 0) ? 0 : 90;
                                else
                                {
                                    double GY_GX = GradY / GradX;
                                    if (GY_GX < 0)  //2nd and 4th quads
                                    {
                                        Orientation = 180 - Math.Atan(-GY_GX) * Rad2Deg;
                                    }
                                    else  //1st and 3rd
                                    {
                                        Orientation = Math.Atan(GY_GX) * Rad2Deg;
                                    }

                                    // Get closest angle
                                    if (Orientation < 22.5)
                                        Orientation = 0;
                                    else if (Orientation < 67.5)
                                        Orientation = 45;
                                    else if (Orientation < 112.5)
                                        Orientation = 90;
                                    else if (Orientation < 157.5)
                                        Orientation = 135;
                                    else Orientation = 0;
                                }
                                Orientations[X, Y, p] = (byte)(Orientation);
                            }
                            else
                            {
                                GradX = Math.Abs(GradX);
                                GradY = Math.Abs(GradY);
                                G = Math.Min(255, GradX + GradY);
                                if (G > Threshold)
                                {
                                    //dstValues[ByteLoc] = rgbValues[ByteLoc];
                                    dstValues[ByteLoc] = 0;// (byte)(255 - rgbValues[ByteLoc]);
                                }
                                else
                                    //dstValues[ByteLoc] = 0;
                                    dstValues[ByteLoc] = 255;
                            }
                            GradX = 0.0; GradY = 0.0;
                        }

                    }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Supresses the non maxima.
        /// </summary>
        private void SupressNonMaxima()
        {
            int ConvMatrixSide = (int)FilterSize / 2;
            int StartX = BmpRect.Left + ConvMatrixSide;
            int StartY = BmpRect.Top + ConvMatrixSide;
            int EndX = BmpRect.Left + BmpRect.Width - ConvMatrixSide;
            int EndY = BmpRect.Top + BmpRect.Height - ConvMatrixSide;

            double LeftPixel = 0, RightPixel = 0;
            for (int Y = StartY; Y < EndY; Y++)
                for (int X = StartX; X < EndX; X++)
                {
                    int ByteStart = Y * bmpData.Stride + X * BytesPerPixel;

                    for (int p = 0; p < BytesPerPixel; p++)
                    {
                        int ByteLoc = ByteStart + p;
                        switch (Orientations[X, Y, p])  // Get adjacent pixels
                        {
                            case 0:
                                LeftPixel = Gradients[X - 1, Y, p];
                                RightPixel = Gradients[X + 1, Y, p];
                                break;
                            case 45:
                                LeftPixel = Gradients[X - 1, Y + 1, p];
                                RightPixel = Gradients[X + 1, Y - 1, p];
                                break;
                            case 90:
                                LeftPixel = Gradients[X, Y + 1, p];
                                RightPixel = Gradients[X, Y - 1, p];
                                break;
                            case 135:
                                LeftPixel = Gradients[X + 1, Y + 1, p];
                                RightPixel = Gradients[X - 1, Y - 1, p];
                                break;

                        }
                        if (Gradients[X, Y, p] < LeftPixel || Gradients[X, Y, p] < RightPixel)
                            dstValues[ByteLoc] = 0;
                        else
                            dstValues[ByteLoc] = (byte)((Gradients[X, Y, p] / MaxGradient) * 255);
                        //dstValues[ByteLoc] = (byte)(Gradients[X, Y, p]);
                    }
                }
        }

        /// <summary>
        /// Applies the filter.
        /// </summary>
        public void ApplyFilter()
        {
            if (!LockBitmap())
                return;

            ApplyInternal();

            UnlockBitmap(ref dstValues);
        }
    }
}

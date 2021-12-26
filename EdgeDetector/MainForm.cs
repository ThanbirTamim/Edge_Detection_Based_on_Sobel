// ***********************************************************************
// Assembly         : EdgeDetector
// Author           : Sheikh Thanbir Alam
// Created          : 12-22-2021
//
// Last Modified By : Sheikh Thanbir Alam
// Last Modified On : 12-22-2021
// ***********************************************************************
// <copyright file="MainForm.cs" company="">
//     Copyright ©  2021
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EdgeDetector
{
    /// <summary>
    /// Class MainForm.
    /// Implements the <see cref="System.Windows.Forms.Form" />
    /// </summary>
    /// <seealso cref="System.Windows.Forms.Form" />
    public partial class MainForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        List<Tuple<int, int>> selected_pixels = new List<Tuple<int, int>>();

        /// <summary>
        /// The input bitmap
        /// </summary>
        Bitmap input_bitmap;
        /// <summary>
        /// Handles the Load event of the Form1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Form1_Load(object sender, EventArgs e)
        {
            //sample image must be "jpg, jpeg, bmp, png"
            string input = @"D:\Varsity\EdgeDetector\Sample Data\cucumber.jpg";
            input_bitmap = new Bitmap(input);
            pictureBoxInput.Image = input_bitmap;
        }

        /// <summary>
        /// The detect
        /// </summary>
        EdgeDetectorHelper _Detect;
        /// <summary>
        /// Handles the Click event of the btnGenerate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            //Sobel edge detection
            if (input_bitmap == null)
                return;
            _Detect = new EdgeDetectorHelper(EdgeDetectorHelper.FilterType.NoEdgeDetection, input_bitmap);
            _Detect.ConvertToGrayScale();
            Bitmap gray_image = _Detect.Bmp;

            //important param: Thresold(range:1-100), lowthresold(range:1-100), sigma (range: 0.1-2.7)
            _Detect = new EdgeDetectorHelper(EdgeDetectorHelper.FilterType.SobelFilter, gray_image);
            _Detect.Threshold = 100;
            _Detect.ApplyFilter();
            var bmp = _Detect.Bmp;

            //Binarilization
            int black_pixel = 0, white_pixel = 0;
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    int r, g, b;
                    if (color.R >= 127)
                        r = 255;
                    else
                        r = 0;
                    if (color.G >= 127)
                        g = 255;
                    else
                        g = 0;
                    if (color.B >= 127)
                        b = 255;
                    else
                        b = 0;
                    bmp.SetPixel(i, j, Color.FromArgb(r, g, b));


                    if (bmp.GetPixel(i, j) == Color.FromArgb(0, 0, 0))
                    {
                        selected_pixels.Add(new Tuple<int, int>(i, j));
                        black_pixel++;
                    }
                    else
                        white_pixel++;

                }
            }
            pictureBoxOutput.Image = bmp;
            MessageBox.Show($"Total pixel: {bmp.Width * bmp.Height}; Black: {black_pixel}; White: {white_pixel} ");
        }
    }
}

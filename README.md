<!-- ABOUT THE PROJECT -->
## About The Project
Edge_Detection_Based_on_Sobel is an image processing project that utilizes the Sobel operator to perform edge detection on digital images. The Sobel algorithm highlights regions of high spatial frequency, effectively identifying edges by calculating the gradient magnitude of pixel intensities in both horizontal and vertical directions. This method emphasizes transitions in brightness, making it ideal for detecting boundaries, shapes, and contours within images. The project supports various image formats and offers options for tuning edge sensitivity through threshold adjustments, enabling precise edge extraction for applications in computer vision, object detection, and image analysis.

## Implementation 
Just use these lines of code to get restult

	EdgeDetectorHelper _Detect;
	Bitmap input_bitmap;
	string input = @"C:\1.jpeg";
	input_bitmap = new Bitmap(input);
	if (input_bitmap == null)
		return;
	_Detect = new EdgeDetectorHelper(EdgeDetectorHelper.FilterType.NoEdgeDetection, input_bitmap);
	_Detect.ConvertToGrayScale();
	Bitmap gray_image = _Detect.Bmp;
	//important param: Thresold(range:1-100), lowthresold(range:1-100), sigma (range: 0.1-2.7)
	_Detect = new EdgeDetectorHelper(EdgeDetectorHelper.FilterType.SobelFilter, gray_image);
	_Detect.Threshold = 100;
	_Detect.ApplyFilter();
	_Detect.Bmp.Save(@"C:\test.jpeg");
	
## Output
![CHEESE!](test.png)

##Project Link
https://github.com/ThanbirTamim/Edge_Detection_Based_on_Sobel/blob/main/README.md

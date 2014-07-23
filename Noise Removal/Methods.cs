using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace Noise_Removal
{
    public class Remover
    {
        // Constants used to address the individual color pixels for generating images
        private const int RedIndex = 2;
        private const int GreenIndex = 1;
        private const int BlueIndex = 0;
        private const int AlphaIndex = 3;
        private const int MaxDepthDistance = 4000;
        private const int MinDepthDistance = 850;
        private const int MaxDepthDistanceOffset = 3150;
        private Queue<short[]> averageQueue;

        //constructor for a new depth remover instance
        public Remover() {
            this.averageQueue = new Queue<short[]>();
        }

        public Bitmap RemoveDepthNoise(DepthImageFrame frame)
        {
            BitmapSource _bitmapsource = applyBothFilters(frame);
            Bitmap returnval = BitmapFromSource(_bitmapsource);
            return returnval;
        }

        //helper method that takes a BitmapSource and creates a Bitmap
        public Bitmap BitmapFromSource(BitmapSource bmps)
        {
            Bitmap bitmap;
            using (MemoryStream outstream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmps));
                enc.Save(outstream);
                bitmap = new Bitmap(outstream);
            }
            return bitmap;
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public BitmapSource BitmapSourceFromBitmap(Bitmap bmp)
        {

            if (bmp != null)
            {
                    IntPtr hBitmap = bmp.GetHbitmap();
                    BitmapSource bmps = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DeleteObject(hBitmap);
                    return bmps;
            }
            else
                return null;
            
           // ret = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());


           // return ret;
        }
        //Takes a DepthImage frame and performs two noise removal techniques:
        //(1)pixel filtering (2)weighted moving average (reduces flicker)
        //It takes a DepthImage Frame and returns a BitmapSource
        public BitmapSource applyBothFilters(DepthImageFrame image)
        {


            // We first want to create a simple array where each index represents a single pixel of depth information.
            // This will make it easier to work with the data to filter and average it for smoothing.
            //   short[] depthArray = CreateDepthArray(image);

            short[] depthArray = new short[image.PixelDataLength];
            image.CopyPixelDataTo(depthArray);
 
            depthArray = applyPixelFilter(depthArray, image.Width, image.Height);
 
           // depthArray = applyWeightedAverageFilter(depthArray, image.Width, image.Height);
            int width = image.Width;
            int height = image.Height;
            // After we have processed the data, we can transform it into color channels for final rendering.
            byte[] colorBytes = CreateColorBytesFromDepthArray(depthArray, width, height);


            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, colorBytes, width * PixelFormats.Bgr32.BitsPerPixel / 8);
        }


        //Takes in a depthArray and crates Color Bytes from the frame
        private byte[] CreateColorBytesFromDepthArray(short[] depthArray, int width, int height)
        {
            // We multiply the product of width and height by 4 because each byte
            // will represent a different color channel per pixel in the final iamge.
            byte[] colorFrame = new byte[width * height * 4];

            // Process each row in parallel
            Parallel.For(0, height, depthArrayRowIndex =>
            {
                // Process each pixel in the row
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < width; depthArrayColumnIndex++)
                {
                    var distanceIndex = depthArrayColumnIndex + (depthArrayRowIndex * width);

                    // Because the colorFrame we are creating has four times as many bytes representing
                    // a pixel in the final image, we set the index to for times of the depth index.
                    var index = distanceIndex * 4;

                    // Map the distance to an intesity that can be represented in RGB
                    var intensity = CalculateIntensityFromDistance(depthArray[distanceIndex]);

                    // Apply the intensity to the color channels
                    colorFrame[index + BlueIndex] = intensity;
                    colorFrame[index + GreenIndex] = intensity;
                    colorFrame[index + RedIndex] = intensity;
                }

            });

            return colorFrame;
        }

        //Main method that performs the pixel removal
        public short[] applyPixelFilter(short[] depthArray, int width, int height)
        {
            /////////////////////////////////////////////////////////////////////////////////////
            // I will try to comment this as well as I can in here, but you should probably refer
            // to my Code Project article for a more in depth description of the method.
            /////////////////////////////////////////////////////////////////////////////////////

            short[] smoothDepthArray = new short[depthArray.Length];

            // We will be using these numbers for constraints on indexes
            int widthBound = width - 1;
            int heightBound = height - 1;

            Parallel.For(0, height, depthArrayRowIndex =>
            {
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < width; depthArrayColumnIndex++)
                {
                    var depthIndex = depthArrayColumnIndex + (depthArrayRowIndex * width);

                    // We are only concerned with eliminating 'white' noise from the data.
                    // We consider any pixel with a depth of 0 as a possible candidate for filtering.
                    if (depthArray[depthIndex] == 0)
                    {
                        // From the depth index, we can determine the X and Y coordinates that the index
                        // will appear in the image.  We use this to help us define our filter matrix.
                        int x = depthIndex % width;
                        int y = (depthIndex - x) / width;

                        // The filter collection is used to count the frequency of each
                        // depth value in the filter array.  This is used later to determine
                        // the statistical mode for possible assignment to the candidate.
                        short[,] filterCollection = new short[24, 2];

                        // The inner and outer band counts are used later to compare against the threshold 
                        // values set in the UI to identify a positive filter result.
                        int innerBandCount = 0;
                        int outerBandCount = 0;

                        // The following loops will loop through a 5 X 5 matrix of pixels surrounding the 
                        // candidate pixel.  This defines 2 distinct 'bands' around the candidate pixel.
                        // If any of the pixels in this matrix are non-0, we will accumulate them and count
                        // how many non-0 pixels are in each band.  If the number of non-0 pixels breaks the
                        // threshold in either band, then the average of all non-0 pixels in the matrix is applied
                        // to the candidate pixel.
                        for (int yi = -2; yi < 3; yi++)
                        {
                            for (int xi = -2; xi < 3; xi++)
                            {
                                // yi and xi are modifiers that will be subtracted from and added to the
                                // candidate pixel's x and y coordinates that we calculated earlier.  From the
                                // resulting coordinates, we can calculate the index to be addressed for processing.

                                // We do not want to consider the candidate pixel (xi = 0, yi = 0) in our process at this point.
                                // We already know that it's 0
                                if (xi != 0 || yi != 0)
                                {
                                    // We then create our modified coordinates for each pass
                                    var xSearch = x + xi;
                                    var ySearch = y + yi;

                                    // While the modified coordinates may in fact calculate out to an actual index, it 
                                    // might not be the one we want.  Be sure to check to make sure that the modified coordinates
                                    // match up with our image bounds.
                                    if (xSearch >= 0 && xSearch <= widthBound && ySearch >= 0 && ySearch <= heightBound)
                                    {
                                        var index = xSearch + (ySearch * width);
                                        // We only want to look for non-0 values
                                        if (depthArray[index] != 0)
                                        {
                                            // We want to find count the frequency of each depth
                                            for (int i = 0; i < 24; i++)
                                            {
                                                if (filterCollection[i, 0] == depthArray[index])
                                                {
                                                    // When the depth is already in the filter collection
                                                    // we will just increment the frequency.
                                                    filterCollection[i, 1]++;
                                                    break;
                                                }
                                                else if (filterCollection[i, 0] == 0)
                                                {
                                                    // When we encounter a 0 depth in the filter collection
                                                    // this means we have reached the end of values already counted.
                                                    // We will then add the new depth and start it's frequency at 1.
                                                    filterCollection[i, 0] = depthArray[index];
                                                    filterCollection[i, 1]++;
                                                    break;
                                                }
                                            }

                                            // We will then determine which band the non-0 pixel
                                            // was found in, and increment the band counters.
                                            if (yi != 2 && yi != -2 && xi != 2 && xi != -2)
                                                innerBandCount++;
                                            else
                                                outerBandCount++;
                                        }
                                    }
                                }
                            }
                        }

                        // Once we have determined our inner and outer band non-zero counts, and accumulated all of those values,
                        // we can compare it against the threshold to determine if our candidate pixel will be changed to the
                        // statistical mode of the non-zero surrounding pixels.
                        int innerBandThreshold = 0;
                        int outerBandThreshold = 0;
                        if (innerBandCount >= innerBandThreshold || outerBandCount >= outerBandThreshold)
                        {
                            short frequency = 0;
                            short depth = 0;
                            // This loop will determine the statistical mode
                            // of the surrounding pixels for assignment to
                            // the candidate.
                            for (int i = 0; i < 24; i++)
                            {
                                // This means we have reached the end of our
                                // frequency distribution and can break out of the
                                // loop to save time.
                                if (filterCollection[i, 0] == 0)
                                    break;
                                if (filterCollection[i, 1] > frequency)
                                {
                                    depth = filterCollection[i, 0];
                                    frequency = filterCollection[i, 1];
                                }
                            }

                            smoothDepthArray[depthIndex] = depth;
                        }

                    }
                    else
                    {
                        // If the pixel is not zero, we will keep the original depth.
                        smoothDepthArray[depthIndex] = depthArray[depthIndex];
                    }
                }
            });

            return smoothDepthArray;
        }

        //Helper Method
        public byte CalculateIntensityFromDistance(int distance)
        {
            // This will map a distance value to a 0 - 255 range
            // for the purposes of applying the resulting value
            // to RGB pixels.
            int newMax = distance - MinDepthDistance;
            if (newMax > 0)
                return (byte)(255 - (255 * newMax
                / (MaxDepthDistanceOffset)));
            else
                return (byte)255;
        }

        
        //Applies the weighted average smoothinhg algorithm on a Depth Array. 
        //return Depth array.
        public short[] applyWeightedAverageFilter(short[] depthArray, int width, int height)
        {
            // This is a method of Weighted Moving Average per pixel coordinate across several frames of depth data.
            // This means that newer frames are linearly weighted heavier than older frames to reduce motion tails,
            // while still having the effect of reducing noise flickering.
            averageQueue.Enqueue(depthArray);

            CheckForDequeue();

            int[] sumDepthArray = new int[depthArray.Length];
            short[] averagedDepthArray = new short[depthArray.Length];

            int Denominator = 0;
            int Count = 5;

            // REMEMBER!!! Queue's are FIFO (first in, first out).  This means that when you iterate
            // over them, you will encounter the oldest frame first.

            // We first create a single array, summing all of the pixels of each frame on a weighted basis
            // and determining the denominator that we will be using later.
            foreach (var item in averageQueue)
            {
                // Process each row in parallel
                Parallel.For(0, height, depthArrayRowIndex =>
                {
                    // Process each pixel in the row
                    for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < width; depthArrayColumnIndex++)
                    {
                        var index = depthArrayColumnIndex + (depthArrayRowIndex * width);
                        sumDepthArray[index] += item[index] * Count;
                    }
                });
                Denominator += Count;
                Count++;
            }

            // Once we have summed all of the information on a weighted basis, we can divide each pixel
            // by our calculated denominator to get a weighted average.

            // Process each row in parallel
            Parallel.For(0, height, depthArrayRowIndex =>
            {
                // Process each pixel in the row
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < width; depthArrayColumnIndex++)
                {
                    var index = depthArrayColumnIndex + (depthArrayRowIndex * width);
                    averagedDepthArray[index] = (short)(sumDepthArray[index] / Denominator);
                }
            });

            return averagedDepthArray;
        }
        //Helper Method
        private void CheckForDequeue()
        {
            // We will recursively check to make sure we have Dequeued enough frames.
            // This is due to the fact that a user could constantly be changing the UI element
            // that specifies how many frames to use for averaging.
            int averageFrameCount = 5;
            if (averageQueue.Count > averageFrameCount)
            {
                averageQueue.Dequeue();
                CheckForDequeue();
            }
        }


        //Creates a Bitmap from a DepthImage Frame for straight rendering from Depth Frame
        public Bitmap CreateBitMapFromDepthFrame(DepthImageFrame frame)
        {
            if (frame != null)
            {
                var bitmapImage = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
                var g = Graphics.FromImage(bitmapImage);
                g.Clear(System.Drawing.Color.FromArgb(0, 34, 68));

                //Copy the depth frame data onto the bitmap 
                var _pixelData = new short[frame.PixelDataLength];
                frame.CopyPixelDataTo(_pixelData);
                BitmapData bmapdata = bitmapImage.LockBits(new Rectangle(0, 0, frame.Width,
                 frame.Height), ImageLockMode.WriteOnly, bitmapImage.PixelFormat);
                IntPtr ptr = bmapdata.Scan0;
                Marshal.Copy(_pixelData, 0, ptr, frame.Width * frame.Height);
                bitmapImage.UnlockBits(bmapdata);

                return bitmapImage;
            }
            return null;
        }


        //distance given in inches
        public BitmapSource ApplyDistanceFilter(DepthImageFrame frame, int dist)
        {
            double depthDist = dist * 25.4;
            short[] depthArray = new short[frame.PixelDataLength];
            frame.CopyPixelDataTo(depthArray);
            depthArray = applyPixelFilter(depthArray, frame.Width, frame.Height);
          //  depthArray = applyWeightedAverageFilter(depthArray, frame.Width, frame.Height);
            byte[] colorFrame = new byte[frame.Width * frame.Height * 4];


            Parallel.For(0, frame.Height, i =>
            {
                // Process each pixel in the row
                for (int j = 0; j < frame.Width; j++)
                {
                    var distanceIndex = j + (i * frame.Width);
                    var index = distanceIndex * 4;
                    double depth = depthArray[distanceIndex]>> DepthImageFrame.PlayerIndexBitmaskWidth;

                    // Map the distance to an intesity that can be represented in RGB

                    if (depth < depthDist && depth > 0)
                    {
                        var intensity = CalculateIntensityFromDistance(depthArray[distanceIndex]);

                        colorFrame[index + BlueIndex] = intensity;
                        colorFrame[index + GreenIndex] = intensity;
                        colorFrame[index + RedIndex] = intensity;
                        colorFrame[index + AlphaIndex] = 250;

                    }
                    else
                    {
                        colorFrame[index + BlueIndex] = 112;
                        colorFrame[index + GreenIndex] = 25;
                        colorFrame[index + RedIndex] = 25;
                        colorFrame[index + AlphaIndex] = 200;

                    }


                }

            });
           
            //rgba
            BitmapSource bmps = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, colorFrame, frame.Width * PixelFormats.Bgr32.BitsPerPixel / 8);

            //rgb
            //BitmapSource bmps = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, colorFrame, frame.Width * PixelFormats.Bgr32.BitsPerPixel / 8);
            return bmps;
        }





    }
}

# VideoDatasetCreator
A tool to generate a cropped sequence of frames from video files written in C# with EmguCV library support.

This tool is created to generate a sequence of images from the specified region and frames slot of the input video. It is better to have 2 monitors to work with this tool.
Binaries compiled for Windows can be found in the _VideoDatasetCreator\bin\Release_ folder

[![Watch the video](http://islab.ulsan.ac.kr/files/graphics/filonenko/vdc_demo.jpg)](https://youtu.be/-ijUDRW9N1o)

# Features:
- Video sequence preview
- An automatic increment of numbers in folder names 

# Controls:

- Left arrow - reverse 100 frames
- Right arrow - skip 100 frames
- Top arrow - skip 1 frame
- Bottom arrow - reverse 1 frame

# Choosing the crop region:
- Left mouse button click starts the selection of the region
- Right mouse click finalizes the selection of the region

# Requirements:
- EmguCV 3.3.0+

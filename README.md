# Duplicate Image Detection and Grouping Tool

## Overview

The "Duplicate Image Detection and Grouping Tool" is a C# program designed to identify and group similar images within a specified directory and its subdirectories. This utility is intended for users who have large collections of images and need to organize, declutter, or remove duplicate and visually similar images. The program aims to reduce the manual effort in identifying duplicates, providing an efficient way to manage large image libraries.

## Features

- **Directory Traversal:** The program navigates through a specified directory and its subdirectories, identifying all image files.
- **Perceptual Hashing:** Uses perceptual hashing to create a unique signature for each image, allowing for the identification of visually similar images.
- **Image Similarity Detection:** Compares the perceptual hashes to find images with high similarity, grouping them together for the user's review.
- **Duplicate Deletion:** Automatically moves similar images to a separate "DELETED" directory for further action, such as permanent deletion.
- **User Interaction:** Prompts users to provide a target directory for processing and offers feedback on the progress of the operation.
- **PDF Reporting:** Generates a PDF report summarizing the duplicate images detected and moved to the "DELETED" directory.
- **Input Validation:** Ensures that user input is sanitized and valid to prevent errors and security risks.

## How It Works
1. User Input: The user provides a directory path, either via command-line arguments or through interactive prompts.
2. Directory Processing: The program traverses the target directory and its subdirectories, collecting all image files.
3. Image Analysis: Each image is resized to a standard size, converted to grayscale, and a perceptual hash is computed. This hash represents the visual characteristics of the image.
4. Similarity Comparison: The program compares the perceptual hashes of all images to determine their similarity. If the similarity score exceeds a defined threshold, the images are considered duplicates.
5. Duplicate Handling: Images identified as duplicates are moved to the "DELETED" directory. A PDF report is generated with details about the deleted images.
6. Completion Notification: The program notifies the user when the process is complete.

## Technical Details
**Languages and Libraries:** The tool is developed in C#. It utilizes the SixLabors.ImageSharp library for image processing, iText for PDF report generation, and System.Drawing for bitmap manipulation.
**Perceptual Hashing:** A perceptual hash is calculated by resizing the image to 32x32, converting it to grayscale, and comparing each pixel's value against the average pixel value. This generates a hash that can be compared to detect similarity.
**Similarity Threshold:** A similarity threshold of 65% is used to determine if two images are duplicates. This threshold can be adjusted based on user preference.
**Error Handling:** The program includes error handling for various scenarios, such as invalid input, non-existent directories, and unsupported image formats (e.g., HEIC, HEIF).
**Unsupported Formats:** Unsupported image formats are moved to a separate "UNSUPPORTED IMAGES" directory to prevent processing errors.

## Usage Instructions
1. Starting the Program: Run the program from the command line, providing the target directory as an argument, or run the program without arguments to be prompted for a directory path.
2. Specify the Directory: Provide the absolute path to the directory containing the images to be processed. The program will traverse this directory and its subdirectories.
3. Wait for Processing: The program will analyze the images and identify duplicates. This may take some time depending on the number of images and their size.
4. Review the Output: Once the process is complete, check the "DELETED" directory for similar images and the PDF report for details on which images were moved.
5. Take Further Action: Based on the output, decide whether to permanently delete the images in the "DELETED" directory or move them back to the original location.

## Notes
- This tool operates synchronously and may take some time to process large directories with many images.
- It's recommended to make a backup of your images before running the tool, especially if you're unsure about the results.

## Contributors
Naif Abdullah

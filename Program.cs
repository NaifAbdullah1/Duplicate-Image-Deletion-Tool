/*
TODO: 
1- Add some kind of filtering where we're only checking images, use a long regex to only take in all image extensions. 


The two main approaches you'll want to think about are: 
1- OOP program: Makes it a lot easier to understand and code. But there would be questions on whether this will make the runtime so long. But also, we want to ask ourselves if the runtime difference is negligible. 

2- Using only a hashmap with a <string, string> key-value pair where the key is the hash and the value is the image's path. 
*/



using System;
using System.IO;
using System.Collections.Generic;

using SystemDrawingImage = System.Drawing.Image;
using System.Drawing;
using System.Runtime.Serialization;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;



using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Text.RegularExpressions;

namespace DuplicateImageDeletionTool
{
    /// <summary>
    /// This class contains the main program logic for removing duplicated 
    /// images from a specified directory. It prompts the user for an 
    /// absolute path, validates and sanitizes the input to prevent 
    /// attacks, and then proceeds with duplicate removal.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The `Main` method prompts the user for an absolute path, 
        /// validates and sanitizes the input, and then initiates the
        /// process of removing duplicated images from the specified directory.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                // TODO: Take the path the was provided in cmd line args
            }
            else
            {
                Console.WriteLine("Provide the Absolute path of the folder "
                    + "containing \nthe images you want to remove duplicates from: ");

                string? targetDirectory;
                // Validate user input and prevent attacks (including injection attacks)
                // Repeat the do-while loop and prompt user to try again? (true if the user enters invalid input)
                string? sanitizedPath;
                string parentDirectory;
                do
                {
                    targetDirectory = Console.ReadLine();
                    sanitizedPath = IsUserInputValid(targetDirectory);
                    parentDirectory = sanitizedPath;
                }
                while (sanitizedPath == null);

                // Commence duplicate removal here: 
                Console.WriteLine("Processing...");

                DeleteDuplicateImages(sanitizedPath, parentDirectory);
            }


        }

        /// <summary>
        /// This method prevents attacks such as directory traversal attacks 
        /// or injection attacks by converting the user input (the path provided
        /// by the user) to it's canonical path form using Path.GetFullPath(). 
        /// If an error happens in this function, it's likely because the user 
        /// provided an invalid address.</summary>
        /// 
        /// <param name="path">
        /// The path to the folder containing the pictures to 
        /// filer out.</param>
        /// 
        /// <returns>
        /// Returns the sanitised path as a string only if the user enters a 
        /// valid string. If the user's input was invalid (e.g., an empty string
        /// or a path that doesn't exist), a null is returned, which would 
        /// signal the main method to prompt the user for inputting a valid 
        /// path again.</returns>
        static string? IsUserInputValid(string path)
        {
            if (path == null || path.Equals("")) // Ensuring input is not empty
            {
                // Error message here
                Console.WriteLine("ERROR: Input cannot have 0 characters.");
                return null; // have the user repeat their input
            }
            else
            {
                // We'll get the canonical paths as a means of sanitizing the user's input
                // Source: https://stackoverflow.com/questions/8092314/c-sharp-canonical-file-names
                string sanitizedPath = Path.GetFullPath(path); // Maybe an error can occurr here, use try-catch with a specific exception class
                if (Path.Exists(sanitizedPath) == false) // If path doesn't exist
                {
                    Console.WriteLine("ERROR: Path doesn't exist. Try entering a valid path.");
                    return null;
                }
                else
                {
                    Console.WriteLine("The target file which will have the images in "
                    + "it filtered is located at: \n" + sanitizedPath);
                    return sanitizedPath;
                }
            }
        }

        static void DeleteDuplicateImages(string sanitizedPath, string parentDirectory)
        {
            // Traverse a directory and its subdirectories. Results is a list 
            // of every image's path
            List<Image> imagesToFilter =
                TraverseTargetDirectory(sanitizedPath, parentDirectory);

            // The directory in which we put deleted images
            string deletionDirectory = $"{parentDirectory}/DELETED";
            if (!Directory.Exists(deletionDirectory))
            {
                Directory.CreateDirectory(deletionDirectory);
                Console.WriteLine("Directory created!");
            }

            // The PDF report with the details of was was deleted
            PdfWriter pdfWriter = new PdfWriter($"{deletionDirectory}/report.pdf");
            PdfDocument pdfDocument = new PdfDocument(pdfWriter);
            Document report = new Document(pdfDocument);


            // Going through all the images in a O(N^2) complexity to populate the SimilarImages variable for each image
 
            // Indicates if an image is similar to another
            const int HammingThreshold = 10;
            foreach (Image imageA in imagesToFilter)
            {
                foreach (Image imageB in imagesToFilter)
                {
                    if (!imageA.Path.Equals(imageB.Path) && imageA.SimilarToAnotherImage == false)
                    {
                        int hammingDistance = ComputeHammingDistance(imageA.DHash, imageB.DHash);
                        if (hammingDistance < HammingThreshold) // Similarity detected? 
                        {
                            imageA.SimilarImages.Add(imageB);
                            imageB.SimilarToAnotherImage = true;
                        }
                    }
                }
            }

            imagesToFilter.RemoveAll(image => image.SimilarToAnotherImage);
            Console.WriteLine("Done");


            report.Close();
        }

        /// <summary>
        /// Explore the directory that was given to us by the user. 
        /// Not only we'll check the directory, but also any other 
        /// subdirectories within the user-provided directory. 
        /// This function will be recursive.
        /// </summary>
        /// <param name="sanitizedPath">The sanitized path to the 
        /// target directory.</param>
        /// <returns>The names of all the pictures found within the 
        /// directory and its sub-directories.</returns>
        static List<Image> TraverseTargetDirectory(string sanitizedPath, string parentDirectory)
        {
            if (Path.GetFileName(sanitizedPath).Equals("DELETED") || Path.GetFileName(sanitizedPath).Equals("UNSUPPORTED IMAGES"))
            {
                return [];
            }
            List<Image> imagesFound = new List<Image>();

            // Adding the images in the current directory, the target one
            string[] picturePaths = Directory.GetFiles(sanitizedPath);

            foreach (string path in picturePaths)
            {
                try
                {
                    // If the extension is HEIC or HEIF, move it to the unsupported files directory
                    string unsupportedFormatsRegex = @"\.(heic|heif)$";
                    if (Regex.IsMatch(GetExtension(path.ToLower()), unsupportedFormatsRegex))
                    {
                        // Create the new directory and move the images
                        string unsupportedImagesDirectory = $"{parentDirectory}/UNSUPPORTED IMAGES";

                        if (Directory.Exists(unsupportedImagesDirectory) == false)
                        {
                            Directory.CreateDirectory(unsupportedImagesDirectory);
                            Console.WriteLine("Directory created for unsupported images.");
                        }
                        // Move
                        try 
                        {
                            File.Move(path, Path.Combine(unsupportedImagesDirectory, Path.GetFileName(path)));
                            Console.WriteLine("An unsupported image has been moved to the unsupported directory: " + path);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: Failed to move {path} to unsupported directory");
                        }

                    }
                    using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(path))
                    {
                        imagesFound.Add(new Image(
                            path,
                            new FileInfo(path).Length,
                            image.Height,
                            image.Width,
                            image.Metadata.VerticalResolution,
                            image.Metadata.HorizontalResolution,
                            ComputeImageDHash(path)));
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    Console.WriteLine($"Error loading image: {path}. {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing image: {path}. {ex.Message}");

                }
            }

            // Getting the paths of subdirectories and exploring each one
            string[] subdirectories = Directory.GetDirectories(sanitizedPath);
            foreach (string subdirectory in subdirectories)
            {
                imagesFound.AddRange(TraverseTargetDirectory(subdirectory, parentDirectory));
            }
            return imagesFound;
        }

        static string ComputeImageDHash(string imagePath)
        {
            // Opening the image as a Bitmap. "using" helps freeing up memory
            using (Bitmap image = new Bitmap(imagePath))
            {
                // Resizing image to 9x8 for dHash computation to standardize 
                // the image size for dHash calculation. The bigger the
                // size (e.g., 16x15), the more accurate the comparison will be
                // at the cost of a longer runtime
                Bitmap resizedImage = new Bitmap(image, new System.Drawing.Size(81, 64)); // TODO: Maybe give the user the choice to tweak these values to increase sensitivity? 

                // Computing dHash
                /*
                Iterating over the pixels of the resized image to compute the 
                dHash. For each row of pixels, it compares the brightness of 
                each pixel with the brightness of the next pixel in the row. 
                If the brightness of the current pixel is greater than the 
                brightness of the next pixel, it appends "1" to the hash; 
                otherwise, it appends "0". This process generates a binary 
                string representing the dHash of the image.
                Brightness comparison helps capture the edge information 
                and basic structure of the image.
                */
                string hash = "";
                for (int y = 0; y < resizedImage.Height; y++)
                {
                    for (int x = 0; x < resizedImage.Width - 1; x++)
                    {
                        hash += (resizedImage.GetPixel(x, y).GetBrightness() > resizedImage.GetPixel(x + 1, y).GetBrightness()) ? "1" : "0";
                    }
                }

                return hash;
            }
        }

        static int ComputeHammingDistance(string hashA, string hashB)
        {
            if (hashA.Length != hashB.Length)
            {
                throw new ArgumentException($"Error: Encountered two hashes that are unequal in length.");
            }

            int distance = 0;
            for (int i = 0; i < hashA.Length; i++)
            {
                if (hashA[i] != hashB[i])
                {
                    distance++;
                }
            }
            return distance;
        }

        static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }
    }


    /// <summary>
    /// A class as a primary constructor representing each image in 
    /// the user-provided target directory
    /// </summary>
    /// <param name="path">The absolute path to the image</param>
    /// <param name="size">The size of the image in bytes</param>
    /// <param name="height">The height of the image in pixels</param>
    /// <param name="width">The width of the image in pixels</param>
    /// <param name="verticalResolution">Vertical Resolution of the image</param>
    /// <param name="horizontalResolution">Horizontal Resolution of the image</param>
    /// <param name="dHash">dHash of this image</param>
    /// <param name="similarToAnotherImage">Prevents an image from having any similar images in its list if it's already similar to another one.</param>
    class Image(string path, long size, int height, int width, double verticalResolution, double horizontalResolution, string dHash, bool similarToAnotherImage = false)
    {
        public string Path { get; set; } = path;
        public long Size { get; set; } = size;
        public int Height { get; set; } = height;
        public int Width { get; set; } = width;
        public double VerticalResolution { get; set; } = verticalResolution;
        public double HorizontalResolution { get; set; } = horizontalResolution;
        public List<Image> SimilarImages { get; set; } = new List<Image>();
        public string DHash { get; set; } = dHash;
        public bool SimilarToAnotherImage { get; set; } = similarToAnotherImage;

        // Add a property to store the ImageSharp Image instance
        //public Image<Rgba32> ImageSharpImage { get; set; } = SixLabors.ImageSharp.Image.Load<Rgba32>(path).Clone();

        /// <summary>
        /// Two images are exactly equal if all of their parameters are the same except the list of similar images and the paths
        /// </summary>
        /// <param name="imageA">The first image to compare</param>
        /// <param name="imageB">The second image to compare</param>
        /// <returns>True if they're identical, false otherwise</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Equals(Image otherImage)
        {
            return Size == otherImage.Size
            && Height == otherImage.Height
            && Width == otherImage.Width
            && VerticalResolution == otherImage.VerticalResolution
            && HorizontalResolution == otherImage.HorizontalResolution
            && DHash == otherImage.DHash;
        }
    }

}
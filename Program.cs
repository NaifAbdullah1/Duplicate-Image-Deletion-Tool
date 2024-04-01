/*
Edge cases to consider: 
1- Two images that have the same size and quality, but different extensions
2- Image is lower in quality but have a higher size


Parameters to use when comparing images: 
1- The image with the greater size
2- The image with the higher resolution
3- The image with the greater dimensions
*/



using System;
using System.IO;
using System.Collections.Generic;
//using System.Security.Cryptography;

using System.Drawing;
using System.Runtime.Serialization;


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
            Console.WriteLine("Provide the Absolute path of the folder "
                + "containing \nthe images you want to remove duplicates from: ");

            string? targetDirectory;
            // Validate user input and prevent attacks (including injection attacks)
            // Repeat the do-while loop and prompt user to try again? (true if the user enters invalid input)
            string? sanitizedPath;
            do
            {
                targetDirectory = Console.ReadLine();
                sanitizedPath = IsUserInputValid(targetDirectory);
            }
            while (sanitizedPath == null);

            // Commence duplicate removal here: 
            Console.WriteLine("Processing...");

            DeleteDuplicateImages(sanitizedPath);

            
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

        static void DeleteDuplicateImages(string sanitizedPath)
        {
            // Traverse a directory and its subdirectories
            List<string> pathsOfImagesToFilter = TraverseTargetDirectory(sanitizedPath);

            
            // A hashmap containing the has of every image and the directory 
            // to the image associated with the hash
            Dictionary<string, string> hashAndImagePathPairs = new Dictionary<string, string>();

            foreach (string imagePath in pathsOfImagesToFilter)
            {
                // Compute hash for image
                string imageDHash = ComputeImageDHash(imagePath);
                
                if (hashAndImagePathPairs.ContainsKey(imageDHash)) // Exact Duplicate?
                {
                    // hashAndImagePathPairs[imageDHash] -> Existing image's path
                    // By "Existing", we're referring to images already 
                    // in hashAndImagePathPairs. imagePath -> Contains new image's path
                    
                    if (ReplaceExistingImageWithTheNewOne(hashAndImagePathPairs[imageDHash], imagePath))
                    {

                    }

                    

                    bool existingImageIsLarger = newImageInfo.Length > existingImageInfo.Length;
                    
                    
                    Console.WriteLine("##existing image: " + hashAndImagePathPairs[imageDHash]);
                    Console.WriteLine("##New Image: " + imagePath);
                    
                }
                else 
                {
                    hashAndImagePathPairs.Add(imageDHash, imagePath);
                }
                

                //string imageHash = ComputeImageHash(imagePath);
                Console.WriteLine("The image: " + imagePath + "\n Has this hash: " + imageDHash);
            }


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
        static List<string> TraverseTargetDirectory(string sanitizedPath)
        {
            List<string> imagesFound = [];

            // Adding the images in the current directory, the target one
            string[] picturePaths = Directory.GetFiles(sanitizedPath);
            imagesFound.AddRange(picturePaths);

            // Getting the paths of subdirectories and exploring each one
            string[] subdirectories = Directory.GetDirectories(sanitizedPath);
            foreach (string subdirectory in subdirectories)
            {
                imagesFound.AddRange(TraverseTargetDirectory(subdirectory));
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
                Bitmap resizedImage = new Bitmap(image, new Size(9, 8)); // TODO: Maybe give the user the choice to tweak these values to increase sensitivity? 

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

        /// <summary>
        /// Two criterions must be met to replace an image already in 
        /// the hashmap "hashAndImagePathPairs" with a new one: 
        /// 1- The new image must be larger in size
        /// 2- New image must have a higher vertical and horizontal resolution
        /// </summary>
        /// <param name="existingImagePath">Path of the image already in 
        /// the hashmap hashAndImagePathPairs</param>
        /// <param name="newImagePath">The path of the image we're 
        /// potentially adding to the hashmap</param>
        /// <returns>Returns true if the two conditions above are met.
        /// Returns false otherwise.</returns>
        static bool ReplaceExistingImageWithTheNewOne(string existingImagePath, string newImagePath)
        {
            FileInfo existingImageInfo = new FileInfo(existingImagePath);
            FileInfo newImageInfo = new FileInfo(newImagePath);

            // TODO: Do the comparisons mentioned in the javadoc

        }
        /*
        static string ComputeImageHash(string imagePath)
        {
            // Creates an instance of the MD5 hashing algorithm from 
            // System.Security.Cryptography
            using (var md5 = MD5.Create())
            {
                // Returns a FileStream object that allows reading from a file
                using (var stream = File.OpenRead(imagePath)) 
                {
                    // Returned hash is in the form of an array of bytes
                    byte [] hash = md5.ComputeHash(stream);

                    //Console.WriteLine("Hash: " + )

                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    
        */
    
    }



    /*
    /// <summary>
    /// A class as a primary constructor representing each image in 
    /// the user-provided target directory
    /// </summary>
    /// <param name="path">The absolute path to the image</param>
    /// <param name="size">The size of the image in bytes</param>
    /// <param name="height">The height of the image in pixels</param>
    /// <param name="width">The height of the image in pixels</param>
    class Image(string path, long size, int height, int width)
    {
        public string Path { get; set; } = path;
        public long Size { get; set; } = size;
        public int Height { get; set; } = height;
        public int Width { get; set; } = width;
    }
    */
}
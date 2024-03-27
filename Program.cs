using System;
using System.IO;

namespace program
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
            List<string> pathsOfImagesToFilter = TraverseImageDirectory(sanitizedPath);

            // TODO: Compute the hash for each image. Consider using a hash map where the key is the image's Hash and the value is the path to the image. 
            // It is also possible that the hash map will do the deletion for you, refresh your memory on how hash maps work, especially when collision (same keys) happen

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
        static List<string> TraverseImageDirectory(string sanitizedPath)
        {
            List<string> imagesFound = [];

            // Adding the images in the current directory, the target one
            string[] picturePaths = Directory.GetFiles(sanitizedPath);
            imagesFound.AddRange(picturePaths);

            // Getting the paths of subdirectories and exploring each one
            string[] subdirectories = Directory.GetDirectories(sanitizedPath);
            foreach (string subdirectory in subdirectories)
            {
                imagesFound.AddRange(TraverseImageDirectory(subdirectory));
            }
            
            return imagesFound;
        }

    }
}
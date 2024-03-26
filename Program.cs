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
            bool promptUserForInputAgain;
            do
            {
                targetDirectory = Console.ReadLine();
                promptUserForInputAgain = IsUserInputValid(targetDirectory);
            } 
            while (promptUserForInputAgain);

            string sanitizedPath = Path.GetFullPath(targetDirectory);

           // Commence duplicate removal here: 
           Console.WriteLine("Processing...");

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
        /// Returns false if the path entered by the user was valid. 
        /// If false is returned, this means that we won't prompt the user
        /// again to provide an address to a directory.</returns>
        static bool IsUserInputValid(string path)
        {
            if (path == null || path.Equals("")) // Ensuring input is not empty
            {
                // Error message here
                Console.WriteLine("ERROR: Input cannot have 0 characters.");
                return true; // have the user repeat their input
            }
            else
            {
                // We'll get the canonical paths as a means of sanitizing the user's input
                // Source: https://stackoverflow.com/questions/8092314/c-sharp-canonical-file-names
                string sanitizedPath = Path.GetFullPath(path); // Maybe an error can occurr here, use try-catch with a specific exception class
                if (Path.Exists(sanitizedPath) == false) // If path doesn't exist
                {
                    Console.WriteLine("ERROR: Path doesn't exist. Try entering a valid path.");
                    return true;
                } 
                else 
                {
                    Console.WriteLine("The target file which will have the images in "
                    + "it filtered is located at: \n" + sanitizedPath);
                    return false;
                }
            }
        }
    }
}
/*
TODO: 
1- Add some kind of filtering where we're only checking images, use a long regex to only take in all image extensions. 


The two main approaches you'll want to think about are: 
1- OOP program: Makes it a lot easier to understand and code. But there would be questions on whether this will make the runtime so long. But also, we want to ask ourselves if the runtime difference is negligible. 

2- Using only a hashmap with a <string, string> key-value pair where the key is the hash and the value is the image's path. 

If two images are identical, make sure to compare the other attributes such as resolution 
*/


using System.Drawing;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using iText.Kernel.Pdf;
using iText.Layout;
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
            string? targetDirectory;
            if (args.Length == 1)
            {
                // Taking the path the was provided in cmd line args
                targetDirectory = args[0];
            }
            else if (args.Length > 1)
            {
                Console.WriteLine("ERROR: There can only be one argument, which is the target directory. Please try again by entering the target directory: ");
            }
            else
            {
                Console.WriteLine("Provide the Absolute path of the folder "
                    + "containing \nthe images you want to remove duplicates from: ");
            }

            // Validate user input and prevent attacks (including injection attacks)
            // Repeat the do-while loop and prompt user to try again? (true if the user enters invalid input)
            string? sanitizedPath;
            //string parentDirectory;
            do
            {
                targetDirectory = Console.ReadLine();
                sanitizedPath = IsUserInputValid(targetDirectory);
                //parentDirectory = sanitizedPath;
            }
            while (sanitizedPath == null);

            // This variable is not supposed to get reassigned throughout execution
            string parentDirectory = sanitizedPath;

            // Commence duplicate removal here: 
            Console.WriteLine("Processing...");

            string? deletionDirectory = CreateDeletionDirectory(parentDirectory);

            if (deletionDirectory == null)
            {

            }

            Document report = CreateReport(deletionDirectory);

            // Traverse a directory and its subdirectories. Results is a list of every image's path
            List<Image> imagesToFilter =
            TraverseTargetDirectory(sanitizedPath, parentDirectory, report);

            DeleteDuplicateImages(sanitizedPath, parentDirectory, imagesToFilter, report);

            Console.WriteLine("Done");

        }

        /// <summary>
        /// Calculate the perceptual hash for an image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        static string CalculatePerceptualHash(Bitmap image)
        {
            // Resize the image to a fixed size
            Bitmap resizedImage = ResizeImage(image, 32, 32);

            // Convert the resized image to grayscale
            Bitmap grayscaleImage = ToGrayscale(resizedImage);

            // Calculate the average pixel value
            double averagePixelValue = CalculateAveragePixelValue(grayscaleImage);

            // Compute the hash
            string hash = ComputeHash(grayscaleImage, averagePixelValue);

            return hash;
        }

        /// <summary>
        /// Resize the image to a fixed size
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            return new Bitmap(image, new System.Drawing.Size(width, height));
        }

        /// <summary>
        /// Convert the image to grayscale
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        static Bitmap ToGrayscale(Bitmap image)
        {
            Bitmap grayscaleImage = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    System.Drawing.Color color = image.GetPixel(x, y);
                    int average = (color.R + color.G + color.B) / 3;
                    grayscaleImage.SetPixel(x, y, System.Drawing.Color.FromArgb(average, average, average));
                }
            }

            return grayscaleImage;
        }

        /// <summary>
        /// Calculate the average pixel value of the grayscale image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        static double CalculateAveragePixelValue(Bitmap image)
        {
            double sum = 0;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    sum += image.GetPixel(x, y).R; // Assuming grayscale, so R=G=B
                }
            }

            return sum / (image.Width * image.Height);
        }

        /// <summary>
        /// Compute the hash based on the image's pixel values and average value
        /// </summary>
        /// <param name="image"></param>
        /// <param name="averagePixelValue"></param>
        /// <returns></returns>
        static string ComputeHash(Bitmap image, double averagePixelValue)
        {
            string hash = "";

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    hash += (image.GetPixel(x, y).R > averagePixelValue) ? "1" : "0";
                }
            }

            return hash;
        }

        /// <summary>
        /// Calculate the similarity score between two hashes
        /// </summary>
        /// <param name="hash1">hash of the first image to compare</param>
        /// <param name="hash2">hash of the 2nd image to compare</param>
        /// <returns>Similarity score in percentage</returns>
        /// <exception cref="ArgumentException"></exception>
        static double CalculateSimilarityScore(string hash1, string hash2)
        {
            if (hash1.Length != hash2.Length)
            {
                throw new ArgumentException("Hashes must be of equal length");
            }

            int matchingBits = 0;

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] == hash2[i])
                {
                    matchingBits++;
                }
            }

            double similarityScore = (double)matchingBits / hash1.Length * 100;

            return similarityScore;
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
        /// Returns the sanitized path as a string only if the user enters a 
        /// valid string. If the user's input was invalid (e.g., an empty string
        /// or a path that doesn't exist), a null is returned, which would 
        /// signal the main method to prompt the user for inputting a valid 
        /// path again.</returns>
        static string? IsUserInputValid(string path)
        {
            if (path == null || path.Equals("")) // Ensuring input is not empty
            {
                Console.WriteLine("ERROR: Input cannot have 0 characters. Please try again: ");
                return null;
            }
            else
            {
                try
                {
                    // We'll get the canonical paths as a means of sanitizing the user's input
                    // Source: https://stackoverflow.com/questions/8092314/c-sharp-canonical-file-names
                    string sanitizedPath = Path.GetFullPath(path); // Maybe an error can occurr here, use try-catch with a specific exception class
                    if (!Path.Exists(sanitizedPath)) // If path doesn't exist
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
                catch (Exception ex)
                {
                    if (ex is ArgumentException)
                    {
                        Console.WriteLine("ERROR: Invalid path. Please try again: ");
                    }
                    else if (ex is PathTooLongException)
                    {
                        Console.WriteLine("ERROR: Path is too long. Please try a shorter one: ");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: An unexpected error occurred. Please try again: ");
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Deletes duplicate images
        /// </summary>
        /// <param name="sanitizedPath">Target directory in which we're looking for images</param>
        /// <param name="parentDirectory">A constant, the parent-most target directory</param>
        /// <param name="imagesToFilter">A list of image objects. The images to filter.</param>
        static void DeleteDuplicateImages(string sanitizedPath, string parentDirectory, List<Image> imagesToFilter, Document report)
        {
            // Going through all the images in a O(N^2) complexity to populate the SimilarImages variable for each image

            // Indicates if an image is similar to another
            const double SimilarityThreshold = 65;
            foreach (Image imageA in imagesToFilter)
            {
                if (imageA.SimilarToAnotherImage)
                {
                    // Already falls under another image as similar
                    continue;
                }
                else
                {
                    foreach (Image imageB in imagesToFilter)
                    {
                        if (!imageA.Path.Equals(imageB.Path) && imageB.SimilarToAnotherImage == false)
                        {
                            // If Image A and B are different images and B hasn't been already assigned to another one as a similar image
                            double similarityScore = CalculateSimilarityScore(imageA.PHash, imageB.PHash);
                            if (similarityScore >= SimilarityThreshold)
                            {
                                // Similarity detected
                                imageA.SimilarImages.Add(imageB);
                                imageB.SimilarToAnotherImage = true;
                            }
                        }
                    }
                }
            }

            // This will leave the array to only have the parent images along with their similar ones. The total number of parent images + their similar images = the total number of images in all directories and subdirectories.  
            imagesToFilter.RemoveAll(image => image.SimilarToAnotherImage);

            // Write the report here. 

            // Move the images here as groups/buckets.
            report.Close();
        }

        static string? CreateDeletionDirectory(string parentDirectory)
        {
            // The directory in which we put deleted images
            string deletionDirectory = $"{parentDirectory}/DELETED";
            try
            {
                if (!Directory.Exists(deletionDirectory))
                {
                    Directory.CreateDirectory(deletionDirectory);
                    Console.WriteLine("Deletion directory created!");
                    return deletionDirectory;
                }
            }
            catch (Exception ex)
            {
                if (ex is PathTooLongException)
                {
                    Console.WriteLine($"Error creating directory. Path was too long: {ex.Message}");
                }
                else if (ex is DirectoryNotFoundException)
                {
                    Console.WriteLine($"Error creating directory, directory wasn't found: {ex.Message}");
                }
                else if (ex is UnauthorizedAccessException)
                {
                    Console.WriteLine($"Permission Error encountered when creating directory: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"An unexpected error occurred while creating the deletion directory: {ex.Message}");
                }
                return null;
            }
            return deletionDirectory;
        }

        static Document CreateReport(string deletionDirectory)
        {
            // TODO: Make a try-catch here
            // The PDF report with the details of was was deleted
            PdfWriter pdfWriter = new PdfWriter($"{deletionDirectory}/report.pdf");
            PdfDocument pdfDocument = new PdfDocument(pdfWriter);
            Document report = new Document(pdfDocument);
            return report;

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
        static List<Image> TraverseTargetDirectory(string sanitizedPath, string parentDirectory, Document report)
        {
            // We won't traverse the DELETED and UNSUPPORTED IMAGES files
            if (Path.GetFileName(sanitizedPath).Equals("DELETED") || Path.GetFileName(sanitizedPath).Equals("UNSUPPORTED IMAGES"))
            {
                return [];
            }

            List<Image> imagesFound = new List<Image>();

            // Creating an array of strings where every string is the absolute path of an image in the sanitizedPath directory
            string[] picturePaths = Directory.GetFiles(sanitizedPath);

            foreach (string path in picturePaths)
            {
                try
                {
                    // Source: https://docs.sixlabors.com/articles/imagesharp/imageformats.html
                    string supportedImageFormatsRegex = @"\.(bmp|gif|jpeg|pbm|png|tiff|tga|webp|jpg)$";

                    if (!Regex.IsMatch(GetExtension(path.ToLower()), supportedImageFormatsRegex))
                    {
                        // Create the new directory and move the images
                        string unsupportedImagesDirectory = $"{parentDirectory}/UNSUPPORTED IMAGES";

                        if (!Directory.Exists(unsupportedImagesDirectory))
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
                            Console.WriteLine($"Error: Failed to move {path} to unsupported directory. Error Message: " + ex.Message);
                        }
                    }
                    else
                    {
                        using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(path))
                        {
                            imagesFound.Add(new Image(
                                path,
                                new FileInfo(path).Length,
                                image.Height,
                                image.Width,
                                image.Metadata.VerticalResolution,
                                image.Metadata.HorizontalResolution,
                                CalculatePerceptualHash(new Bitmap(path))));
                        }
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    Console.WriteLine($"Memory Error: Happened while loading image: {path}. {ex.Message}");
                    // TODO: Write to the report 
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"ERROR: File wasn't found. Image: {path},Error message: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"ERROR: File isn't accessible. Image: {path}, error message: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing image: {path}. error message: {ex.Message}");
                }
            }

            // Getting the paths of subdirectories and exploring each one
            string[] subdirectories = Directory.GetDirectories(sanitizedPath);
            foreach (string subdirectory in subdirectories)
            {
                imagesFound.AddRange(TraverseTargetDirectory(subdirectory, parentDirectory, report));
            }
            return imagesFound;
        }

        /// <summary>
        /// Given an absolute path of a file, this function returns the extension of that file. 
        /// </summary>
        /// <param name="path">Absolute path</param>
        /// <returns>a string containing the extension of the file.</returns>
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
    /// <param name="pHash">Perceptual hash of this image</param>
    /// <param name="similarToAnotherImage">Prevents an image from having any similar images in its list if it's already similar to another one.</param>
    class Image(string path, long size, int height, int width, double verticalResolution, double horizontalResolution, string pHash, bool similarToAnotherImage = false)
    {
        public string Path { get; set; } = path;
        public long Size { get; set; } = size;
        public int Height { get; set; } = height;
        public int Width { get; set; } = width;
        public double VerticalResolution { get; set; } = verticalResolution;
        public double HorizontalResolution { get; set; } = horizontalResolution;
        public List<Image> SimilarImages { get; set; } = new List<Image>();
        public string PHash { get; set; } = pHash;
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
            && PHash == otherImage.PHash;
        }
    }

}


// Deprecated
/*
static string ComputeImageDHash(string imagePath)
{
    // Opening the image as a Bitmap. "using" helps freeing up memory
    using (Bitmap image = new Bitmap(imagePath))
    {
        // Resizing image to 9x8 for dHash computation to standardize 
        // the image size for dHash calculation. The bigger the
        // size (e.g., 16x15), the more accurate the comparison will be
        // at the cost of a longer runtime
        Bitmap resizedImage = new Bitmap(image, new System.Drawing.Size(32, 32)); // TODO: Maybe give the user the choice to tweak these values to increase sensitivity? 

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

        string hash = "";
        for (int y = 0; y < resizedImage.Height; y++)
        {
            for (int x = 0; x < resizedImage.Width - 1; x++)
            {
                // Enhance sensitivity by considering more information, such as color intensity or multiple channels
                hash += (GetIntensity(resizedImage.GetPixel(x, y)) > GetIntensity(resizedImage.GetPixel(x + 1, y))) ? "1" : "0";

            }
        }

        return hash;
    }
}
*/

// Helper function to calculate intensity (brightness) of a color
/*
static double GetIntensity(System.Drawing.Color color)
{
    // Calculate intensity as a weighted sum of RGB components
    // You can experiment with different weightings to reflect human perception better
    return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
}
*/

/*
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
*/
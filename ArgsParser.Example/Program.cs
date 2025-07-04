namespace ArgsParser.Example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Parser? parser = null;
            var indent = 2;

            try
            {
                // Force example arguments.
                args = new string[] { "-serve", "-from", "15 APR 1980 GMT", "-verbose", "9999", "-write", "../output" };

                Console.WriteLine();
                Console.WriteLine("EXAMPLE APPLICATION");

                // Define the options and flags, including whether required and any default values.
                var now = DateTime.Now.ToString("s");
                parser = new Parser(args)
                  .SupportsOption<int>("port", "Port to start the dev server on", 1337)    // Optional, with default.
                  .RequiresOption<string>("read", "Folder to read the site from", "site")  // Required, with default.
                  .RequiresOption<string>("write", "CSV file to write the result to")      // Required, no default.
                  .RequiresOption<DateTime>("from", "Earliest date/time", "01 JAN 1980")   // Required, with default.
                  .SupportsFlag("serve", "Start the site going in a dev server")           // Optional flag.
                  .SupportsFlag("force", "Overwrite any destination content")              // Optional flag.
                  .AddCustomValidator("write", IsCSV)  // Automatic extra check.
                  .ShowHelpLegend(true)  // Include explanatory notes in Help text?
                  .AddExtraHelp(
                      "Notes:",
                      "Specifying a port does nothing without adding the -serve flag.",
                      "Choosing -force is potentially destructive!")
                  .Help(indent, "Usage:")  // Show instructions to the user.
                  .Parse()  // Check the provided input arguments.
                  .ShowProvided(indent, "Provided:");  // Summarise the provided options and flags.

                // Show any errors, and abort.
                if (parser.HasErrors) throw new ApplicationException();

                // Examples of accessing the options/flags.
                var shouldServe = parser.IsFlagProvided("serve");
                var port = parser.GetOption<int>("port");
                var readFromFolder = parser.GetOption<string>("read");
                var writeToFolder = parser.GetOption<string>("write");
            }
            catch (Exception)
            {
                Console.WriteLine();
                parser!.ShowErrors(indent, "Issues:");
                Console.WriteLine();
            }

            // For the avoidance of confusion.
            Console.WriteLine();
            Console.WriteLine("IMPORTANT NOTE");
            Console.WriteLine("The arguments in this example are hard-coded into the program.");
            Console.WriteLine("It DOES NOT look at what you pass in on the command line.");
            Console.WriteLine();
        }

        /// <summary>Sample validator function which checks for a CSV filename.</summary>
        /// <param name="key">Name of the argument.</param>
        /// <param name="value">Content passed in.</param>
        /// <returns>A list of any errors to be added to the parser's automatic ones.</returns>
        private static List<string> IsCSV(string key, object value)
        {
            // In reality we would also need null checks etc.
            var errs = new List<string>();
            var ext = Path.GetExtension($"{value}").ToLowerInvariant();
            if (ext != ".csv") errs.Add($"-{key} does not hold a CSV filename");
            return errs;
        }
    }
}

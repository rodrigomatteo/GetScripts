using System;

namespace ConsoleApplication7
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Application that gets the script files from TFS Server and merges them in one file");

            if (ValidateArguments(args))
            {
                try
                {
                    var tfsApi = new TSS.CS.TFS.Client.Api();
                    var fileName = tfsApi.GetScripts(args[0], args[1], args[2], args[3], args[4]);

                    Console.WriteLine("File {0} was generated", fileName);
                    Console.WriteLine("Process has ended. Press any key to quit");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("getScripts <version number> <TFS server url> <path to search the files> <from changeset number> <to changeset number>");
                Console.WriteLine("Press any kay to close the window");
            }

            Console.ReadKey();
        }

        private static bool ValidateArguments(string[] args)
        {
            return args.Length == 5;
        }
    }
}

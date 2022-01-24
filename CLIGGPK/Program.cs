using CommandLine;
using CommandLine.Text;
using LibDat2;
using LibBundle;
using LibGGPK2;
using LibGGPK2.Records;
using FileRecord = LibBundle.Records.FileRecord;

namespace CLIGGPK;

using static LibBundle.IndexContainer;

class Program
{
    internal enum GgpkSource
    {
        Bundle,
        Steam,
    }

    private class Options
    {
        [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; } = false;

        [Option('g', "ggpkSource", Required = false, Default = GgpkSource.Bundle,
            HelpText = "Where to get the ggpk from (Bundle or Steam)")]
        public GgpkSource GgpkSource { get; set; }

        [Option('p', "ggpkPath", Required = true, HelpText = "The path to the ggpk (Bundle) or to index.bin (Steam)")]
        public string GgpkFilePath { get; set; } = null!;

        [Value(0, MetaName = "InputDatFiles", HelpText = "A space-separated list of paths to .dat files in the ggpk to convert.")]
        public IEnumerable<string> DatFiles { get; set; } = null!;

        [Option('f', "format", Required = false, Default = "csv", HelpText = "The file format for the output.")]
        public string Format { get; set; } = "csv";

        [Option('o', "outDirectory", Required = false, Default = "./",
            HelpText = "The directory to write output file in.")]
        public string OutDirectory { get; set; } = "./";

    }

    private static GGPKContainer GgpkContainer;

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                if (o.Verbose)
                {
                    Console.WriteLine($"Verbose output enabled. Current Arguments: -v {o.Verbose}");
                    Console.WriteLine("Quick Start Example! App is in Verbose mode!");
                }
                else
                {
                    Console.WriteLine($"Current Arguments: -v {o.Verbose}");
                    Console.WriteLine("Quick Start Example!");
                }
                GgpkContainer = new GGPKContainer(
                    o.GgpkFilePath, 
                    o.GgpkSource == GgpkSource.Bundle,
                    o.GgpkSource == GgpkSource.Steam);

                var ioTasks = new List<Task>(o.DatFiles.Count());
                Console.WriteLine($"CWD: {Directory.GetCurrentDirectory()}");
                var outDir = new FileInfo(o.OutDirectory).FullName;
                Directory.CreateDirectory(outDir);
                Console.WriteLine($"Directory Path: {outDir}");
                foreach (var path in o.DatFiles) {

                    Console.WriteLine($"Searching for record: {path}");
                    var datContainer = GetDat(path);
                    Console.WriteLine($"Found record: {datContainer.Name}");
                    var outFileName = Path.ChangeExtension(datContainer.Name, "csv");
                    ioTasks.Add(File.WriteAllTextAsync($"{outDir}/{outFileName}", datContainer.ToCsv()));
                }

                Task.WaitAll(ioTasks.ToArray());
            });

    }

    static FileRecord? GetFileByPath(string path)
        => GgpkContainer.Index.FindFiles[FNV1a64Hash(path)];
    
    static DatContainer GetDat(string path)
    {
        var node = GgpkContainer.FindRecord($"Data/{path}")
            ?? throw new FileNotFoundException($"Could not find specified dat file in bundle: {path}");

        if (node is IFileRecord fr) {
            Console.WriteLine(fr.DataFormat);
            Console.WriteLine(node.Name);

            if (fr.DataFormat == IFileRecord.DataFormats.Dat) {
                return new DatContainer(fr.ReadFileContent(GgpkContainer.fileStream), node.Name, true);
            }
        }

        throw new ArgumentException("Provided path was not a Dat file.", path);
    }
}
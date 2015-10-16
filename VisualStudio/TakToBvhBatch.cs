using MotiveTools;
using NMotive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

class TakToBvhBatch
{
    [STAThreadAttribute]
    public static void Main( string[] args )
    {
        // Print Dependencies.
        //DependenciesDisplayer.PrintDependencies(typeof(TakToBvhBatch).Assembly);
        string logFilePath = Path.GetFullPath(@".\TakToBvhBatch.log");

        using (StreamWriter loger = new StreamWriter(logFilePath))
        {
            // Select Directory containing Takes.
            string takeFileDirectory = SelectTakesRootDirectory();

            if (Directory.Exists(takeFileDirectory))
            {
                // Get a list of take processors. Each take will be run each process.
                IEnumerable<TakeProcessor> takeProcessors = CreateTakeProcessors();

                // After a take is processed it will exported to BVH format.
                BVHExporter exporter = CreateBVHExporter();

                // Process each take.
                foreach (Take take in TakeFilesInDirectory(takeFileDirectory, loger))
                {
                    Log(loger, string.Format("\t==> Processing take..."));
                    // We will export the take to BVH only if all processors succeed.
                    bool allProcessesSucceeded = true;
                    foreach (TakeProcessor processor in takeProcessors)
                    {
                        Result processResult;
                        try
                        {
                            processResult = processor.Process(take);
                        }
                        catch(NMotiveException e)
                        {
                            Log(loger, String.Format("\tError : process {0} failed : {1}", processor.Name, e.Message));
                            allProcessesSucceeded = false;
                            break;
                        }

                        if (!processResult.Success)
                        {
                            Log(loger, String.Format("\tError : process {0} failed : {1}", processor.Name, processResult.Message));
                            allProcessesSucceeded = false;
                            break;
                        }
                    }
                    if (allProcessesSucceeded)
                    {
                        // The processed take in BVH format will be saved in a file with the same name as the take file, but with a .bvh extension.
                        string exportedTakeFileName = ChangeFileNameExtension(take.FileName, exporter.Extension);
                        string exportedTakeFilePath = Path.GetFullPath(Path.Combine(takeFileDirectory, exportedTakeFileName));
                        // Export to BVH. The third parameter is boolean indicating whether or not to overwrite an existing file if found. 
                        // We are setting overwrite existing file to true.
                        Result exportResult = exporter.Export(take, exportedTakeFilePath, true);
                        if (exportResult.Success)
                        {

                            Log(loger, String.Format("\t==> Take exported successfully to file {0}", exportedTakeFilePath));
                        }
                        else
                        {
                            Log(loger, String.Format("\tError : cannot export file {0}: {1}", exportedTakeFilePath, exportResult.Message));
                        }
                    }
                    // Takes can use a lot of memory. Might not want to wait around for the garbage collector to do its clean up.
                    take.Dispose();
                    Log(loger, "");
                }
            }
            else
            {
                Log(loger, "No Directory selected ! Abort...");
            }
            Console.WriteLine("\nBatch finished\nSee {0} for details", logFilePath);
            Console.WriteLine("\nAppuyer sur Entrée pour quitter...");
            Console.ReadLine();
        }
    }

    #region Private Methods
    static void Log(StreamWriter pWriter, string message)
    {
        pWriter.WriteLine(message);
        Console.WriteLine(message);
    }

    static IEnumerable<TakeProcessor> CreateTakeProcessors()
    {
        List<TakeProcessor> takeProcessors = new List<TakeProcessor>();
       
        // Trajectorize...
        Trajectorizer traj = new Trajectorizer();
        takeProcessors.Add( traj );

        // Trim tails...
        TrimTails tailTrimming = new TrimTails();
        tailTrimming.Automatic = true;
        tailTrimming.LeadingTrimSize = 4;
        tailTrimming.TrailingTrimSize = 4;
        takeProcessors.Add( tailTrimming );

        // Fill gaps...
        AdaptativeFillGaps gapFilling = new AdaptativeFillGaps();
        gapFilling.InterpolationMode = InterpolationType.ModelBased;
        gapFilling.MaxGapFillWidth = 6;
        takeProcessors.Add(gapFilling);

        // Filter...
        Filter filtering = new Filter();
        filtering.CutOffFrequency = 8; // Hz
        takeProcessors.Add( filtering );

        return takeProcessors;
    }

    static IEnumerable<Take> TakeFilesInDirectory( string takeFileDirectory, StreamWriter pLoger )
    {
        foreach (string takeFile in Directory.EnumerateFiles(takeFileDirectory, "*.tak", SearchOption.AllDirectories))
        {
            string takeFileFullPath = Path.GetFullPath( takeFile );
            Take t = null;
            try
            {
                t = new Take( takeFileFullPath );
            }
            catch ( NMotiveException nmotiveEx )
            {
                Log(pLoger, String.Format("Error : failed to load take {0}: {1}", takeFileFullPath, nmotiveEx.Message));
                continue;
            }
            Log(pLoger, String.Format("==> Take {0} loaded successfully", takeFileFullPath));
            yield return t;
        }
    }

    static string ChangeFileNameExtension( string fileName, string newExtension )
    {
        var dotString = newExtension.StartsWith( "." ) ? "" : ".";
        var nameWithoutExtension = Path.GetFileNameWithoutExtension( fileName );
        return string.Format( "{0}{1}{2}", nameWithoutExtension, dotString, newExtension );
    }

    static string SelectTakesRootDirectory()
    {
        string dir = "";
        FolderBrowserDialog folderDiag = new FolderBrowserDialog();
        folderDiag.Description = "Select directory containing the .tak files (sub-directories will be included)";
        folderDiag.ShowNewFolderButton = false;
        folderDiag.RootFolder = Environment.SpecialFolder.MyComputer;
        if (DialogResult.OK == folderDiag.ShowDialog())
        {
            dir = folderDiag.SelectedPath;
        }
        return dir;
    }

    static BVHExporter CreateBVHExporter()
    {
        BVHExporter exporter = new BVHExporter();
        exporter.ExcludeFingers = false;
        exporter.ExcludeToes = false;
        exporter.HandsDownward = false; 
        exporter.MotionBuilderNames = true;
        exporter.SingleJointTorso = false;
        exporter.Units = LengthUnits.Units_Centimeters;
        return exporter;
    }

    #endregion
}

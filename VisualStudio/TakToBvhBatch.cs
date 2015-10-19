using MotiveTools;
using NMotive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;

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
			int loadingErros = 0;
			int bvhExportErrors = 0;
			int videoExportErrors = 0;

            // Select Directory containing Takes.
            string takeFileDirectory = SelectTakesRootDirectory();
            if (Directory.Exists(takeFileDirectory))
            {
                // Create take processors : to trajectorize, fill gaps, filter.
                IEnumerable<TakeProcessor> takeProcessors = CreateTakeProcessors();
                
				// Exporters
                BVHExporter bvhExporter = CreateBVHExporter();
				VideoExporter videoExporter = new VideoExporter();

				// Process each take.
				IEnumerable<string> takeFiles = Directory.EnumerateFiles(takeFileDirectory, "*.tak", SearchOption.AllDirectories);
				foreach (string takeFile in takeFiles) 
				{
					string takeFileFullPath = Path.GetFullPath(takeFile);
					Log(loger, string.Format("==> Loading take : {0}", takeFileFullPath));
					try
					{
						Take take = new Take(takeFileFullPath);
						Log(loger, "\t==> Take loaded successfully !");
						bvhExportErrors += ExportBVHs(take, takeProcessors, bvhExporter, loger);
						videoExportErrors += ExportVideo(take, videoExporter, loger);
						// Takes can use a lot of memory. Might not want to wait around for the garbage collector to do its clean up.
						take.Dispose();
					}
					catch (Exception e)
					{
						Log(loger, String.Format("\tError : failed to load take {0}: {1}", takeFileFullPath, e.Message));
						++loadingErros;
					}
					Log(loger, "\n================================================================================");
                }
            }
            else
            {
                Log(loger, "No Directory selected ! Abort...");
            }

			int totalErrors = loadingErros + bvhExportErrors + videoExportErrors;
			if (totalErrors > 0)
			{
				Log(loger, "==> Batch finished !");
				Log(loger, string.Format("{0} Take Load Error(s)", loadingErros));
				Log(loger, string.Format("{0} BVH Export Error(s)", bvhExportErrors));
				Log(loger, string.Format("{0} Video Export Error(s)", videoExportErrors));
			}
			else
			{
				Log(loger, "==> Batch finished with success (no errors) !");
			}

			Console.WriteLine("See {0} for details", logFilePath);
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

	static int ExportBVHs(Take pTake, IEnumerable<TakeProcessor> pTakeProcessors, BVHExporter pExporter, StreamWriter pLoger)
	{
		int nbErrors = 0;
		Log(pLoger, "==> Exporting BVH files (1 per skeleton)... !");
		IEnumerable<Node> activeSkels = pTake.Scene.AllSkeletons().Where(skel => skel.Active);
		if (activeSkels.Count() > 0)
		{
			// We will export BVHs only if all processors succeed.
			bool allProcessesSucceeded = true;
			foreach (TakeProcessor processor in pTakeProcessors)
			{
				try
				{
					Result processResult = processor.Process(pTake);
					if (!processResult.Success)
					{
						Log(pLoger, String.Format("\tError : process {0} failed : {1}", processor.Name, processResult.Message));
						allProcessesSucceeded = false;
						++nbErrors;
						break;
					}
				}
				catch (Exception e)
				{
					Log(pLoger, String.Format("\tError : process {0} failed : {1}", processor.Name, e.Message));
					allProcessesSucceeded = false;
					++nbErrors;
					break;
				}
			}

			if (allProcessesSucceeded)
			{
				string takeDir = Path.GetDirectoryName(pTake.FileName);
				// Exporting all active skeletons to a bvh file.
				for (int i = 0; i < activeSkels.Count(); ++i)
				{
					Node skeleton = activeSkels.ElementAt(i);

					string bvhFilePath = Path.GetFileNameWithoutExtension(pTake.FileName) + "_" + skeleton.Name + "." + pExporter.Extension;
					bvhFilePath = Path.GetFullPath(Path.Combine(takeDir, bvhFilePath));
					pExporter.SkeletonName = skeleton.Name;
					try
					{
						Result exportResult = pExporter.Export(pTake, bvhFilePath, true);
						if (exportResult.Success)
						{
							Log(pLoger, String.Format("\t==> BVH {0}/{1} exported successfully to file {2}", i+1, activeSkels.Count(), bvhFilePath));
						}
						else
						{
							Log(pLoger, String.Format("\tError : cannot export BVH {0}/{1} : {2} => {3}", i+1, activeSkels.Count(), bvhFilePath, exportResult.Message));
							++nbErrors;
						}
					}
					catch (Exception e)
					{
						Log(pLoger, String.Format("\tError : cannot export BVH {0}/{1} : {2} => {3}", i+1, activeSkels.Count(), bvhFilePath, e.Message));
						++nbErrors;
					}
				}
			}
		}
		else
		{
			Log(pLoger, "\t==> No active skeleton found !");
		}
		return nbErrors;
	}

	static int ExportVideo(Take pTake, VideoExporter pExporter, StreamWriter pLoger)
	{
		int nbErrors = 0;
		Log(pLoger, "==> Exporting video...");
		if (pTake.IsVideoDataStale())
		{
			string takeDir = Path.GetDirectoryName(pTake.FileName);
			string videoFilePath = Path.GetFileNameWithoutExtension(pTake.FileName) + "." + pExporter.Extension;
			videoFilePath = Path.GetFullPath(Path.Combine(takeDir, videoFilePath));
			try
			{
				Result exportResult = pExporter.Export(pTake, videoFilePath, true);
				if (exportResult.Success)
				{
					Log(pLoger, String.Format("\t==> Video exported successfully to file {0}", videoFilePath));
				}
				else
				{
					Log(pLoger, String.Format("\tError : cannot export Video {0} => {1}", videoFilePath, exportResult.Message));
					nbErrors = 1;
				}
			}
			catch (Exception e)
			{
				Log(pLoger, String.Format("\tError : cannot export Video {0} => {1}", videoFilePath, e.Message));
				nbErrors = 1;
			}
		}
		else
		{
			Log(pLoger, "\t==> No video found !");
		}
		return nbErrors;
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

    static string SelectTakesRootDirectory()
    {
        string dir = "";
        FolderBrowserDialog folderDiag = new FolderBrowserDialog();
        folderDiag.Description = "Select directory containing the .tak files (sub-directories are included)";
        folderDiag.ShowNewFolderButton = false;
        folderDiag.RootFolder = Environment.SpecialFolder.MyComputer;
        if (DialogResult.OK == folderDiag.ShowDialog())
        {
            dir = folderDiag.SelectedPath;
        }
        return dir;
    }

    #endregion
}

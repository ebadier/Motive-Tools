# NMotiveTools
C# scripts relative to the [NMotive API](http://wiki.optitrack.com/index.php?title=Motive_Batch_Processor)

# TakToBvhBatch
This software allows you to automatically export the existing takes in a given directory and its sub-directories.
All the takes will be processed (automatic reconstruction, filling gaps, filtering trajectories) and exported as BVH files.
For each take, there will be as BVH files exported as active skeletons.
If a Reference camera is present in the take, a video file will be generated as well.

Usage:
- Double-click on TakToBvhBatch.exe
- Choose a directory containing the *.tak files (all the sub-directories will be evaluated)
- Check TakToBvhBatch.log to see if some errors occured during the process.

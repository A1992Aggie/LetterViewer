using LetterViewer.Models;

namespace LetterViewer.Services;

public class FileOperationService
{
    private readonly UndoService _undoService;

    public FileOperationService(UndoService undoService)
    {
        _undoService = undoService;
    }

    public string RenameFile(string filePath, string newFileName)
    {
        string directory = Path.GetDirectoryName(filePath)!;
        string newPath = Path.Combine(directory, newFileName);

        if (File.Exists(newPath))
            throw new IOException($"A file named '{newFileName}' already exists.");

        File.Move(filePath, newPath);
        _undoService.RegisterAction(new RenameUndoAction(filePath, newPath));
        return newPath;
    }

    public string MoveFile(string filePath, string destDirectory)
    {
        string fileName = Path.GetFileName(filePath);
        string destPath = Path.Combine(destDirectory, fileName);

        if (File.Exists(destPath))
            throw new IOException($"A file named '{fileName}' already exists in the destination.");

        if (!Directory.Exists(destDirectory))
            Directory.CreateDirectory(destDirectory);

        File.Move(filePath, destPath);
        _undoService.RegisterAction(new MoveUndoAction(filePath, destPath));
        return destPath;
    }

    public void MoveToRecycleBin(string filePath)
    {
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
            filePath,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
    }

    public List<(string OldPath, string NewPath)> BatchRename(
        string[] filePaths, BatchRenamePattern pattern)
    {
        var renames = new List<(string OldPath, string NewPath)>();
        var dateTakenCache = new Dictionary<string, DateTime?>();

        // Pre-load date taken if needed
        if (pattern.UseDatePrefix)
        {
            foreach (var path in filePaths)
            {
                dateTakenCache[path] = Utilities.ExifHelper.GetDateTaken(path);
            }
        }

        for (int i = 0; i < filePaths.Length; i++)
        {
            string oldPath = filePaths[i];
            string oldFileName = Path.GetFileName(oldPath);
            DateTime? dateTaken = pattern.UseDatePrefix ? dateTakenCache.GetValueOrDefault(oldPath) : null;

            string newFileName = pattern.Apply(oldFileName, i, dateTaken);
            string directory = Path.GetDirectoryName(oldPath)!;
            string newPath = Path.Combine(directory, newFileName);

            if (oldPath != newPath && !File.Exists(newPath))
            {
                File.Move(oldPath, newPath);
                renames.Add((oldPath, newPath));
            }
        }

        if (renames.Count > 0)
        {
            _undoService.RegisterAction(new BatchRenameUndoAction(renames));
        }

        return renames;
    }
}

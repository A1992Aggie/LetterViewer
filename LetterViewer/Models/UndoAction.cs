namespace LetterViewer.Models;

public abstract class UndoAction
{
    public abstract string Description { get; }
    public abstract void Undo();
}

public class RenameUndoAction : UndoAction
{
    private readonly string _oldPath;
    private readonly string _newPath;

    public RenameUndoAction(string oldPath, string newPath)
    {
        _oldPath = oldPath;
        _newPath = newPath;
    }

    public override string Description => $"Rename: {Path.GetFileName(_newPath)} → {Path.GetFileName(_oldPath)}";

    public override void Undo()
    {
        if (File.Exists(_newPath))
            File.Move(_newPath, _oldPath);
    }
}

public class MoveUndoAction : UndoAction
{
    private readonly string _sourcePath;
    private readonly string _destPath;

    public MoveUndoAction(string sourcePath, string destPath)
    {
        _sourcePath = sourcePath;
        _destPath = destPath;
    }

    public override string Description => $"Move: {Path.GetFileName(_destPath)} back to {Path.GetDirectoryName(_sourcePath)}";

    public override void Undo()
    {
        if (File.Exists(_destPath))
            File.Move(_destPath, _sourcePath);
    }
}

public class BatchRenameUndoAction : UndoAction
{
    private readonly List<(string OldPath, string NewPath)> _renames;

    public BatchRenameUndoAction(List<(string OldPath, string NewPath)> renames)
    {
        _renames = renames;
    }

    public override string Description => $"Batch rename: {_renames.Count} files";

    public override void Undo()
    {
        // Undo in reverse order
        for (int i = _renames.Count - 1; i >= 0; i--)
        {
            var (oldPath, newPath) = _renames[i];
            if (File.Exists(newPath))
                File.Move(newPath, oldPath);
        }
    }
}

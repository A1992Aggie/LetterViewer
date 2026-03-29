using LetterViewer.Models;

namespace LetterViewer.Services;

public class UndoService
{
    private readonly Stack<UndoAction> _undoStack = new();
    public event Action? UndoStackChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public int Count => _undoStack.Count;

    public string? LastActionDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    public void RegisterAction(UndoAction action)
    {
        _undoStack.Push(action);
        UndoStackChanged?.Invoke();
    }

    public bool Undo()
    {
        if (!CanUndo) return false;

        var action = _undoStack.Pop();
        try
        {
            action.Undo();
            UndoStackChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to undo: {ex.Message}", "Undo Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            UndoStackChanged?.Invoke();
            return false;
        }
    }
}

namespace LetterViewer.Models;

public class BatchRenamePattern
{
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public bool AddSequenceNumber { get; set; }
    public int SequenceStart { get; set; } = 1;
    public int SequencePadding { get; set; } = 3;
    public string? FindText { get; set; }
    public string? ReplaceText { get; set; }
    public bool UseRegex { get; set; }
    public bool UseDatePrefix { get; set; }

    public string Apply(string fileName, int sequenceIndex, DateTime? dateTaken)
    {
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);

        // Find/Replace
        if (!string.IsNullOrEmpty(FindText))
        {
            if (UseRegex)
            {
                try
                {
                    nameWithoutExt = System.Text.RegularExpressions.Regex.Replace(
                        nameWithoutExt, FindText, ReplaceText ?? "");
                }
                catch { }
            }
            else
            {
                nameWithoutExt = nameWithoutExt.Replace(FindText, ReplaceText ?? "");
            }
        }

        // Date prefix
        if (UseDatePrefix && dateTaken.HasValue)
        {
            nameWithoutExt = dateTaken.Value.ToString("yyyyMMdd") + "_" + nameWithoutExt;
        }

        // Prefix
        if (!string.IsNullOrEmpty(Prefix))
        {
            nameWithoutExt = Prefix + nameWithoutExt;
        }

        // Suffix
        if (!string.IsNullOrEmpty(Suffix))
        {
            nameWithoutExt = nameWithoutExt + Suffix;
        }

        // Sequence number
        if (AddSequenceNumber)
        {
            int num = SequenceStart + sequenceIndex;
            string seq = num.ToString().PadLeft(SequencePadding, '0');
            nameWithoutExt = nameWithoutExt + "_" + seq;
        }

        return nameWithoutExt + ext;
    }
}

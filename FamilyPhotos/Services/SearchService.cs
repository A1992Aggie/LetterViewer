using FamilyPhotos.Models;

namespace FamilyPhotos.Services;

public class SearchService
{
    public List<PhotoItem> Search(IEnumerable<PhotoItem> photos, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return photos.ToList();

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return photos.Where(p => MatchesAllTerms(p, terms)).ToList();
    }

    private static bool MatchesAllTerms(PhotoItem photo, string[] terms)
    {
        return terms.All(term => MatchesTerm(photo, term));
    }

    private static bool MatchesTerm(PhotoItem photo, string term)
    {
        // Search filename
        if (photo.DriveItem.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            return true;

        if (photo.Metadata == null) return false;

        // Search face tags
        if (photo.Metadata.FaceTags.Any(t =>
            t.PersonName.Contains(term, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Search notes
        if (!string.IsNullOrEmpty(photo.Metadata.Notes) &&
            photo.Metadata.Notes.Contains(term, StringComparison.OrdinalIgnoreCase))
            return true;

        // Search date
        if (!string.IsNullOrEmpty(photo.Metadata.ExtractedDate) &&
            photo.Metadata.ExtractedDate.Contains(term, StringComparison.OrdinalIgnoreCase))
            return true;

        // Search audio recorded-by
        if (photo.Metadata.AudioRecordings.Any(a =>
            a.RecordedBy.Contains(term, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}

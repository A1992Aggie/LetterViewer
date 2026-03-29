using Tesseract;

namespace LetterViewer.Services;

public static class OcrService
{
    public static string ExtractText(string imagePath, string tessDataPath = "./tessdata")
    {
        if (!Directory.Exists(tessDataPath))
        {
            return "Error: tessdata folder not found. Please download eng.traineddata from " +
                   "https://github.com/tesseract-ocr/tessdata and place it in a 'tessdata' folder " +
                   "next to the application.";
        }

        try
        {
            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            return page.GetText();
        }
        catch (Exception ex)
        {
            return $"OCR Error: {ex.Message}";
        }
    }

    public static async Task<string> ExtractTextAsync(string imagePath, string tessDataPath = "./tessdata")
    {
        return await Task.Run(() => ExtractText(imagePath, tessDataPath));
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public class TextToBitmapWithFirstImage
{
	public static void CreateBitmapFromTextFile(string imagePath, string textFilePath, string outputPath, int dpi = 300)
	{
		string rawText = System.IO.File.ReadAllText(textFilePath);
		CreateBitmapFromText(imagePath, rawText, outputPath, dpi);
	}

	public static void CreateBitmapFromText(string imagePath, string text, string outputPath, int dpi = 300)
	{
		text = FilterText(text);

		if (string.IsNullOrWhiteSpace(text))
		{
			throw new Exception("Text is empty after filtering.");
		}

		var combinedBitmap = CreateCombinedBitmap(text, imagePath, dpi);

		// Get JPEG encoder with quality settings
		var encoder = GetEncoder(ImageFormat.Jpeg);
		if (encoder == null)
		{
			throw new Exception("JPEG encoder not found.");
		}

		var encoderParams = new EncoderParameters(2);
		encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
		encoderParams.Param[1] = new EncoderParameter(Encoder.ColorDepth, 24L);

		combinedBitmap.Save(outputPath, encoder, encoderParams);

		Console.WriteLine($"Saved combined bitmap: {outputPath}");
		Console.WriteLine($"  Dimensions: {combinedBitmap.Width}x{combinedBitmap.Height} pixels at {dpi} DPI");

		combinedBitmap.Dispose();
	}

	private static string FilterText(string text)
	{
		var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
		bool ignoring = false;
		var filteredLines = new System.Collections.Generic.List<string>();

		foreach (var line in lines)
		{
			var trimmedLine = line.TrimStart();

			if (trimmedLine.StartsWith("-->ignore start", StringComparison.OrdinalIgnoreCase))
			{
				ignoring = true;
				continue;
			}

			if (trimmedLine.StartsWith("-->ignore end", StringComparison.OrdinalIgnoreCase))
			{
				ignoring = false;
				continue;
			}

			if (!ignoring && !trimmedLine.StartsWith("-->ignore", StringComparison.OrdinalIgnoreCase))
			{
				filteredLines.Add(line);
			}
		}

		return string.Join(Environment.NewLine, filteredLines);
	}
	
	public static Bitmap CreateCombinedBitmap(string text, string imagePath, int dpi = 300)
	{
		// Target 16:9 aspect ratio at 4K resolution: 3840 x 2160 pixels
		int totalWidth = 3840;
		int totalHeight = 2160;
		
		// Load the first page image
		Bitmap firstPageImage;
		using (var tempImage = Image.FromFile(imagePath))
		{
			firstPageImage = new Bitmap(tempImage);
		}
		
		// Calculate how much width to allocate for the image
		// We'll aim for roughly 35% of width for image, 65% for text
		float imageWidthRatio = 0.35f;
		int imageWidth = (int)(totalWidth * imageWidthRatio);
		int textWidth = totalWidth - imageWidth;
		
		// Scale the image to fit the allocated height while maintaining aspect ratio
		float imageAspect = (float)firstPageImage.Width / firstPageImage.Height;
		int scaledImageHeight = totalHeight;
		int scaledImageWidth = (int)(scaledImageHeight * imageAspect);
		
		// If scaled image is wider than allocated space, scale by width instead
		if (scaledImageWidth > imageWidth)
		{
			scaledImageWidth = imageWidth;
			scaledImageHeight = (int)(scaledImageWidth / imageAspect);
		}
		
		Console.WriteLine($"  Image scaled to: {scaledImageWidth}x{scaledImageHeight} pixels");
		
		// Create the text bitmap with narrower width
		var textBitmap = CreateTextBitmap(text, textWidth, totalHeight, dpi);
		
		// Create final combined bitmap
		var combinedBitmap = new Bitmap(totalWidth, totalHeight);
		combinedBitmap.SetResolution(dpi, dpi);
		
		using (var graphics = Graphics.FromImage(combinedBitmap))
		{
			graphics.Clear(Color.Black);
			graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			
			// Draw text on the left side
			graphics.DrawImage(textBitmap, 0, 0, textWidth, totalHeight);
			
			// Draw image on the right side, centered vertically
			int imageY = (totalHeight - scaledImageHeight) / 2;
			int imageX = textWidth + ((imageWidth - scaledImageWidth) / 2); // Center horizontally in allocated space
			graphics.DrawImage(firstPageImage, imageX, imageY, scaledImageWidth, scaledImageHeight);
			
			// Draw a subtle separator line between text and image
			using (var pen = new Pen(Color.DarkGray, 2))
			{
				graphics.DrawLine(pen, textWidth, 0, textWidth, totalHeight);
			}
		}
		
		textBitmap.Dispose();
		firstPageImage.Dispose();
		
		return combinedBitmap;
	}
	
	private static Bitmap CreateTextBitmap(string text, int width, int height, int dpi)
	{
		var bitmap = new Bitmap(width, height);
		bitmap.SetResolution(dpi, dpi);
		
		using (var graphics = Graphics.FromImage(bitmap))
		{
			// Set high quality rendering
			graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
			graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			
			// Fill with black background
			graphics.Clear(Color.Black);
			
		// Start with smaller margins
		float marginInches = 0.05f;
		const float minReadableFontSize = 6f;
		float marginPixels = marginInches * dpi;
		var textRect = new RectangleF(
			marginPixels, 
			marginPixels, 
			width - (2 * marginPixels), 
			height - (2 * marginPixels)
		);
		
		// Process text (remove line breaks if needed)
		string processedText = text;
		using (var testFont = new Font("Arial", minReadableFontSize, FontStyle.Bold, GraphicsUnit.Point))
		{
			var measuredSize = graphics.MeasureString(text, testFont, (int)textRect.Width);
			
			if (measuredSize.Height > textRect.Height)
			{
				// Text doesn't fit even at minimum size - remove line breaks but preserve page headings and /r markers
				Console.WriteLine($"  Text too long ({measuredSize.Height:F0}px > {textRect.Height:F0}px at {minReadableFontSize}pt)");
				Console.WriteLine("  Removing line breaks (preserving page headings and /r markers) to fit text on page...");
				
				// First, replace literal "/r" with a unique placeholder
				const string lineBreakMarker = "###LINEBREAK###";
				string workingText = text.Replace("/r", lineBreakMarker);
				
				// Split into lines and process
				var lines = workingText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
				var rebuiltText = new System.Text.StringBuilder();
				string currentParagraph = "";
				
				for (int i = 0; i < lines.Length; i++)
				{
					var line = lines[i]; 
					
					// Check if this line contains our line break marker
					if (line.Contains(lineBreakMarker))
					{
						// Split by the marker and process each part
						var parts = line.Split(new[] { lineBreakMarker }, StringSplitOptions.None);
						for (int j = 0; j < parts.Length; j++)
						{
							var part = parts[j].Trim();
							
							// Add content if not empty
							if (!string.IsNullOrWhiteSpace(part))
							{
								// Add space if needed before appending
								if (!string.IsNullOrWhiteSpace(currentParagraph) && 
								    !currentParagraph.EndsWith(" ") && 
								    !part.StartsWith(" "))
								{
									currentParagraph += " ";
								}
								currentParagraph += part;
							}
							
							// Add line break after each part except the last
							// This preserves multiple consecutive /r markers as multiple line breaks
							if (j < parts.Length - 1)
							{
								if (!string.IsNullOrWhiteSpace(currentParagraph))
								{
									rebuiltText.Append(currentParagraph);
									currentParagraph = "";
								}
								rebuiltText.Append("\n");
							}
						}
					}
					// Check if this is a page heading
					else if (line.StartsWith("=== PAGE:") && line.EndsWith("==="))
					{
						// Save any accumulated paragraph
						if (!string.IsNullOrWhiteSpace(currentParagraph))
						{
							rebuiltText.Append(currentParagraph.Trim());
							rebuiltText.Append("\n\n");
							currentParagraph = "";
						}
						
						// Add the heading with line breaks
						rebuiltText.Append(line);
						rebuiltText.Append("\n");
					}
					else if (string.IsNullOrWhiteSpace(line))
					{
						// Empty line - just add a space to current paragraph if it doesn't already end with one
						if (!string.IsNullOrWhiteSpace(currentParagraph) && !currentParagraph.EndsWith(" "))
						{
							currentParagraph += " ";
						}
					}
					else
					{
						// Regular content line - add to current paragraph
						var trimmedLine = line.Trim();
						if (!string.IsNullOrWhiteSpace(currentParagraph) && 
						    !currentParagraph.EndsWith(" ") && 
						    !trimmedLine.StartsWith(" "))
						{
							currentParagraph += " ";
						}
						currentParagraph += trimmedLine;
					}
				}
				
				// Add any remaining paragraph
				if (!string.IsNullOrWhiteSpace(currentParagraph))
				{
					rebuiltText.Append(currentParagraph.Trim());
				}
				
				processedText = rebuiltText.ToString();
				
				// Clean up multiple spaces
				while (processedText.Contains("  "))
				{
					processedText = processedText.Replace("  ", " ");
				}
			}
		}
		
		// Try to find the largest font size that fits with current margins
		float fontSize = 72f;
		Font? font = null;
		
		// First attempt with standard margins
		for (fontSize = 72f; fontSize >= minReadableFontSize; fontSize -= 0.5f)
		{
			font?.Dispose();
			font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Point);
			
			var measuredSize = graphics.MeasureString(processedText, font, (int)textRect.Width);
			
			if (measuredSize.Height <= textRect.Height)
			{
				break;
			}
		}
		
		// If font size is at minimum and still doesn't fit, reduce margins and try again
		if (fontSize <= minReadableFontSize)
		{
			font?.Dispose();
			
			var testFont = new Font("Arial", minReadableFontSize, FontStyle.Bold, GraphicsUnit.Point);
			var measuredSize = graphics.MeasureString(processedText, testFont, (int)textRect.Width);
			
			if (measuredSize.Height > textRect.Height)
			{
				Console.WriteLine($"  Text doesn't fit with standard margins, reducing to 0.05 inches");
				marginInches = 0.05f;
				marginPixels = marginInches * dpi;
				textRect = new RectangleF(
					marginPixels, 
					marginPixels, 
					width - (2 * marginPixels), 
					height - (2 * marginPixels)
				);
				
				// Try font sizes again with reduced margins
				for (fontSize = 72f; fontSize >= minReadableFontSize; fontSize -= 0.5f)
				{
					font?.Dispose();
					font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Point);
					
					measuredSize = graphics.MeasureString(processedText, font, (int)textRect.Width);
					
					if (measuredSize.Height <= textRect.Height)
					{
						break;
					}
				}
			}
			
			testFont.Dispose();
		}
		
		if (font == null)
		{
			font = new Font("Arial", minReadableFontSize, FontStyle.Bold, GraphicsUnit.Point);
		}
		
		Console.WriteLine($"  Using font size: {fontSize:F1}pt for text section with {marginInches} inch margins");
		 
		// Draw the text
		using (var brush = new SolidBrush(Color.White))
		{
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Near,
				LineAlignment = StringAlignment.Near,
				Trimming = StringTrimming.Word
			};
			
			graphics.DrawString(processedText, font, brush, textRect, stringFormat);
		}
		
		font?.Dispose();
		}
		
		return bitmap;
	}
	
	private static ImageCodecInfo? GetEncoder(ImageFormat format)
	{
		var codecs = ImageCodecInfo.GetImageDecoders();
		foreach (var codec in codecs)
		{
			if (codec.FormatID == format.Guid)
			{
				return codec;
			}
		}
		return null;
	}
}

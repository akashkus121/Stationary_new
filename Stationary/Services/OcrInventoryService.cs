using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Tesseract;

namespace Stationary.Services
{
	public class OcrInventoryItem
	{
		public string ProductName { get; set; } = string.Empty;
		public int Quantity { get; set; }
	}

	public class OcrInventoryService
	{
		public List<OcrInventoryItem> ExtractItems(IFormFile file, string? tesseractDataPath, out string message)
		{
			message = string.Empty;
			var items = new List<OcrInventoryItem>();

			if (file == null || file.Length == 0)
			{
				message = "No file provided.";
				return items;
			}

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			string text = string.Empty;

			try
			{
				if (ext == ".pdf")
				{
					message = "PDF OCR is not enabled. Please upload an image (PNG/JPG) of the bill.";
					return items;
				}
				else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".tif" || ext == ".tiff")
				{
					using var ms = new MemoryStream();
					file.CopyTo(ms);
					ms.Position = 0;
					if (string.IsNullOrEmpty(tesseractDataPath))
					{
						message = "Tesseract data path is not configured.";
						return items;
					}
					using var engine = new TesseractEngine(tesseractDataPath, "eng", EngineMode.Default);
					using var img = Pix.LoadFromMemory(ms.ToArray());
					using var page = engine.Process(img);
					text = page.GetText();
				}
				else
				{
					message = "Unsupported file type. Upload PNG/JPG image.";
					return items;
				}

				items = ParseTextToItems(text);
				if (items.Count == 0)
				{
					message = "No recognizable items found in document.";
				}
			}
			catch (Exception ex)
			{
				message = "Error processing document: " + ex.Message;
			}

			return items;
		}

		private static List<OcrInventoryItem> ParseTextToItems(string text)
		{
			var results = new List<OcrInventoryItem>();
			if (string.IsNullOrWhiteSpace(text)) return results;

			// Simple heuristics: lines like "Name, Qty" or "Name Qty" (qty integer at end)
			var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
						 .Select(l => l.Trim())
						 .Where(l => l.Length > 0 && l.Any(char.IsLetter))
						 .ToList();

			foreach (var line in lines)
			{
				var cleaned = line.Replace("\t", " ").Replace("  ", " ");
				var parts = cleaned.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
								 .Select(p => p.Trim())
								 .ToArray();
				if (parts.Length >= 2 && int.TryParse(parts[^1], out int qtyCsv))
				{
					var nameCsv = string.Join(", ", parts.Take(parts.Length - 1));
					AddOrAggregate(results, nameCsv, qtyCsv);
					continue;
				}

				// Space-delimited: last token integer
				var spaceTokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (spaceTokens.Length >= 2 && int.TryParse(spaceTokens[^1], out int qtySp))
				{
					var nameSp = string.Join(' ', spaceTokens.Take(spaceTokens.Length - 1));
					AddOrAggregate(results, nameSp, qtySp);
				}
			}

			return results;
		}

		private static void AddOrAggregate(List<OcrInventoryItem> results, string name, int qty)
		{
			name = (name ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(name) || qty <= 0) return;
			var existing = results.FirstOrDefault(x => string.Equals(x.ProductName, name, StringComparison.OrdinalIgnoreCase));
			if (existing != null) existing.Quantity += qty;
			else results.Add(new OcrInventoryItem { ProductName = name, Quantity = qty });
		}
	}
}
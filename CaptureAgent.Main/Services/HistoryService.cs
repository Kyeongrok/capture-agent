using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using CaptureAgent.Main.Models;

namespace CaptureAgent.Main.Services;

public interface IHistoryService
{
    List<CaptureHistoryItem> LoadHistory(string? directory = null);
    void AddToHistory(string filePath);
    void ClearHistory(string? directory = null);
}

public class HistoryService : IHistoryService
{
    private readonly List<CaptureHistoryItem> _history = new();

    public List<CaptureHistoryItem> LoadHistory(string? directory = null)
    {
        _history.Clear();

        if (string.IsNullOrEmpty(directory))
        {
            directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DateTime.Now.ToString("yyyy-MM-dd"));
        }

        if (!Directory.Exists(directory))
        {
            return _history;
        }

        // 지원하는 이미지 형식
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
        var files = Directory.GetFiles(directory)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
            .OrderByDescending(f => new FileInfo(f).CreationTime)
            .ToList();

        foreach (var file in files)
        {
            var item = CaptureHistoryItem.FromFile(file);

            // 이미지 크기 정보 로드
            try
            {
                using var img = Image.FromFile(file);
                item.Width = img.Width;
                item.Height = img.Height;
            }
            catch
            {
                // 이미지 로드 실패 시 무시
            }

            _history.Add(item);
        }

        return _history;
    }

    public void AddToHistory(string filePath)
    {
        if (File.Exists(filePath))
        {
            var item = CaptureHistoryItem.FromFile(filePath);

            try
            {
                using var img = Image.FromFile(filePath);
                item.Width = img.Width;
                item.Height = img.Height;
            }
            catch
            {
                // 무시
            }

            _history.Insert(0, item);  // 최신 항목을 앞에 추가
        }
    }

    public void ClearHistory(string? directory = null)
    {
        _history.Clear();
    }
}

using System;
using System.IO;

namespace CaptureAgent.Main.Models;

public class CaptureHistoryItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CapturedTime { get; set; }
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public static CaptureHistoryItem FromFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        return new CaptureHistoryItem
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            CapturedTime = fileInfo.CreationTime,
            FileSizeBytes = fileInfo.Length,
            Width = 0,  // 이미지 로드 시 실제 값 설정
            Height = 0
        };
    }
}

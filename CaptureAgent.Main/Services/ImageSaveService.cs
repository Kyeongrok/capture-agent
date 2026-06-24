using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using CaptureAgent.Main.Models;

namespace CaptureAgent.Main.Services;

public interface IImageSaveService
{
    Task<string> SaveAsync(Bitmap bitmap, string directory, Models.ImageFormat format, string? filenamePrefix = null);
}

public class ImageSaveService : IImageSaveService
{
    public async Task<string> SaveAsync(Bitmap bitmap, string directory, Models.ImageFormat format, string? filenamePrefix = null)
    {
        // 디렉토리 생성 (없으면)
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 파일명 생성: 접두사_HH-mm-ss.확장자
        var timestamp = DateTime.Now.ToString("HH-mm-ss");
        filenamePrefix ??= "캡춰";

        string extension = format switch
        {
            Models.ImageFormat.PNG => "png",
            Models.ImageFormat.JPG => "jpg",
            Models.ImageFormat.BMP => "bmp",
            _ => "png"
        };

        string filename = $"{filenamePrefix}_{timestamp}.{extension}";
        string filePath = Path.Combine(directory, filename);

        // 같은 파일명이 있으면 숫자 추가
        int counter = 1;
        while (File.Exists(filePath))
        {
            filename = $"{filenamePrefix}_{timestamp}_{counter}.{extension}";
            filePath = Path.Combine(directory, filename);
            counter++;
        }

        // 비트맵 저장
        return await Task.Run(() =>
        {
            System.Drawing.Imaging.ImageFormat imageFormat = format switch
            {
                Models.ImageFormat.PNG => System.Drawing.Imaging.ImageFormat.Png,
                Models.ImageFormat.JPG => System.Drawing.Imaging.ImageFormat.Jpeg,
                Models.ImageFormat.BMP => System.Drawing.Imaging.ImageFormat.Bmp,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };

            bitmap.Save(filePath, imageFormat);
            return filePath;
        });
    }
}

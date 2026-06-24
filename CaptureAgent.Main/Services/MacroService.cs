using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CaptureAgent.Main.Models;
using CaptureAgent.Main.PInvoke;

namespace CaptureAgent.Main.Services;

public interface IMacroService
{
    Task ExecuteAsync(
        List<MacroStep> steps,
        int repeatCount,
        double defaultWaitSeconds,
        string saveDirectory,
        Models.ImageFormat format,
        IProgress<MacroProgress>? progress = null);

    void Cancel();
}

public class MacroProgress
{
    public int CurrentRepeat { get; set; }
    public int TotalRepeats { get; set; }
    public int CurrentStepIndex { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentStepName { get; set; } = string.Empty;
}

public class MacroService : IMacroService
{
    private readonly IMouseInteropService _mouseService;
    private readonly IScreenCaptureService _captureService;
    private readonly IImageSaveService _saveService;
    private CancellationTokenSource? _cancellationTokenSource;

    public MacroService(
        IMouseInteropService mouseService,
        IScreenCaptureService captureService,
        IImageSaveService saveService)
    {
        _mouseService = mouseService;
        _captureService = captureService;
        _saveService = saveService;
    }

    public async Task ExecuteAsync(
        List<MacroStep> steps,
        int repeatCount,
        double defaultWaitSeconds,
        string saveDirectory,
        ImageFormat format,
        IProgress<MacroProgress>? progress = null)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            for (int repeatIdx = 0; repeatIdx < repeatCount; repeatIdx++)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                for (int stepIdx = 0; stepIdx < steps.Count; stepIdx++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    var step = steps[stepIdx];
                    var macroProgress = new MacroProgress
                    {
                        CurrentRepeat = repeatIdx + 1,
                        TotalRepeats = repeatCount,
                        CurrentStepIndex = stepIdx + 1,
                        TotalSteps = steps.Count,
                        CurrentStepName = step.Title
                    };

                    progress?.Report(macroProgress);

                    if (step.Type == "Click")
                    {
                        if (step.X.HasValue && step.Y.HasValue)
                        {
                            _mouseService.ClickMouse(step.X.Value, step.Y.Value, 100);

                            // 대기 시간
                            double waitMs = (step.WaitSeconds > 0 ? step.WaitSeconds : defaultWaitSeconds) * 1000;
                            await Task.Delay((int)waitMs, _cancellationTokenSource.Token);
                        }
                    }
                    else if (step.Type == "Capture")
                    {
                        if (step.CaptureRegion.HasValue)
                        {
                            await _captureService.CaptureScreenToFileAsync(
                                Path.Combine(saveDirectory, $"macro_capture_{DateTime.Now:HH-mm-ss}.{GetExtension(format)}"),
                                step.CaptureRegion.Value,
                                format);
                        }
                    }
                }
            }
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    private static string GetExtension(Models.ImageFormat format) => format switch
    {
        Models.ImageFormat.PNG => "png",
        Models.ImageFormat.JPG => "jpg",
        Models.ImageFormat.BMP => "bmp",
        _ => "png"
    };
}

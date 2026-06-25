using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CaptureAgent.Forms.ViewModels;

/// <summary>
/// 캡처 영역 좌표 ViewModel. (StartX, StartY)를 앵커로 두고
/// 끝점(EndX/Y)과 크기(Width/Height)를 서로 일관되게 유지한다.
///
/// - Start를 바꾸면: 크기를 유지한 채 영역이 이동 (End 재계산)
/// - End를 바꾸면: Start를 유지한 채 크기 재계산
/// - 크기를 바꾸면: Start를 유지한 채 End 재계산
///
/// 모든 좌표는 물리 픽셀 기준이다.
/// </summary>
public partial class RegionViewModel : ObservableObject
{
    // 변경 핸들러의 상호 재진입을 막는 가드 (한 번의 사용자 편집이 여러 필드를 갱신할 때 안정화).
    private bool _isAdjusting;

    [ObservableProperty]
    private int startX;

    [ObservableProperty]
    private int startY;

    [ObservableProperty]
    private int endX;

    [ObservableProperty]
    private int endY;

    [ObservableProperty]
    private int width;

    [ObservableProperty]
    private int height;

    public RegionViewModel()
    {
        // 기본 영역 설정: 100x100 at (100, 100)
        StartX = 100;
        StartY = 100;
        Width = 100;
        Height = 100;
        EndX = 200;
        EndY = 200;
    }

    partial void OnStartXChanged(int value)
    {
        if (_isAdjusting) return;
        // 이동: 크기 유지, 끝점 재계산
        Adjust(() => EndX = value + Width);
    }

    partial void OnStartYChanged(int value)
    {
        if (_isAdjusting) return;
        Adjust(() => EndY = value + Height);
    }

    partial void OnEndXChanged(int value)
    {
        if (_isAdjusting) return;
        // 리사이즈: Start 유지, 크기 재계산
        Adjust(() => Width = Math.Max(1, value - StartX));
    }

    partial void OnEndYChanged(int value)
    {
        if (_isAdjusting) return;
        Adjust(() => Height = Math.Max(1, value - StartY));
    }

    partial void OnWidthChanged(int value)
    {
        if (_isAdjusting) return;
        // 크기 변경: Start 유지, 끝점 재계산
        Adjust(() => EndX = StartX + Math.Max(1, value));
    }

    partial void OnHeightChanged(int value)
    {
        if (_isAdjusting) return;
        Adjust(() => EndY = StartY + Math.Max(1, value));
    }

    private void Adjust(Action action)
    {
        _isAdjusting = true;
        try
        {
            action();
        }
        finally
        {
            _isAdjusting = false;
        }
    }

    /// <summary>
    /// Rectangle 객체로 영역 설정
    /// </summary>
    public void SetRegion(Rectangle region)
    {
        _isAdjusting = true;
        try
        {
            StartX = region.X;
            StartY = region.Y;
            Width = Math.Max(1, region.Width);
            Height = Math.Max(1, region.Height);
            EndX = StartX + Width;
            EndY = StartY + Height;
        }
        finally
        {
            _isAdjusting = false;
        }
    }

    /// <summary>
    /// 현재 영역을 Rectangle 객체로 반환
    /// </summary>
    public Rectangle GetRegion()
    {
        return new Rectangle(StartX, StartY, Math.Max(1, Width), Math.Max(1, Height));
    }
}

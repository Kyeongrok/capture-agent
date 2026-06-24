using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CaptureAgent.Forms.ViewModels;

/// <summary>
/// 좌표 업데이트 모드: 어떤 필드를 변경할 때 다른 필드들이 어떻게 반응할지 결정
/// </summary>
public enum CoordinateUpdateMode
{
    /// <summary>끝점(EndX/Y) 변경 시 Width/Height 계산</summary>
    EndPoint,

    /// <summary>Width/Height 변경 시 EndX/Y 계산</summary>
    Size
}

public partial class RegionViewModel : ObservableObject
{
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

    [ObservableProperty]
    private CoordinateUpdateMode updateMode = CoordinateUpdateMode.EndPoint;

    public RegionViewModel()
    {
        // 기본 영역 설정: 100x100 at (100, 100)
        StartX = 100;
        StartY = 100;
        EndX = 200;
        EndY = 200;
        Width = 100;
        Height = 100;
    }

    partial void OnStartXChanged(int value)
    {
        if (UpdateMode == CoordinateUpdateMode.EndPoint)
        {
            // EndX 유지, Width 자동 계산
            Width = Math.Max(1, EndX - value);
        }
        else if (UpdateMode == CoordinateUpdateMode.Size)
        {
            // Width 유지, EndX 자동 계산
            EndX = value + Width;
        }
    }

    partial void OnStartYChanged(int value)
    {
        if (UpdateMode == CoordinateUpdateMode.EndPoint)
        {
            // EndY 유지, Height 자동 계산
            Height = Math.Max(1, EndY - value);
        }
        else if (UpdateMode == CoordinateUpdateMode.Size)
        {
            // Height 유지, EndY 자동 계산
            EndY = value + Height;
        }
    }

    partial void OnEndXChanged(int value)
    {
        if (UpdateMode == CoordinateUpdateMode.EndPoint)
        {
            // Width 자동 계산
            Width = Math.Max(1, value - StartX);
        }
        else if (UpdateMode == CoordinateUpdateMode.Size)
        {
            // EndX를 StartX + Width로 제한
            if (value != StartX + Width)
            {
                EndX = StartX + Width;
            }
        }
    }

    partial void OnEndYChanged(int value)
    {
        if (UpdateMode == CoordinateUpdateMode.EndPoint)
        {
            // Height 자동 계산
            Height = Math.Max(1, value - StartY);
        }
        else if (UpdateMode == CoordinateUpdateMode.Size)
        {
            // EndY를 StartY + Height로 제한
            if (value != StartY + Height)
            {
                EndY = StartY + Height;
            }
        }
    }

    partial void OnWidthChanged(int value)
    {
        if (UpdateMode == CoordinateUpdateMode.Size)
        {
            // EndX 자동 계산
            EndX = StartX + Math.Max(1, value);
        }
        else if (UpdateMode == CoordinateUpdateMode.EndPoint)
        {
            // Width를 EndX - StartX로 제한
            if (value != EndX - StartX)
            {
                Width = Math.Max(1, EndX - StartX);
            }
        }
    }

    partial void OnHeightChanged(int value)
    {
        if (UpdateMode == CoordinateUpdateMode.Size)
        {
            // EndY 자동 계산
            EndY = StartY + Math.Max(1, value);
        }
        else if (UpdateMode == CoordinateUpdateMode.EndPoint)
        {
            // Height를 EndY - StartY로 제한
            if (value != EndY - StartY)
            {
                Height = Math.Max(1, EndY - StartY);
            }
        }
    }

    /// <summary>
    /// Rectangle 객체로 영역 설정
    /// </summary>
    public void SetRegion(Rectangle region)
    {
        StartX = region.X;
        StartY = region.Y;
        EndX = region.X + region.Width;
        EndY = region.Y + region.Height;
        Width = region.Width;
        Height = region.Height;
    }

    /// <summary>
    /// 현재 영역을 Rectangle 객체로 반환
    /// </summary>
    public Rectangle GetRegion()
    {
        return new Rectangle(StartX, StartY, Width, Height);
    }

    /// <summary>
    /// 마우스 좌표로 새 영역 시작
    /// </summary>
    public void StartDragFrom(int x, int y)
    {
        UpdateMode = CoordinateUpdateMode.Size;
        StartX = x;
        StartY = y;
        Width = 1;
        Height = 1;
    }

    /// <summary>
    /// 드래그 중 마우스 좌표 업데이트
    /// </summary>
    public void UpdateDragTo(int x, int y)
    {
        if (x > StartX && y > StartY)
        {
            EndX = x;
            EndY = y;
        }
    }
}

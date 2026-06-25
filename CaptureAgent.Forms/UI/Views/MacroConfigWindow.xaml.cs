using System.Windows;
using System.Windows.Threading;
using CaptureAgent.Main.PInvoke;

namespace CaptureAgent.Forms.UI.Views;

public partial class MacroConfigWindow : Window
{
    private readonly IMouseInteropService _mouseService;
    private DispatcherTimer? _countdownTimer;
    private int _countdownRemaining;
    private System.Threading.CancellationTokenSource? _cts;
    private bool _isRunning;

    /// <summary>실행 중 반복 루프를 취소하기 위한 토큰.</summary>
    public System.Threading.CancellationToken RunToken => _cts?.Token ?? System.Threading.CancellationToken.None;

    /// <summary>지정된 마우스 포인터 X 좌표(물리 픽셀).</summary>
    public int PositionX { get; private set; }

    /// <summary>지정된 마우스 포인터 Y 좌표(물리 픽셀).</summary>
    public int PositionY { get; private set; }

    /// <summary>지정된 딜레이(초).</summary>
    public double DelaySeconds { get; private set; }

    /// <summary>반복 횟수.</summary>
    public int RepeatCount { get; private set; }

    /// <summary>"실행" 버튼이 눌려 좌표/딜레이가 확정되면 발생한다. 창은 닫히지 않고 제자리에 그대로 있다.</summary>
    public event EventHandler? RunRequested;

    public MacroConfigWindow()
        : this(AppServices.GetRequired<IMouseInteropService>())
    {
    }

    public MacroConfigWindow(IMouseInteropService mouseService)
    {
        InitializeComponent();
        _mouseService = mouseService;

        // 주 모니터 작업 영역 중앙에 고정 배치한다. 이후 Left/Top을 다시 건드리지 않으므로
        // 실행/캡춰 중에도 창 위치가 움직이지 않는다.
        var work = System.Windows.SystemParameters.WorkArea;
        Left = work.Left + (work.Width - Width) / 2;
        Top = work.Top + (work.Height - Height) / 2;

        // 현재 커서 위치를 초기값으로 채워 둔다.
        var (x, y) = _mouseService.GetCursorPosition();
        PositionXBox.Text = x.ToString();
        PositionYBox.Text = y.ToString();

        PickPositionButton.Click += PickPositionButton_Click;
        ConfirmButton.Click += ConfirmButton_Click;
        CancelButton.Click += (s, e) => Close();
    }

    /// <summary>실행 결과 등 상태 메시지를 창 안에 표시한다.</summary>
    public void SetStatus(string message, bool isError = false)
    {
        StatusText.Foreground = isError
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LightGreen;
        StatusText.Text = message;
    }

    /// <summary>
    /// 3초 카운트다운 후 현재 마우스 커서 위치를 읽어 X/Y 입력란에 채운다.
    /// 카운트다운 동안 사용자가 원하는 위치로 마우스를 옮길 수 있다.
    /// </summary>
    private void PickPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_countdownTimer != null) return; // 이미 진행 중

        _countdownRemaining = 3;
        PickPositionButton.IsEnabled = false;
        StatusText.Text = $"{_countdownRemaining}초 후 마우스 위치를 캡처합니다...";

        _countdownTimer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (ts, te) =>
        {
            _countdownRemaining--;
            if (_countdownRemaining > 0)
            {
                StatusText.Text = $"{_countdownRemaining}초 후 마우스 위치를 캡처합니다...";
                return;
            }

            _countdownTimer!.Stop();
            _countdownTimer = null;

            var (x, y) = _mouseService.GetCursorPosition();
            PositionXBox.Text = x.ToString();
            PositionYBox.Text = y.ToString();
            StatusText.Text = $"위치 캡처됨: ({x}, {y})";
            PickPositionButton.IsEnabled = true;
        };
        _countdownTimer.Start();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        // 실행 중이면 "중지" 동작: 반복 루프를 취소한다.
        if (_isRunning)
        {
            _cts?.Cancel();
            ConfirmButton.Content = "중지 중...";
            ConfirmButton.IsEnabled = false;
            return;
        }

        if (!int.TryParse(PositionXBox.Text, out int x) ||
            !int.TryParse(PositionYBox.Text, out int y))
        {
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            StatusText.Text = "X/Y 좌표를 올바르게 입력하세요.";
            return;
        }

        if (!double.TryParse(DelayBox.Text, out double delay) || delay < 0)
        {
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            StatusText.Text = "딜레이를 올바르게 입력하세요.";
            return;
        }

        if (!int.TryParse(RepeatBox.Text, out int repeat) || repeat < 1)
        {
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            StatusText.Text = "반복 횟수를 1 이상으로 입력하세요.";
            return;
        }

        PositionX = x;
        PositionY = y;
        DelaySeconds = delay;
        RepeatCount = repeat;

        // 실행 상태로 전환: 버튼을 "중지"로 바꾸고 취소 토큰을 만든다.
        _isRunning = true;
        _cts = new System.Threading.CancellationTokenSource();
        ConfirmButton.Content = "중지";
        ConfirmButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(229, 57, 53)); // 빨강

        // 창을 닫지 않고 실행만 요청한다. (창 위치 유지)
        RunRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>실행/중지가 끝나면 버튼을 "실행" 상태로 되돌린다.</summary>
    public void EndRun()
    {
        _isRunning = false;
        _cts?.Dispose();
        _cts = null;
        ConfirmButton.Content = "실행";
        ConfirmButton.IsEnabled = true;
        ConfirmButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80)); // 초록 #4CAF50
    }
}

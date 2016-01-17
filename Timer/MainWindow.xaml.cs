using System;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Timer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Bell = new SoundPlayer(Properties.Resources.Bell);
            Bell.Load();
        }

        /// <summary>
        /// <see cref="txtTime"/>의 <c>Text</c>에 <c>mm:ss</c> 형식의 문자열을 씀.
        /// </summary>
        /// <param name="time">초</param>
        private void DrawTime(int time)
        {
            txtTime.Text = string.Format("{0:00}:{1:00}", time / 60, time % 60);
        }

        private SoundPlayer Bell;

        private int Time;
        private DateTime StartTime;

        private CancellationTokenSource TimerThreadCTS;
        private Task TimerThreadHandle;
        private void TimerThread()
        {
            int leftTime;
            do
            {
                Thread.Sleep(200);

                var ellapsed = DateTime.Now.Subtract(StartTime).TotalSeconds;
                leftTime = Time - (int) ellapsed;

                if (TimerThreadCTS.IsCancellationRequested)
                {
                    return;
                }

                Dispatcher.Invoke(new Action(() => DrawTime(leftTime)));
            } while (leftTime > 0);
            
            Bell.PlayLooping();
        }

        /// <summary>
        /// <see cref="btnStart"/>를 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="txtTime"/>의 Text를 초 단위로 변환해 <see cref="Time"/>에 대입하고,
        /// <see cref="TimerThread"/>를 Run한다.
        /// </remarks>
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.Visibility = Visibility.Hidden;

            StartTime = DateTime.Now;

            var time = 0;
            var timeArray = txtTime.Text.Split(':');
            int tmp;
            // dd:HH:mm:ss
            Debug.Assert(timeArray.Length <= 3, "Do not support converting day to time");
            // HH:?mm:ss
            if (timeArray.Length >= 2)
            {
                foreach (var timePart in timeArray)
                {
                    time = time * 60 + (int.TryParse(timePart, out tmp) ? tmp : 0);
                }
            }
            // ss?
            else
            {
                time = int.TryParse(timeArray.LastOrDefault(), out tmp) ? tmp : 0;
            }
            Time = time;

            TimerThreadCTS = new CancellationTokenSource();
            TimerThreadHandle = Task.Factory.StartNew(TimerThread, TimerThreadCTS.Token);

            WinAPI.SetThreadExecutionState(WinAPI.EXECUTION_STATE.ES_CONTINUOUS |
                WinAPI.EXECUTION_STATE.ES_DISPLAY_REQUIRED | WinAPI.EXECUTION_STATE.ES_SYSTEM_REQUIRED);

            btnPause.Visibility = Visibility.Visible;
            btnPause.Focus();
        }

        /// <summary>
        /// <see cref="btnPause"/>를 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="TimerThread"/>를 Abort한다.
        /// </remarks>
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            btnPause.Visibility = Visibility.Hidden;

            TimerThreadCTS?.Cancel();
            TimerThreadHandle?.Wait();

            Bell.Stop();

            WinAPI.SetThreadExecutionState(WinAPI.EXECUTION_STATE.ES_CONTINUOUS);

            btnStart.Visibility = Visibility.Visible;
            if (ReferenceEquals(sender, btnPause))
            {
                btnStart.Focus();
            }
        }

        /// <summary>
        /// <see cref="btnReset"/>을 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="btnPause_Click"/>을 호출하고,
        /// <see cref="txtText"/>의 <c>Text</c>를 <see cref="Time"/>으로 바꾼다.
        /// </remarks>
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            btnPause_Click(sender, e);

            DrawTime(Time);
        }

        /// <summary>
        /// <see cref="txtTime"/>에 글자를 입력받아 삽입하기 전에 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="txtTime"/>에 <c>HH:mm:ss</c> 형식의 글자만 입력되게 하고, 기타 편의를 제공한다.
        /// </remarks>
        private void txtTime_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ":")
            {
                return;
            }

            e.Handled = true;
            
            int res;
            if (!int.TryParse(e.Text, out res))
            {
                // 엔터 키 처리
                if (e.Text.FirstOrDefault() == 13)
                {
                    btnPause_Click(sender, e);
                    btnStart_Click(sender, e);
                }
                return;
            }

            var selectionStart = txtTime.SelectionStart;
            // 숫자 삽입 모드
            if (selectionStart >= txtTime.Text.Length)
            {
                txtTime.Text += e.Text;
            }
            // 숫자 수정 모드
            else
            {
                var selectionLength = 1;
                if (txtTime.Text[selectionStart] == ':')
                {
                    var prevSplitterIdx = txtTime.Text.Substring(0, selectionStart).LastIndexOf(':');
                    // 부분 시간이 2글자 이상이면 : 뛰어넘기
                    if (selectionStart - prevSplitterIdx - 1 >= 2)
                    {
                        selectionStart++;
                    }
                    // 미만이면 숫자 삽입 모드로 전환
                    else
                    {
                        selectionLength = 0;
                    }
                }
                txtTime.Text = txtTime.Text.Substring(0, selectionStart) + e.Text + txtTime.Text.Substring(selectionStart + selectionLength);
            }
            txtTime.SelectionStart = selectionStart + 1;
        }

        /// <summary>
        /// <see cref="lblPin"/>을 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="lblPin"/>의 <c>Opacity</c> 속성을 기준으로 항상 위 표시 기능을 켜거나 끈다.
        /// </remarks>
        private void lblPin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            const double UNPINNED = 0.3;

            if (lblPin.Opacity == UNPINNED)
            {
                Topmost = true;
                lblPin.Opacity = 1;
            }
            else
            {
                Topmost = false;
                lblPin.Opacity = UNPINNED;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                btnReset_Click(sender, e);
                txtTime.Focus();
            }
        }

        private void txtTime_GotFocus(object sender, RoutedEventArgs e)
        {
            btnPause_Click(sender, e);
        }
    }
}

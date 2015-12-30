using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            HistoryPanel.RecordClicked = LoadTime;
        }

        private void DrawTime(int leftTime)
        {
            Timer.Text = string.Format("{0:00}:{1:00}", leftTime / 60, leftTime % 60);
        }

        private SoundPlayer Bell = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("Timer.Bell.wav"));

        private CancellationTokenSource TimerThreadCTS = new CancellationTokenSource();
        private async void TimerThread()
        {
            int leftTime;
            do
            {
                await Task.Delay(200);

                var ellapsed = DateTime.Now.Subtract(HistoryPanel.Last.Value).TotalSeconds;
                leftTime = HistoryPanel.Last.Key - (int) ellapsed;

                if (TimerThreadCTS.IsCancellationRequested)
                {
                    return;
                }

                Dispatcher.Invoke(() => DrawTime(leftTime));
            } while (leftTime > 0);

            // 알람
            Bell.PlayLooping();
        }

        /// <summary>
        /// <see cref="Start"/>를 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="Timer"/>의 Text를 초 단위로 변환해 <see cref="SetTime"/>의 값으로 지정하고,
        /// <see cref="TimerThread"/>를 Run한다.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start.Visibility = Visibility.Hidden;

            TimerThreadCTS.Cancel();

            int time = 0;
            var timeArray = Timer.Text.Split(':');
            // dd:?HH:mm:ss
            if (timeArray.Length > 3)
            {
                throw new OverflowException("TimeValue Overflow!");
            }
            // HH:?mm:ss
            if (timeArray.Length >= 2)
            {
                foreach (var part in timeArray)
                {
                    time = time * 60 + int.Parse(string.IsNullOrEmpty(part) ? "0" : part);
                }
            }
            // ss?
            else
            {
                time = int.Parse(timeArray.LastOrDefault() ?? "0");
            }

            HistoryPanel.Insert(time);
            TimerThreadCTS = new CancellationTokenSource();
            Task.Run((Action) TimerThread, TimerThreadCTS.Token);

            Pause.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// <see cref="Pause"/>를 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="TimerThread"/>를 Abort한다.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Pause.Visibility = Visibility.Hidden;

            TimerThreadCTS.Cancel();

            Bell.Stop();

            Start.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// <see cref="Reset"/>을 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="Pause_Click"/>을 호출하고,
        /// <see cref="Timer"/>의 Text를 <see cref="History.Last"/>의 Key로 바꾼다.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Pause_Click(sender, e);

            DrawTime(HistoryPanel.Last.Key);
        }

        /// <summary>
        /// <see cref="HistoryPanel"/>의 항목을 클릭하면 호출되는 함수.
        /// </summary>
        /// <remarks>
        /// <see cref="Pause_Click"/>을 호출하고,
        /// <see cref="Timer"/>의 Text를 <paramref name="time"/>으로 바꾼다.
        /// </remarks>
        /// <param name="time"></param>
        private void LoadTime(int time)
        {
            Pause_Click(null, null);

            DrawTime(time);
        }

        private void Timer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ":")
            {
                return;
            }

            e.Handled = true;

            int res;
            if (!int.TryParse(e.Text, out res))
            {
                return;
            }

            var start = Timer.SelectionStart;
            // 숫자 삽입 모드
            if (start >= Timer.Text.Length)
            {
                Timer.Text += e.Text;
            }
            // 숫자 수정 모드
            else
            {
                var end = start + 1;
                if (Timer.Text[start] == ':')
                {
                    var prevSplitterIdx = Timer.Text.Substring(0, start).LastIndexOf(':');
                    // 부분 시간이 2글자 이상이면 : 뛰어넘기
                    if (start - prevSplitterIdx - 1 >= 2)
                    {
                        start++;
                        end++;
                    }
                    // 미만이면 숫자 채우기
                    else
                    {
                        end = start;
                    }
                }
                Timer.Text = Timer.Text.Substring(0, start) + e.Text + Timer.Text.Substring(end);
            }
            Timer.SelectionStart = start + 1;
        }

        private void Pin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            const double UNPINNED = 0.3;

            if (Pin.Opacity == UNPINNED)
            {
                Topmost = true;
                Pin.Opacity = 1;
            }
            else
            {
                Topmost = false;
                Pin.Opacity = UNPINNED;
            }
        }
    }
}

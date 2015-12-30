using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for History.xaml
    /// </summary>
    public partial class History : UserControl
    {
        public History()
        {
            InitializeComponent();
        }

        private List<KeyValuePair<int, DateTime>> DB = new List<KeyValuePair<int, DateTime>>();

        public KeyValuePair<int, DateTime> Last = new KeyValuePair<int, DateTime>(0, DateTime.Now);

        public void Insert(int time)
        {
            Last = new KeyValuePair<int, DateTime>(time, DateTime.Now);
            DB.RemoveAll(i => i.Key == Last.Key);
            DB.Add(Last);

            UpdateUI();
        }

        private void UpdateUI()
        {
            using (var d = Dispatcher.DisableProcessing())
            {
                container.Children.Clear();

                foreach (var rec in DB.Reverse<KeyValuePair<int, DateTime>>())
                {
                    var wrapper = new Grid();
                    wrapper.Tag = rec;
                    wrapper.ColumnDefinitions.Add(new ColumnDefinition());
                    wrapper.ColumnDefinitions.Add(new ColumnDefinition());
                    wrapper.ColumnDefinitions[1].Width = new GridLength(20);
                    wrapper.Margin = new Thickness(0, 7, 0, 7);

                    var textBlock = new TextBlock();
                    textBlock.Text = string.Format("{0:00}:{1:00}", rec.Key / 60, rec.Key % 60);
                    textBlock.PreviewMouseDown += Time_Click;
                    textBlock.FontSize = 14;
                    textBlock.Margin = new Thickness(7, 0, 7, 0);
                    Grid.SetColumn(textBlock, 0);
                    wrapper.Children.Add(textBlock);

                    var closeBtn = new TextBlock();
                    closeBtn.Text = "🗙";
                    closeBtn.PreviewMouseDown += Close_Click;
                    Grid.SetColumn(closeBtn, 1);
                    wrapper.Children.Add(closeBtn);

                    container.Children.Add(wrapper);
                }
            }
        }

        public Action<int> RecordClicked;

        private void Time_Click(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock) sender;
            var wrapper = (Grid) textBlock.Parent;
            var rec = (KeyValuePair<int, DateTime>) wrapper.Tag;
            if (RecordClicked != null)
            {
                RecordClicked(rec.Key);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var closeBtn = (TextBlock) sender;
            var wrapper = (Grid) closeBtn.Parent;
            var rec = (KeyValuePair<int, DateTime>) wrapper.Tag;
            DB.RemoveAll(i => i.Key == rec.Key);

            UpdateUI();
        }
    }
}

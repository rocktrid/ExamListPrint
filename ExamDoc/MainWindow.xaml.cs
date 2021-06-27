using System.Windows;

namespace ExamDoc
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();
            ForFrames.MyFrames = ForWindows;
            ForWindows.Navigate(new ExamListPg());
        }
    }
}

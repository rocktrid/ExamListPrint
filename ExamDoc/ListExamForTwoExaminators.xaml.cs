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
using System.Windows.Forms;
using System.IO.Packaging;
using System.IO;
using PdfSharp.Xps;
using PdfSharp;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
namespace ExamDoc
{
    /// <summary>
    /// Логика взаимодействия для ListExamForTwoExaminators.xaml
    /// </summary>
    public partial class ListExamForTwoExaminators : Page
    {
        public ListExamForTwoExaminators()
        {
            InitializeComponent();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Print.Visibility = Visibility.Collapsed;
            MemoryStream lMemoryStream = new MemoryStream(); // поток для чтения wpf
            Package package = Package.Open(lMemoryStream, FileMode.Create); //забиваем wpf в контейнер
            XpsDocument doc = new XpsDocument(package); //представление wpf в xps документ
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc); // запись wpf в xps
            writer.Write(this); //записывает текущее окно в xps
            doc.Close(); //закрывает представление
            package.Close(); //закрывает контейнер
            // Конвертируем xps в pdf
            MemoryStream outStream = new MemoryStream(); //поток для pdf
            PdfSharp.Xps.XpsConverter.Convert(lMemoryStream, outStream, false); //конвертация потока xps в поток pdf с закрытием потока pdf после 
            // Запись в pdf
            string CheckName = "";
            string StudName = string.Empty;
            string savepath = string.Empty;
            SaveFileDialog SaveFile = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = "Допуск на пересдачу" + StudName + ".pdf"
            };
            if (SaveFile.ShowDialog() == DialogResult.OK)
            {
                savepath = System.IO.Path.GetFullPath(SaveFile.FileName);
                System.Windows.MessageBox.Show("Данные экспортируются в документ");
                if (File.Exists(CheckName))
                {
                    try
                    {
                        File.Delete(CheckName);

                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
            }
            if (savepath != null)
            {
                FileStream fileStream = new FileStream(savepath, FileMode.Create); //поток для записи документа
                outStream.CopyTo(fileStream); //поток pdf копируется по месту filestream
                System.Windows.MessageBox.Show("Документ создан");
                // Очистка потока outstream
                outStream.Flush(); //чистит буфер потока
                outStream.Close(); //закрывает буфер потока
                fileStream.Flush(); //чистит буфер потока
                fileStream.Close(); //закрывает буфер потока
            }
            else
            {
                System.Windows.MessageBox.Show("путь не выбран, повторите снова");
            }
        }
    }
}

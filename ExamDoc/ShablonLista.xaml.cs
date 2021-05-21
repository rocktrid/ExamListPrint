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
using MySql.Data.MySqlClient;
using System.Data;

namespace ExamDoc
{
    /// <summary>
    /// Логика взаимодействия для ShablonLista.xaml
    /// </summary>
    public partial class ShablonLista : Page
    {
        DataTable DTable;
        struct ExamData
        {
            public int idExamListsRegist { get; set; }
            public int ExamListsRegistTeacherid { get; set; }
            public int? ExamListsRegistTeacherid2 { get; set; }
            public int? ExamListsRegistTeacherid3 { get; set; }
            public int ExamListsRegistDisciplineid { get; set; }
            public int? ExamListsRegistSecondDisciplineid { get; set; }
            public int ExamListsRegistStudid { get; set; }
            public int examlistsregistTypeOfExam { get; set; }
            public DateTime DateOfExam { get; set; }
            public DateTime DateOfApproving { get; set; }
            public DateTime ExpirationDate { get; set; }
        };
        List<ExamData> ExDt = new List<ExamData>();
        public ShablonLista()
        {
            InitializeComponent();
            //подключение к БД
            String ConnectionToBase = "Database = diplomalocalserver; Data Source = 127.0.0.1; User Id = root; Password = Password";
            BaseConn.BuildConnection = new MySqlConnection(ConnectionToBase);
            BaseConn.BuildConnection.Open();
            //получение данных с последней строки таблицы и помещение её в struct
            string Query = "SELECT * FROM examlistsregist ORDER BY idExamListsRegist DESC LIMIT 1"; // по идее, должно вывести самую первую позицию снизу aka последняя добавленная запись
            MySqlCommand Selecter = new MySqlCommand(Query, BaseConn.BuildConnection);
            MySqlDataReader DataReader = Selecter.ExecuteReader();
            DTable = new DataTable();
            DTable.Load(DataReader);
            DataRowCollection PosSelection = DTable.Rows;
            foreach (DataRow D in PosSelection) // новая позиция в листе
            {
                try
                {
                    object[] O = D.ItemArray;
                    ExDt.Add(new ExamData()
                    {
                        //DBNull.Value ? null : (int?)O[n] а также приравнивание в структурах к int? необходимо, если есть null значения в таблицах  
                        idExamListsRegist = (int)O[0],
                        ExamListsRegistTeacherid = (int)O[1],
                        ExamListsRegistTeacherid2 = O[2] == DBNull.Value ? null : (int?)O[2],
                        ExamListsRegistTeacherid3 = O[3] == DBNull.Value ? null : (int?)O[3],
                        ExamListsRegistDisciplineid = (int)O[4],
                        ExamListsRegistSecondDisciplineid = O[5] == DBNull.Value ? null : (int?)O[5],
                        ExamListsRegistStudid = (int)O[6],
                        examlistsregistTypeOfExam = (int)O[7],
                        DateOfExam = (DateTime)O[8],
                        DateOfApproving = (DateTime)O[9],
                        ExpirationDate = (DateTime)O[10]
                    }
                    );

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Ошибка " + ex);
                }
            }
            {
                ForIdExam.Text = ExDt[0].idExamListsRegist.ToString();
                DateOfExamData.Text = ExDt[0].DateOfExam.ToString("d/MM/yyy");
                DateOfAquiring.Text = ExDt[0].DateOfApproving.ToString("d/MM/yyy");
                DateOfExpirationData.Text = ExDt[0].ExpirationDate.ToString("d/MM/yyy");


            }
            // однако, это лишь добавит idшники из таблицы. Надо перевести их в данные
            // ну че, брутфорс. На каждый id надо сделать запрос на выборку и замену на string
            string Query1 = "SELECT * FROM "; 
        }

        // Конверт из wpf В xps
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

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
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

            public int? ExamListsModuleId { get; set; }
            public int? ExamListsSpecialDisciplineId { get; set; }
            public int? ExamListsSecondSpecialDisciplineId { get; set; }

            public int? ExamListsHeadMasterId { get; set; }
            public int? ExamListsRegistDisciplineid { get; set; }
            public int ExamListsRegistStudid { get; set; }
            public int examlistsregistTypeOfExam { get; set; }
            public DateTime DateOfApproving { get; set; }
            public DateTime ExpirationDate { get; set; }
        };
        List<ExamData> ExDt = new List<ExamData>();

        public ShablonLista()
        {
            InitializeComponent();
            //подключение к БД
            String ConnectionToBase = "server=ngknn.ru; port=20009; port =20009; user=allowed; database=allowed; User Id = allowed; Password = KemOW4seYumi"; //после миграции поменять строку
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
                        ExamListsSpecialDisciplineId = O[3] == DBNull.Value ? null : (int?)O[3],
                        ExamListsSecondSpecialDisciplineId = O[4] == DBNull.Value ? null : (int?)O[4],
                        ExamListsModuleId = O[5] == DBNull.Value ? null : (int?)O[5],
                        ExamListsRegistDisciplineid = O[6] == DBNull.Value ? null : (int?)O[6],
                        ExamListsRegistStudid = (int)O[7],
                        examlistsregistTypeOfExam = (int)O[8],
                        DateOfApproving = (DateTime)O[9],
                        ExpirationDate = (DateTime)O[10],
                        ExamListsHeadMasterId = O[11] == DBNull.Value ? null : (int?)O[11]
                    }
                    );
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Ошибка " + ex);
                }
            }
            try
            {
                if (ExDt[0].ExamListsSecondSpecialDisciplineId != null) //присутствует два модуля
                {
                    // тут два модуля выводятся в одну строку
                    // выводится тип предмета УП ПП и пр
                    Query = "SELECT * FROM disciplinestypes";
                    MySqlCommand DisciplinesSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader DSearch = DisciplinesSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(DSearch);
                    DataRowCollection DisciplinesFind = DTable.Rows;
                    DisciplineData.Text = string.Empty;
                    foreach (DataRow D in DisciplinesFind)  // проход, чтобы найти нужные модули
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsSecondSpecialDisciplineId)
                        {
                            DisciplineData.Text += " " + (string)O[1] + ",";
                        }
                        if ((int)O[0] == ExDt[0].ExamListsSpecialDisciplineId)
                        {
                            DisciplineData.Text += " " + (string)O[1] + ",";
                        }
                    }
                    DSearch.Close();
                    DTable.Clear();
                    DisciplinesFind.Clear(); // чищу, т.к. оно пригодится после, дабы не вводить новые переменные в одном отдельном куске кода
                    //выводится код ПМа
                    Query = "SELECT idmodules, modulesdescr FROM modules";
                    DisciplinesSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    DSearch = DisciplinesSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(DSearch);
                    DisciplinesFind = DTable.Rows;
                    foreach (DataRow D in DisciplinesFind)  // проход, чтобы найти нужные модули
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsModuleId)
                        {
                            DisciplineData.Text = DisciplineData.Text.Remove(DisciplineData.Text.Length - 1);
                            DisciplineData.Text += " " + (string)O[1];
                        }
                    }
                    if (ExDt[0].ExamListsRegistTeacherid2 != null) // если несколько преподов принимают пересдачу
                    {
                        IfTwoExaminators.Visibility = Visibility.Visible;
                        FirstExaminator.Text += " #1";
                        Query = "SELECT idTeacherList, TeacherListFName, TeacherListLName, TeacherListPatronymicName FROM teacherlist";

                        MySqlCommand TeachersSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                        MySqlDataReader TSearch = TeachersSearch.ExecuteReader();
                        DTable = new DataTable();
                        DTable.Load(TSearch);
                        DataRowCollection TeachersFind = DTable.Rows;
                        foreach (DataRow D in TeachersFind) // первый проход, чтобы найти первого препода
                        {
                            object[] O = D.ItemArray;
                            if ((int)O[0] == ExDt[0].ExamListsRegistTeacherid)
                            {
                                TeacherData.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                            }
                            if ((int)O[0] == ExDt[0].ExamListsRegistTeacherid2)
                            {
                                TeacherData1.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                            }
                        }
                    }
                    else //если препод один принимает
                    {
                        Query = "SELECT idTeacherList, TeacherListFName, TeacherListLName, TeacherListPatronymicName FROM teacherlist";

                        MySqlCommand TeachersSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                        MySqlDataReader TSearch = TeachersSearch.ExecuteReader();
                        DTable = new DataTable();
                        DTable.Load(TSearch);
                        DataRowCollection TeachersFind = DTable.Rows;
                        foreach (DataRow D in TeachersFind) // первый проход, чтобы найти первого препода
                        {
                            object[] O = D.ItemArray;
                            if ((int)O[0] == ExDt[0].ExamListsRegistTeacherid)
                            {
                                TeacherData.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                            }
                        }
                    }
                    Query = "SELECT * FROM teacherlist";
                    MySqlCommand HeadMasterSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader HeadSearch = HeadMasterSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(HeadSearch);
                    DataRowCollection HeadMFind = DTable.Rows;
                    foreach (DataRow D in HeadMFind) // первый проход, чтобы найти первого препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsHeadMasterId)
                        {
                            ExamHead.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                        }
                    }
                    Query = "SELECT * FROM studentslist";
                    MySqlCommand StudSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader Stud = StudSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(Stud);
                    DataRowCollection StudFind = DTable.Rows;
                    Query = "SELECT * FROM groupslist";
                    MySqlCommand GroupSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader Group = GroupSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(Group);
                    DataRowCollection GroupFind = DTable.Rows;
                    Query = "SELECT * FROM examtypes";
                    MySqlCommand ExamTypeSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader ExamType = ExamTypeSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(ExamType);
                    DataRowCollection ExamTypeFind = DTable.Rows;
                    foreach (DataRow D in StudFind) // первый проход, чтобы найти первого препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistStudid)
                        {
                            DateOfAquiring.Text = ExDt[0].DateOfApproving.ToString("d/MM/yyy");
                            DateOfExpirationData.Text = ExDt[0].ExpirationDate.ToString("d/MM/yyy");
                            ForIdExam.Text = ExDt[0].idExamListsRegist.ToString();
                            StudFLPNameData.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3]; //нашли студика
                            foreach (DataRow S in GroupFind) // здесь же найдем и его группу
                            {
                                object[] P = S.ItemArray;
                                if ((int)P[0] == (int)O[5])
                                {
                                    GroupData.Text = (string)P[1];
                                }
                            }
                            foreach (DataRow V in ExamTypeFind)
                            {
                                object[] Q = V.ItemArray;
                                if (ExDt[0].examlistsregistTypeOfExam == (int)V[0])
                                {
                                    TypeOfExamData1.Text = (string)V[1];
                                }
                            }
                        }
                    }
                }
                else
                {
                    Query = "SELECT idDisciplines, DisciplineDescription FROM disciplines";
                    MySqlCommand DiscSearching = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader DSearch = DiscSearching.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(DSearch);
                    DataRowCollection DisciplineFind = DTable.Rows;
                    foreach (DataRow D in DisciplineFind) // первый проход, чтобы найти первого препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistDisciplineid)
                        {
                            DisciplineData.Text = (string)O[1];
                        }
                    }
                    Query = "SELECT idTeacherList, TeacherListFName, TeacherListLName, TeacherListPatronymicName FROM teacherlist";
                    MySqlCommand TeachersSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader TSearch = TeachersSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(TSearch);
                    DataRowCollection TeachersFind = DTable.Rows;
                    foreach (DataRow D in TeachersFind) // первый проход, чтобы найти первого препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistTeacherid)
                        {
                            TeacherData.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                        }
                    }
                    Query = "SELECT * FROM teacherlist";
                    MySqlCommand HeadMasterSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader HeadSearch = HeadMasterSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(HeadSearch);
                    DataRowCollection HeadMFind = DTable.Rows;
                    foreach (DataRow D in HeadMFind) // первый проход, чтобы найти первого препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsHeadMasterId)
                        {
                            ExamHead.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                        }
                    }
                    Query = "SELECT * FROM studentslist";
                    MySqlCommand StudSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader Stud = StudSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(Stud);
                    DataRowCollection StudFind = DTable.Rows;
                    Query = "SELECT * FROM groupslist";
                    MySqlCommand GroupSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader Group = GroupSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(Group);
                    DataRowCollection GroupFind = DTable.Rows;
                    Query = "SELECT * FROM examtypes";
                    MySqlCommand ExamTypeSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader ExamType = ExamTypeSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(ExamType);
                    DataRowCollection ExamTypeFind = DTable.Rows;
                    foreach (DataRow D in StudFind) // первый проход, чтобы найти первого препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistStudid)
                        {
                            DateOfAquiring.Text = ExDt[0].DateOfApproving.ToString("d/MM/yyy");
                            DateOfExpirationData.Text = ExDt[0].ExpirationDate.ToString("d/MM/yyy");
                            ForIdExam.Text = ExDt[0].idExamListsRegist.ToString();
                            StudFLPNameData.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3]; //нашли студика
                            foreach (DataRow S in GroupFind) // здесь же найдем и его группу
                            {
                                object[] P = S.ItemArray;
                                if ((int)P[0] == (int)O[5])
                                {
                                    GroupData.Text = (string)P[1];
                                }
                            }
                            foreach (DataRow V in ExamTypeFind)
                            {
                                object[] Q = V.ItemArray;
                                if (ExDt[0].examlistsregistTypeOfExam == (int)V[0])
                                {
                                    TypeOfExamData1.Text = (string)V[1];
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("" + ex);
            }
        }

        // Конверт из wpf В xps данный метод с конвертации я заменю сразу на печать документа
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            string name = "Табаков допуск";
            // Send to the printer.
            SaveToPDF.Visibility = Visibility.Collapsed;
            Print.Visibility = Visibility.Collapsed;
            GoBack.Visibility = Visibility.Collapsed;
            Prnt(ShablonGrid, name);

            //System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            //printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
            //printDialog.PrintVisual(ShablonGrid, "My First Print Job");
            SaveToPDF.Visibility = Visibility.Visible;
            Print.Visibility = Visibility.Visible;
            GoBack.Visibility = Visibility.Visible;
        }
        public static void Prnt(Visual elementToPrint, string description)
        {
            using (var printServer = new LocalPrintServer())
            {
                var dialog = new System.Windows.Controls.PrintDialog();
                //Find the PDF printer
                var qs = printServer.GetPrintQueues();
                var queue = qs.FirstOrDefault(q => q.Name.Contains("PDF"));
                if (queue == null) {/*handle what you want to do here (possibly use XPS?)*/}
                dialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                dialog.PrintTicket.CopyCount = 2;
                dialog.PrintQueue = queue;
                dialog.PrintVisual(elementToPrint, description);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {

            ForFrames.MyFrames.GoBack();

        }

        private void SaveToPDF_Click(object sender, RoutedEventArgs e) // сохранение с конвертацией в pdf
        {
            SaveToPDF.Visibility = Visibility.Collapsed;
            Print.Visibility = Visibility.Collapsed;
            GoBack.Visibility = Visibility.Collapsed;
            string StudName = StudFLPNameData.Text;
            MemoryStream lMemoryStream = new MemoryStream(); // поток для чтения wpf
            Package package = Package.Open(lMemoryStream, FileMode.Create); //забиваем wpf в контейнер
            XpsDocument doc = new XpsDocument(package); //представление wpf в xps документ
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc); // запись wpf в xps
            writer.Write(this); //записывает текущее окно в xps
            doc.Close(); //закрывает представление
            package.Close(); //закрывает контейнер
            // Запись в pdf
            SaveFileDialog SaveFile = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = "Допуск на пересдачу" + " " + StudName + ".pdf"
            };
            if (SaveFile.ShowDialog() == DialogResult.OK)
            {
                string savepath;
                savepath = System.IO.Path.GetFullPath(SaveFile.FileName);
                System.Windows.MessageBox.Show("Данные экспортируются в документ");
                if (File.Exists(savepath))
                {
                    try
                    {
                        File.Delete(savepath);

                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
                if (savepath != null)
                {
                    // Конвертируем xps в pdf
                    MemoryStream outStream = new MemoryStream(); //поток для pdf
                    PdfSharp.Xps.XpsConverter.Convert(lMemoryStream, outStream, false); //конвертация потока xps в поток pdf с закрытием потока pdf после 
                    FileStream fileStream = new FileStream(savepath, FileMode.Create); //поток для записи документа
                    outStream.CopyTo(fileStream); //поток pdf копируется по месту filestream
                    System.Windows.MessageBox.Show("Документ создан");
                    // Очистка потока outstream
                    outStream.Flush(); //чистит буфер потока
                    outStream.Close(); //закрывает буфер потока
                    fileStream.Flush(); //чистит буфер потока
                    fileStream.Close(); //закрывает буфер потока
                }

            }

            else
            {
                System.Windows.MessageBox.Show("путь не выбран, повторите снова");
            }
            SaveToPDF.Visibility = Visibility.Visible;
            Print.Visibility = Visibility.Visible;
            GoBack.Visibility = Visibility.Visible;
        }

    }
}

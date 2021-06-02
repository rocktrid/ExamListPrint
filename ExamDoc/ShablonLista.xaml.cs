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
            public int? ExamListsHeadMasterId { get; set; }
            public int ExamListsRegistDisciplineid { get; set; }
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
            String ConnectionToBase = "Database = diplomalocalserver; Data Source = 127.0.0.1; User Id = root; Password = Password"; //после миграции поменять строку
            BaseConn.BuildConnection = new MySqlConnection(ConnectionToBase);
            BaseConn.BuildConnection.Open();
            //получение данных с последней строки таблицы и помещение её в struct
            string Query = "SELECT * FROM examlistsregist ORDER BY idExamListsRegist DESC LIMIT 2"; // по идее, должно вывести самую первую позицию снизу aka последняя добавленная запись
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
                        ExamListsRegistDisciplineid = (int)O[3],
                        ExamListsRegistStudid = (int)O[4],
                        examlistsregistTypeOfExam = (int)O[5],
                        DateOfApproving = (DateTime)O[6],
                        ExpirationDate = (DateTime)O[7],
                        ExamListsHeadMasterId = (int)O[8]
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
                if (ExDt[0].ExamListsRegistStudid == ExDt[1].ExamListsRegistStudid) // если если есть две записи подряд по одному студенту (то будет иная форма для печати). Дополнительно проверяю дату аппрува, чтоб наверняка
                {
                    // тут будет сцепка первой дисциплины со второй дисциплиной через запятую
                    Query = "SELECT idDisciplines, DisciplineDescription FROM disciplines";
                    MySqlCommand DisciplinesSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader DSearch = DisciplinesSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(DSearch);
                    DataRowCollection DisciplinesFind = DTable.Rows;
                    foreach (DataRow D in DisciplinesFind)  // первый проход, чтобы найти первую дисциплину
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistDisciplineid)
                        {
                            DisciplineData.Text = (string)O[1];
                        }
                    }
                    foreach (DataRow D in DisciplinesFind)  // второй проход, чтобы найти вторую дисциплину
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[1].ExamListsRegistDisciplineid)
                        {
                            DisciplineData.Text += ", \r" + (string)O[1];
                        }
                    }
                    // тут будет сцепка первого преподавателя со вторым

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
                    foreach (DataRow D in TeachersFind)  // второй проход, чтобы найти второго препода
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[1].ExamListsRegistTeacherid)
                        {
                            TeacherData.Text += ", \r" + (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                        }
                    }
                    foreach (DataRow D in TeachersFind)
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
                    foreach (DataRow D in StudFind) // первый проход, чтобы найти студиков
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistStudid)
                        {
                            DateOfAquiring.Text = ExDt[0].DateOfApproving.ToString("d/MM/yyy");
                            DateOfExpirationData.Text = ExDt[0].ExpirationDate.ToString("d/MM/yyy");
                            ForIdExam.Text = ExDt[0].idExamListsRegist.ToString();
                            StudFLPNameData.Text = (string)O[1] + " " + (string)O[2] + " " + (string)O[3];
                            foreach (DataRow S in GroupFind) // здесь же найдем и его группу
                            {
                                object[] P = S.ItemArray;
                                if ((int)P[0] == (int)O[5])
                                {
                                    GroupData.Text = (string)P[1];
                                }
                            }
                        }
                    }
                    Query = "SELECT * FROM examtypes";
                    MySqlCommand ExamTypeSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader ExamType = ExamTypeSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(ExamType);
                    DataRowCollection ExamTypeFind = DTable.Rows;
                    foreach (DataRow V in ExamTypeFind)
                    {
                        object[] Q = V.ItemArray;
                        if (ExDt[0].examlistsregistTypeOfExam == (int)V[0])
                        {
                            TypeOfExamData1.Text = (string)V[1];

                        }
                        if (ExDt[1].examlistsregistTypeOfExam == (int)V[0])
                        {
                            ForSecondDiscType.Visibility = Visibility.Visible;
                            TypeOfExamData2.Text = (string)V[1];
                        }
                    }


                }

                else
                {
                    // тут просто первая дисциплина
                    Query = "SELECT idDisciplines, DisciplineDescription FROM disciplines";
                    MySqlCommand DisciplinesSearch = new MySqlCommand(Query, BaseConn.BuildConnection);
                    MySqlDataReader DSearch = DisciplinesSearch.ExecuteReader();
                    DTable = new DataTable();
                    DTable.Load(DSearch);
                    DataRowCollection DisciplinesFind = DTable.Rows;
                    foreach (DataRow D in DisciplinesFind)  // первый проход, чтобы найти первую дисциплину
                    {
                        object[] O = D.ItemArray;
                        if ((int)O[0] == ExDt[0].ExamListsRegistDisciplineid)
                        {
                            DisciplineData.Text = (string)O[1];
                        }
                    }
                    // а тут просто первый преподаватель
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
            }

            else
            {
                System.Windows.MessageBox.Show("путь не выбран, повторите снова");
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            ForFrames.MyFrames.GoBack();

        }
    }
}

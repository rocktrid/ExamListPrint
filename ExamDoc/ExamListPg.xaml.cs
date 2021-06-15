using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Packaging;
using System.IO;
using PdfSharp.Xps;
using PdfSharp;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;

namespace ExamDoc
{
    /// <summary>
    /// Логика взаимодействия для ExamListPg.xaml
    /// </summary>
    public partial class ExamListPg : Page
    {
        // собснно, сам адрес сервера.
        private readonly string OpenConnection = "server=ngknn.ru; port=20009; port =20009; user=allowed; database=allowed; User Id = allowed; Password = KemOW4seYumi";

        // сохранить id чтобы проверить его в таблице с переэкзаменовками и для получения наименования группы
        public int IdStudent;
        int zav = 0; //сохранить id заведующего
        int IdGroup = 0;
        int[] CountReexams = new int[2]; // запомнить id типов пересдачи
        DataTable DTable = new DataTable();
        // поиск учителя по id
        public int TeachId;
        ///
        /// <summary>
        /// Для добавления в БД
        /// </summary>
        struct Insert
        {
            public int FirstTeacherId { get; set; }
            public int SecondTeacherId { get; set; }
            public int HeadM { get; set; }
            public int FirstDisciplineId { get; set; }
            public int StudId { get; set; }
            public int ExamTypeId { get; set; }
            public DateTime DateOfExpiration { get; set; }
            public DateTime DateOfApproving { get; set; }


        };
        Insert NewInsert = new Insert();
        struct DisciplineType
        {
            public int disctypeid { get; set; }
            public string disctypedescr { get; set; }
        };
        List<DisciplineType> DiscT = new List<DisciplineType>();
        struct ModulesInfo
        {
            public int moduleid { get; set; }
            public string moduledescr { get; set; }
            public int moduleteacher { get; set; }
        }
        List<ModulesInfo> Modules = new List<ModulesInfo>();


        /// 
        /// <summary>
        /// для будущего поиска по ФИО
        /// </summary>
        struct ForStudSearching
        {
            public int idStud { get; set; }
            public string Fname { get; set; }
            public string Lname { get; set; }
            public string Patronymic { get; set; }
            public int GroupId { get; set; }
        };
        List<ForStudSearching> ForSrch = new List<ForStudSearching>();
        /// <summary>
        /// структура для поиска по группам  + под него лист
        /// </summary>
        struct ForGroupDeclaration
        {
            public int idGroupList { get; set; }

            public string GroupDescr { get; set; }
        };
        List<ForGroupDeclaration> ToDeclare = new List<ForGroupDeclaration>();
        /// <summary>
        /// структура для поиска дисциплин + под него лист
        /// </summary>
        struct Disciplines
        {
            public string DisciplineDescr { get; set; }
            public int DisciplineTeacher { get; set; }
        };
        List<Disciplines> DisciplinesList = new List<Disciplines>();
        /// <summary>
        /// структура для поиска учителей
        /// </summary>
        struct TeachersList
        {
            public int TeacherId { get; set; }
            public string Fname { get; set; }
            public string Lname { get; set; }
            public string Patronymic { get; set; }
            public string TeachersRole { get; set; }

        }
        List<TeachersList> Teachers = new List<TeachersList>();
        /// <summary>
        /// поиск по количеству пересдач, для автоматического добавления типа пересдачи
        /// </summary>
        struct ForExamCheck
        {
            public int idStud { get; set; } // id студента
            public int? idFirstDiscipline { get; set; } // проверка по первой дисциплине
            public int idTypeExam { get; set; } //определяет тип

        }
        List<ForExamCheck> ExamChecker = new List<ForExamCheck>();
        public ExamListPg()
        {
            //подключение к бд
            InitializeComponent();
            try
            {
                BaseConn.BuildConnection = new MySqlConnection(OpenConnection);
                BaseConn.BuildConnection.Open();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("" + ex);
                System.Windows.MessageBox.Show("Ошибка подключения к БД");
            }
            //сразу перенос данных по студентам в структуру
            try
            {
                string Query = "Select idStudentsList, StudentFName, StudentLName, StudentPatronymic, StudentGroupId FROM studentslist"; // запрос на выборку по ФИО
                MySqlCommand FillIt = new MySqlCommand(Query, BaseConn.BuildConnection);
                MySqlDataReader DReader = FillIt.ExecuteReader();
                DTable = new DataTable();
                DTable.Load(DReader);
                DataRowCollection ToSrchStud = DTable.Rows;
                foreach (DataRow D in ToSrchStud) // добавляю новую позицию в лист
                {
                    object[] O = D.ItemArray;
                    ForSrch.Add(new ForStudSearching()
                    {
                        idStud = (int)O[0],
                        Fname = (string)O[1],
                        Lname = (string)O[2],
                        Patronymic = (string)O[3],
                        GroupId = (int)O[4]
                    }
                    );
                }
                DTable.Clear(); //чистка таблицы, на всякий случай.
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка" + ex);
            }
        }

        /// <summary>
        /// переадресация на метод ChooseConnection, который найдет имя в БД
        /// </summary>
        private void Check_Name_Click(object sender, RoutedEventArgs e)
        {
            FormStackPan.Visibility = Visibility.Visible;
            foreach(var str in ForSrch)
            {
                if (str.Fname+" "+str.Lname+" "+str.Patronymic == Search_Name.Text)
                {
                    NewInsert.StudId = str.idStud;
                    JumpToNextStep();
                }
            }
        }
        /// <summary>
        /// Эвент, чтобы открыть календарик
        /// </summary>
        private void UnlockCalendar_Click(object sender, RoutedEventArgs e)
        {
            DateOfExamCalendar.Visibility = Visibility.Visible;
            UnlockCalendar.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// при закрытии комбобокса, при наличии выбранной позиции, будет выполнено действие
        /// </summary>
        private void StudDisciplCb_DropDownClosed(object sender, EventArgs e)
        {
            if (StudExamPersonTb.SelectedIndex == -1)
            {
                StudExamPersonTb.Items.Clear();
            }
            if (StudDisciplCb.SelectedIndex != -1)
            {
                StudExamPersonTb1.Items.Clear();
                HeadMasterNameCb.Visibility = Visibility.Visible;
                StudExamPersonTb.Visibility = Visibility.Visible;
                Examinator1Tb.Visibility = Visibility.Visible;
                ForColl2.Visibility = Visibility.Visible;
                MethodToFindExamsCounters(1);
                string str = StudDisciplCb.SelectedItem.ToString(); // строка с наименованием предмета для отсеивания
                if (str != null) //получаем здесь код учителя, для поиска по БД
                {
                    if (IfSpecExam.IsChecked == true) //проверка на выборку по специальным видам дисциплин
                    {
                        for (int i = 0; i < Modules.Count; i++)
                        {
                            if (str == Modules[i].moduledescr)
                            {
                                TeachId = Modules[i].moduleteacher;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < DisciplinesList.Count; i++)
                        {
                            if (DisciplinesList[i].DisciplineDescr == str)
                            {
                                TeachId = DisciplinesList[i].DisciplineTeacher;
                            }
                        }
                    }
                }
                for (int i = 0; i + 1 <= Teachers.Count; i++)
                {
                    if (Teachers[i].TeacherId == TeachId)
                    {
                        StudExamPersonTb.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                    }
                    else
                    StudExamPersonTb1.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                }
               
            }

        }
        /// <summary>
        /// Получение даты
        /// </summary>
        private void DateOfExamCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            NewInsert.DateOfApproving = DateTime.Today;
            NewInsert.DateOfExpiration = (DateTime)DateOfExamCalendar.SelectedDate;

            // далее добавить это в структуру
        }

        ///
        ///<summary>метод осуществляющий экзекут sql команды</summary>
        ///
        private void ExecSqlComm( DateTime? b, DateTime c)
        {
            try
            {
                
                if (IfSpecExam.IsChecked == true && DiscTypeCb.SelectedIndex !=-1) // если специальный экзамен, тогда в таблицу добавляется экзамен с поправкой на вид дисциплины
                {
                    
                    MySqlCommand InsertCommand = new MySqlCommand("INSERT INTO examlistsregist (ExamListsRegistTeacherid, ExamListsRegistTeacherid2, ExamListsModuleId, ExamListsSpecialDisciplineId," +
                    " ExamListsSecondSpecialDisciplineId, ExamListsRegistStudid, examlistsregistTypeOfExam, DateOfApproving, ExpirationDate, ExamListsHeadMasterId)" +
                    " VALUES(@ExamListsRegistTeacherid, @ExamListsRegistTeacherid2, @ExamListsModuleId," +
                    " @ExamListsSpecialDisciplineId , @ExamListsSecondSpecialDisciplineId, @ExamListsRegistStudid, @examlistsregistTypeOfExam," +
                    " @DateOfApproving, @ExpirationDate, @ExamListsHeadMasterId)", BaseConn.BuildConnection);
                    InsertCommand.CommandType = CommandType.Text;
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistStudid", NewInsert.StudId); //ид студента
                    InsertCommand.Parameters.AddWithValue("@examlistsregistTypeOfExam", CountReexams[0]); // ид типа пересдачи
                    InsertCommand.Parameters.AddWithValue("@DateOfApproving", c); //дата выдачи
                    InsertCommand.Parameters.AddWithValue("@ExpirationDate", b); // последняя дата
                    InsertCommand.Parameters.AddWithValue("@ExamListsModuleId", StudDisciplCb.SelectedIndex + 1); //ид модуля для спец
                    InsertCommand.Parameters.AddWithValue("@ExamListsSpecialDisciplineId", DiscTypeCb.SelectedIndex + 1); // ид УП ПП ...
                    if (CheckForSecondDiscipline.IsChecked == true) // при наличии второго особого "экзамена"
                    {
                        InsertCommand.Parameters.AddWithValue("@ExamListsSecondSpecialDisciplineId", DiscTypeCb2.SelectedIndex + 1); // ид УП ПП ...
                    }
                    else
                        InsertCommand.Parameters.AddWithValue("@ExamListsSecondSpecialDisciplineId", null); // ид УП ПП ...

                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)StudExamPersonTb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsRegistTeacherid", Teachers[i].TeacherId); // ид учителя
                        }
                        if ((string)StudExamPersonTb1.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsRegistTeacherid2", Teachers[i].TeacherId); // ид учителя

                        }
                    }
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)HeadMasterNameCb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsHeadMasterId", Teachers[i].TeacherId);// ид учителя
                        }
                    }
                    InsertCommand.ExecuteNonQuery();
                    System.Windows.MessageBox.Show("Данные по пересдаче добавлены!");
                }
                else if (IfSpecExam.IsChecked == true && DiscTypeCb.SelectedIndex == -1)// если чекнута галка, но нет особой дисциплины
                {
                    System.Windows.MessageBox.Show("Не выбрана дисциплина");
                }
                else if (IfSpecExam.IsChecked == false && DiscTypeCb.SelectedIndex != -1) // если не чекнута галка, но есть особая дисциплина
                {
                    System.Windows.MessageBox.Show("Не стоит галочка для Особый экзамен");
                }
                else // если экзамен не специальный
                {
                    MySqlCommand InsertCommand = new MySqlCommand("INSERT INTO examlistsregist (ExamListsRegistTeacherid, ExamListsRegistDisciplineid," +
                        " ExamListsRegistStudid, examlistsregistTypeOfExam, DateOfApproving, ExpirationDate, ExamListsHeadMasterId)" +
                        " VALUES( @ExamListsRegistTeacherid,@ExamListsRegistDisciplineid, @ExamListsRegistStudid," +
                        " @examlistsregistTypeOfExam, @DateOfApproving, @ExpirationDate, @ExamListsHeadMasterId)", BaseConn.BuildConnection);
                    InsertCommand.CommandType = CommandType.Text;
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)StudExamPersonTb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsRegistTeacherid", Teachers[i].TeacherId);
                        }
                    }
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistDisciplineid", StudDisciplCb.SelectedIndex + 1); // ид простой дисциплины
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistStudid", NewInsert.StudId); // ид студента
                    InsertCommand.Parameters.AddWithValue("@examlistsregistTypeOfExam", CountReexams[0]); // ид счетчика пересдач
                    InsertCommand.Parameters.AddWithValue("@DateOfApproving", c);  //дата выдачи
                    InsertCommand.Parameters.AddWithValue("@ExpirationDate", b);// последняя дата
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)HeadMasterNameCb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsHeadMasterId", Teachers[i].TeacherId);// ид учителя
                        }
                    }
                    InsertCommand.ExecuteNonQuery();
                    System.Windows.MessageBox.Show("Данные по пересдаче добавлены!");
                }
              
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка " + ex);
            }
        }
        ///
        /// метод перехода на окно предварительного просмотра
        ///
        private void ToPrint_Click(object sender, RoutedEventArgs e)
        {
            DateTime? DT = DateOfExamCalendar.SelectedDate;
            DateTime Today = DateTime.Today;
            // а, всё ж можно сделать куда элегантнее. Я ж из комбобоксов значения беру. Вместо того, чтобы делать лишние структуры, можно просто индексами боксов воспользоваться. BigBrain
            //if (StudDisciplCb2.SelectedIndex == -1) // здесь на случай, если на одну дисциплину лист пересдачи. Да и вообще, вынести исполнение sql команды в отдельный метод, передав подобие флага как аргумент, чтобы было понятно по какому кол-ву дисциплин работать
            //{
            //    /// упд: не сработает с Заведующим, т.к. в списке будет их мало, а заведующие находятся с учителями вместе, но показываются в списке только заведующие. Кароч, не пошаманить с селектед индекс
            //    /// /// упд упд: нашел способ исправить это через индексы
            //    ExecSqlComm(true, DT, Today);
            //}
            //else
            //{
            //    ExecSqlComm(false, DT, Today);
            //}
            //
            ExecSqlComm(DT, Today);
            if(IfSpecExam.IsChecked == true)
            {
                ForFrames.MyFrames.Navigate(new ShablonLista());

            }
            else
            ForFrames.MyFrames.Navigate(new ShablonLista());
        }
        /// <summary>
        /// подключение к БД ака основной метод, где вложена куча операций, для предварительного вывода данных из БД
        /// </summary>
        /// <param name="ConnectionToBase"></param>
        public void JumpToNextStep()
        {
            namechanged.Visibility = Visibility.Collapsed;
            ForStudFLPNames.Text = Search_Name.Text;
            Search_Name.Clear();
            try
            {                            
                string QueryForGroupDeclare = "SELECT * FROM groupslist";
                MySqlCommand Declare = new MySqlCommand(QueryForGroupDeclare, BaseConn.BuildConnection);
                MySqlDataReader DataRead = Declare.ExecuteReader();
                DTable = new DataTable();
                DTable.Load(DataRead);
                DataRowCollection Declaration = DTable.Rows;

                foreach (DataRow D in Declaration) // добавляю новую группу в лист
                {
                    object[] O = D.ItemArray;
                    ToDeclare.Add(new ForGroupDeclaration()
                    {
                        idGroupList = (int)O[0],
                        GroupDescr = (string)O[1]
                    }
                    );
                }
                foreach (ForGroupDeclaration fgd in ToDeclare) // печать тэга группы
                {
                    if (fgd.idGroupList == IdGroup)
                    {
                        StudGroupTb.Text = fgd.GroupDescr;
                    }
                }
                string QueryForDisciplines = "SELECT * FROM disciplines";
                MySqlCommand Discipl = new MySqlCommand(QueryForDisciplines, BaseConn.BuildConnection);
                MySqlDataReader DataR = Discipl.ExecuteReader();
                DTable = new DataTable();
                DTable.Load(DataR);
                DataRowCollection DiscDecl = DTable.Rows;
                string str = string.Empty;
                foreach (DataRow D in DiscDecl) // добавляю новую группу в лист
                {
                    object[] O = D.ItemArray;
                    DisciplinesList.Add(new Disciplines()
                    {
                        DisciplineDescr = (string)O[1],
                        DisciplineTeacher = (int)O[2]
                    }
                    );
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("" + ex);
            }
            for (int i = 0; i + 1 <= DisciplinesList.Count; i++)
            {
                StudDisciplCb.Items.Add(DisciplinesList[i].DisciplineDescr);
            }
            string QueryTeach = "Select * FROM teacherlist"; // поиск учителей
            MySqlCommand Teach = new MySqlCommand(QueryTeach, BaseConn.BuildConnection);
            MySqlDataReader Data = Teach.ExecuteReader();
            DTable = new DataTable();
            DTable.Load(Data);
            DataRowCollection TeachFind = DTable.Rows;
            foreach (DataRow D in TeachFind) // добавляем учителей для дальнейшего поиска
            {
                object[] O = D.ItemArray;
                Teachers.Add(new TeachersList()
                {
                    TeacherId = (int)O[0],
                    Fname = (string)O[1],
                    Lname = (string)O[2],
                    Patronymic = (string)O[3],
                    TeachersRole = (string)O[4]
                }
                );
            }
            NewInsert.FirstTeacherId = Teachers[0].TeacherId;
            for (int i = 0; i + 1 <= Teachers.Count; i++) // поиск заведующего отделением
            {
                if (Teachers[i].TeachersRole == "Заведующий отделением")
                {
                    HeadMasterNameCb.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                    zav = i;
                }
            }
        }

        private void MethodToFindExamsCounters(int a)
        {
            DataTable DTable;
            string QueryForExamSrch = "Select ExamListsRegistStudid, ExamListsRegistDisciplineid, examlistsregistTypeOfExam FROM examlistsregist"; //строка для запроса на поиск были ли у студента пересдачи
            MySqlCommand Finder = new MySqlCommand(QueryForExamSrch, BaseConn.BuildConnection);
            MySqlDataReader DataReader = Finder.ExecuteReader();
            DTable = new DataTable();
            DTable.Load(DataReader);
            DataRowCollection ToSrchExams = DTable.Rows;
            foreach (DataRow D in ToSrchExams) // добавляю новую позицию в лист
            {
                object[] O = D.ItemArray;
                ExamChecker.Add(new ForExamCheck()
                {
                    idStud = (int)O[0],
                    idFirstDiscipline = O[1] == DBNull.Value ? null : (int?)O[1],
                    idTypeExam = (int)O[2]
                }
                );
            }
            // поиск по реестру экзаменов студента и его пересдачи
            foreach (ForExamCheck fec in ExamChecker)
            {
                try
                {
                    if (fec.idStud == IdStudent) //т.е. в базе пересдач есть студент
                    {
                        if (a == 1) // т.е. нет второй дисциплины aka только если одна дисциплина
                        {
                            if (StudDisciplCb.SelectedIndex + 1 == fec.idFirstDiscipline) //т.е. есть совпадение и по дисциплине
                            {
                                int num = fec.idTypeExam;
                                switch (num)
                                {
                                    case 1: // если есть одна пересдача
                                        TypeOfExam.Text = "Повторная";
                                        CountReexams[0] = 2; //т.к. нужно для будущего внесения данных
                                        break;
                                    case 2:  // если есть две пересдачи
                                        TypeOfExam.Text = "Комиссионная";
                                        CountReexams[0] = 3; //т.к. нужно для будущего внесения данных
                                        break;
                                    case 3:  // если есть три пересдачи
                                        System.Windows.MessageBox.Show("Превышен порог пересдач");
                                        StudDisciplCb.SelectedItem = null;
                                        Search_Name.Text = string.Empty;
                                        TypeOfExam.Text = string.Empty;
                                        StudGroupTb.Text = string.Empty;
                                        //скрыть все и очистить поле поиска
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(" " + ex);
                }
            }
            // если в базе пересдач нет, то присваивается первичная
            if (StudDisciplCb.SelectedIndex >= 0 && TypeOfExam.Text == "")
            {
                TypeOfExam.Text = "Первичная";
                CountReexams[0] = 1;
            }
        }
        private void GetLast_Click(object sender, RoutedEventArgs e)
        {
            ForFrames.MyFrames.Navigate(new ShablonLista());
        }
        /// <summary>
        /// метод осуществляет поиск по листу, чтобы вывести в комбо ФИО студентов на выбор
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texttosearch = Search_Name.Text;
            List<string> ToFillTextBox = new List<string>();
            ToFillTextBox.Clear();
            foreach (var data in ForSrch)
            {
                if (!string.IsNullOrEmpty(Search_Name.Text))
                {
                    if (data.Fname.StartsWith(texttosearch))
                    {
                        ToFillTextBox.Add(data.Fname + " " + data.Lname + " " + data.Patronymic);
                    }
                }

            }
            if (ToFillTextBox.Count > 0)
            {
                namechanged.ItemsSource = ToFillTextBox;
                namechanged.Visibility = Visibility.Visible;
                namechanged.IsDropDownOpen = true;
            }
            else if (Search_Name.Text.Equals(""))
            {
                namechanged.Visibility = Visibility.Collapsed;
                namechanged.ItemsSource = null;
                namechanged.IsDropDownOpen = false;

            }
        }
        /// <summary>
        /// При выборе итема из бокса, он переходит в текстблок.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void namechanged_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (namechanged.ItemsSource != null)
            {
                namechanged.Visibility = Visibility.Collapsed;
                Search_Name.TextChanged -= new TextChangedEventHandler(Search_Name_TextChanged); //удаляется результат предыдущего эвента, чтобы его варианты не мешали
                if (namechanged.SelectedIndex != -1)
                {
                    Search_Name.Text = namechanged.SelectedItem.ToString();
                }
                Search_Name.TextChanged -= new TextChangedEventHandler(Search_Name_TextChanged);
                foreach (var data in ForSrch)
                {
                    if (data.Fname + " " + data.Lname + " " + data.Patronymic == Search_Name.Text)
                    {
                        IdGroup = data.GroupId;
                    }
                }
            }
        }
        /// <summary>
        /// Если дисциплина для пересдачи является комплексной или не просто дисциплиной (ака ПП УП ПДП), то будет выдан список таких модулей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IfSpecExam_Checked(object sender, RoutedEventArgs e)
        {
            IfExaminatorsMoreThatOne.Visibility = Visibility.Visible;
            DataTable DTable;
            string QueryForDiscSearch = "Select * FROM disciplinestypes"; //строка для запроса на поиск типа дисциплины
            MySqlCommand Finder = new MySqlCommand(QueryForDiscSearch, BaseConn.BuildConnection);
            MySqlDataReader DataReader = Finder.ExecuteReader();
            DTable = new DataTable();
            DTable.Load(DataReader);
            DataRowCollection ToSrchTypes = DTable.Rows;
                if(DiscT.Count ==0) // условие, дабы избежать повторения позиций
            {
                foreach (DataRow D in ToSrchTypes) // добавляю новую позицию в лист
                {
                    object[] O = D.ItemArray;
                    DiscT.Add(new DisciplineType()
                    {
                        disctypeid = (int)O[0],
                        disctypedescr = (string)O[1]
                    }
                    );
                }
                foreach (var str in DiscT)
                {
                    DiscTypeCb.Items.Add(str.disctypedescr);
                    DiscTypeCb2.Items.Add(str.disctypedescr);
                }
            }       
            ForSpecialDisciplines.Visibility = Visibility.Visible;
            
        }

        private void IfSpecExam_Unchecked(object sender, RoutedEventArgs e)
        {
            StudDisciplCb.Items.Clear();
            ForSpecialDisciplines.Visibility = Visibility.Collapsed;
            foreach (var str in DisciplinesList)
            {
                StudDisciplCb.Items.Add(str.DisciplineDescr);
            }
            IfExaminatorsMoreThatOne.Visibility = Visibility.Collapsed;
            StudExamPersonTb1.Items.Clear();
        }

        private void DiscTypeCb_DropDownClosed(object sender, EventArgs e)
        {
            if (DiscTypeCb.SelectedIndex != -1)
            {
                Modules.Clear();
                DataTable DTable;
                string QueryForDisc = "Select * FROM modules"; //строка для запроса на поиск ПМ
                MySqlCommand Finder = new MySqlCommand(QueryForDisc, BaseConn.BuildConnection);
                MySqlDataReader DataReader = Finder.ExecuteReader();
                DTable = new DataTable();
                DTable.Load(DataReader);
                DataRowCollection ToSrchMods = DTable.Rows;
                foreach (DataRow D in ToSrchMods) // добавляю новую позицию в лист
                {
                    object[] O = D.ItemArray;
                    Modules.Add(new ModulesInfo()
                    {
                        moduleid = (int)O[0],
                        moduledescr = (string)O[1],
                        moduleteacher = (int)O[2]
                    }
                    );
                }
                StudDisciplCb.Items.Clear();
                foreach (var str in Modules)
                {
                    StudDisciplCb.Items.Add(str.moduledescr);
                }
            }
        }

        private void CheckForSecondDiscipline_Checked(object sender, RoutedEventArgs e)
        {
            ForSecondSpecialDiscipline.Visibility = Visibility.Visible;
        }

        private void DiscTypeCb2_DropDownOpened(object sender, EventArgs e)
        {
            // здесь вызвать обработчик предыдущего события IfSpecExam_Checked, чтобы снова добавить в комбобокс данные, т.к. они будут, в последствии, удаляться
            if (DiscTypeCb.SelectedIndex != -1)
            {
                string str = DiscTypeCb.SelectedItem.ToString();
                DiscTypeCb2.Items.Remove(str); // как  тебе такое, Илон Маск?
            }
        }
    }
}
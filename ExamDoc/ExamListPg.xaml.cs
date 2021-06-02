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
        int[] CountReexams = new int[2]; // запомнить id типов пересдачи
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
            public int idFirstDiscipline { get; set; } // проверка по первой дисциплине
            public int idTypeExam { get; set; } //определяет тип

        }
        List<ForExamCheck> ExamChecker = new List<ForExamCheck>();

        public ExamListPg()
        {
            InitializeComponent();
        }

        /// <summary>
        /// переадресация на метод ChooseConnection, который найдет имя в БД
        /// </summary>
        private void Check_Name_Click(object sender, RoutedEventArgs e)
        {
            FormStackPan.Visibility = Visibility.Visible;
            ChooseConnection(OpenConnection);
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
                HeadMasterNameCb.Visibility = Visibility.Visible;
                StudExamPersonTb.Visibility = Visibility.Visible;
                Examinator1Tb.Visibility = Visibility.Visible;
                ForColl2.Visibility = Visibility.Visible;
                MethodToFindExamsCounters(1);
                string str = StudDisciplCb.SelectedItem.ToString(); // строка с наименованием предмета для отсеивания
                if (str != null) //получаем здесь код учителя, для поиска по БД
                {
                    for (int i = 0; i < DisciplinesList.Count; i++)
                    {
                        if (DisciplinesList[i].DisciplineDescr == str)
                        {
                            TeachId = DisciplinesList[i].DisciplineTeacher;
                        }
                    }
                }
                for (int i = 0; i + 1 <= Teachers.Count; i++)
                {
                    if (Teachers[i].TeacherId == TeachId)
                    {
                        StudExamPersonTb.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                    }
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

        private void StudDisciplCb2_DropDownClosed(object sender, EventArgs e)
        {
            if (StudExamPersonTb1.SelectedIndex == -1)
            {
                StudExamPersonTb1.Items.Clear();
            }
            if (StudDisciplCb2.SelectedIndex != -1)
            {
                string str = StudDisciplCb2.SelectedItem.ToString(); // строка с наименованием предмета для отсеивания
                if (str != null) //получаем здесь код учителя, для поиска по БД
                {
                    for (int i = 0; i < DisciplinesList.Count; i++)
                    {
                        if (DisciplinesList[i].DisciplineDescr == str)
                        {
                            TeachId = DisciplinesList[i].DisciplineTeacher;
                        }
                    }
                }

                for (int i = 0; i + 1 <= Teachers.Count; i++)
                {
                    if (Teachers[i].TeacherId == TeachId)
                    {
                        StudExamPersonTb1.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                    }
                }
                MethodToFindExamsCounters(2);
            }
        }
        private void IfCoupleDisciplines_Checked(object sender, RoutedEventArgs e)
        {
            ForVisibilityScndDiscpl.Visibility = Visibility.Visible;
            IfExaminatorsMoreThatOne.Visibility = Visibility.Visible;

        }
        private void IfCoupleDisciplines_Unchecked(object sender, RoutedEventArgs e)
        {
            StudDisciplCb2.Items.Clear();
            ForVisibilityScndDiscpl.Visibility = Visibility.Collapsed;
            StudExamPersonTb.Visibility = Visibility.Collapsed;
        }
        ///
        ///<summary>метод осуществляющий экзекут sql команды</summary>
        ///
        private void ExecSqlComm(bool a, DateTime? b, DateTime c)
        {
            try
            {
                MySqlCommand InsertCommand = new MySqlCommand("INSERT INTO examlistsregist (ExamListsRegistTeacherid, ExamListsRegistDisciplineid,"
                                       + " ExamListsRegistStudid, examlistsregistTypeOfExam, DateOfApproving, ExpirationDate, ExamListsHeadMasterId)"
                                       + " VALUES(@ExamListsRegistTeacherid, @ExamListsRegistDisciplineid,"
                                       + " @ExamListsRegistStudid, @examlistsregistTypeOfExam, @DateOfApproving, @ExpirationDate, @ExamListsHeadMasterId)", BaseConn.BuildConnection);
                InsertCommand.CommandType = CommandType.Text;

                if (a == true) //т.е. если нет второй дисциплины
                {
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)StudExamPersonTb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsRegistTeacherid", Teachers[i].TeacherId);
                        }
                    }
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistDisciplineid", StudDisciplCb.SelectedIndex + 1);
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistStudid", NewInsert.StudId);
                    InsertCommand.Parameters.AddWithValue("@examlistsregistTypeOfExam", CountReexams[0]);
                    InsertCommand.Parameters.AddWithValue("@DateOfApproving", c);
                    InsertCommand.Parameters.AddWithValue("@ExpirationDate", b);
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)HeadMasterNameCb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsHeadMasterId", Teachers[i].TeacherId);
                        }
                    }
                    InsertCommand.ExecuteNonQuery();
                    System.Windows.MessageBox.Show("Данные по первой пересдаче добавлены!");                                                      
                }              
                else if (a==false)
                {

                    //добавляется первая пересдача в БД по первой дисциплине
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)StudExamPersonTb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsRegistTeacherid", Teachers[i].TeacherId);
                        }
                    }
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistDisciplineid", StudDisciplCb.SelectedIndex + 1);
                    InsertCommand.Parameters.AddWithValue("@ExamListsRegistStudid", NewInsert.StudId);
                    InsertCommand.Parameters.AddWithValue("@examlistsregistTypeOfExam", CountReexams[0]);
                    InsertCommand.Parameters.AddWithValue("@DateOfApproving", c);
                    InsertCommand.Parameters.AddWithValue("@ExpirationDate", b);
                    for (int i=0; i< Teachers.Count;i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)HeadMasterNameCb.SelectedItem == str)
                        {
                            InsertCommand.Parameters.AddWithValue("@ExamListsHeadMasterId", Teachers[i].TeacherId);
                        }
                    }
                    InsertCommand.ExecuteNonQuery();
                    MySqlCommand InsertNewCommand = new MySqlCommand("INSERT INTO examlistsregist (ExamListsRegistTeacherid, ExamListsRegistDisciplineid,"
                                       + " ExamListsRegistStudid, examlistsregistTypeOfExam, DateOfApproving, ExpirationDate, ExamListsHeadMasterId)"
                                       + " VALUES(@ExamListsRegistTeacherid, @ExamListsRegistDisciplineid,"
                                       + " @ExamListsRegistStudid, @examlistsregistTypeOfExam, @DateOfApproving, @ExpirationDate, @ExamListsHeadMasterId)", BaseConn.BuildConnection); ;
                    InsertNewCommand.CommandType = CommandType.Text;
                    //добавляется вторая пересдача в БД по второй дисциплине
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)StudExamPersonTb1.SelectedItem == str)
                        {
                            InsertNewCommand.Parameters.AddWithValue("@ExamListsRegistTeacherid", Teachers[i].TeacherId);
                        }
                    }
                    InsertNewCommand.Parameters.AddWithValue("@ExamListsRegistDisciplineid", StudDisciplCb2.SelectedIndex+1);
                    InsertNewCommand.Parameters.AddWithValue("@ExamListsRegistStudid", NewInsert.StudId);
                    InsertNewCommand.Parameters.AddWithValue("@examlistsregistTypeOfExam", CountReexams[1]);
                    InsertNewCommand.Parameters.AddWithValue("@DateOfApproving", c);
                    InsertNewCommand.Parameters.AddWithValue("@ExpirationDate", b);
                    for (int i = 0; i < Teachers.Count; i++)
                    {
                        string str = Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic;
                        if ((string)HeadMasterNameCb.SelectedItem == str)
                        {
                            InsertNewCommand.Parameters.AddWithValue("@ExamListsHeadMasterId", Teachers[i].TeacherId);
                        }
                    }
                    InsertNewCommand.ExecuteNonQuery();
                    System.Windows.MessageBox.Show("Данные по второй пересдаче добавлены!");
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
            if (StudDisciplCb2.SelectedIndex == -1) // здесь на случай, если на одну дисциплину лист пересдачи. Да и вообще, вынести исполнение sql команды в отдельный метод, передав подобие флага как аргумент, чтобы было понятно по какому кол-ву дисциплин работать
            {
                /// упд: не сработает с Заведующим, т.к. в списке будет их мало, а заведующие находятся с учителями вместе, но показываются в списке только заведующие. Кароч, не пошаманить с селектед индекс
                ExecSqlComm(true, DT, Today);
            }
            else
            {
                ExecSqlComm(false, DT, Today);
            }
            //
            ForFrames.MyFrames.Navigate(new ShablonLista());
        }
        /// <summary>
        /// подключение к БД ака основной метод, где вложена куча операций, для предварительного вывода данных из БД
        /// </summary>
        /// <param name="ConnectionToBase"></param>
        public void ChooseConnection(string ConnectionToBase)
        {


            DataTable DTable;
            try
            {
                BaseConn.BuildConnection = new MySqlConnection(ConnectionToBase);
                BaseConn.BuildConnection.Open();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("" + ex);
                System.Windows.MessageBox.Show("Ошибка подключения к БД");
            }
            try
            {
                int IdGroup = 0;
                string Query = "Select idStudentsList, StudentFName, StudentLName, StudentPatronymic, StudentGroupId FROM studentslist"; // запрос на выборку по ФИО
                string NameStud = Search_Name.Text;
                string[] a = NameStud.Split(' ');
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
                NewInsert.StudId = ForSrch[0].idStud;// добавление в структуру на запись в БД
                DTable.Clear(); //чистка таблицы, на всякий случай.
                bool flag = true; //флаг для того, чтобы не было повторений вывода сообщений
                                  // поиск в листе ФИО по условию
                foreach (ForStudSearching st in ForSrch)
                {
                    if (st.Fname == a[0] && st.Lname == a[1] && st.Patronymic == a[2]) // ну да, сделал по-колхозному, но тут же лишь ФИО проверяется - незачем тут всякие i-тые элементы делать. Сразу цифрами проще
                    {
                        flag = true; // флагую, чтобы после не шло по if
                        System.Windows.MessageBox.Show("Студент найден");
                        IdStudent = st.idStud;
                        ForStudFLPNames.Text = Search_Name.Text;
                        ForSrch.Clear();

                        FormStackPan.Visibility = Visibility.Visible;
                        IdGroup = st.GroupId;
                        break;
                    }
                    else flag = false;
                }
                DTable.Clear();
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
                if (flag == false) //так избегу повторения вывода messagebox с предупреждением об отсутствии пользователя
                {
                    System.Windows.MessageBox.Show("Пользователь не найден");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("" + ex);
            }
            for (int i = 0; i + 1 <= DisciplinesList.Count; i++)
            {
                StudDisciplCb.Items.Add(DisciplinesList[i].DisciplineDescr);
                StudDisciplCb2.Items.Add(DisciplinesList[i].DisciplineDescr);

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
                    idFirstDiscipline = (int)O[1],
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
                            if (StudDisciplCb.SelectedIndex+1 == fec.idFirstDiscipline) //т.е. есть совпадение и по дисциплине
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
                                        //скрыть все и очистить поле поиска
                                        break;
                                    default:
                                        break;
                                }
                            }                        
                        }
                        else
                        {
                        if (StudDisciplCb2.SelectedIndex + 1 == fec.idFirstDiscipline) 
                        { 
                            int num = fec.idTypeExam;
                            
                                switch (num)
                                {
                                    case 1: // если есть одна пересдача
                                        TypeOfExam2.Text = " + Повторная";
                                        CountReexams[1] = 2; //т.к. нужно для будущего внесения данных
                                        break;
                                    case 2:  // если есть две пересдачи
                                        TypeOfExam2.Text = " + Комиссионная";
                                        CountReexams[1] = 3; //т.к. нужно для будущего внесения данных

                                        break;
                                    case 3:  // если есть три пересдачи
                                        System.Windows.MessageBox.Show("Превышен порог пересдач");
                                        //скрыть все и очистить поле поиска
                                        FormStackPan.Visibility = Visibility.Collapsed;
                                        Search_Name.Text = string.Empty;
                                        BaseConn.BuildConnection.Close();
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
                if (StudDisciplCb.SelectedIndex >= 0 && TypeOfExam.Text =="")
                {
                    TypeOfExam.Text = "Первичная";
                    CountReexams[0] = 1;
                }
                if (StudDisciplCb2.SelectedIndex >= 0 && TypeOfExam2.Text == "")
                {
                    TypeOfExam2.Text = " + Первичная";
                    CountReexams[1] = 1;
                }
                // да почему ты не работаешь, ирод!
            }     
        private void GetLast_Click(object sender, RoutedEventArgs e)
        {
            ForFrames.MyFrames.Navigate(new ShablonLista());
        }
    }
}
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

namespace ExamDoc
{
    /// <summary>
    /// Логика взаимодействия для ExamListPg.xaml
    /// </summary>
    public partial class ExamListPg : Page
    {
        // собснно, сам адрес сервера.
        private readonly string OpenConnection = "Database = diplomalocalserver; Data Source = 127.0.0.1; User Id = root; Password = Password";
        // для таблицы временной
        DataTable DTable;
        // сохранить имя, чтобы перенести в форму
        public string NameStud;
        // сохранить id чтобы проверить его в таблице с переэкзаменовками и для получения наименования группы
        public int IdStudent;
        //Поиск по группе
        public int IdGroup;
        // поиск учителя по id
        public int TeachId;  
        ///<summary>
        ///для регистрации переэкзаменовки в БД
        /// </summary>

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
            public int idTypeExam { get; set; } //определяет тип
        }
        List<ForExamCheck> ExamChecker = new List<ForExamCheck>();
        public ExamListPg()
        {
            InitializeComponent();
        }
        ///
        /// метод для печати экрана в pdf
        ///
        private void ToPrint_Click(object sender, RoutedEventArgs e)
        {
            string Query;
            int TypeOfList = 0;
            if (IfCoupleOfExaminators.IsChecked == true)
            {
                Query = "INSERT INTO examlistregist (ExamListsRegistTeacherid, ExamListsRegistDisciplineid, ExamListsRegistStudid, DateOfExam, DateOfApproving, ExpirationDate, examlistsregistTypeOfExam, ExamListsRegistTeacherid3, ExamListsRegistTeacherid2) VALUES(";
                TypeOfList = 1;
            }
            else if (IfCoupleDisciplines.IsChecked == true)
            {
                TypeOfList = 2;
                Query = "INSERT INTO examlistregist (ExamListsRegistTeacherid, ExamListsRegistDisciplineid, ExamListsRegistStudid, DateOfExam, DateOfApproving, ExpirationDate, examlistsregistTypeOfExam) VALUES(";

            }
            else
            {
                Query = "INSERT INTO examlistregist (ExamListsRegistTeacherid, ExamListsRegistDisciplineid, ExamListsRegistStudid, DateOfExam, DateOfApproving, ExpirationDate, examlistsregistTypeOfExam)"+
             "VALUES(@ExamListsRegistTeacherid, @ExamListsRegistDisciplineid, @ExamListsRegistStudid, @DateOfExam, @DateOfApproving, @ExpirationDate, @examlistsregistTypeOfExam)";


            }
            if (TypeOfList == 0)
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(Query, BaseConn.BuildConnection);
                    BaseConn.BuildConnection.Open();
                    cmd.Parameters.AddWithValue("@ExamListRegistTeacherid",null);
                    cmd.ExecuteNonQuery();
                    System.Windows.MessageBox.Show("Данные добавлены!");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Ошибка "+ex);

                }
                ForFrames.MyFrames.Navigate(new ShablonLista());
            }
            else
            {
                ForFrames.MyFrames.Navigate(new ListExamForTwoExaminators());
            }

            //catch (Exception ex)
            //{
            //    System.Windows.MessageBox.Show("" + ex);
            //}
        }

        /// <summary>
        /// подключение к БД ака основной метод, где вложена куча операций, для предварительного вывода данных из БД
        /// </summary>
        /// <param name="ConnectionToBase"></param>
        public void ChooseConnection(string ConnectionToBase)
        {
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
                        idStud =(int)O[0],
                        Fname = (string)O[1],
                        Lname = (string)O[2],
                        Patronymic = (string)O[3],
                        GroupId = (int)O[4]
                    }
                    );
                }
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
                        ForStudFLPNames.Text = NameStud;
                        ForSrch.Clear();
                        string QueryForExamSrch = "Select ExamListsRegistStudid, examlistsregistTypeOfExam FROM examlistsregist"; //строка для запроса на поиск были ли у студента пересдачи
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
                                idTypeExam = (int)O[1]                             
                            }
                            );
                        }
                        string q = string.Empty;
                        int counter = 0; //счетчик для поиска количества вхождения результатов. Необходим если студент в первый раз пересдает и его нет в списках переэкзаменовки
                        // поиск по реестру экзаменов студента и его пересдачи
                        foreach (ForExamCheck fec in ExamChecker) 
                        { 
                            if (fec.idStud == IdStudent)
                            {
                                counter++;
                                int num = fec.idTypeExam; // определяем тип пересдачи, на основе данных из реестра. Если пересдача была, то выведется следующий тип пересдачи
                                switch (num)
                                {
                                    case 1: // если пересдача 1
                                        q = "Повторная";
                                        break;
                                    case 2: // если пересдач 2
                                        q = "Комиссионная";
                                        break;
                                    case 3: // если пересдач 3
                                        System.Windows.MessageBox.Show("Такой студент уже пересдавал экзамен три раза. Отказано.");
                                        FormStackPan.Visibility = Visibility.Collapsed;
                                         NameStud = string.Empty;
                                         IdStudent = 0;
                                        break;
                                    default:
                                        q = "Первичная";
                                        break;
                                }
                            }
                        }
                        TypeOfExam.Text = q; // если пересдавал, то присваиваем q
                        if (counter==0)// если нет, то первичную по умолчанию
                        {
                            TypeOfExam.Text = "Первичная"; 
                        }                   
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
                foreach (ForGroupDeclaration fgd in ToDeclare)
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
                        DisciplineTeacher =(int)O[2]
                    }
                    ) ;
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
            for(int i=0;i+1<=DisciplinesList.Count;i++)
            {
                StudDisciplCb.Items.Add(DisciplinesList[i].DisciplineDescr);
            }
            DTable.Clear();
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
            for(int i=0; i + 1 <= Teachers.Count; i++)
            {
                if( Teachers[i].TeachersRole == "Заведующий отделением")
                {
                    HeadMasterNameCb.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                }
            }
        }
        /// <summary>
        /// переадресация на метод ChooseConnection, который найдет имя в БД
        /// </summary>
        private void Check_Name_Click(object sender, RoutedEventArgs e)
        {
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
        /// Эвент, если экзаменаторов больше чем один
        /// </summary>
        private void IfCoupleOfExaminators_Checked(object sender, RoutedEventArgs e)
        {
            IfExaminatorsMoreThatOne.Visibility = Visibility.Visible;
            IfExaminatorsMoreThatOne1.Visibility = Visibility.Visible;
            for (int i = 0; i + 1 <= Teachers.Count; i++)
            {
                if (Teachers[i].TeacherId == TeachId)
                {
                    //два лица, принимающие экзамен, которые компетентные в сдаваемом предмете
                    StudExamPersonTb.Items.Clear();
                    StudExamPersonTb.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                    StudExamPersonTb1.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                }
                else
                {
                    StudExamPersonTb2.Items.Add(Teachers[i].Fname + " " + Teachers[i].Lname + " " + Teachers[i].Patronymic);
                }
            }
        }
        /// <summary>
        /// при закрытии комбобокса, при наличии выбранной позиции, будет выполнено действие
        /// </summary>
        private void StudDisciplCb_DropDownClosed(object sender, EventArgs e)
        {
            string str = StudDisciplCb.SelectedItem.ToString(); // строка с наименованием предмета для отсеивания
            if (str !=null) //получаем здесь код учителя, для поиска по БД
            {              
                for(int i=0;i<DisciplinesList.Count;i++)
                {
                    if (DisciplinesList[i].DisciplineDescr == str)
                    {
                        TeachId = DisciplinesList[i].DisciplineTeacher;
                    }
                }
            }
          
            if(IfCoupleOfExaminators.IsChecked ==false)
            {
                for (int i = 0; i + 1 <= Teachers.Count; i++)
                {
                    if(Teachers[i].TeacherId==TeachId)
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
            DateTime? DT = DateOfExamCalendar.SelectedDate;
            DateTime Today = DateTime.Today;
        }
    }
}

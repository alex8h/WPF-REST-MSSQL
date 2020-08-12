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
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace Countries
{

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Table
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Capital { get; set; }
            public string Region { get; set; }
            public string Population { get; set; }
            public string Area { get; set; }
        }
        HttpClient client = new HttpClient();
        HttpRequestMessage request = new HttpRequestMessage();
        string URL = "https://restcountries.eu/rest/v2/name/";
        JSON json;
        SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString);
        DataTable dataTable = new DataTable("Countries");
        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Обработчик кнопки "Поиск"
        /// Берет значение из текстового поля для ввода и ищет страну в restcountries.eu;
        /// Если страна найдена, выводит ее данные и предлагает сохранить в базу данных;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Search(object sender, RoutedEventArgs e)
        {
            Save.Visibility = Visibility.Hidden;
            Save_click.Visibility = Visibility.Hidden;
            string text = Input.Text;
            if (text != "")
            {
                request.RequestUri = new Uri(URL + text);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");

                HttpResponseMessage response = await client.GetAsync(request.RequestUri);
                if (!response.IsSuccessStatusCode)
                {
                    Output_REST.Text = "Not Found";
                }
                else
                {
                    var resp = await response.Content.ReadAsStringAsync();
                    json = JsonConvert.DeserializeObject<List<JSON>>(resp)[0];
                    Output_REST.Text = "Name: " + json.name + "\n" + "Code: " + json.alpha2Code + "\n" + "Capital: " + json.capital + "\n" + "Region: " + json.region + "\n" + "Population: " + json.population + "\n" + "Area: " + json.area;
                    Save.Visibility = Visibility.Visible;
                }
            }

        }
        /// <summary>
        /// Обработчик кнопки ""Сохранить в БД
        /// Выполняет обновление либо сохранение информации о стране в бд
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Save(object sender, RoutedEventArgs e)
        {
            Save.Visibility = Visibility.Hidden;
            try
            {
                connection.Open();
                Table insert = new Table();
                DataTable table_Cities = Select("SELECT * FROM Cities", "Cities");
                bool flag = false;
                int i;
                for (i = 0; i < table_Cities.Rows.Count; i++)
                {
                    if (json.capital == table_Cities.Rows[i][1].ToString())
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    insert.Capital = table_Cities.Rows[i][0].ToString();
                }
                else
                {
                    insert.Capital = Insert("INSERT INTO Cities (Name) VALUES('" + json.capital + "') SELECT SCOPE_IDENTITY()");
                    Save_click.Visibility = Visibility.Visible;
                }
     
                DataTable table_Regions = Select("SELECT * FROM Regions", "Regions");
                flag = false;
                for (i = 0; i < table_Regions.Rows.Count; i++)
                {
                    if (json.region == table_Regions.Rows[i][1].ToString())
                    {
                        flag = true;
                        break;
                    }
                }
                
                if (flag)
                {
                    insert.Region = table_Regions.Rows[i][0].ToString();
                }
                else
                {
                    insert.Region = Insert("INSERT INTO Regions (Name) VALUES('" + json.region + "') SELECT SCOPE_IDENTITY()");
                    Save_click.Visibility = Visibility.Visible;
                }

                dataTable = Select("SELECT * FROM Countries", "Countries");
                flag = false;
                for (i = 0; i < dataTable.Rows.Count; i++)
                {
                    if (json.alpha2Code == dataTable.Rows[i][2].ToString())
                    {
                        flag = true;
                        break;
                    }
                }
                
                if (flag)
                {
                    Update("UPDATE Countries " +
                        "SET Capital ="+ insert.Capital + ", Region=" + insert.Region +  ", Population=" + json.population.ToString() + ", Area=" + json.area.ToString() +
                        "WHERE Code='" + json.alpha2Code + "'");
                }
                else
                {
                    insert.Id = Insert(String.Format("INSERT INTO Countries (Name, Code, Capital, Region, Population, Area) " +
                        "VALUES('{0}','{1}',{2},{3},{4},{5})",json.name, json.alpha2Code, insert.Capital, insert.Region, json.population.ToString(), json.area.ToString()) + "SELECT SCOPE_IDENTITY()");
                    Save_click.Visibility = Visibility.Visible;
                }
               
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Вывести список всех стран с БД"
        /// Происходит открытие соединения с БД,
        /// запрос на вывод информации о странах
        /// и вывод этой информации в таблицу.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_DB(object sender, RoutedEventArgs e)
        {
            try
            {
                table_of_countries.Items.Clear();
                connection.Open();
                SqlCommand command = new SqlCommand();
                dataTable = Select("SELECT Countries.ID, Countries.Name, Code, Cities.Name, Regions.Name, Population, Area FROM Countries " +
                                   "JOIN Cities ON Countries.Capital = Cities.ID " +
                                   "JOIN Regions ON Countries.Region=Regions.ID", 
                                   "Coutries");
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    Table countries = new Table()
                    {
                        Id = dataTable.Rows[i][0].ToString(),
                        Name = dataTable.Rows[i][1].ToString(),
                        Code = dataTable.Rows[i][2].ToString(),
                        Capital = dataTable.Rows[i][3].ToString(),
                        Region = dataTable.Rows[i][4].ToString(),
                        Population = dataTable.Rows[i][5].ToString(),
                        Area = dataTable.Rows[i][6].ToString()
                    };
                    table_of_countries.Items.Add(countries);
                }

            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        private DataTable Select(string request, string table_name)
        {
            DataTable table = new DataTable(table_name);
            SqlCommand command = new SqlCommand();
            command.CommandText = request;
            command.Connection = connection;
            SqlDataAdapter sqlDataAdapter_r = new SqlDataAdapter(command);
            sqlDataAdapter_r.Fill(table);
            return table;
        }

        private void Update(string request)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = request;
            command.Connection = connection;
            command.ExecuteNonQuery();
        }
        private string Insert(string request)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = request;
            command.Connection = connection;
            return command.ExecuteScalar().ToString();

        }

    }
 
    public class Currency
    {
        public string code { get; set; }
        public string name { get; set; }
        public string symbol { get; set; }

    }

    public class Language
    {
        public string iso639_1 { get; set; }
        public string iso639_2 { get; set; }
        public string name { get; set; }
        public string nativeName { get; set; }

    }

    public class Translations
    {
        public string de { get; set; }
        public string es { get; set; }
        public string fr { get; set; }
        public string ja { get; set; }
        public string it { get; set; }
        public string br { get; set; }
        public string pt { get; set; }
        public string nl { get; set; }
        public string hr { get; set; }
        public string fa { get; set; }

    }

    public class RegionalBloc
    {
        public string acronym { get; set; }
        public string name { get; set; }
        public List<string> otherAcronyms { get; set; }
        public List<string> otherNames { get; set; }

    }

    public class JSON
    {
        public string name { get; set; }
        public List<string> topLevelDomain { get; set; }
        public string alpha2Code { get; set; }
        public string alpha3Code { get; set; }
        public List<string> callingCodes { get; set; }
        public string capital { get; set; }
        public List<string> altSpellings { get; set; }
        public string region { get; set; }
        public string subregion { get; set; }
        public int population { get; set; }
        public List<double> latlng { get; set; }
        public string demonym { get; set; }
        public double area { get; set; }
        public double? gini { get; set; }
        public List<string> timezones { get; set; }
        public List<string> borders { get; set; }
        public string nativeName { get; set; }
        public string numericCode { get; set; }
        public List<Currency> currencies { get; set; }
        public List<Language> languages { get; set; }
        public Translations translations { get; set; }
        public string flag { get; set; }
        public List<RegionalBloc> regionalBlocs { get; set; }
        public string cioc { get; set; }

    }
}

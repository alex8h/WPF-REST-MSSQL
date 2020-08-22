using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using Countries.Model;

namespace Countries.ViewModel
{
    public class CountriesViewModel : ObservableObject
    {

        private static HttpClient client = new HttpClient();
        private static HttpRequestMessage request = new HttpRequestMessage();
        private static SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString);
        private DataTable dataTable = new DataTable("Countries");

        private JSON json;
        private CountriesModel model;
        private Visibility button_visibility;
        private Visibility textblock_visibility;
        public Visibility Button_Visibility
        {
            get => button_visibility;
            set
            {
                button_visibility = value;
                OnPropertyChanged(nameof(Button_Visibility));
            }
        }
        public Visibility Textblock_Visibility
        {
            get => textblock_visibility;
            set
            {
                textblock_visibility = value;
                OnPropertyChanged(nameof(Textblock_Visibility));
            }
        }
        public RelayCommand SearchCommand { get; protected set; }
        public RelayCommand SaveCommand { get; protected set; }
        public RelayCommand OutputDBCommand { get; protected set; }
        public CountriesViewModel()
        {
            model = new CountriesModel(); 
            Button_Visibility = Visibility.Hidden;
            Textblock_Visibility = Visibility.Hidden;
            SearchCommand = new RelayCommand(Unload_Country_From_REST);
            SaveCommand = new RelayCommand(Upload_To_DB);
            OutputDBCommand = new RelayCommand(Unload_Table_From_DB);
        }

        public CountriesModel Model
        {
            get => model;
            set
            {
                model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        public async void Unload_Country_From_REST()
        {
            string URL = "https://restcountries.eu/rest/v2/name/";
            Button_Visibility = Visibility.Hidden;
            Textblock_Visibility = Visibility.Hidden;
            if (Model.Input_Country != "")
            {
                request.RequestUri = new Uri(URL + Model.Input_Country);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");

                HttpResponseMessage response = await client.GetAsync(request.RequestUri);
                if (!response.IsSuccessStatusCode)
                {
                    Model.Rest_Message = "Not Found";
                }
                else
                {
                    var resp = await response.Content.ReadAsStringAsync();
                    json = JsonConvert.DeserializeObject<List<JSON>>(resp)[0];
                    Model.Rest_Message = "Name: " + json.name + "\n" + "Code: " + json.alpha2Code + "\n" + "Capital: " + json.capital + "\n" + "Region: " + json.region + "\n" + "Population: " + json.population + "\n" + "Area: " + json.area;
                    Button_Visibility = Visibility.Visible;
                }
            }

        }

        private void Upload_To_DB()
        {
            Button_Visibility = Visibility.Hidden;
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
                    Textblock_Visibility = Visibility.Visible;
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
                    Textblock_Visibility = Visibility.Visible;
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
                        "SET Capital =" + insert.Capital + ", Region=" + insert.Region + ", Population=" + json.population.ToString() + ", Area=" + json.area.ToString() +
                        "WHERE Code='" + json.alpha2Code + "'");
                }
                else
                {
                    insert.Id = Insert(String.Format("INSERT INTO Countries (Name, Code, Capital, Region, Population, Area) " +
                        "VALUES('{0}','{1}',{2},{3},{4},{5})", json.name, json.alpha2Code, insert.Capital, insert.Region, json.population.ToString(), json.area.ToString()) + "SELECT SCOPE_IDENTITY()");
                    Textblock_Visibility = Visibility.Visible;
                }

            }
            catch (SqlException ex)
            {
                Model.DB_Message = ex.Message;
            }
            finally
            {
                connection.Close();
            }
        }

        public void Unload_Table_From_DB()
        {
            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand();
                dataTable = Select("SELECT Countries.ID, Countries.Name, Code, Cities.Name, Regions.Name, Population, Area FROM Countries " +
                                   "JOIN Cities ON Countries.Capital = Cities.ID " +
                                   "JOIN Regions ON Countries.Region=Regions.ID",
                                   "Coutries");
                Model.Table_Of_Countries = null;
                Model.Table_Of_Countries = new ObservableCollection<Table>();
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    Model.Table_Of_Countries.Add(new Table
                    {
                        Id = dataTable.Rows[i][0].ToString(),
                        Name = dataTable.Rows[i][1].ToString(),
                        Code = dataTable.Rows[i][2].ToString(),
                        Capital = dataTable.Rows[i][3].ToString(),
                        Region = dataTable.Rows[i][4].ToString(),
                        Population = dataTable.Rows[i][5].ToString(),
                        Area = dataTable.Rows[i][6].ToString()
                    });
                }
                Model.DB_Message = "";

            }
            catch (SqlException err)
            {
                Model.DB_Message = err.Message;
            }
            finally
            {
                connection.Close();
            }
        }

        private DataTable Select(string request, string table_name)
        {
            DataTable table = new DataTable(table_name);
            SqlCommand command = new SqlCommand
            {
                CommandText = request,
                Connection = connection
            };
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
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Countries.Model
{
    public class CountriesModel : ObservableObject
    {
        private ObservableCollection<Table> countries;
        private string input_country;
        private string db_message;
        private string rest_message;

        public CountriesModel()
        {
            input_country = "";
            db_message = "";
            rest_message = "";
        }

        public string Input_Country
        {
            get => input_country;
            set
            {
                input_country = value;
                OnPropertyChanged(nameof(Input_Country));
            }
        }

        public string Rest_Message
        {
            get
            {
                return rest_message;
            }
            set
            {
                rest_message = value;
                OnPropertyChanged(nameof(Rest_Message));
            }
        }
        public ObservableCollection<Table> Table_Of_Countries
        {
            get
            {
                return countries;
            }
            set
            {
                countries = value;
                OnPropertyChanged(nameof(Table_Of_Countries));
            }
        }
        public string DB_Message
        {
            get => db_message;
            set
            {
                db_message = value;
                OnPropertyChanged(nameof(DB_Message));
            }
        }
    }
}

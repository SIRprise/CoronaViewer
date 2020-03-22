using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoronaView
{
    public class CCountryDataset
    {
        public string countryName = string.Empty;
        public List<CDateValue> container = new List<CDateValue>();

        public override string ToString()
        {
            string valueString = String.Join("; ", container);
            return valueString;
        }
    }

    public class CDateValue
    {
        public DateTime date;
        public int value;
        public double increaseRate = 0;

        public CDateValue(DateTime date, int value)
        {
            this.date = date;
            this.value = value;
        }

        public override string ToString()
        {
            return date.ToShortDateString() + ": " + value;
        }
    }
}

using System.Collections.Generic;

namespace CoronaView
{
    public interface ICImporter
    {
        Dictionary<string, CCountryDataset> Import();
    }
}
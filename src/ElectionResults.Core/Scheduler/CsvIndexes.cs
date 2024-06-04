using System.Collections.Generic;
using System.Threading.Tasks;
using CsvHelper;

namespace ElectionResults.Core.Scheduler
{
    public class CsvIndexes
    {
        private Dictionary<string, int> _columns = new Dictionary<string, int>();

        public int EligibleVotersIndex { get; set; }
        public int TotalVotesIndex { get; set; }
        public int NullVotesIndex { get; set; }
        public int ValidVotesIndex { get; set; }
        public int SirutaIndex { get; set; }
        public int CountryNameIndex { get; set; }
        public int CandidatesIndex { get; set; }
        public int NullVotesIndex2 { get; set; }
        private CsvMode CsvMode { get; set; }
        public CsvIndexes(CsvMode csvMode)
        {
            if (csvMode == CsvMode.National || csvMode == CsvMode.Diaspora)
                SetIndexesForNationalResults();
            else
            {
                SetIndexesForCorrespondenceResults();
            }

            CsvMode = csvMode;
        }

        private void SetIndexesForNationalResults()
        {
            EligibleVotersIndex = 12;
            TotalVotesIndex = 16;
            NullVotesIndex = 23;
            ValidVotesIndex = 22;
            SirutaIndex = 5;
            CountryNameIndex = 4;
            CandidatesIndex = 25;
        }

        private void SetIndexesForCorrespondenceResults()
        {
            EligibleVotersIndex = 12;
            TotalVotesIndex = 13;
            NullVotesIndex = 15;
            NullVotesIndex2 = 21;
            ValidVotesIndex = 20;
            SirutaIndex = 5;
            CountryNameIndex = 4;
            CandidatesIndex = 22;
        }

        public Task Map(CsvReader csvParser)
        {
            var index = 0;
            while (index < CandidatesIndex)
            {
                var field = csvParser.GetField(index);
                if (CsvMode == CsvMode.National)
                {
                    switch (field)
                    {
                        case "a":
                            EligibleVotersIndex = index;
                            break;
                        case "b":
                            TotalVotesIndex = index;
                            break;
                        case "e":
                            ValidVotesIndex = index;
                            break;
                        case "f":
                            NullVotesIndex = index;
                            break;
                        case "g":
                            NullVotesIndex2 = index;
                            break;

                    }
                }
                else if (CsvMode == CsvMode.Diaspora)
                {
                    switch (field)
                    {
                        case "a":
                            EligibleVotersIndex = index;
                            break;
                        case "b":
                            TotalVotesIndex = index;
                            break;
                        case "e":
                            ValidVotesIndex = index;
                            break;
                        case "f":
                            NullVotesIndex = index;
                            break;
                        case "g":
                            NullVotesIndex2 = index;
                            break;

                    }
                }
                else if (CsvMode == CsvMode.Correspondence)
                {
                    NullVotesIndex2 = 0;
                    switch (field)
                    {
                        case "a":
                            EligibleVotersIndex = index;
                            break;
                        case "b":
                            TotalVotesIndex = index;
                            break;
                        case "d1":
                            ValidVotesIndex = index;
                            break;
                        case "d2":
                            NullVotesIndex = index;
                            break;

                    }
                }
                index++;
            }

            return Task.CompletedTask;
        }
    }
}
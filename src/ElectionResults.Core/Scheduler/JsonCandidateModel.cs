using System.Collections.Generic;

namespace ElectionResults.Core.Scheduler
{
    public class LocationResult
    {
        public List<CandidateModel> Candidates { get; set; }

        public int Siruta { get; set; }
    }

    public class CandidateModel
    {
        public string Name { get; set; }

        public int Votes { get; set; }

        public string Party { get; set; }
    }

    public class JsonCandidateModel
    {
        public int? uat_siruta { get; set; }

        public Field[] Fields { get; set; }
        public Vote[] Votes { get; set; }
        public string report_id { get; set; }
        public string report_version { get; set; }
        public object precinct_id { get; set; }
        public object precinct_nr { get; set; }
        public object precinct_name { get; set; }
        public string uat_id { get; set; }
        public string uat_name { get; set; }
        public string county_id { get; set; }
        public string county_code { get; set; }
        public string county_nce { get; set; }
        public string county_name { get; set; }
        public object precinct_county_id { get; set; }
        public object precinct_county_code { get; set; }
        public object precinct_county_name { get; set; }
        public object precinct_county_nce { get; set; }
        public string report_type_scope_code { get; set; }
        public string report_type_code { get; set; }
        public string report_type_category_code { get; set; }
        public string report_stage_code { get; set; }
    }

    public class Field
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Vote
    {
        public string Candidate { get; set; }
        public string Party { get; set; }
        public int Votes { get; set; }

        public int Mandates1 { get; set; }

        public int Mandates2 { get; set; }
    }

}

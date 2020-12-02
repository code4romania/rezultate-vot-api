using System.Collections.Generic;
using System.Text;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Repositories;
using Microsoft.Extensions.Options;

namespace ElectionResults.Core.Elections
{
    public class LiveElectionUrlBuilder : ILiveElectionUrlBuilder
    {
        private readonly LiveElectionSettings _settings;
        private readonly Dictionary<(BallotType, ElectionDivision), string> _electionFileData = new Dictionary<(BallotType, ElectionDivision), string>();

        public LiveElectionUrlBuilder(IOptions<LiveElectionSettings> options)
        {
            _settings = options.Value;

            _electionFileData[(BallotType.Mayor, ElectionDivision.Locality)] = "uat_p";
            _electionFileData[(BallotType.LocalCouncil, ElectionDivision.Locality)] = "uat_cl";
            _electionFileData[(BallotType.CountyCouncil, ElectionDivision.County)] = "cnty_cj";
            _electionFileData[(BallotType.CountyCouncilPresident, ElectionDivision.County)] = "cnty_pcj";
            _electionFileData[(BallotType.Mayor, ElectionDivision.County)] = "cnty_pcj";
        }

        public Result<string> GetFileUrl(BallotType ballotType, ElectionDivision division, string countyShortName, int? siruta)
        {
            if (_electionFileData.ContainsKey((ballotType, division)) == false)
            {
                return Result.Failure<string>($"Key {ballotType}-{division} not allowed");
            }
            StringBuilder builder = new StringBuilder();
            builder.Append($"https://prezenta.roaep.ro/locale27092020/data/csv/sicpv/pv");
            builder.Append($"_{_settings.ResultsType}");
            builder.Append($"_{_electionFileData[(ballotType, division)]}");
            if (countyShortName.IsNotEmpty())
            {
                builder.Append($"_{countyShortName.ToLower()}");
            }
            if (siruta != null)
            {
                builder.Append($"_{siruta}");
            }

            builder.Append(".csv");
            var url = builder.ToString();
            return url;
        }
    }
}
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
        private readonly Dictionary<(BallotType, ElectionDivision), string> _territoryCodes = new Dictionary<(BallotType, ElectionDivision), string>();

        public LiveElectionUrlBuilder(IOptions<LiveElectionSettings> options)
        {
            _settings = options.Value;

            // Local elections
            _territoryCodes[(BallotType.Mayor, ElectionDivision.Locality)] = "uat_p";
            _territoryCodes[(BallotType.LocalCouncil, ElectionDivision.Locality)] = "uat_cl";
            _territoryCodes[(BallotType.CountyCouncil, ElectionDivision.County)] = "cnty_cj";
            _territoryCodes[(BallotType.CountyCouncilPresident, ElectionDivision.County)] = "cnty_pcj";
            _territoryCodes[(BallotType.Mayor, ElectionDivision.County)] = "cnty_pcj";

            // https://code4storage.blob.core.windows.net/results/pv_part_cnty_s_ab.csv
            // Parliament election
            _territoryCodes[(BallotType.Senate, ElectionDivision.County)] = "cnty_s"; // county
            _territoryCodes[(BallotType.Senate, ElectionDivision.Diaspora_Country)] = "cntry_s"; // diaspora
            _territoryCodes[(BallotType.Senate, ElectionDivision.Diaspora_Country)] = "cntry_sc"; // correspondence

            _territoryCodes[(BallotType.House, ElectionDivision.County)] = "cnty_cd"; // county
            _territoryCodes[(BallotType.House, ElectionDivision.Diaspora_Country)] = "cntry_cd"; // diaspora
            _territoryCodes[(BallotType.House, ElectionDivision.Diaspora_Country)] = "cntry_cdc"; // correspondence
        }

        public Result<string> GetFileUrl(BallotType ballotType, ElectionDivision division, string countyShortName, int? siruta)
        {
            if (_territoryCodes.ContainsKey((ballotType, division)) == false)
            {
                return Result.Failure<string>($"Key {ballotType}-{division} not allowed");
            }
            StringBuilder builder = new StringBuilder();
            builder.Append(_settings.ResultsUrl);
            builder.Append("pv");
            builder.Append($"_{_settings.ResultsType}");
            builder.Append($"_{_territoryCodes[(ballotType, division)]}");
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
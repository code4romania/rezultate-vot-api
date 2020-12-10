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
        private Dictionary<(BallotType, ElectionDivision), string> _territoryCodes;

        public LiveElectionUrlBuilder(IOptions<LiveElectionSettings> options)
        {
            _settings = options.Value;

            // Local elections
            CreateDefaultCodes();
        }

        private void CreateDefaultCodes()
        {
            _territoryCodes = new Dictionary<(BallotType, ElectionDivision), string>();
            _territoryCodes[(BallotType.Mayor, ElectionDivision.Locality)] = "uat_p";
            _territoryCodes[(BallotType.LocalCouncil, ElectionDivision.Locality)] = "uat_cl";
            _territoryCodes[(BallotType.CountyCouncil, ElectionDivision.County)] = "cnty_cj";
            _territoryCodes[(BallotType.CountyCouncilPresident, ElectionDivision.County)] = "cnty_pcj";
            _territoryCodes[(BallotType.Mayor, ElectionDivision.County)] = "cnty_pcj";

            // https://code4storage.blob.core.windows.net/results/pv_part_cnty_s_ab.csv
            // Parliament election
            _territoryCodes[(BallotType.Senate, ElectionDivision.County)] = "cnty_s"; // county
            _territoryCodes[(BallotType.Senate, ElectionDivision.Diaspora)] = "cnty_s_sr"; // division

            _territoryCodes[(BallotType.House, ElectionDivision.County)] = "cnty_cd"; // county
            _territoryCodes[(BallotType.House, ElectionDivision.Diaspora)] = "cnty_cd_sr"; // division
        }

        public Result<string> GetFileUrl(BallotType ballotType, ElectionDivision division, string shortName, int? siruta)
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
            if (shortName.IsNotEmpty())
            {
                builder.Append($"_{shortName.ToLower()}");
            }
            if (siruta != null)
            {
                builder.Append($"_{siruta}");
            }

            builder.Append(".csv");
            var url = builder.ToString();
            return url;
        }

        public Result<string> GetCorrespondenceUrl(BallotType ballotType, ElectionDivision division)
        {
            _territoryCodes[(BallotType.Senate, ElectionDivision.Diaspora)] = "cnty_sc_sr";
            _territoryCodes[(BallotType.House, ElectionDivision.Diaspora)] = "cnty_cdc_sr";
            var correspondenceUrl = GetFileUrl(ballotType, division, null, null);
            CreateDefaultCodes();
            return correspondenceUrl;
        }
    }
}
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalyticsReportingApi
{
    public class PopularUrlService
    {
        private readonly AppSettings _configs;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public PopularUrlService(AppSettings config)
        {
            _configs = config;
        }

        /// <summary>
        /// read google analytics information for most popular articles
        /// </summary>
        /// <returns><see cref="Tuple{T1, T2}"/> representing: (pageUrl, numberOfViews)</returns>
        internal List<(string, int)> GetParsedResult()
        {
            var response = QueryAnalyticsApi();
            if (response.Reports == null || response.Reports.Count == 0)
            {
                throw new Exception("GA query didn't included any result");
            }
            var items = new List<(string, int)>();

            var responseReport = response.Reports[0].Data;
            foreach (var item in responseReport.Rows)
            {
                var pageUrl = item.Dimensions.FirstOrDefault();
                var pageViews = item.Metrics.Select(x => x.Values.Select(v => v).FirstOrDefault()).FirstOrDefault();
                if (int.TryParse(pageViews, out var pageViewsNr))
                {
                    items.Add((pageUrl, pageViewsNr));
                }
            }
            return items;
        }

        private GetReportsResponse QueryAnalyticsApi()
        {
            if (string.IsNullOrEmpty(_configs.GoogleConfigFilePath))
                throw new Exception("google credentials config file path is not provided");

            string json = System.IO.File.ReadAllText(_configs.GoogleConfigFilePath);
            var credentialsObj = JsonConvert.DeserializeObject<GoogleCredentialsModel>(json);

            var xCred = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(credentialsObj.Client_email)
            {
                Scopes = new[] { AnalyticsReportingService.Scope.AnalyticsReadonly }
            }.FromPrivateKey(credentialsObj.Private_key));

            var baseClientInitializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = xCred,
                ApplicationName = _configs.ApplicationName
            };

            using (var svc = new AnalyticsReportingService(baseClientInitializer))
            {
                var pageViewsRequest = new ReportRequest
                {
                    ViewId = _configs.ViewId,
                    Dimensions = new List<Dimension> { new Dimension { Name = "ga:pagePath" } },
                    Metrics = new List<Metric> { new Metric { Expression = "ga:pageviews", Alias = "PageViews" } },
                    OrderBys = new List<OrderBy> { new OrderBy { FieldName = "ga:pageviews", SortOrder = "descending" } },
                    HideValueRanges = true,
                };

                if (StartDate.HasValue && EndDate.HasValue)
                {
                    pageViewsRequest.DateRanges = new List<DateRange>() { new DateRange() { StartDate = StartDate.Value.ToString("yyyy-MM-dd"), EndDate = EndDate.Value.ToString("yyyy-MM-dd") } };
                }
                if (!string.IsNullOrEmpty(_configs.Filter))
                    pageViewsRequest.FiltersExpression = $"ga:pagePath={_configs.Filter}";
                if (_configs.PageSize != 0)
                    pageViewsRequest.PageSize = _configs.PageSize;

                // Create the GetReportsRequest object.
                GetReportsRequest getReport = new GetReportsRequest()
                {
                    ReportRequests = new List<ReportRequest>
                    {
                        pageViewsRequest
                    }
                };

                var response = svc.Reports.BatchGet(getReport).Execute();
                return response;
            }
        }
    }
}

// See https://aka.ms/new-console-template for more information

using GitHubReader;
using Newtonsoft.Json;

internal class Program
{
    private static readonly HttpClient client = new();

    private static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run <owner> <repo> [<github-token>]");
            return;
        }

        var owner = args[0];
        var repo = args[1];
        var token = args.Length == 3 ? args[2] : null;

        try
        {
            var averageTime = await CalculateAverageOpenTime(owner, repo, token);
            Console.WriteLine($"The average time a pull request is open for {owner}/{repo} is {averageTime} hours.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task<double> CalculateAverageOpenTime(string owner, string repo, string token)
    {
        var pullRequests = await GetPullRequests(owner, repo, token);
        double totalOpenTime = 0;
        var count = 0;

        foreach (var pr in pullRequests)
        {
            if (pr.State == "closed" && pr.MergedAt.HasValue)
            {
                var openTime = CalculateTimeSpan(pr.CreatedAt, pr.MergedAt.Value, true);
                Console.WriteLine($"PR was open for {openTime.Hours}:{openTime.Minutes}.");
                totalOpenTime += openTime.TotalHours;
                count++;
            }
        }

        return count > 0 ? totalOpenTime / count : 0;
    }

    private static TimeSpan CalculateTimeSpan(DateTime start, DateTime end, bool removeWeekends = false)
    {
        var result = end - start;

        if (removeWeekends)
        {
            var weekendDays = 0;

            for (var date = start; date.Date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    weekendDays++;
                }
            }

            var daysToRemove = TimeSpan.FromDays(weekendDays);
            result = result.Add(-daysToRemove);
        }

        return result;
    }

    private static async Task<List<PullRequest>> GetPullRequests(string owner, string repo, string token)
    {
        var pullRequests = new List<PullRequest>();
        var page = 1;
        const int perPage = 100;

        while (true)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/pulls?state=all&per_page={perPage}&page={page}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "dotnet-app");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"token {token}");
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var prs = JsonConvert.DeserializeObject<List<PullRequest>>(responseBody);

            if (prs.Count == 0)
            {
                break;
            }

            pullRequests.AddRange(prs);
            page++;
        }

        return pullRequests;
    }
}

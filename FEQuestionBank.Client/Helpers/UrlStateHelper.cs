// File: Client/Helpers/UrlStateHelper.cs

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace FEQuestionBank.Client.Helpers
{
    public static class UrlStateHelper
    {
        public static Dictionary<string, string> GetQueryParams(NavigationManager nav)
        {
            var uri = nav.ToAbsoluteUri(nav.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);
            return query.ToDictionary(k => k.Key, v => v.Value.ToString());
        }

        public static void UpdateUrl(NavigationManager nav, Dictionary<string, string?> newParams)
        {
            var uri = nav.Uri.Split('?')[0];
            var current = GetQueryParams(nav);

            foreach (var p in newParams)
            {
                if (p.Value == null)
                    current.Remove(p.Key);
                else
                    current[p.Key] = p.Value;
            }

            if (current.Any())
                uri = QueryHelpers.AddQueryString(uri, current);

            nav.NavigateTo(uri, forceLoad: false);
        }
    }
}
/*
 * Copyright 2019 University of Oxford
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * SPDX-License-Identifier: Apache-2.0
 */
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Oxford.Brc.Clinical.Data.Warehouse.Properties;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Oxford.Brc.Clinical.Data.Warehouse
{
    public class RedactionDictionary
    {
        private const string BoilerPlateRegexQuery = "select boilerplate_regex from data_products.safeguard.report_removal_boilerplate_regex";
        private const string ReportRemovalRegexQuery = "select report_regex from data_products.safeguard.report_removal_regex";

        private static RedactionDictionary _instance;
        private readonly List<string> _masterDictionary = new List<string>();
        private readonly string _prefixedData;

        private bool _boilerPlateRegexLoadedFromDatabase;
        private bool _reportRemovalRegexLoadedFromDatabase;

        private RedactionDictionary()
        {
            _prefixedData = $"[Init:{DateTime.UtcNow:o}] ";

            LoadMasterDictionary();

            LoadBoilerPlateFromFile();

            LoadRegexCache();

            _boilerPlateRegexLoadedFromDatabase = false;
        }

        public List<Regex> RegexCache { get; } = new List<Regex>();

        public List<Regex> BoilerPlateRegexCache { get; } = new List<Regex>();

        public List<Regex> ReportRemovalRegexCache { get; } = new List<Regex>();

        public static RedactionDictionary GetInstance()
        {
            return _instance ?? (_instance = new RedactionDictionary());
        }

        public static void ResetInstance()
        {
            _instance = null;
        }

        public int BinarySearch(string item)
        {
            return _masterDictionary.BinarySearch(item);
        }

        public void AddToBoilerPlate(string item)
        {
            BoilerPlateRegexCache.Add(new Regex(item, RegexOptions.Compiled));
        }

        public void AddToReportRemoval(string item)
        {
            if (!item.StartsWith("^")) item = $"^{item}";
            if (!item.EndsWith("$")) item = $"{item}$";
            ReportRemovalRegexCache.Add(new Regex(item, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }

        public string ReloadBoilerPlateCache()
        {
            _boilerPlateRegexLoadedFromDatabase = false;
            BoilerPlateRegexCache.Clear();
            LoadBoilerPlateFromFile();
            return AttemptToLoadBoilerPlateRegexFromDatabase();
        }

        public string ReloadReportRemovalCache()
        {
            _reportRemovalRegexLoadedFromDatabase = false;
            ReportRemovalRegexCache.Clear();
            return AttemptToLoadReportRemovalRegexFromDatabase();
        }

        public string AttemptToLoadBoilerPlateRegexFromDatabase()
        {
            // Already loaded then return null
            if (_boilerPlateRegexLoadedFromDatabase) return null;

            // Otherwise try to load from the database
            try
            {
                using (var connection = new SqlConnection("context connection=true"))
                {
                    var cmd = new SqlCommand(BoilerPlateRegexQuery, connection);
                    connection.Open();
                    // Use the connection
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) AddToBoilerPlate(reader[0].ToString());
                    }
                }

                _boilerPlateRegexLoadedFromDatabase = true;
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return null;
        }

        public string AttemptToLoadReportRemovalRegexFromDatabase()
        {
            // Already loaded then return null
            if (_reportRemovalRegexLoadedFromDatabase) return null;

            // Otherwise try to load from the database
            try
            {
                using (var connection = new SqlConnection("context connection=true"))
                {
                    var cmd = new SqlCommand(ReportRemovalRegexQuery, connection);
                    connection.Open();
                    // Use the connection
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) AddToReportRemoval(reader[0].ToString());
                    }
                }

                _reportRemovalRegexLoadedFromDatabase = true;
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return null;
        }

        private void LoadMasterDictionary()
        {
            foreach (var word in Resources.words_alpha.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
                _masterDictionary.Add(word.Trim().ToLowerInvariant());

            // Taken from https://github.com/glutanimate/wordlist-medicalterms-en
            foreach (var word in Resources.medical_terms.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
                _masterDictionary.Add(word.Trim().ToLowerInvariant());

            foreach (var word in Resources.custom_dictionary.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
                _masterDictionary.Add(word.Trim().ToLowerInvariant());

            foreach (var word in Resources.medical_acronyms.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
                _masterDictionary.Add(word.Trim().ToLowerInvariant());

            _masterDictionary.Sort();
        }

        private void LoadBoilerPlateFromFile()
        {
            foreach (var marker in Resources.custom_full_text_markers.Split(new[] {"\n"},
                StringSplitOptions.RemoveEmptyEntries))
                BoilerPlateRegexCache.Add(new Regex(marker.Trim(), RegexOptions.Compiled));
        }

        private void LoadRegexCache()
        {
            const string weekdaysRegex = @"(Sun|Mon|Tue|Wed|Thu|Fri|Sat)";
            const string monthDayRegex = @"((0?[1-9]|[1-2][0-9]|3[01])(st|nd|rd|th)?)";
            var wordedMonthRegex =
                "((Jan(uary)?)|(Feb(ruary)?)|(Mar(ch)?)|(Apr(il)?)|May|(Jun(e)?)|(Jul(y)?)|(Aug(ust)?)|(Sep(tember)?)|(Oct(ober?))|(Nov(ember)?)|(Dec(ember)?))";
            wordedMonthRegex = $"({wordedMonthRegex}|{wordedMonthRegex.ToUpperInvariant()})";
            const string numberedMonthRegex = @"((0?[1-9]|1[0-2]))";
            const string yearRegex = "(19[0-9]{2}|[2-9][0-9]{3}|[0-9]{2})";
            const string timeRegex = @"(\s+(2[0-3]|[0-1]?[0-9]):([0-5][0-9])(:(60|[0-5][0-9]))?)";
            const string timezoneRegex = @"(([-\\+][0-9]{2}[0-5][0-9]|(?:UT|GMT|(?:E|C|M|P)(?:ST|DT)|[A-IK-Z])))";

            var dateTimeFormats = new[]
            {
                $@"\s*{weekdaysRegex}?{monthDayRegex}[.\\/-]\s*{numberedMonthRegex}[.\\/-]\s*{yearRegex}{timeRegex}?{timezoneRegex}?",
                $@"\s*{weekdaysRegex}?{monthDayRegex}[ .\\/-]{wordedMonthRegex}[ .\\/-]{yearRegex}{timeRegex}?{timezoneRegex}?",
                $@"\s*{weekdaysRegex}?{numberedMonthRegex}[.\\/-]{yearRegex}[ .\\/-]{timeRegex}?{timezoneRegex}?",
                $@"\s*{weekdaysRegex}?{wordedMonthRegex}[ .\\/-]{monthDayRegex}[ .\\/-]{timeRegex}?{timezoneRegex}?\s+{yearRegex}",
                $@"\s*{wordedMonthRegex}\s*{yearRegex}([ .\\/-]{timeRegex}{timezoneRegex}?)?"
            };

            var streetAbbreviations = new[]
            {
                "Ave",
                "Blvd",
                "Bdwy",
                "Cir",
                "Cl",
                "Ct",
                "Cr",
                "Dr",
                "Gdn",
                "Gdns",
                "Gn",
                "Gr",
                "Ln",
                "Mt",
                "Pl",
                "Pk",
                "Rdg",
                "Rd",
                "Sq",
                "St",
                "Ter",
                "Val"
            };

            var streetTypeRegexes = streetAbbreviations.Select(i =>
            {
                var regex = new StringBuilder("(\\s");
                foreach (var c in i) regex.Append($"[{c.ToString().ToUpperInvariant()}{c.ToString().ToLowerInvariant()}]");

                regex.Append(".)");
                return regex.ToString();
            }).ToArray();

            // date/times
            RegexCache.Add(new Regex(string.Join("|", dateTimeFormats), RegexOptions.Compiled));
            // phone numbers
            RegexCache.Add(new Regex(@"(([0]|((\+|00)[0-9]{1-3}))[0-9][0-9][0-9]\s*[0-9]\s*[0-9][0-9]\s*[0-9]\s*[0-9][0-9][0-9])",
                RegexOptions.Compiled));
            // nhs numbers
            RegexCache.Add(new Regex(@"([0-9][0-9][0-9][ -]?[0-9][0-9][0-9][ -]?[0-9][0-9][0-9][0-9])", RegexOptions.Compiled));
            RegexCache.Add(new Regex(@"([0-9][0-9][0-9][ -][0-9][0-9][0-9][0-9][ -][0-9][0-9][0-9])", RegexOptions.Compiled));
            // email addresses
            RegexCache.Add(new Regex(
                @"((?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\]))",
                RegexOptions.Compiled));
            // post codes
            RegexCache.Add(new Regex(
                $@"(([,\sa-zA-Z0-9]|{string.Join("|", streetTypeRegexes)})*[,]\s)?(([gG][iI][rR] {{0,}}0[aA]{{2}})|((([a-pr-uwyzA-PR-UWYZ][a-hk-yA-HK-Y]?[0-9][0-9]?)|(([a-pr-uwyzA-PR-UWYZ][0-9][a-hjkstuwA-HJKSTUW])|([a-pr-uwyzA-PR-UWYZ][a-hk-yA-HK-Y][0-9O][abehmnprv-yABEHMNPRV-Y])))\s*[0-9O][abd-hjlnp-uw-zABD-HJLNP-UW-Z]{{2}}))",
                RegexOptions.Compiled));
        }
    }
}
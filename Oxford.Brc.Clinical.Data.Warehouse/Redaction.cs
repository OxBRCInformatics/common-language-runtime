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
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Oxford.Brc.Clinical.Data.Warehouse
{
    public class Redaction
    {
        private readonly List<string> _blacklist;

        private Redaction()
        {
            _blacklist = new List<string>();
        }

        // ReSharper disable once UnusedMember.Global
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        [return: SqlFacet]
        public static string ReloadBoilerPlateCache()
        {
            return RedactionDictionary.GetInstance().ReloadBoilerPlateCache();
        }

        // ReSharper disable once UnusedMember.Global
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        [return: SqlFacet]
        public static string ReloadReportRemovalCache()
        {
            return RedactionDictionary.GetInstance().ReloadReportRemovalCache();
        }

        // ReSharper disable once UnusedMember.Global
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        [return: SqlFacet]
        public static string CleanBoilerPlate(string report)
        {
            var redactionTool = new Redaction();

            // Make sure the database boilerplate is loaded.
            // This will only run once per dictionary instance therefore use the ReloadBoilerPlateCache method to refresh from the database
            var b = RedactionDictionary.GetInstance().AttemptToLoadBoilerPlateRegexFromDatabase();
            return b ?? redactionTool.CleanOutAllBoilerPlate(report);
        }

         // ReSharper disable once UnusedMember.Global
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        [return: SqlFacet]
        public static string WipeoutReportsMatchingRemovalMarkers(string report)
        {
            var redactionTool = new Redaction();

            // Make sure the database report removal is loaded.
            // This will only run once per dictionary instance therefore use the ReloadReportRemovalCache method to refresh from the database
            var b = RedactionDictionary.GetInstance().AttemptToLoadReportRemovalRegexFromDatabase();
            return b ?? redactionTool.PerformReportWipeout(report);
        }

        [SqlFunction]
        [return: SqlFacet]
        public static string Redact(string report, string csvScrubStrings)
        {
            var redactionTool = new Redaction();
            foreach (var scrub in csvScrubStrings.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)) redactionTool.Scrub(scrub);

            return redactionTool.PerformRedaction(report);
        }

        private string PerformRedaction(string originalReport)
        {
            var words = new List<string>();
            var wordBuilder = new StringBuilder();

            var newReportBuilder = new StringBuilder();
            var newReportProcessing = originalReport;

            foreach (var regex in RedactionDictionary.GetInstance().RegexCache)
                newReportProcessing = regex.Replace(newReportProcessing, " [REDACTED]");

            var characterGap = 0;
            foreach (var c in newReportProcessing)
                if (char.IsLetter(c) || (c == '-' || c == '.') && wordBuilder.Length > 0)
                {
                    wordBuilder.Append(c);
                    if (c == '-' || c == '.')
                        characterGap++;
                    else
                        characterGap = 0;
                }
                else
                {
                    if (wordBuilder.Length > 0) CompleteWord(wordBuilder, newReportBuilder, words, characterGap);

                    newReportBuilder.Append(c);
                }

            if (wordBuilder.Length > 0) CompleteWord(wordBuilder, newReportBuilder, words, characterGap);

            var newReport = newReportBuilder.ToString();
            for (var i = 0; i < words.Count; i++)
            {
                if (!DictionaryContains(words[i])) words[i] = "[REDACTED]";

                newReport = newReport.Replace($"{{{i}}}", words[i]);
            }

            // Proven init only happens the first time the code is called after its been reloaded from file
            return newReport.Trim();
        }

        private string CleanOutAllBoilerPlate(string originalReport)
        {
            var cleanedReport = originalReport.Trim();

            foreach (var regex in RedactionDictionary.GetInstance().BoilerPlateRegexCache) cleanedReport = regex.Replace(cleanedReport, "");

            return cleanedReport.Trim();
        }

        private string PerformReportWipeout(string report)
        {
            var trimmed = report.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;

            foreach (var regex in RedactionDictionary.GetInstance().ReportRemovalRegexCache)
            {
                if (regex.IsMatch(trimmed))
                    return null;
            }

            return trimmed;
        }

        private bool DictionaryContains(string word)
        {
            var lower = word.ToLowerInvariant();

            if (BlacklistContains(lower)) return false;

            if (RedactionDictionary.GetInstance().BinarySearch(lower) > -1) return true;

            if (!word.Contains('-')) return false;

            var subwords = lower.Split('-');

            foreach (var subword in subwords)
            {
                if (BlacklistContains(lower)) return false;
                if (RedactionDictionary.GetInstance().BinarySearch(subword) > -1) return true;
            }

            return false;
        }

        private static void CompleteWord(StringBuilder wordBuilder, StringBuilder newReportBuilder, List<string> words, int characterGap)
        {
            var newWord = wordBuilder.ToString();
            newReportBuilder.Append($"{{{words.Count}}}");
            words.Add(newWord.Substring(0, wordBuilder.Length - characterGap));
            if (characterGap > 0) newReportBuilder.Append(newWord.Substring(wordBuilder.Length - characterGap, characterGap));

            wordBuilder.Clear();
        }

        private void Scrub(string word)
        {
            AddToBlacklist(word.ToLowerInvariant());
            AddToBlacklist(word.ToLowerInvariant() + "s");
        }

        private void AddToBlacklist(string value)
        {
            _blacklist.Add(value);
        }

        private bool BlacklistContains(string value)
        {
            return _blacklist.Contains(value);
        }

        public static string TestCleanBoilerPlate(string report)
        {
            var redactionTool = new Redaction();

            // Make sure the database boilerplate is loaded.
            // This will only run once per dictionary instance therefore use the ReloadBoilerPlateCache method to refresh from the database
            return redactionTool.CleanOutAllBoilerPlate(report);
        }

        public static string TestWipeoutReportsMatchingRemovalMarkers(string report)
        {
            var redactionTool = new Redaction();

            // Make sure the database report removal is loaded.
            // This will only run once per dictionary instance therefore use the ReloadReportRemovalCache method to refresh from the database
            return redactionTool.PerformReportWipeout(report);
        }
    }
}
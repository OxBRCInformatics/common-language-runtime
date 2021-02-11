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
using System.Text;
using Microsoft.SqlServer.Server;

namespace Oxford.Brc.Clinical.Data.Warehouse
{
    public static class Cleaning
    {
        // ReSharper disable once UnusedMember.Global
        [SqlFunction]
        [return: SqlFacet(MaxSize = 10, IsNullable = true)]
        public static string CleanAndValidateNhsNumber(string nhsNumberStr)
        {
            if (string.IsNullOrEmpty(nhsNumberStr)) return null;

            var stringBuilder = new StringBuilder();
            foreach (var c in nhsNumberStr)
                if (char.IsDigit(c))
                    stringBuilder.Append(c);

            // Remove all non-numeric characters
            var cleanedNhsNumberStr = stringBuilder.ToString();
            // Check for validity against NHS Number algorithm, return cleaned string or null
            if (IsValidNhsNumber(cleanedNhsNumberStr))
                // If valid NHS number check against known test/unassigned NHS range
                return long.Parse(cleanedNhsNumberStr) % 1111111111L == 0L ? null : cleanedNhsNumberStr;

            return null;
        }

        // ReSharper disable once UnusedMember.Global
        [SqlFunction]
        [return: SqlFacet(IsNullable = true)]
        public static string Trim(string value)
        {
            var clean = value?.Trim();
            return string.IsNullOrEmpty(clean) ? null : clean;
        }

        private static bool IsValidNhsNumber(string nhsNumberStr)
        {
            // If not 10 digits then return null
            if (nhsNumberStr.Length != 10) return false;

            // This method is adapted from 
            // http://www.evilscience.co.uk/checking-the-validity-of-an-nhs-number-using-1-line-of-c/
            var remainder = 0;
            var index = 0;
            foreach (var value in nhsNumberStr.Substring(0, 9))
            {
                remainder += (10 - index) * (value - 48);
                index++;
            }

            remainder %= 11;

            return nhsNumberStr[9] - 48 == (remainder == 0 ? 0 : 11 - remainder);
        }
    }
}
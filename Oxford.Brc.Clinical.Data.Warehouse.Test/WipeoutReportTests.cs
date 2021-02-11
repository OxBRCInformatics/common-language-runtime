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
using Xunit;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Oxford.Brc.Clinical.Data.Warehouse.Test
{
    public class WipeoutReportTests
    {
        [Fact]
        public void TestWipeoutNoContent()
        {
            Assert.Null(Wipeout(""));
        }

        [Fact]
        public void TestWipeoutPeriod()
        {
            RedactionDictionary.ResetInstance();
            Add("\\.+");
            Assert.Null(Wipeout("."));
            Assert.Null(Wipeout(".........."));
            Assert.Null(Wipeout("......"));
        }

        [Fact]
        public void TestWipeoutText()
        {
            RedactionDictionary.ResetInstance();
            Add("^CT\\s\\w+\\s:\\s*\n*.?$");

            Assert.Null(Wipeout("CT Chest :"));
            Assert.Null(Wipeout(@"CT Chest :

                ."));
            Assert.Null(Wipeout(@"CT Chest :
                ."));
            Assert.Null(Wipeout(@"CT Chest : ."));
            Assert.Null(Wipeout(@"CT Chest : 1"));
            Assert.Null(Wipeout(@"CT Head :"));
            Assert.Null(Wipeout(@"CT Head : 
                ."));
            Assert.Null(Wipeout(@"CT Head : ."));
        }

        private static void Add(string regex)
        {
            RedactionDictionary.GetInstance().AddToReportRemoval(regex);
        }

        private static string Wipeout(string report)
        {
            return Redaction.TestWipeoutReportsMatchingRemovalMarkers(report);
        }
    }
}
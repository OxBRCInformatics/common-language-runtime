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
using System.Collections.Generic;
using System.Linq;
using Xunit;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Oxford.Brc.Clinical.Data.Warehouse.Test
{
    public class CleaningTests
    {
        [Fact]
        public void TestNhsNumberCleaning()
        {
            // All invalid so should be null
            Assert.Null(Cleaning.CleanAndValidateNhsNumber(null));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber(""));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("sdfghjkl"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("12345"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("12345rtyuio"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("1234567890"));

            Assert.Equal("1103396005", Cleaning.CleanAndValidateNhsNumber("1103396005"));
            Assert.Equal("1126883867", Cleaning.CleanAndValidateNhsNumber("1126883867"));
            Assert.Equal("1653224894", Cleaning.CleanAndValidateNhsNumber("1653224894"));
            Assert.Equal("1704571790", Cleaning.CleanAndValidateNhsNumber("1704571790"));
            Assert.Equal("1167742664", Cleaning.CleanAndValidateNhsNumber("1167742664"));
            Assert.Equal("1420642626", Cleaning.CleanAndValidateNhsNumber("1420642626"));
            Assert.Equal("1201545757", Cleaning.CleanAndValidateNhsNumber("1201545757"));
            Assert.Equal("1619090120", Cleaning.CleanAndValidateNhsNumber("1619090120"));
            Assert.Equal("1713647656", Cleaning.CleanAndValidateNhsNumber("1713647656"));
            Assert.Equal("1896417531", Cleaning.CleanAndValidateNhsNumber("1896417531"));

            Assert.Null(Cleaning.CleanAndValidateNhsNumber("0000000000"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("1111111111"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("2222222222"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("3333333333"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("4444444444"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("5555555555"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("6666666666"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("7777777777"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("8888888888"));
            Assert.Null(Cleaning.CleanAndValidateNhsNumber("9999999999"));
        }

        [Fact]
        public void TestRandom()
        {
            var t = new List<string>();
            var output = new SortedSet<string>(t);
            Assert.Equal("||", $"|{string.Join("|", output)}|");

            t.Add("hello");
            t.Add(null);
            output = new SortedSet<string>(t.Where(Utils.IsNotNullAndNotEmpty));
            Assert.Equal("|hello|", $"|{string.Join("|", output)}|");
        }

        [Fact]
        public void TestTrim()
        {
            Assert.Null(Cleaning.Trim(""));
            Assert.Null(Cleaning.Trim(" "));
            Assert.Null(Cleaning.Trim("   "));
            
            Assert.Equal("hello",Cleaning.Trim("hello"));
            Assert.Equal("hello",Cleaning.Trim("hello "));
            Assert.Equal("hello",Cleaning.Trim(" hello"));
            Assert.Equal("hello",Cleaning.Trim(" hello "));
            Assert.Equal("hello",Cleaning.Trim("hello\n"));
            Assert.Equal("hello",Cleaning.Trim("\nhello"));
            Assert.Equal("hello",Cleaning.Trim("\thello"));
            Assert.Equal("hello",Cleaning.Trim("\t \thello"));
            Assert.Equal("hello",Cleaning.Trim("\n \thello"));
            Assert.Equal("hello",Cleaning.Trim("\n    hello"));
        }
    }
}
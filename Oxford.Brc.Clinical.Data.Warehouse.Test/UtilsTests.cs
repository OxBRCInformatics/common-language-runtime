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
    public class UtilsTests
    {
        [Fact]
        public void TestIsNullOrEmpty()
        {
            Assert.True(Utils.IsNullOrEmpty(""));
            Assert.True(Utils.IsNullOrEmpty(null));
            Assert.True(Utils.IsNullOrEmpty("   "));
            Assert.True(Utils.IsNullOrEmpty("\t  "));
            Assert.True(Utils.IsNullOrEmpty("\n"));
            Assert.False(Utils.IsNullOrEmpty("hello"));
            Assert.False(Utils.IsNullOrEmpty(" hello "));
        }
        
        [Fact]
        public void TestIsNotNullAndNotEmpty()
        {
            Assert.False(Utils.IsNotNullAndNotEmpty(""));
            Assert.False(Utils.IsNotNullAndNotEmpty(null));
            Assert.False(Utils.IsNotNullAndNotEmpty("   "));
            Assert.False(Utils.IsNotNullAndNotEmpty("\t  "));
            Assert.False(Utils.IsNotNullAndNotEmpty("\n"));
            Assert.True(Utils.IsNotNullAndNotEmpty("hello"));
            Assert.True(Utils.IsNotNullAndNotEmpty(" hello "));
        }
    }
}
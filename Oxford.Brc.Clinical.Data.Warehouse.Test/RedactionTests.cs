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
using Xunit.Abstractions;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Oxford.Brc.Clinical.Data.Warehouse.Test
{
    public class RedactionTests
    {
        private readonly ITestOutputHelper _output;

        public RedactionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestRedaction()
        {
            Assert.Equal("Hello my name is [REDACTED] [REDACTED]. [REDACTED]",
                Redaction.Redact("Hello my name is John Smith. 77 Broadway Ave., Oxford, OX12 9GE", "John,Smith"));
            Assert.Equal("Hello my name is [REDACTED] [REDACTED]. I have an appointment with a doctor. [REDACTED]",
                Redaction.Redact("Hello my name is John Smith. I have an appointment with a doctor. 77 Broadway Ave., Oxford, OX12 9GE",
                    "John,Smith"));
        }

        [Fact]
        public void TestNhsNumberRedaction()
        {
            Assert.Equal("[REDACTED]  [REDACTED]  [REDACTED]",
                Redaction.Redact("1234567891 123 456 7891 123-456-7891", ""));
        }

        [Fact]
        public void AcronymTest()
        {
            var redacted = Redact("the plaque increases to 25% in distal CCA.");
            Assert.Equal("the plaque increases to 25% in distal CCA.", redacted);
        }

        [Fact]
        public void AcronymTestFullText()
        {
            var redacted = Redact(
                "US Doppler Right Carotid Artery : Intimal thickening seen along common carotid artery; the plaque increases to 25% in distal CCA.  Nothing abnormal noted from origin of internal carotid artery; flow signals were within normal limits.  External carotid artery appears normal but small.  Normal antegrade flow signals detected in vertebral artery.");
            Assert.Equal(
                "US Doppler Right Carotid Artery : Intimal thickening seen along common carotid artery; the plaque increases to 25% in distal CCA.  Nothing abnormal noted from origin of internal carotid artery; flow signals were within normal limits.  External carotid artery appears normal but small.  Normal antegrade flow signals detected in vertebral artery.",
                redacted);
        }

        [Fact]
        public void AnotherAcronymTestFullText()
        {
            var redacted = Redact(
                @"CAROTID DUPLEX.
LEFT SIDE.  Plaques seen along common carotid artery; the stenosis appears 30%.  Abnormal high resistance flow signals detected in CCA suggest severe distal disease.  Extensive plaque seen at bifurcation extending into both internal and external carotid arteries.  Flow signals in internal carotid artery were markedly enhanced indicating stenosis of >80%.  Distal internal carotid artery appears normal.  External carotid artery appears >70% stenosed.  Enhanced antegrade flow signals detected in vertebral artery.
RIGHT SIDE.  Plaques seen along common carotid artery; the stenosis appears 30%.  Plaque seen in internal carotid artery.  Approximately 1.1cm from the origin, flow signals were markedly enhanced indicating stenosis of >80%.  Distal internal carotid artery appears normal.  External carotid artery appears >60% stenosed.  Normal antegrade flow signals detected in vertebral artery.
A copy of this report will be sent to Mr J. Bloggs.");
            Assert.Equal(
                @"CAROTID DUPLEX.
LEFT SIDE.  Plaques seen along common carotid artery; the stenosis appears 30%.  Abnormal high resistance flow signals detected in CCA suggest severe distal disease.  Extensive plaque seen at bifurcation extending into both internal and external carotid arteries.  Flow signals in internal carotid artery were markedly enhanced indicating stenosis of >80%.  Distal internal carotid artery appears normal.  External carotid artery appears >70% stenosed.  Enhanced antegrade flow signals detected in vertebral artery.
RIGHT SIDE.  Plaques seen along common carotid artery; the stenosis appears 30%.  Plaque seen in internal carotid artery.  Approximately 1.1cm from the origin, flow signals were markedly enhanced indicating stenosis of >80%.  Distal internal carotid artery appears normal.  External carotid artery appears >60% stenosed.  Normal antegrade flow signals detected in vertebral artery.
A copy of this report will be sent to Mr J. [REDACTED].",
                redacted);
        }

        [Fact]
        public void NothingToRedact()
        {
            var text = @"CAROTID DUPLEX.
LEFT SIDE. Smooth plaque seen along common carotid artery; the stenosis appears 35-40%.  Calcified plaque seen in internal carotid artery origin; flow signals were within normal limits and the stenosis appears 35-40%.  Distal internal carotid artery appears normal.  External carotid artery appears normal.  The vertebral artery was not seen.
RIGHT SIDE.  Smooth plaque seen along common carotid artery; the stenosis appears 25%.  Plaque seen in internal carotid artery origin; flow signals were within normal limits and the stenosis appears 35-40%.  Distal internal carotid artery appears normal.  External carotid artery appears 30% stenosed.  Normal antegrade flow signals detected in vertebral artery.";
            var redacted = Redact(text);
            Assert.Equal(text,
                redacted);
        }

        [Fact]
        public void AdditionalEnglishTerms()
        {
            var redacted = Redact(
                @"US Doppler Carotid Arteries : 
Normal blood flow velocities obtained from both carotid vessels.
Normal cephaled flow noted in both vertebral artery, in particular, the waveform demonstrated pulsatile forward flow throughout the cardiac cycle.

Reported by Joe Bloggs, Superintendent Vascular Sonographer.
");
            Assert.Equal(
                @"US Doppler Carotid Arteries : 
Normal blood flow velocities obtained from both carotid vessels.
Normal cephaled flow noted in both vertebral artery, in particular, the waveform demonstrated pulsatile forward flow throughout the cardiac cycle.

Reported by [REDACTED] [REDACTED], Superintendent Vascular Sonographer.",
                redacted);
        }

        [Fact]
        public void AddtlDateFormat()
        {
            var redacted = Redact(
                @"CAROTID DUPLEX.
Irregular heart rate noted.
Plaques seen along both common carotid arteries, the stenosis appears 20-25%.
Left internal carotid artery is 15% stenosed. The right internal carotid artery is difficult to visualise due to depth but velocities are normal and stenosis is therefore <40%.
External carotid arteries are 20-25% stenosed.
No change from previous scan in December 1994.
");
            Assert.Equal(
                @"CAROTID DUPLEX.
Irregular heart rate noted.
Plaques seen along both common carotid arteries, the stenosis appears 20-25%.
Left internal carotid artery is 15% stenosed. The right internal carotid artery is difficult to visualise due to depth but velocities are normal and stenosis is therefore <40%.
External carotid arteries are 20-25% stenosed.
No change from previous scan in [REDACTED].",
                redacted);

            redacted = Redact(
                @"CAROTID DUPLEX.
Irregular heart rate noted.
Plaques seen along both common carotid arteries, the stenosis appears 20-25%.
Left internal carotid artery is 15% stenosed. The right internal carotid artery is difficult to visualise due to depth but velocities are normal and stenosis is therefore <40%.
External carotid arteries are 20-25% stenosed.
No change from previous scan in 11th May 1999.
");
            Assert.Equal(
                @"CAROTID DUPLEX.
Irregular heart rate noted.
Plaques seen along both common carotid arteries, the stenosis appears 20-25%.
Left internal carotid artery is 15% stenosed. The right internal carotid artery is difficult to visualise due to depth but velocities are normal and stenosis is therefore <40%.
External carotid arteries are 20-25% stenosed.
No change from previous scan in [REDACTED].",
                redacted);
        }

        [Fact]
        public void DotSeparatedDates()
        {
            var redacted = Redact(
                @"XR Chest :  Comparison film 11.3.96.

Stable position of the right internal jugular Tesio line.
There is mild unchanged right basal atelectasis. The lungs are otherwise clear.
The heart is enlarged (CTR 13/56).
");
            Assert.Equal(
                @"XR Chest :  Comparison film [REDACTED].

Stable position of the right internal jugular Tesio line.
There is mild unchanged right basal atelectasis. The lungs are otherwise clear.
The heart is enlarged (CTR 13/56).",
                redacted);
        }

        [Fact]
        public void HypenSeparatedDates()
        {
            var redacted = Redact(
                @"29-Mar-2020");
            Assert.Equal(
                @"[REDACTED]",
                redacted);
        }

        [Fact]
        public void Check()
        {
            var redacted = Redact(
                @"XR Chest : The right inter-costal drain has been removed. Small persistent right pleural effusion and overlying consolidation. There is a left pleural effusion which has increased since the previous film. Hiatus hernia noted. 
");
            Assert.Equal(
                @"XR Chest : The right inter-costal drain has been removed. Small persistent right pleural effusion and overlying consolidation. There is a left pleural effusion which has increased since the previous film. Hiatus hernia noted.",
                redacted);
        }

        private string Redact(string report)
        {
            var redacted = Redaction.Redact(report, "");
            _output?.WriteLine("REDACTED TEXT:\n---\n{0}\n---", redacted);
            return redacted;
        }
    }
}
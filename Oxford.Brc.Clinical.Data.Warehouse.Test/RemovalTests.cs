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
using Xunit;
using Xunit.Abstractions;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Oxford.Brc.Clinical.Data.Warehouse.Test
{
    public class RemovalTests
    {
        private readonly ITestOutputHelper _output;

        private static readonly List<string> Regexes = new List<string>
        {
            "^.$",
            "need to delete",
            "no gad given",
            "\\.(\\s|\n|\r)*for mdt discussion",
            "\\p{P}?(see?)?\\s*\\w+(\\s+\\w+)?\\s*\\p{P}?",
            "findings\\s*\\p{P}?",
            "conclusion\\s*\\p{P}?",
            "impression\\s*\\p{P}?",
            "please see (above|below) report\\.?",
            "see patient's notes for procedure details\\.",
            "solus report in( medical)? notes",
            "this report has been reprinted as part of radiology housekeeping measures - there have been no changes to the body of the report",
            "^\\p{P}+((\\s|\\n|\\r)*\\p{P}+)?$",
            "^\\p{P}+\\s+(a|of|the|there is|is)$",
            "^\\[redacted\\]( report in notes|5.)?$",
            "^~i.~i$",
            "^\\d+\\p{P}?$",
            "^\\p{P}?(\\s|\n|\r)*(ct|mri|mra|us|xr)(\\s+\\w+)+\\s*[:.](\\s|\n|\r)*(the|and|see above|\\d+)?\\p{P}?$",
            "^\\w+(\\s+\\w+){0,2}\\s*:(\\s|\n|\r)*\\p{P}?$"
        };

        public RemovalTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestAllCleaned()
        {
            RedactionDictionary.ResetInstance();
            foreach (var word in Regexes)
            {
                RedactionDictionary.GetInstance().AddToReportRemoval(word);
            }

            var needToClean = new List<string>
            {
                "",
                "'",
                "-",
                "(As above)",
                "****",
                ",",
                ",.",
                ".",
                ". A",
                ". of",
                ". The",
                ". There is",
                @".
.",
                "..",
                ".. is",
                "...",
                "....",
                ".....",
                "......",
                ".......",
                "........",
                ".........",
                "..........",
                "...........",
                "../......",
                "/",
                "[REDACTED]",
                "[REDACTED] report in notes",
                "[REDACTED]5.",
                "~I.~i",
                ">",
                "0.",
                "5531",
                "A",
                "ADDITIONAL",
                "Angio  Cerebral :",
                "As above",
                "as above.",
                "As below",
                "As below.",
                "Auto reported",
                "Conclusion :",
                "CT Abdomen :",
                "CT Angiogram :",
                "CT Ankle Lt :",
                "CT Ankle Rt :",
                "CT Chest :",
                @"CT Chest : 

.",
                @"CT Chest : 
.",
                "CT Chest : .",
                "CT Chest : 1",
                "CT Face :",
                "CT Face : .",
                "CT Foot Rt :",
                "CT Head :",
                @"CT Head : 
.",
                "CT Head : .",
                "CT Head :.",
                "CT Hip Rt : .",
                "CT Knee Lt :",
                "CT Knee Lt : .",
                "CT Knee Rt :",
                "CT Knee Rt : ,",
                "CT Liver :",
                "CT Mandible :",
                "CT Neck :",
                "CT Orbits :",
                @"CT Orbits : 
.",
                "CT Pancreas :",
                "CT Pelvis :",
                "CT Sinuses :",
                "CT Sinuses : .",
                "CT Urogram : .",
                "CT Venogram :",
                // "DR NC COWAN",
                // "DR NIGEL COWAN",
                "F:",
                "Findings.",
                "Findings:",
                //  "GA Dr Speirs",
                "Impression:",
                "MRA Head :",
                @"MRA Head : 
.",
                "MRA Head : .",
                "MRA Head : The",
                "MRA Head :.",
                "MRCP :",
                "MRI Abdomen :",
                "MRI Ankle Rt :",
                "MRI cancelled",
                "MRI consultation :",
                "MRI Elbow Lt :",
                "MRI Femur Lt :",
                "MRI Femur Rt :",
                "MRI Foot Lt :",
                "MRI Foot Rt :",
                "MRI Foot Rt :.",
                "MRI Head",
                "MRI Head :",
                @"MRI Head : 
.",
                "MRI Head : .",
                "MRI Head : and",
                "MRI Head : See above.",
                "MRI Head : The",
                "MRI Head :.",
                "MRI Head.",
                "MRI Hip Both :",
                "MRI Hip Lt :",
                "MRI Hip Rt :",
                "MRI Knee Rt :",
                "MRI Marrow :",
                "MRI Neck :",
                @"MRI Neck : 
.",
                "MRI Neck : .",
                "MRI Orbits :",
                "MRI Orbits : .",
                "MRI Pancreas :",
                "MRI Pelvis :",
                "MRI Pelvis : .",
                "MRI Pituitary",
                "MRI Renal :",
                "MRI Sinuses :",
                "MRI Sinuses.",
                "MRI Spine Cervical : see above",
                "MRI Thorax :",
                "MRI Thorax : .",
                "MRI Wrist Lt :",
                "MRI Wrist Rt :",
                "Need to delete",
                "no gad given",
                "no images",
                //"Norma brain CT",
                "not repeated",
                "Not scanned.",
                "Open MRI",
                "Please see above report.",
                "please see below report",
                "PRIVATE REPORT",
                "PROTOCOL 1",
                "r",
                "Research MRI",
                "Research Scan",
                "se above",
                "se ct colon",
                "see",
                "see above",
                "See above .",
                "See above&",
                "see above.",
                "see below",
                "See below.",
                "see Ct colon",
                "See CT Head.",
                "See CT report",
                "See CT Urogram",
                "See MRI neck",
                "See patient's notes for procedure details.",
                "see report",
                "See report.",
                "see right",
                "see the report",
                "seen below",
                "She above",
                "SOLUS report in medical notes",
                "solus report in notes",
                "The",
                "This report has been reprinted as part of Radiology housekeeping measures - there have been no changes to the body of the report",
                "v",
                "X",
                ".CT Pancreas : .",
                @".
For MDT discussion",
                @".
MRI Pelvis with Gadolinium :",
                "incorrect booking.",
                "XR Skeletal Survey General :",
                "XR Skeletal Survey Myeloma :"
            };

            foreach (var report in needToClean)
            {
                var wiped = WipeoutReports(report);
                Assert.Null(wiped);
            }
        }

        [Fact]
        public void TestFailsCleaned()
        {
            RedactionDictionary.ResetInstance();
            foreach (var word in Regexes)
            {
                RedactionDictionary.GetInstance().AddToReportRemoval(word);
            }

            Assert.NotNull(WipeoutReports(@"Name of the Procedure: CT Head

Exam Date: [REDACTED]

Title/Technique: CT head

Clinical Indication: 10 days post expressive dysphasia followed by headache

Findings: bilateral basal ganglia calcification. Symmetrical ventricles cisterns and sulci. No intracranial mass lesion or recent haemorrhage.

Comment: no evidence of a recent focal event."));
        }

        private string WipeoutReports(string report)
        {
            var clean = Redaction.TestWipeoutReportsMatchingRemovalMarkers(report);
            if (clean != null) _output?.WriteLine("FAILED TEXT:\n---\n{0}\n---", clean);
            return clean;
        }
    }
}
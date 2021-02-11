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
    public class BoilerPlateTests
    {
        private readonly ITestOutputHelper _output;

        public BoilerPlateTests(ITestOutputHelper output)
        {
            _output = output;
            //CRIS
            const string marker = @"\*\*\* This exam record has been migrated from the 'old CRIS'.+\*\*\*";
            RedactionDictionary.GetInstance().AddToBoilerPlate(marker);
            // Radiology
            RedactionDictionary.GetInstance().AddToBoilerPlate(@"^Radiology Report(\n.+)+\nReferring Consultant:.+");
            RedactionDictionary.GetInstance().AddToBoilerPlate(@"\nReporting Consultant:\s*(\n.+){3}\n?$");
            // Dexa
            RedactionDictionary.GetInstance()
                .AddToBoilerPlate(
                    "\n((Name|Patient ID|Age|Indication|Referring Physician|Study|Accession number|Referring Provider|Clinical Indications|NHS):.+\n+)+");
        }

        [Fact]
        public void TestRemovalOfOddTildaUChars()
        {
            var cleaned = CleanBoilerPlate(
                @"~URight knee:~u Advanced OA noted: significant lateral and moderate medial tibiofemoral and patellofemoral compartment joint space narrowing, patella osteophyte formation and medial compartment chondrocalcinosis. Old laterql plateau fracture is noted. There is impression of osteopenia. almost unchanged compared to 21/01/2021.
~ULeft knee:~u TKR. In comparison to patient's 15/12/2015, posterolateral subluxation of the tibial element is again noted. Lateral tibiofemoral compartment is much wider. Referral for orthopedic opinion is needed. 
");
            Assert.Equal(
                @"Right knee: Advanced OA noted: significant lateral and moderate medial tibiofemoral and patellofemoral compartment joint space narrowing, patella osteophyte formation and medial compartment chondrocalcinosis. Old laterql plateau fracture is noted. There is impression of osteopenia. almost unchanged compared to 21/01/2021.
Left knee: TKR. In comparison to patient's 15/12/2015, posterolateral subluxation of the tibial element is again noted. Lateral tibiofemoral compartment is much wider. Referral for orthopedic opinion is needed.",
                cleaned);
        }

        [Fact]
        public void TestRemovalOfOddTildaBChars()
        {
            var cleaned = CleanBoilerPlate(
                @"~BXR Chest :~b no previous chest x-rays available for comparison.

Heart size normal.  
The lungs are hyperinflated.
There is a partially well circumscribed 4 cm nodule projected over the right mid zone.

~BConclusion:~b The appearances raise the suspicion of a lung neoplasm. A chest clinic referral for further investigation is advised.


");
            Assert.Equal(@"XR Chest : no previous chest x-rays available for comparison.

Heart size normal.  
The lungs are hyperinflated.
There is a partially well circumscribed 4 cm nodule projected over the right mid zone.

Conclusion: The appearances raise the suspicion of a lung neoplasm. A chest clinic referral for further investigation is advised.", cleaned);
        }

        [Fact]
        public void CleaningOldCrisMarker()
        {
            var cleaned = CleanBoilerPlate(
                @"US Doppler Carotid Arteries : 
Normal blood flow velocities obtained from both carotid vessels.
Normal cephaled flow noted in both vertebral artery, in particular, the waveform demonstrated pulsatile forward flow throughout the cardiac cycle.

*** This exam record has been migrated from the 'old CRIS' and will not be updated in any way after 13th April 2013. For an up-to-date copy of the exam report please contact the originating Trust. ***
");
            Assert.Equal(
                @"US Doppler Carotid Arteries : 
Normal blood flow velocities obtained from both carotid vessels.
Normal cephaled flow noted in both vertebral artery, in particular, the waveform demonstrated pulsatile forward flow throughout the cardiac cycle.",
                cleaned);
        }

        [Fact]
        public void TestBoilerPlateRemoval()
        {
            var cleaned = CleanBoilerPlate(@"Radiology Report
Report Date: 14/07/2010 12:15
Patient Name:  DOE JANE
D.O.B: 21/07/1978
NHS No. /Unique Identifier: RTH123456
Referring Hospital: Horton Hospital
Referring Consultant: BOB HOB

Name of the Procedure: CT Head

Exam Date: 22th June 2020

Title/Technique: CT head

Clinical Indication: 15 days post expressive dysphasia followed by headache

Findings: bilateral basal ganglia calcification. Symmetrical ventricles cisterns and sulci. No intracranial mass lesion or recent haemorrhage.

Comment: no evidence of a recent focal event.


Reporting Consultant: 
Joe Bloggs
Consultant Radiologist
4 Ways Healthcare
");
            Assert.Equal(
                @"Name of the Procedure: CT Head

Exam Date: 22th June 2020

Title/Technique: CT head

Clinical Indication: 15 days post expressive dysphasia followed by headache

Findings: bilateral basal ganglia calcification. Symmetrical ventricles cisterns and sulci. No intracranial mass lesion or recent haemorrhage.

Comment: no evidence of a recent focal event.",
                cleaned);

            var redacted = Redact(cleaned);

            Assert.Equal(@"Name of the Procedure: CT Head

Exam Date: [REDACTED]

Title/Technique: CT head

Clinical Indication: 15 days post expressive dysphasia followed by headache

Findings: bilateral basal ganglia calcification. Symmetrical ventricles cisterns and sulci. No intracranial mass lesion or recent haemorrhage.

Comment: no evidence of a recent focal event.", redacted);
        }

        [Fact]
        public void TestDexaBoilerPlateRemoval()
        {
            var cleaned = CleanBoilerPlate(@" Bone Density Report

Name: DOE, JANE Sex: Female
Patient ID: 345678 Ethnicity: White
Age: 99 Date of Birth: 12/11/2164
Indication: Follow- up patient, NHS 534 4534 544
Referring Physician: BLOGGS JOHN, OX14 3LB
Study: Bone densitometry was performed.
Accession number: RBF2345678

Bone Density:
Region Exam Date BMD
(g/cm2) T-Score Z-Score Classification
AP Spine (L1-L4) 25/05/2019 1.234  1.2  3.4 Normal
Femoral Neck (Left) 25/05/2019 0.987  6.5  4.3 Normal
Total Hip (Left) 25/05/2019 3.456  7.8  9.0 Normal
World Health Organization criteria for BMD interpretation classify patients as Normal (T-score at or above ?1.0), Osteopenic (T-score between ?1.0 and ?2.5), or Osteoporotic (T-score at or below ?2.5). 



Previous Exams:  
Region Exam
Date Age BMD (g/cm2) T-Score BMD Change vs. Baseline BMD Change vs. Previous
AP Spine(L1-L4) 30/04/2010 34 1.234  5.6 7.8%# 9.0%#
 30/04/2020 39 1.234  5.6  
Total Hip(Left) 30/04/2010 21 2.543  6.5 8.7%# 0.9%#
 30/04/2010 12 1.234  5.6  
# Denotes dissimilar scan types or analysis methods



Interpretation:
The patient has normal bone density as determined by WHO criteria. 
The T score for the hip and spine are both greater than -1 and there is no added fracture risk.

RATE of CHANGE
There have been insignificant increases in both the spine and hip BMD measurements since the previous scans.

50 year Fracture Risk
    Without prior fracture  With prior fracture
Major Osteoporotic Fracture  =  5.6%    7.8%
Hip Fracture              = <0.6%    <0.6%
¹ FRAX? Version 4.00. Fracture probability calculated for an untreated patient. Fracture probability may be lower if the patient has received treatment.

Unless there are any clinical changes, there is no need for further DEXA scans associated with Coeliac disease.

Reported by John Smith, Senior Radiographer
:   . 
");
            Assert.Equal(
                @"Bone Density Report
Bone Density:
Region Exam Date BMD
(g/cm2) T-Score Z-Score Classification
AP Spine (L1-L4) 25/05/2019 1.234  1.2  3.4 Normal
Femoral Neck (Left) 25/05/2019 0.987  6.5  4.3 Normal
Total Hip (Left) 25/05/2019 3.456  7.8  9.0 Normal
World Health Organization criteria for BMD interpretation classify patients as Normal (T-score at or above ?1.0), Osteopenic (T-score between ?1.0 and ?2.5), or Osteoporotic (T-score at or below ?2.5). 



Previous Exams:  
Region Exam
Date Age BMD (g/cm2) T-Score BMD Change vs. Baseline BMD Change vs. Previous
AP Spine(L1-L4) 30/04/2010 34 1.234  5.6 7.8%# 9.0%#
 30/04/2020 39 1.234  5.6  
Total Hip(Left) 30/04/2010 21 2.543  6.5 8.7%# 0.9%#
 30/04/2010 12 1.234  5.6  
# Denotes dissimilar scan types or analysis methods



Interpretation:
The patient has normal bone density as determined by WHO criteria. 
The T score for the hip and spine are both greater than -1 and there is no added fracture risk.

RATE of CHANGE
There have been insignificant increases in both the spine and hip BMD measurements since the previous scans.

50 year Fracture Risk
    Without prior fracture  With prior fracture
Major Osteoporotic Fracture  =  5.6%    7.8%
Hip Fracture              = <0.6%    <0.6%
¹ FRAX? Version 4.00. Fracture probability calculated for an untreated patient. Fracture probability may be lower if the patient has received treatment.

Unless there are any clinical changes, there is no need for further DEXA scans associated with Coeliac disease.

Reported by John Smith, Senior Radiographer
:   .",
                cleaned);
        }

        [Fact]
        public void TestDexaBoilerPlateRemoval2()
        {
            var cleaned = CleanBoilerPlate(@"  Bone Density and Vertebral Assessment Report

Name: DFSDFS, SDFSDFSD Sex: Female
Patient ID: 4234545645 Ethnicity: White
Age: 73 Date of Birth: 12/06/1234
Indication: New patient, NHS 234 342 7665
Referring Physician: DGDGDF FF, OX7 5AA, Copy to: Dr. FGDFGDF, Metabolic bones clinic, NOC
Study: Bone densitometry and vertebral deformity assessment were performed.
Accession number: RBF3245678

Bone Density:
Region Exam Date BMD
(g/cm2) T-Score Z-Score Classification
AP Spine (L1-L4) 01/02/1956 0.733 -2.9 -0.6 Osteoporotic
Femoral Neck (Left) 01/02/1956 0.587 -2.4 -0.4 Osteopenic
Total Hip (Left) 01/02/1956 0.630 -2.6 -0.9 Osteoporotic
World Health Organization criteria for BMD interpretation classify patients as Normal (T-score at or above ?1.0), Osteopenic (T-score between ?1.0 and ?2.5), or Osteoporotic (T-score at or below ?2.5). 




In accordance with the previously circulated guidelines of the Oxfordshire Osteoporosis Service (p.5) this patient will be referred to the osteoporosis clinic at the Nuffield Orthopaedic Centre.
Please advise, if this is not required.
Tel. 01234 456789 or 01234 456780




Interpretation:
At least one T score for the spine or hip is less than -2.5 and the Z score is less than -1.0.
These results confirm OSTEOPOROSIS with a high risk of fracture.

Treatment recommendations:
Lifestyle optimisation 
Ensure calcium replete (1000mg approx.) & Vitamin D replete (>25nmol/l)

Prescribe anti resorptive drugs (bisphosphonates).
Also check their biochemistry for secondary causes ie Blood / Urine investigations, Bone function (Serum calcium, phosphate, ALP, Albumin, 25OH vitamin D), Renal function, ALT / AST, FBC, ESR,TSH.
If there is an unexplained high ESR then do a Serum & urine electrophoretic strip.

Additional investigations if indicated:
* Coeliac screen if ever history of unexplained anaemia.
* 24 Hour urinary calcium (especially if hypercalcaemia / renal stones).
* 24 hour urinary cortisol.

Review therapy after 5 years.

 
Vertebral Deformity Assessment: Exam date 01/02/1956
Vertebral Level      Impression
T7       Normal
T8       Normal
T9       Normal
T10       Normal
T11       Normal
T12       Normal
L1       Normal
L2       Normal
L3       Normal
L4       Normal

A spine fracture indicates 5X risk for subsequent spine fracture and 2X risk for subsequent hip fracture.
No vertebral fractures identified.
NOTE: VFA is designed to detect vertebral fractures and not other abnormalities.

10 year Fracture Risk
    Without prior fracture  With prior fracture
Major Osteoporotic Fracture  =  25%    37%
Hip Fracture              = 8.4%    12%
¹ FRAX? Version 1.00. Fracture probability calculated for an untreated patient. Fracture probability may be lower if the patient has received treatment.


Repeat scan in 2 years.
Reported by Joe Bloggs, Senior Radiographer
:   . 
");
            Assert.Equal(
                @"Bone Density and Vertebral Assessment Report
Bone Density:
Region Exam Date BMD
(g/cm2) T-Score Z-Score Classification
AP Spine (L1-L4) 01/02/1956 0.733 -2.9 -0.6 Osteoporotic
Femoral Neck (Left) 01/02/1956 0.587 -2.4 -0.4 Osteopenic
Total Hip (Left) 01/02/1956 0.630 -2.6 -0.9 Osteoporotic
World Health Organization criteria for BMD interpretation classify patients as Normal (T-score at or above ?1.0), Osteopenic (T-score between ?1.0 and ?2.5), or Osteoporotic (T-score at or below ?2.5). 




In accordance with the previously circulated guidelines of the Oxfordshire Osteoporosis Service (p.5) this patient will be referred to the osteoporosis clinic at the Nuffield Orthopaedic Centre.
Please advise, if this is not required.
Tel. 01234 456789 or 01234 456780




Interpretation:
At least one T score for the spine or hip is less than -2.5 and the Z score is less than -1.0.
These results confirm OSTEOPOROSIS with a high risk of fracture.

Treatment recommendations:
Lifestyle optimisation 
Ensure calcium replete (1000mg approx.) & Vitamin D replete (>25nmol/l)

Prescribe anti resorptive drugs (bisphosphonates).
Also check their biochemistry for secondary causes ie Blood / Urine investigations, Bone function (Serum calcium, phosphate, ALP, Albumin, 25OH vitamin D), Renal function, ALT / AST, FBC, ESR,TSH.
If there is an unexplained high ESR then do a Serum & urine electrophoretic strip.

Additional investigations if indicated:
* Coeliac screen if ever history of unexplained anaemia.
* 24 Hour urinary calcium (especially if hypercalcaemia / renal stones).
* 24 hour urinary cortisol.

Review therapy after 5 years.

 
Vertebral Deformity Assessment: Exam date 01/02/1956
Vertebral Level      Impression
T7       Normal
T8       Normal
T9       Normal
T10       Normal
T11       Normal
T12       Normal
L1       Normal
L2       Normal
L3       Normal
L4       Normal

A spine fracture indicates 5X risk for subsequent spine fracture and 2X risk for subsequent hip fracture.
No vertebral fractures identified.
NOTE: VFA is designed to detect vertebral fractures and not other abnormalities.

10 year Fracture Risk
    Without prior fracture  With prior fracture
Major Osteoporotic Fracture  =  25%    37%
Hip Fracture              = 8.4%    12%
¹ FRAX? Version 1.00. Fracture probability calculated for an untreated patient. Fracture probability may be lower if the patient has received treatment.


Repeat scan in 2 years.
Reported by Joe Bloggs, Senior Radiographer
:   .",
                cleaned);
        }

        [Fact]
        public void TestDexaBoilerPlateRemoval3()
        {
            var cleaned = CleanBoilerPlate(@"Bone Density and Vertebral Assessment Report

Name: SDFSDFS, GDFGDF Sex: Female
Patient ID: RTH53454654 Ethnicity: White
Age: 23 Date of Birth: 04/11/2019

Indication: Follow-up patient NHS 343 563 3242
Referring Physician: FSFEWWER WRWER;Metabolic Bone Clinic, NOC
Study: Bone densitometry and vertebral deformity assessment were performed.
Accession number: RBF2345678987

Clinical Indications: The patient was recalled for a vertebral fracture assessment as this could not be completed at their previous appointment due to a scanner malfunction.


This patient will be reviewed by the Metabolic Bone Clinic and treatment recommendations if needed, will come from there.

Follow-up: Re-scan as required.
 
Vertebral Deformity Assessment: Exam date 12/10/2009
Vertebral Level      Impression
T9       Normal
T10       Normal
T11       Normal
T12       Normal
L1       Normal
L2       Normal
L3       Normal
L4       Normal

A spine fracture indicates 5X risk for subsequent spine fracture and 2X risk for subsequent hip fracture.
No vertebral fractures are identified on the vertebrae that have been assessed. The VFA is normal. 
Note: VFA is designed to detect vertebral fractures and not other abnormalities.

Reported by: DFDF DFGDFGDF, Reporting Radiographer on  

Name: SDFSDFS, GDFGDF Sex: Female Height: 154 cm
Patient ID: RTH53454654 Ethnicity: White Weight: 54  kg
Age: 23 Date of Birth: 04/11/2019
 
Scan Date: 16 July 2015
Scan ID: A12345678K
Scan Type: f SE Lateral Image
 
 
Scan Date: 16 July 2015
Scan ID: A12345678J
Scan Type: f SE AP Image

Name: SDFSDFS, GDFGDF Sex: Female Height: 154 cm
Patient ID: RTH53454654 Ethnicity: White Weight: 54  kg
Age: 43 Date of Birth: 04/11/2019 

Scan Date: 16 July 2015
Scan ID: A12345678K
Scan Type: f SE Lateral Image

Vertebral Evaluation:
Vertebrae Impression
T9 Normal
T10 Normal
T11 Normal
T12 Normal
L1 Normal
L2 Normal
L3 Normal
L4 Normal

Vertebral Assessment:
 Height (mm) Percent Deformation
Vertebrae Post Mid Ant Wedge Biconcave Crush
T9  18.6  16.1  16.5  11.1%  13.5%  0.0%
T10  20.1  17.8  18.7  7.2%  11.4%  0.0%
T11  21.8  19.8  19.2  12.0%  8.8%  0.0%
T12  21.5  19.4  21.6  0.0%  9.9%  0.4%
L1  24.5  21.9  22.3  8.8%  10.7%  0.0%
L2  26.0  22.3  25.4  2.4%  14.3%  0.0%
L3  25.1  22.5  25.7  0.0%  10.4%  2.4%
L4  25.7  22.7  26.9  0.0%  11.6%  4.5%
Std Dev   1.0   1.0   1.0   5.0%   5.0%   5.0%

 
 
 
");
            Assert.Equal(
                @"Bone Density and Vertebral Assessment Report
This patient will be reviewed by the Metabolic Bone Clinic and treatment recommendations if needed, will come from there.

Follow-up: Re-scan as required.
 
Vertebral Deformity Assessment: Exam date 12/10/2009
Vertebral Level      Impression
T9       Normal
T10       Normal
T11       Normal
T12       Normal
L1       Normal
L2       Normal
L3       Normal
L4       Normal

A spine fracture indicates 5X risk for subsequent spine fracture and 2X risk for subsequent hip fracture.
No vertebral fractures are identified on the vertebrae that have been assessed. The VFA is normal. 
Note: VFA is designed to detect vertebral fractures and not other abnormalities.

Reported by: DFDF DFGDFGDF, Reporting Radiographer on  
 
Scan Date: 16 July 2015
Scan ID: A12345678K
Scan Type: f SE Lateral Image
 
 
Scan Date: 16 July 2015
Scan ID: A12345678J
Scan Type: f SE AP Image
Scan Date: 16 July 2015
Scan ID: A12345678K
Scan Type: f SE Lateral Image

Vertebral Evaluation:
Vertebrae Impression
T9 Normal
T10 Normal
T11 Normal
T12 Normal
L1 Normal
L2 Normal
L3 Normal
L4 Normal

Vertebral Assessment:
 Height (mm) Percent Deformation
Vertebrae Post Mid Ant Wedge Biconcave Crush
T9  18.6  16.1  16.5  11.1%  13.5%  0.0%
T10  20.1  17.8  18.7  7.2%  11.4%  0.0%
T11  21.8  19.8  19.2  12.0%  8.8%  0.0%
T12  21.5  19.4  21.6  0.0%  9.9%  0.4%
L1  24.5  21.9  22.3  8.8%  10.7%  0.0%
L2  26.0  22.3  25.4  2.4%  14.3%  0.0%
L3  25.1  22.5  25.7  0.0%  10.4%  2.4%
L4  25.7  22.7  26.9  0.0%  11.6%  4.5%
Std Dev   1.0   1.0   1.0   5.0%   5.0%   5.0%",
                cleaned);
        }


        private string Redact(string report)
        {
            var redacted = Redaction.Redact(report, "");
            _output?.WriteLine("REDACTED TEXT:\n---\n{0}\n---", redacted);
            return redacted;
        }

        private string CleanBoilerPlate(string report)
        {
            var clean = Redaction.TestCleanBoilerPlate(report);
            _output?.WriteLine("CLEANED TEXT:\n---\n{0}\n---", clean);
            return clean;
        }
    }
}
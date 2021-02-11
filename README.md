# Common Language Runtime

This repository contains code for a common language runtime for use inside a MS SQL Server instance.

## Requirements

* SQL Server 2016+
* Target Framework 4.7.1

## Deployment

* Build the `Oxford.Brc.Clinical.Data.Warehouse` namespace
* Run the `database/ddl.sql` file inside SQL Server
* Deploy the built `Oxford.Brc.Clinical.Data.Warehouse/bin/Release/Oxford.Brc.Clinical.Data.Warehouse.dll` file into SQL Server
```tsql
CREATE ASSEMBLY OxBrcCdwFunctions
    FROM 'Oxford.Brc.Clinical.Data.Warehouse/bin/Release/Oxford.Brc.Clinical.Data.Warehouse.dll'
    WITH PERMISSION_SET = UNSAFE
```
* Load the desired SQL functions into the SQL Server from `database/functions`

## Available SQL Functions

* Files can be found in the `database/functions` directory.
* Functions in the SQL files are deployed into `utilties` database into the `oxbrccdw` schema. This can be changed by altering the SQL file before running.

### Cleaning

| Function Name | File | Description |
|---------------|------|-------------|
| `trim` | `trim.sql` | Removes all forms of whitespace from around a string. Returns `null` or cleaned string. |
| `cleanAndValidateNhsNumber` | `cleanAndValidateNhsNumber.sql` | Checks any number for its validity against the NHS number algorithm. Returns null or valid NHS number. |

### Redaction

| Function Name | File | Description |
|---------------|------|-------------|
| `reloadBoilerPlateCache` | `reloadBoilerPlateCache.sql` | Reloads the Boiler Plate entries from the database tables into the RedactionDictionary. |
| `reloadReportRemovalCache` | `reloadReportRemovalCache.sql` | Reloads the Report Removal entries from the database tables into the RedactionDictionary. |
| `cleanBoilerPlate` | `cleanBoilerPlate.sql` | Removes all boiler plate text from the provided report. |
| `redactConfidentials` | `redactConfidentials.sql` | Redacts all confidential data from the provided report text. Takes an comma separated string of known patient confidential data which should also be redacted. |
| `wipeoutReportsMatchingRemovalMarkers` | `wipeoutReportsMatchingRemovalMarkers.sql` | Checks provided report to see if it matches the known wipeout regex and returns `null` if it does. |

## How to Redact Reports

This methodology can be used to anonymise any free text, from simple single lines to full text reports.

It is a 3 stage process which is best performed by performing insert then updates, saving the result of each stage into a new column in the same table.

1. Clean boiler plate from the report
1. Redact confidential data from the report
1. Wipeout reports with meaningless data

Please be aware this process is not perfect with respect to patient names as there are names in the medical terms whitelist which if redacted would make a medical report impossible to understand as they are the names given to medical diagnoses.

### Process

We recommend extracting all reports of interest into a table before you start, this will allow easier identification of issues after redaction has been completed.

In the following code examples the schema `dp_xxx` has been chosen to denote a unique data product for which the deidentified reports will be submitted. If you have the server space you may wish to create a standalone schema with all the reports pre-deidentified.

### Identifiable Reports

Extract identifiable reports into the following suggested DDL (add additional columns as required).

```tsql
CREATE TABLE dp_xxx.identifiable_imaging_reports (
    report_id       UNIQUEIDENTIFIER DEFAULT newid() NOT NULL, -- Required
    patient_id      VARCHAR(32),   -- Required
    exam_date       DATETIME2,     -- Probably required
    date_reported   DATETIME2,     -- Probably required
    report          VARCHAR(MAX),  -- Required
    source          VARCHAR(30),
    phi             VARCHAR(255)   -- Required
)
GO
```

The `phi` column is a comma-separated list of the patient’s protected health information. This will be used later to improve redaction.

```tsql
phi = coalesce(p.nhs_number, '') + ',' +
      coalesce(p.mrn_number, '') + ',' +
      coalesce(p.surname, '') + ',' +
      coalesce(p.forenames, '') as phi
```

### Redaction Report Table

The following DDL is the suggested table format for the redacted reports (again add additional columns as required).

```tsql
CREATE TABLE dp_xxx.redacted_imaging_reports (
    report_id                  UNIQUEIDENTIFIER NOT NULL, -- Required
    patient_id                 VARCHAR(32),   -- Required
    offset_exam_date           DATETIME2,     -- Probably required
    offset_date_reported       DATETIME2,     -- Probably required
    boilerplate_cleaned_report VARCHAR(MAX),  -- Required
    redacted_report            VARCHAR(MAX),  -- Required
    wiped_report               VARCHAR(MAX),  -- Required
    source                     VARCHAR(64),    
    phi                        VARCHAR(255)   -- Required
)
GO
```

### 1. Clean boilerplate

This step is designed to remove any boilerplate text from reports. The boilerplate is based on regular expressions, and is loaded from `data_products.safeguard.report_removal_boilerplate_regex`.

**Please see the [section about the redaction cache](#redaction-cache)**

The following SQL is suggested for performing step 1 of the redaction to clean the boilerplate. It will insert each report with the boilerplate cleaned and the dates offset into the `redacted_imaging_reports` table.

```tsql
INSERT INTO
    dp_xxx.redacted_imaging_reports
SELECT
    img.cdw_report_id,
    img.cdw_patient_id,
    convert(DATE, (dateadd(DAY, mpi.day_offset, img.exam_date)), 103)     AS offset_exam_date,
    convert(DATE, (dateadd(DAY, mpi.day_offset, img.date_reported)), 103) AS offset_date_reported,
    utilities.cdw.cleanBoilerPlate(report)                                AS boilerplate_cleaned_report,
    NULL                                                                  AS redacted_report,
    NULL                                                                  AS wiped_report,
    img.source,
    img.phi
FROM
    dp_xxx.identifiable_imaging_reports_angio img
        INNER JOIN dp_xxx.cohort coh ON img.cdw_patient_id = coh.cdw_patient_id
        INNER JOIN dp_xxx.cohort_mpi_salting mpi ON mpi.master_patient_id = coh.master_patient_id
```

### 2. Redact Report

This step is designed to perform the actual redaction of the reports. It will perform whitelist checking using the `phi` column as a blacklist, any redacted data will be replaced with `[REDACTED]`. The following SQL is suggested for performing step 2 of the redaction to perform the redaction. It will update each row setting the redacted column in the table.

**Please see the [section about the whitelist dictionaries](#whitelist-dictionaries)**

```tsql
UPDATE dp_xxx.redacted_imaging_reports
SET
    redacted_report = utilities.cdw.redactConfidentials(boilerplate_cleaned_report, phi),
    wiped_report = null
```

### 3. Wipeout Reports

This step is designed to wipeout all “empty” reports. This is done using regular expressions loaded from `data_products.safeguard.report_removal_regex`.

**Please see the [section about the redaction cache](#redaction-cache)**

The following SQL is suggested for performing step 3 of the redaction to wipeout the empty reports. It will update each row setting the wiped report column in the table.

```tsql
UPDATE dp_xxx.redacted_imaging_reports
SET
    wiped_report = utilities.cdw.wipeoutReportsMatchingRemovalMarkers(redacted_report)
```

## Redaction Cache

The entire redaction system is run inside Common Runtime Language C# assembly. When the assembly redaction method is called for the first time it will cache the entire redaction dictionary to enable faster redactions. This includes the 2 tables of regular expressions for boilerplate cleaning and report wiping

* `data_products.safeguard.report_removal_boilerplate_regex`
* `data_products.safeguard.report_removal_regex`

Any changes to the database will require the cache to be reloaded. This can easily be acheived by the use of the stored procedure
`utilities.cdw.reloadBoilerPlateCache`, call this if you make any alterations to the tables. Or add it to the SQL stored procedure which performs the cleaning of the boilerplate, this will ensure the cache is updated just before you run the boilerplate cleaning.

## Whitelist Dictionaries

The following files are loaded as whitelist dictionaries, you can add to or edit these files to alter the whitelist, you will need to recompile the assembly `.dll` and redeploy it to the SQL Server. All entries should be lowercase as word checking is performed by converting the report to lowercase then matching.

All files are in the `Oxford.Brc.Clinical.Data.Warehouse/Resources` directory.

| Dictionary | Filepath |
|------------|----------|
| List of custom regex markers | `custom_full_text_markers.txt` |
| Empty dictionary for users to add to  | `custom-dictionary.txt` |
| Known medicaly acronyms | `medical-acronyms.txt` |
| Known medical terms | `medical-terms.txt` |
| List of dictionary words with all names removed | `words_alpha.txt` |

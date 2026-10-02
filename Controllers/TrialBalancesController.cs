using BackendAcctTask.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BackendAcctTask.Controllers;

[ApiController]
[Route("api/TrialBalances")]
public class TrialBalancesController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    private readonly IMongoCollection<TrialBalance> _trialBalances;
    private readonly IMongoCollection<TrialBalanceImport> _imports;
    private readonly IMongoCollection<AccountingPeriod> _accountingPeriods;
    private readonly IMongoCollection<ChartAccount> _chartAccounts;

    public TrialBalancesController(
        IMongoDatabase database,
        IWebHostEnvironment environment)
    {
        _environment = environment;

        _trialBalances =
            database.GetCollection<TrialBalance>("TrialBalances");

        _imports =
            database.GetCollection<TrialBalanceImport>(
                "TrialBalanceImports"
            );

        _accountingPeriods =
            database.GetCollection<AccountingPeriod>(
                "AccountingPeriods"
            );

        _chartAccounts =
            database.GetCollection<ChartAccount>(
                "ChartAccounts"
            );
    }

    // ============================================================
    // FILE SAVE
    // ============================================================

    private async Task<string> SaveFile(
        IFormFile file,
        string folder)
    {
        var rootPath = Path.Combine(
            _environment.ContentRootPath,
            "Uploads",
            "TrialBalances",
            folder
        );

        Directory.CreateDirectory(rootPath);

        var extension =
            Path.GetExtension(file.FileName);

        var fileName =
            $"{Guid.NewGuid():N}{extension}";

        var fullPath =
            Path.Combine(rootPath, fileName);

        await using var stream =
            new FileStream(
                fullPath,
                FileMode.Create
            );

        await file.CopyToAsync(stream);

        // Store relative path in MongoDB
        return Path.Combine(
            "Uploads",
            "TrialBalances",
            folder,
            fileName
        ).Replace("\\", "/");
    }


    // ============================================================
    // GENERATE TRIAL BALANCE NUMBER
    // TB-01, TB-02, TB-03...
    // ============================================================

    private async Task<string> GenerateTrialBalanceNumber()
    {
        var latest = await _trialBalances
            .Find(FilterDefinition<TrialBalance>.Empty)
            .SortByDescending(x => x.RefNo)
            .FirstOrDefaultAsync();

        if (latest == null ||
            string.IsNullOrWhiteSpace(latest.RefNo))
        {
            return "TB-01";
        }

        var numberPart =
            latest.RefNo
                .Replace("TB-", "");

        if (!int.TryParse(
                numberPart,
                out var currentNumber))
        {
            currentNumber = 0;
        }

        return $"TB-{currentNumber + 1:00}";
    }


    // ============================================================
    // GENERATE IMPORT NUMBER
    //
    // IMPORTANT:
    // Numbering is maintained PER trial balance.
    //
    // TB-20-I01
    // TB-20-I02
    //
    // TB-21-I01
    // ============================================================

    //private async Task<string> GenerateImportNumber(
    //    string trialBalanceId,
    //    string trialBalanceNumber)
    //{
    //    var latest = await _imports
    //        .Find(x =>
    //            x.TrialBalanceId == trialBalanceId
    //        )
    //        .SortByDescending(x => x.CreatedAt)
    //        .FirstOrDefaultAsync();

    //    var number = 1;

    //    if (latest != null &&
    //        !string.IsNullOrWhiteSpace(latest.RefNo))
    //    {
    //        var existingNumber =
    //            latest.RefNo
    //                .Split("-I")
    //                .LastOrDefault();

    //        if (int.TryParse(
    //                existingNumber,
    //                out var parsed))
    //        {
    //            number = parsed + 1;
    //        }
    //    }

    //    return $"{trialBalanceNumber}-I{number:00}";
    //}

    private async Task<string> GenerateImportNumber(
    string trialBalanceId,
    string trialBalanceNumber)
    {
        var latest = await _imports
            .Find(x => x.TrialBalanceId == trialBalanceId)
            .SortByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        var number = 1;

        if (latest != null &&
            !string.IsNullOrWhiteSpace(latest.Number))
        {
            var existingNumber = latest.Number
                .Split("-I")
                .LastOrDefault();

            if (int.TryParse(existingNumber, out var parsed))
            {
                number = parsed + 1;
            }
        }

        return $"{trialBalanceNumber}-I{number:00}";
    }


    // ============================================================
    // RESOLVE CHART ACCOUNTS BY CODE
    //
    // TrialBalanceItem does NOT store ChartAccountId.
    //
    // AccountCode
    //      ↓
    // ChartAccounts.Code
    // ============================================================

    private async Task<
        (bool Success,
         List<TrialBalanceItem> Items,
         string? Error)
    >
    ResolveItems(
        List<TrialBalanceItem> items)
    {
        var result =
            new List<TrialBalanceItem>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(
                    item.AccountCode))
            {
                return (
                    false,
                    result,
                    "Account code is required."
                );
            }

            var account =
                await _chartAccounts
                    .Find(x =>
                        x.Code == item.AccountCode
                    )
                    .FirstOrDefaultAsync();

            if (account == null)
            {
                return (
                    false,
                    result,
                    $"Chart of account '{item.AccountCode}' was not found."
                );
            }

            // ChartAccount is authoritative.
            result.Add(
                new TrialBalanceItem
                {
                    AccountCode =
                        account.Code,

                    AccountName =
                        account.AccountName,

                    Nature =
                        item.Nature,

                    Debit =
                        item.Debit,

                    Credit =
                        item.Credit,

                    Note =
                        item.Note
                }
            );
        }

        return (
            true,
            result,
            null
        );
    }


    // ============================================================
    // GET CHART ACCOUNT ID
    // Used only when constructing API response.
    // ============================================================

    //private async Task<int> GetChartAccountId(
    //    string accountCode)
    //{
    //    var account =
    //        await _chartAccounts
    //            .Find(x =>
    //                x.Code == accountCode
    //            )
    //            .FirstOrDefaultAsync();

    //    return account?.Id ?? 0;
    //}

    private async Task<string> GetChartAccountId(
    string accountCode)
    {
        var account =
            await _chartAccounts
                .Find(x =>
                    x.Code == accountCode
                )
                .FirstOrDefaultAsync();

        return account?.Id ?? string.Empty;
    }


    // ============================================================
    // CREATE TRIAL BALANCE
    //
    // POST
    // /api/trial-balances
    //
    // multipart/form-data
    // ============================================================

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] string? periodId,
        [FromForm] DateTime? periodStart,
        [FromForm] DateTime? periodEnd,
        [FromForm] TrialBalanceType type,
        [FromForm] ImportType importType,
        [FromForm] CsvImportType csvImportType,
        [FromForm] string? description,
        [FromForm] decimal turnover,
        IFormFile? csvFile,
        IFormFile? attachment)
    {
        DateTime finalPeriodStart;
        DateTime finalPeriodEnd;


        // ========================================================
        // STATUTORY
        // ========================================================

        if (type == TrialBalanceType.Statutory)
        {
            if (string.IsNullOrWhiteSpace(periodId))
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "PeriodId is required for statutory trial balance."
                });
            }

            var period =
                await _accountingPeriods
                    .Find(x =>
                        x.Id == periodId
                    )
                    .FirstOrDefaultAsync();

            if (period == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Accounting period not found."
                });
            }

            finalPeriodStart =
                period.PeriodFrom;

            finalPeriodEnd =
                period.PeriodTo;
        }

        // ========================================================
        // MANAGEMENT
        // ========================================================

        else
        {
            if (!periodStart.HasValue ||
                !periodEnd.HasValue)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "PeriodStart and PeriodEnd are required for management trial balance."
                });
            }

            finalPeriodStart =
                periodStart.Value;

            finalPeriodEnd =
                periodEnd.Value;
        }


        // ========================================================
        // PERIOD VALIDATION
        // ========================================================

        if (finalPeriodStart >
            finalPeriodEnd)
        {
            return BadRequest(new
            {
                status = false,
                message =
                    "PeriodStart cannot be greater than PeriodEnd."
            });
        }


        // ========================================================
        // CSV VALIDATION
        // ========================================================

        if (importType == ImportType.Csv &&
            csvFile == null)
        {
            return BadRequest(new
            {
                status = false,
                message =
                    "CSV file is required when import type is CSV."
            });
        }


        // ========================================================
        // GENERATE NUMBER
        // ========================================================

        var refNo =
            await GenerateTrialBalanceNumber();


        // ========================================================
        // CREATE TRIAL BALANCE
        // ========================================================

        var trialBalance =
            new TrialBalance
            {
                Id =
                    ObjectId.GenerateNewId()
                        .ToString(),

                RefNo =
                    refNo,

                PeriodId =
                    type ==
                    TrialBalanceType.Statutory
                        ? periodId
                        : null,

                PeriodStart =
                    finalPeriodStart,

                PeriodEnd =
                    finalPeriodEnd,

                Type =
                    type,

                Description =
                    description ?? string.Empty,

                ImportType =
                    importType,

                CsvImportType =
                    csvImportType,

                Status =
                    TrialBalanceStatus.Unbalanced,

                IsLocked =
                    false,

                Turnover =
                    turnover,

                TotalDebit =
                    0,

                TotalCredit =
                    0,

                // Do not derive P&L from
                // totalCredit - totalDebit.
                TotalProfitLoss =
                    0,

                AccountReports =
                    null,

                Items =
                    new List<TrialBalanceItem>()
            };


        // ========================================================
        // SAVE CSV
        // ========================================================

        if (csvFile != null)
        {
            trialBalance.CsvFilePath =
                await SaveFile(
                    csvFile,
                    "csv"
                );
        }


        // ========================================================
        // SAVE ATTACHMENT
        // ========================================================

        if (attachment != null)
        {
            trialBalance.AttachmentFilePath =
                await SaveFile(
                    attachment,
                    "attachments"
                );
        }


        // ========================================================
        // INSERT
        // ========================================================

        await _trialBalances
            .InsertOneAsync(trialBalance);


        return Ok(new
        {
            status = true,

            message =
                "Trial balance created successfully.",

            result =
                trialBalance
        });
    }


    // ============================================================
    // GET ALL TRIAL BALANCES
    //
    // GET
    // /api/trial-balances
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trialBalances =
            await _trialBalances
                .Find(
                    FilterDefinition<TrialBalance>
                        .Empty
                )
                .SortBy(x => x.RefNo)
                
                .ToListAsync();

        var result =
            new List<object>();


        foreach (var tb in trialBalances)
        {
            object? period = null;


            // ----------------------------------------------------
            // Get Accounting Period
            // ----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    tb.PeriodId))
            {
                period =
                    await _accountingPeriods
                        .Find(x =>
                            x.Id == tb.PeriodId
                        )
                        .FirstOrDefaultAsync();
            }


            result.Add(
                new
                {
                    refNo =
                        tb.RefNo,

                    periodStart =
                        tb.PeriodStart,

                    periodEnd =
                        tb.PeriodEnd,

                    period,

                    description =
                        tb.Description,

                    type =
                        tb.Type,

                    turnover =
                        tb.Turnover,

                    importType =
                        tb.ImportType,

                    csvImportType =
                        tb.CsvImportType,

                    totalDebit =
                        tb.TotalDebit,

                    totalCredit =
                        tb.TotalCredit,

                    totalProfitLoss =
                        tb.TotalProfitLoss,

                    status =
                        tb.Status,

                    accountReports =
                        tb.AccountReports,

                    isLocked =
                        tb.IsLocked,

                    csvFilePath =
                        tb.CsvFilePath,

                    attachmentFilePath =
                        tb.AttachmentFilePath,

                    itemsCount =
                        tb.Items.Count,

                    id =
                        tb.Id
                }
            );
        }


        return Ok(new
        {
            status = true,
            result
        });
    }


    // ============================================================
    // GET TRIAL BALANCE BY ID
    //
    // GET
    // /api/trial-balances/{id}
    // ============================================================

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        string id)
    {
        var tb =
            await _trialBalances
                .Find(x =>
                    x.Id == id
                )
                .FirstOrDefaultAsync();


        if (tb == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Trial balance not found."
            });
        }


        // ========================================================
        // ACCOUNTING PERIOD
        // ========================================================

        object? period = null;

        if (!string.IsNullOrWhiteSpace(
                tb.PeriodId))
        {
            period =
                await _accountingPeriods
                    .Find(x =>
                        x.Id == tb.PeriodId
                    )
                    .FirstOrDefaultAsync();
        }


        // ========================================================
        // ITEMS
        // ========================================================

        var items =
            new List<object>();


        foreach (var item in tb.Items)
        {
            var account =
                await _chartAccounts
                    .Find(x =>
                        x.Code ==
                        item.AccountCode
                    )
                    .FirstOrDefaultAsync();


            items.Add(
                new
                {
                    account =
                        account == null
                            ? new
                            {
                                id = string.Empty,
                                name =
                                    item.AccountName,
                                code =
                                    item.AccountCode
                            }
                            : new
                            {
                                id =
                                    account.Id,
                                name =
                                    account.AccountName,
                                code =
                                    account.Code
                            },

                    note =
                        item.Note,

                    // Signed amount used by
                    // your frontend/reference.
                    amount =
                        item.Credit > 0
                            ? -item.Credit
                            : item.Debit,

                    debit =
                        item.Debit,

                    credit =
                        item.Credit
                }
            );
        }


        // ========================================================
        // RESPONSE
        // ========================================================

        return Ok(new
        {
            result =
                new
                {
                    trialBalance =
                        new
                        {
                            name =
                                tb.RefNo,

                            id =
                                tb.Id
                        },

                    period,

                    description =
                        tb.Description,

                    items,

                    type =
                        tb.Type,

                    status =
                        tb.Status,

                    itemsCount =
                        tb.Items.Count,

                    totalDebit =
                        tb.TotalDebit,

                    totalCredit =
                        tb.TotalCredit,

                    totalProfitLoss =
                        tb.TotalProfitLoss,

                    turnover =
                        tb.Turnover,

                    validation =
                        new { },

                    importType =
                        tb.ImportType,

                    csvImportType =
                        tb.CsvImportType,

                    attachments =
                        string.IsNullOrWhiteSpace(
                            tb.AttachmentFilePath
                        )
                            ? new List<object>()
                            : new List<object>
                            {
                                new
                                {
                                    name =
                                        Path.GetFileName(
                                            tb.AttachmentFilePath
                                        ),

                                    path =
                                        tb.AttachmentFilePath
                                }
                            },

                    csvFilePath =
                        tb.CsvFilePath,

                    id =
                        tb.Id
                },

            status = true
        });
    }


    // ============================================================
    // UPDATE TRIAL BALANCE
    //
    // PUT
    // /api/trial-balances/{id}
    //
    // This is the FINAL POST from your UI.
    //
    // It saves:
    // - items
    // - debit
    // - credit
    // - totals
    // - status
    // - description
    // - turnover
    // - profit/loss value supplied by frontend
    // ============================================================

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] TrialBalance request)
    {
        var existing =
            await _trialBalances
                .Find(x =>
                    x.Id == id
                )
                .FirstOrDefaultAsync();


        if (existing == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Trial balance not found."
            });
        }


        // ========================================================
        // VALIDATE PERIOD
        // ========================================================

        if (request.Type ==
            TrialBalanceType.Statutory)
        {
            if (string.IsNullOrWhiteSpace(
                    request.PeriodId))
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "PeriodId is required for statutory trial balance."
                });
            }

            var period =
                await _accountingPeriods
                    .Find(x =>
                        x.Id ==
                        request.PeriodId
                    )
                    .FirstOrDefaultAsync();


            if (period == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Accounting period not found."
                });
            }


            // Statutory dates should come
            // from AccountingPeriod.
            request.PeriodStart =
                period.PeriodFrom;

            request.PeriodEnd =
                period.PeriodTo;
        }
        else
        {
            if (request.PeriodStart >
                request.PeriodEnd)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "PeriodStart cannot be greater than PeriodEnd."
                });
            }

            // Management does not reference
            // AccountingPeriod.
            request.PeriodId = null;
        }


        // ========================================================
        // RESOLVE CHART ACCOUNTS
        //
        // IMPORTANT:
        // Do NOT directly save request.Items.
        // Resolve every account using AccountCode.
        // ========================================================

        var resolved =
            await ResolveItems(
                request.Items ??
                new List<TrialBalanceItem>()
            );


        if (!resolved.Success)
        {
            return BadRequest(new
            {
                status = false,
                message =
                    resolved.Error
            });
        }


        existing.Items =
            resolved.Items;


        // ========================================================
        // UPDATE METADATA
        // ========================================================

        existing.PeriodId =
            request.PeriodId;

        existing.PeriodStart =
            request.PeriodStart;

        existing.PeriodEnd =
            request.PeriodEnd;

        existing.Type =
            request.Type;

        existing.Description =
            request.Description;

        existing.ImportType =
            request.ImportType;

        existing.CsvImportType =
            request.CsvImportType;

        existing.Turnover =
            request.Turnover;


        // ========================================================
        // FILE PATHS
        //
        // If frontend sends null/empty,
        // preserve the existing path.
        // ========================================================

        if (!string.IsNullOrWhiteSpace(
                request.CsvFilePath))
        {
            existing.CsvFilePath =
                request.CsvFilePath;
        }

        if (!string.IsNullOrWhiteSpace(
                request.AttachmentFilePath))
        {
            existing.AttachmentFilePath =
                request.AttachmentFilePath;
        }


        // ========================================================
        // CALCULATE TOTALS
        // ========================================================

        existing.TotalDebit =
            existing.Items.Sum(
                x => x.Debit
            );

        existing.TotalCredit =
            existing.Items.Sum(
                x => x.Credit
            );


        // ========================================================
        // PROFIT / LOSS
        //
        // DO NOT use:
        //
        // TotalCredit - TotalDebit
        //
        // because a balanced trial balance has equal
        // debit and credit totals.
        //
        // Preserve the value supplied by frontend.
        // ========================================================

        existing.TotalProfitLoss =
            request.TotalProfitLoss;


        // ========================================================
        // BALANCED / UNBALANCED
        // ========================================================

        existing.Status =
            existing.TotalDebit ==
            existing.TotalCredit
                ? TrialBalanceStatus.Balanced
                : TrialBalanceStatus.Unbalanced;


        // ========================================================
        // UPDATE MONGODB
        // ========================================================

        var update =
            Builders<TrialBalance>
                .Update

                .Set(
                    x => x.PeriodId,
                    existing.PeriodId
                )

                .Set(
                    x => x.PeriodStart,
                    existing.PeriodStart
                )

                .Set(
                    x => x.PeriodEnd,
                    existing.PeriodEnd
                )

                .Set(
                    x => x.Type,
                    existing.Type
                )

                .Set(
                    x => x.Description,
                    existing.Description
                )

                .Set(
                    x => x.ImportType,
                    existing.ImportType
                )

                .Set(
                    x => x.CsvImportType,
                    existing.CsvImportType
                )

                .Set(
                    x => x.Turnover,
                    existing.Turnover
                )

                .Set(
                    x => x.CsvFilePath,
                    existing.CsvFilePath
                )

                .Set(
                    x => x.AttachmentFilePath,
                    existing.AttachmentFilePath
                )

                .Set(
                    x => x.Items,
                    existing.Items
                )

                .Set(
                    x => x.TotalDebit,
                    existing.TotalDebit
                )

                .Set(
                    x => x.TotalCredit,
                    existing.TotalCredit
                )

                .Set(
                    x => x.TotalProfitLoss,
                    existing.TotalProfitLoss
                )

                .Set(
                    x => x.Status,
                    existing.Status
                );


        await _trialBalances
            .UpdateOneAsync(
                x => x.Id == id,
                update
            );


        return Ok(new
        {
            status = true,

            message =
                "Trial balance updated successfully.",

            result =
                existing
        });
    }


    // ============================================================
    // IMPORT CSV
    //
    // POST
    // /api/trial-balances/{trialBalanceId}/imports
    //
    // This creates the TrialBalanceImport document.
    //
    // It does NOT immediately copy the rows into
    // TrialBalance.Items.
    //
    // Your flow is:
    //
    // CSV
    // ↓
    // Import
    // ↓
    // Edit
    // ↓
    // Final PUT TrialBalance
    // ============================================================

    [HttpPost("{trialBalanceId}/imports")]
    public async Task<IActionResult> Import(
        string trialBalanceId,
        [FromBody] TrialBalanceImport request)
    {
        var trialBalance =
            await _trialBalances
                .Find(x =>
                    x.Id == trialBalanceId
                )
                .FirstOrDefaultAsync();


        if (trialBalance == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Trial balance not found."
            });
        }


        if (request.Rows == null ||
            request.Rows.Count == 0)
        {
            return BadRequest(new
            {
                status = false,
                message =
                    "Import must contain at least one row."
            });
        }


        // ========================================================
        // VALIDATE EVERY CSV ACCOUNT CODE
        // ========================================================

        foreach (var row in request.Rows)
        {
            if (string.IsNullOrWhiteSpace(
                    row.Code))
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Account code is required for every import row."
                });
            }


            var account =
                await _chartAccounts
                    .Find(x =>
                        x.Code == row.Code
                    )
                    .FirstOrDefaultAsync();


            if (account == null)
            {
                return BadRequest(new
                {
                    status = false,

                    message =
                        $"Chart of account with code '{row.Code}' was not found."
                });
            }


            // Master account name is authoritative.
            row.Name =
                account.AccountName;
        }


        // ========================================================
        // GENERATE IMPORT NUMBER
        // ========================================================

        var importNumber =
            await GenerateImportNumber(
                trialBalanceId,
                trialBalance.RefNo
            );


        // ========================================================
        // CREATE IMPORT
        // ========================================================

        var import =
            new TrialBalanceImport
            {
                Id =
                    ObjectId.GenerateNewId()
                        .ToString(),

                Number =
                    importNumber,

                TrialBalanceId =
                    trialBalanceId,

                Columns =
                    request.Columns ??
                    new List<ImportColumn>(),

                Headers =
                    request.Headers ??
                    new List<string>(),

                PeriodStart =
                    trialBalance.PeriodStart,

                PeriodEnd =
                    trialBalance.PeriodEnd,

                Rows =
                    request.Rows,

                ImportType =
                    ImportType.Csv,

                CsvImportType =
                    request.CsvImportType,

                CreatedAt =
                    DateTime.UtcNow
            };


        await _imports
            .InsertOneAsync(import);


        // ========================================================
        // RESPONSE
        // ========================================================

        return Ok(new
        {
            executionTime = 0,

            result =
                new
                {
                    import =
                        new
                        {
                            name =
                                import.Number,

                            id =
                                import.Id
                        },

                    trialBalance =
                        new
                        {
                            name =
                                trialBalance.RefNo,

                            id =
                                trialBalance.Id
                        },

                    number =
                        import.Number,

                    items =
                        request.Rows.Select(
                            x => new
                            {
                                account =
                                    new
                                    {
                                        code =
                                            x.Code,

                                        name =
                                            x.Name
                                    },

                                note =
                                    x.Note,

                                amount =
                                    x.Credit > 0
                                        ? -x.Credit
                                        : x.Debit,

                                debit =
                                    x.Debit,

                                credit =
                                    x.Credit
                            }
                        ),

                    type =
                        trialBalance.Type,

                    status =
                        TrialBalanceStatus.Unbalanced,

                    itemsCount =
                        request.Rows.Count,

                    totalDebit =
                        request.Rows.Sum(
                            x => x.Debit
                        ),

                    totalCredit =
                        request.Rows.Sum(
                            x => x.Credit
                        ),

                    validation =
                        new { },

                    importType =
                        ImportType.Csv,

                    csvImportType =
                        request.CsvImportType,

                    id =
                        import.Id
                },

            status = true
        });
    }


    // ============================================================
    // GET IMPORTS BY TRIAL BALANCE
    //
    // GET
    // /api/trial-balances/{trialBalanceId}/imports
    //
    // Response is based on the IMPORT rows, not
    // TrialBalance.Items.
    //
    // amounts[] contains values from the imports.
    // ============================================================

    [HttpGet("{trialBalanceId}/imports")]
    public async Task<IActionResult> GetImports(
        string trialBalanceId)
    {
        var trialBalance =
            await _trialBalances
                .Find(x =>
                    x.Id == trialBalanceId
                )
                .FirstOrDefaultAsync();


        if (trialBalance == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Trial balance not found."
            });
        }


        var imports =
            await _imports
                .Find(x =>
                    x.TrialBalanceId ==
                    trialBalanceId
                )
                .SortBy(x => x.CreatedAt)
                .ToListAsync();


        // ========================================================
        // BUILD UNIQUE ACCOUNT LIST
        // ========================================================

        var codes =
            imports
                .SelectMany(x => x.Rows)
                .Select(x => x.Code)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();


        var items =
            new List<object>();


        foreach (var code in codes)
        {
            var account =
                await _chartAccounts
                    .Find(x =>
                        x.Code == code
                    )
                    .FirstOrDefaultAsync();


            // All rows for this account
            var rows =
                imports
                    .SelectMany(x => x.Rows)
                    .Where(x =>
                        x.Code == code)
                    .ToList();


            // One amount per import.
            var amounts =
                imports
                    .Select(import =>
                    {
                        var row =
                            import.Rows
                                .FirstOrDefault(
                                    x =>
                                        x.Code == code
                                );

                        if (row == null)
                        {
                            return 0m;
                        }

                        return row.Credit > 0
                            ? -row.Credit
                            : row.Debit;
                    })
                    .ToList();


            var firstRow =
                rows.FirstOrDefault();


            items.Add(
                new
                {
                    type =
                        firstRow?.Nature ??
                        AccountNature.Debit,

                    code,

                    amounts,

                    rAmounts =
                        imports
                            .Select(x => 0m)
                            .ToList(),

                    accountNature =
                        firstRow?.Nature ??
                        AccountNature.Debit,

                    name =
                        account?.AccountName ??
                        firstRow?.Name ??
                        string.Empty,

                    id =
                        account?.Id ??
                        string.Empty
                }
            );
        }


        // ========================================================
        // RESPONSE
        // ========================================================

        return Ok(new
        {
            trialBalances =
                new[]
                {
                    new
                    {
                        periodStart =
                            trialBalance.PeriodStart,

                        periodEnd =
                            trialBalance.PeriodEnd,

                        totalDebit =
                            trialBalance.TotalDebit,

                        totalCredit =
                            trialBalance.TotalCredit,

                        name =
                            trialBalance.RefNo,

                        id =
                            trialBalance.Id
                    }
                },

            items
        });
    }


    // ============================================================
    // GET SINGLE IMPORT
    //
    // GET
    // /api/trial-balances/imports/{importId}
    // ============================================================

    [HttpGet("imports/{importId}")]
    public async Task<IActionResult> GetImport(
        string importId)
    {
        var import =
            await _imports
                .Find(x =>
                    x.Id == importId
                )
                .FirstOrDefaultAsync();


        if (import == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Import not found."
            });
        }


        var trialBalance =
            await _trialBalances
                .Find(x =>
                    x.Id ==
                    import.TrialBalanceId
                )
                .FirstOrDefaultAsync();


        if (trialBalance == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Associated trial balance not found."
            });
        }


        var items =
            new List<object>();


        foreach (var row in import.Rows)
        {
            var account =
                await _chartAccounts
                    .Find(x =>
                        x.Code == row.Code
                    )
                    .FirstOrDefaultAsync();


            items.Add(
                new
                {
                    account =
                        new
                        {
                            id =
                                account?.Id ??
                                string.Empty,

                            name =
                                account?.AccountName ??
                                row.Name,

                            code =
                                row.Code
                        },

                    note =
                        row.Note,

                    amount =
                        row.Credit > 0
                            ? -row.Credit
                            : row.Debit,

                    debit =
                        row.Debit,

                    credit =
                        row.Credit
                }
            );
        }


        return Ok(new
        {
            result =
                new
                {
                    import =
                        new
                        {
                            name =
                                import.Number,

                            id =
                                import.Id
                        },

                    trialBalance =
                        new
                        {
                            name =
                                trialBalance.RefNo,

                            id =
                                trialBalance.Id
                        },

                    number =
                        import.Number,

                    items,

                    type =
                        trialBalance.Type,

                    status =
                        trialBalance.Status,

                    itemsCount =
                        import.Rows.Count,

                    totalDebit =
                        import.Rows.Sum(
                            x => x.Debit
                        ),

                    totalCredit =
                        import.Rows.Sum(
                            x => x.Credit
                        ),

                    validation =
                        new { },

                    importType =
                        import.ImportType,

                    csvImportType =
                        import.CsvImportType,

                    id =
                        import.Id
                },

            status = true
        });
    }


    // ============================================================
    // UPDATE IMPORT
    //
    // PUT
    // /api/trial-balances/imports/{importId}
    //
    // This is used when the user edits the imported CSV
    // rows before the final TrialBalance PUT.
    // ============================================================

    [HttpPut("imports/{importId}")]
    public async Task<IActionResult> UpdateImport(
        string importId,
        [FromBody] TrialBalanceImport request)
    {
        var existing =
            await _imports
                .Find(x =>
                    x.Id == importId
                )
                .FirstOrDefaultAsync();


        if (existing == null)
        {
            return NotFound(new
            {
                status = false,
                message =
                    "Import not found."
            });
        }


        if (request.Rows == null ||
            request.Rows.Count == 0)
        {
            return BadRequest(new
            {
                status = false,
                message =
                    "Import must contain at least one row."
            });
        }


        // ========================================================
        // VALIDATE ACCOUNT CODES
        // ========================================================

        foreach (var row in request.Rows)
        {
            if (string.IsNullOrWhiteSpace(
                    row.Code))
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Account code is required."
                });
            }


            var account =
                await _chartAccounts
                    .Find(x =>
                        x.Code == row.Code
                    )
                    .FirstOrDefaultAsync();


            if (account == null)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        $"Chart account '{row.Code}' does not exist."
                });
            }


            // Use master account name.
            row.Name =
                account.AccountName;
        }



        // ========================================================
        // UPDATE ONLY EDITABLE IMPORT DATA
        //
        // Keep:
        // - Id
        // - Number
        // - TrialBalanceId
        // - CreatedAt
        // ========================================================

        existing.Rows =
            request.Rows;

        existing.Columns =
            request.Columns ??
            new List<ImportColumn>();

        existing.Headers =
            request.Headers ??
            new List<string>();


        await _imports
            .ReplaceOneAsync(
                x => x.Id == importId,
                existing
            );


        return Ok(new
        {
            status = true,

            message =
                "Import updated successfully.",

            result =
                existing
        });
    }
}
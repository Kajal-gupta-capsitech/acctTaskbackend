using BackendAcctTask.Models;
using BackendAcctTask.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace BackendAcctTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrialBalancesController : ControllerBase
{
    private readonly TrialBalanceService _trialBalanceService;

    public TrialBalancesController(
        TrialBalanceService trialBalanceService)
    {
        _trialBalanceService = trialBalanceService;
    }

    // GET: api/TrialBalances
    [HttpGet]
    public async Task<ActionResult<List<TrialBalanceResponse>>> Get()
    {
        var trialBalances =
            await _trialBalanceService.GetAsync();

        return Ok(trialBalances);
    }

    // GET: api/TrialBalances/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TrialBalanceResponse>> Get(
        string id)
    {
        var trialBalance =
            await _trialBalanceService.GetAsync(id);

        if (trialBalance == null)
        {
            return NotFound();
        }

        return Ok(trialBalance);
    }

    // POST: api/TrialBalances
    [HttpPost]
    public async Task<ActionResult<TrialBalance>> Create(
        [FromForm] CreateTrialBalanceRequest request)
    {
        /*
         * =====================================================
         * ACCOUNTING PERIOD VALIDATION
         * =====================================================
         */
       
        //Console.WriteLine($"TrialBalanceType: {request.TrialBalanceType}");
        //Console.WriteLine($"AccountingPeriodId: '{request.AccountingPeriodId}'");
        //Console.WriteLine($"ImportMode: {request.ImportMode}");

        //Console.WriteLine("========== MODEL ==========");
        //Console.WriteLine(
        //    $"request.AccountingPeriodId = '{request.AccountingPeriodId}'"
        //);

        //Console.WriteLine("========== RAW FORM ==========");

        //if (Request.HasFormContentType)
        //{
        //    var form = await Request.ReadFormAsync();

        //    foreach (var item in form)
        //    {
        //        Console.WriteLine(
        //            $"FORM: '{item.Key}' = '{item.Value}'"
        //        );
        //    }
        //}



        Console.WriteLine("============================");
        if (
            request.TrialBalanceType ==
            TrialBalanceType.Statutory
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    request.AccountingPeriodId
                )
            )
            {
                return BadRequest(
                    "AccountingPeriodId is required for Statutory trial balance."
                );
            }

            var accountingPeriodExists =
                await _trialBalanceService
                    .AccountingPeriodExistsAsync(
                        request.AccountingPeriodId
                    );

            if (!accountingPeriodExists)
            {
                return BadRequest(
                    "Invalid AccountingPeriodId."
                );
            }
        }

        /*
         * =====================================================
         * CSV VALIDATION
         * =====================================================
         */

        if (
            request.ImportMode == ImportMode.Csv
        )
        {
            if (
                request.File == null ||
                request.File.Length == 0
            )
            {
                return BadRequest(
                    "CSV file is required."
                );
            }

            if (
                !Path.GetExtension(
                    request.File.FileName
                ).Equals(
                    ".csv",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(
                    "Only CSV files are allowed."
                );
            }
        }

        /*
         * =====================================================
         * CREATE TRIAL BALANCE ENTITY
         * =====================================================
         */

        var trialBalance = new TrialBalance
        {
            Id =
                ObjectId.GenerateNewId()
                    .ToString(),

            TrialBalanceType =
                request.TrialBalanceType,

            AccountingPeriodId =
                request.AccountingPeriodId,

            ChartAccountId =
                request.ChartAccountId,

            ImportMode =
                request.ImportMode,

            ImportFormat =
                request.ImportFormat,

            FileName =
                request.File?.FileName,

            JournalId =
                Guid.NewGuid().ToString("N"),

            CreatedAt =
                DateTime.UtcNow,

            Status =
                TrialBalanceStatus.Draft
        };

        string? savedFilePath = null;

        try
        {
            /*
             * =================================================
             * SAVE CSV LOCALLY
             * =================================================
             */

            if (
                request.ImportMode == ImportMode.Csv &&
                request.File != null
            )
            {
                var uploadFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "UploadedFiles",
                        "TrialBalances",
                        trialBalance.Id
                    );

                Directory.CreateDirectory(
                    uploadFolder
                );

                var safeFileName =
                    Path.GetFileName(
                        request.File.FileName
                    );

                savedFilePath =
                    Path.Combine(
                        uploadFolder,
                        safeFileName
                    );

                await using var stream =
                    new FileStream(
                        savedFilePath,
                        FileMode.Create
                    );

                await request.File.CopyToAsync(
                    stream
                );
            }

            /*
             * =================================================
             * CREATE MONGODB DOCUMENT
             * =================================================
             */

            await _trialBalanceService
                .CreateAsync(trialBalance);

            /*
             * =================================================
             * RETURN CREATED TRIAL BALANCE
             * =================================================
             */

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    id = trialBalance.Id
                },
                trialBalance
            );
        }
        catch
        {
            /*
             * =================================================
             * COMPENSATION
             *
             * If MongoDB creation fails after the file
             * has already been saved, remove the file.
             * =================================================
             */

            if (
                !string.IsNullOrWhiteSpace(
                    savedFilePath
                ) &&
                System.IO.File.Exists(
                    savedFilePath
                )
            )
            {
                try
                {
                    System.IO.File.Delete(
                        savedFilePath
                    );
                }
                catch
                {
                    // Preserve the original exception.
                }
            }

            throw;
        }
    }

    // PATCH: api/TrialBalances/{id}
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        string id,
        UpdateTrialBalanceRequest request)
    {
        var existingTrialBalance =
            await _trialBalanceService.GetAsync(id);

        if (existingTrialBalance == null)
        {
            return NotFound();
        }

        // If AccountingPeriodId is being changed,
        // validate the new ID.
        if (request.AccountingPeriodId != null)
        {
            var accountingPeriodExists =
                await _trialBalanceService
                    .AccountingPeriodExistsAsync(
                        request.AccountingPeriodId);

            if (!accountingPeriodExists)
            {
                return BadRequest(
                    "Invalid AccountingPeriodId.");
            }
        }

        // If ChartAccountId is being changed,
        // validate the new ID.
        if (request.ChartAccountId != null)
        {
            var chartAccountExists =
                await _trialBalanceService
                    .ChartAccountExistsAsync(
                        request.ChartAccountId);

            if (!chartAccountExists)
            {
                return BadRequest(
                    "Invalid ChartAccountId.");
            }
        }

        var updated =
            await _trialBalanceService.UpdateAsync(
                id,
                request);

        if (!updated)
        {
            return BadRequest(
                "At least one field must be provided.");
        }


        return NoContent();
    }

    // DELETE: api/TrialBalances/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id)
    {
        var existingTrialBalance =
            await _trialBalanceService.GetAsync(id);

        if (existingTrialBalance == null)
        {
            return NotFound();
        }

        await _trialBalanceService.DeleteAsync(id);

        return NoContent();
    }
}
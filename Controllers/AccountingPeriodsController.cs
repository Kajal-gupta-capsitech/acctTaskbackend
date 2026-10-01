using BackendAcctTask.Models;
using BackendAcctTask.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace BackendAcctTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountingPeriodsController : ControllerBase
{
    private readonly AccountingPeriodService _accountingPeriodService;

    public AccountingPeriodsController(
        AccountingPeriodService accountingPeriodService)
    {
        _accountingPeriodService = accountingPeriodService;
    }

    // GET: api/AccountingPeriods
    [HttpGet]
    public async Task<ActionResult<List<AccountingPeriod>>> Get()
    {
        var accountingPeriods =
            await _accountingPeriodService.GetAsync();

        return Ok(accountingPeriods);
    }

    // GET: api/AccountingPeriods/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountingPeriod>> Get(string id)
    {
        var accountingPeriod =
            await _accountingPeriodService.GetAsync(id);

        if (accountingPeriod == null)
        {
            return NotFound();
        }

        return Ok(accountingPeriod);
    }

    // POST: api/AccountingPeriods
    [HttpPost]
    public async Task<ActionResult<AccountingPeriod>> Create(
        AccountingPeriod accountingPeriod)
    {
        accountingPeriod.Id =
            ObjectId.GenerateNewId().ToString();

        await _accountingPeriodService.CreateAsync(
            accountingPeriod);

        return CreatedAtAction(
            nameof(Get),
            new { id = accountingPeriod.Id },
            accountingPeriod);
    }

    // PATCH: api/AccountingPeriods/{id}
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        string id,
        UpdateAccountingPeriodRequest request)
    {
        var existingAccountingPeriod =
            await _accountingPeriodService.GetAsync(id);

        if (existingAccountingPeriod == null)
        {
            return NotFound();
        }

        var updated =
            await _accountingPeriodService.UpdateAsync(
                id,
                request);

        if (!updated)
        {
            return BadRequest(
                "At least one field must be provided.");
        }

        return NoContent();
    }

    // DELETE: api/AccountingPeriods/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existingAccountingPeriod =
            await _accountingPeriodService.GetAsync(id);

        if (existingAccountingPeriod == null)
        {
            return NotFound();
        }

        await _accountingPeriodService.DeleteAsync(id);

        return NoContent();
    }
}
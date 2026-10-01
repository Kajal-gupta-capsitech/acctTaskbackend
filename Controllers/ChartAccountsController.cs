using BackendAcctTask.Models;
using BackendAcctTask.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;

namespace BackendAcctTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChartAccountsController : ControllerBase
{
    private readonly ChartAccountService _chartAccountService;
    private readonly AccountTypeService _accountTypeService;

    public ChartAccountsController(
        ChartAccountService chartAccountService,
        AccountTypeService accountTypeService)
    {
        _chartAccountService = chartAccountService;
        _accountTypeService = accountTypeService;
    }

    // POST: api/ChartAccounts
    [HttpPost]
    public async Task<ActionResult<ChartAccount>> Create(
     ChartAccount chartAccount)
    {
        var accountType =
            await _accountTypeService.GetAsync(
                chartAccount.AccountTypeId);

        if (accountType == null)
        {
            return BadRequest("Invalid AccountTypeId.");
        }

        chartAccount.Id =
            ObjectId.GenerateNewId().ToString();

        try
        {
            await _chartAccountService.CreateAsync(
                chartAccount);
        }
        catch (MongoException ex)
            when (ex.Message != null && ex.Message.Contains("duplicate key"))
        {
            return Conflict(
                $"AccountName '{chartAccount.AccountName}' already exists.");
        }

        return CreatedAtAction(
            nameof(Get),
            new { id = chartAccount.Id },
            chartAccount);
    }

    // GET: api/ChartAccounts
    [HttpGet]
    public async Task<ActionResult<List<ChartAccountResponse>>> Get()
    {
        var chartAccounts =
            await _chartAccountService.GetAsync();

        return Ok(chartAccounts);
    }

    // GET: api/ChartAccounts/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ChartAccountResponse>> Get(
        string id)
    {
        var chartAccount =
            await _chartAccountService.GetAsync(id);

        if (chartAccount == null)
        {
            return NotFound();
        }

        return Ok(chartAccount);
    }

    // PATCH: api/ChartAccounts/{id}
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        string id,
        UpdateChartAccountRequest request)
    {
        // Check ChartAccount exists
        var existingChartAccount =
            await _chartAccountService.GetAsync(id);

        if (existingChartAccount == null)
        {
            return NotFound();
        }

        // Only validate AccountTypeId if it is being changed
        if (request.AccountTypeId != null)
        {
            var accountType =
                await _accountTypeService.GetAsync(
                    request.AccountTypeId);

            if (accountType == null)
            {
                return BadRequest(
                    "Invalid AccountTypeId.");
            }
        }

        var updated =
            await _chartAccountService.UpdateAsync(
                id,
                request);

        if (!updated)
        {
            return BadRequest(
                "At least one field must be provided.");
        }

        return NoContent();
    }

    // DELETE: api/ChartAccounts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id)
    {
        var existingChartAccount =
            await _chartAccountService.GetAsync(id);

        if (existingChartAccount == null)
        {
            return NotFound();
        }

        await _chartAccountService.DeleteAsync(id);

        return NoContent();
    }
}
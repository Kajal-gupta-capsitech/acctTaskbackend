using BackendAcctTask.Models;
using BackendAcctTask.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace BackendAcctTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountTypesController : ControllerBase
{
    private readonly AccountTypeService _accountTypeService;

    public AccountTypesController(
        AccountTypeService accountTypeService)
    {
        _accountTypeService = accountTypeService;
    }

    // GET: api/AccountTypes
    [HttpGet]
    public async Task<ActionResult<List<AccountType>>> Get()
    {
        var accountTypes = await _accountTypeService.GetAsync();

        return Ok(accountTypes);
    }

    // GET: api/AccountTypes/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountType>> Get(string id)
    {
        var accountType = await _accountTypeService.GetAsync(id);

        if (accountType == null)
        {
            return NotFound();
        }

        return Ok(accountType);
    }

    // POST: api/AccountTypes
    [HttpPost]
    public async Task<ActionResult<AccountType>> Create(
        AccountType accountType)
    {
        accountType.Id = ObjectId.GenerateNewId().ToString();

        await _accountTypeService.CreateAsync(accountType);

        return CreatedAtAction(
            nameof(Get),
            new { id = accountType.Id },
            accountType);
    }

    // PUT: api/AccountTypes/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        AccountType accountType)
    {
        var existingAccountType =
            await _accountTypeService.GetAsync(id);

        if (existingAccountType == null)
        {
            return NotFound();
        }

        accountType.Id = id;

        await _accountTypeService.UpdateAsync(
            id,
            accountType);

        return NoContent();
    }

    // DELETE: api/AccountTypes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existingAccountType =
            await _accountTypeService.GetAsync(id);

        if (existingAccountType == null)
        {
            return NotFound();
        }

        await _accountTypeService.DeleteAsync(id);

        return NoContent();
    }
}
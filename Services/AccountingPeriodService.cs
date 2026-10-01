using BackendAcctTask.Models;
using BackendAcctTask.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BackendAcctTask.Services;

public class AccountingPeriodService
{
    private readonly IMongoCollection<AccountingPeriod> _accountingPeriods;

    public AccountingPeriodService(
        IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(
            mongoDbSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            mongoDbSettings.Value.DatabaseName);

        _accountingPeriods = mongoDatabase.GetCollection<AccountingPeriod>(
            "AccountingPeriods");
    }

    // GET ALL
    public async Task<List<AccountingPeriod>> GetAsync()
    {
        return await _accountingPeriods
            .Find(_ => true)
            .ToListAsync();
    }

    // GET BY ID
    public async Task<AccountingPeriod?> GetAsync(string id)
    {
        return await _accountingPeriods
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    // CREATE
    public async Task CreateAsync(AccountingPeriod accountingPeriod)
    {
        await _accountingPeriods.InsertOneAsync(accountingPeriod);
    }

    // PARTIAL UPDATE
    public async Task<bool> UpdateAsync(
      string id,
      UpdateAccountingPeriodRequest request)
    {
        var updates = new List<UpdateDefinition<AccountingPeriod>>();

        if (request.PeriodFrom.HasValue)
        {
            updates.Add(
                Builders<AccountingPeriod>.Update.Set(
                    x => x.PeriodFrom,
                    request.PeriodFrom.Value));
        }

        if (request.PeriodTo.HasValue)
        {
            updates.Add(
                Builders<AccountingPeriod>.Update.Set(
                    x => x.PeriodTo,
                    request.PeriodTo.Value));
        }

        if (request.IsActive.HasValue)
        {
            updates.Add(
                Builders<AccountingPeriod>.Update.Set(
                    x => x.IsActive,
                    request.IsActive.Value));
        }

        if (request.IsClosed.HasValue)
        {
            updates.Add(
                Builders<AccountingPeriod>.Update.Set(
                    x => x.IsClosed,
                    request.IsClosed.Value));
        }

        if (updates.Count == 0)
        {
            return false;
        }

        var combinedUpdate =
            Builders<AccountingPeriod>.Update.Combine(updates);

        var result = await _accountingPeriods.UpdateOneAsync(
            x => x.Id == id,
            combinedUpdate);

        return result.MatchedCount > 0;
    }


    // DELETE
    public async Task DeleteAsync(string id)
    {
        await _accountingPeriods.DeleteOneAsync(
            x => x.Id == id);
    }
}
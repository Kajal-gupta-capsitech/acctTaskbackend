using BackendAcctTask.Models;
using BackendAcctTask.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BackendAcctTask.Services;

public class AccountTypeService
{
    private readonly IMongoCollection<AccountType> _accountTypes;

    public AccountTypeService(
        IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(
            mongoDbSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            mongoDbSettings.Value.DatabaseName);

        _accountTypes = mongoDatabase.GetCollection<AccountType>(
            mongoDbSettings.Value.AccountTypesCollectionName);
    }

    public async Task<List<AccountType>> GetAsync()
    {
        return await _accountTypes
            .Find(_ => true)
            .ToListAsync();
    }

    public async Task<AccountType?> GetAsync(string id)
    {
        return await _accountTypes
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(AccountType accountType)
    {
        await _accountTypes.InsertOneAsync(accountType);
    }

    public async Task UpdateAsync(
        string id,
        AccountType accountType)
    {
        await _accountTypes.ReplaceOneAsync(
            x => x.Id == id,
            accountType);
    }

    public async Task DeleteAsync(string id)
    {
        await _accountTypes.DeleteOneAsync(
            x => x.Id == id);
    }
}
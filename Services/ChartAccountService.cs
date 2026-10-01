using BackendAcctTask.Models;
using BackendAcctTask.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BackendAcctTask.Services;

public class ChartAccountService
{
    private readonly IMongoCollection<ChartAccount> _chartAccounts;
    private readonly IMongoCollection<AccountType> _accountTypes;

    public ChartAccountService(
     IOptions<MongoDbSettings> mongoDbSettings)
    {
        var mongoClient = new MongoClient(
            mongoDbSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            mongoDbSettings.Value.DatabaseName);

        _chartAccounts =
            mongoDatabase.GetCollection<ChartAccount>(
                mongoDbSettings.Value.ChartAccountsCollectionName);

        _accountTypes =
            mongoDatabase.GetCollection<AccountType>(
                mongoDbSettings.Value.AccountTypesCollectionName);

        CreateUniqueIndex();
    }
    // CREATE UNIQUE INDEX FOR ACCOUNT NAME
    private void CreateUniqueIndex()
    {
        var indexKeys =
            Builders<ChartAccount>.IndexKeys
                .Ascending(x => x.AccountName);

        var indexOptions =
            new CreateIndexOptions
            {
                Unique = true
            };

        var indexModel =
            new CreateIndexModel<ChartAccount>(
                indexKeys,
                indexOptions);

        _chartAccounts.Indexes.CreateOne(indexModel);
    }

    // GET ALL
    public async Task<List<ChartAccountResponse>> GetAsync()
    {
        var chartAccounts = await _chartAccounts
            .Find(_ => true)
            .ToListAsync();

        var result = new List<ChartAccountResponse>();

        foreach (var chartAccount in chartAccounts)
        {
            var accountType = await _accountTypes
                .Find(x => x.Id == chartAccount.AccountTypeId)
                .FirstOrDefaultAsync();

            result.Add(new ChartAccountResponse
            {
                Id = chartAccount.Id,
                Code = chartAccount.Code,
                AccountName = chartAccount.AccountName,

                AccountTypeId = chartAccount.AccountTypeId,
                AccountType = accountType,

                AccountGroup = chartAccount.AccountGroup,
                ForClients = chartAccount.ForClients,
                Archive = chartAccount.Archive
            });
        }

        return result;
    }

    // GET BY ID
    public async Task<ChartAccountResponse?> GetAsync(string id)
    {
        var chartAccount = await _chartAccounts
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (chartAccount == null)
        {
            return null;
        }

        var accountType = await _accountTypes
            .Find(x => x.Id == chartAccount.AccountTypeId)
            .FirstOrDefaultAsync();

        return new ChartAccountResponse
        {
            Id = chartAccount.Id,
            Code = chartAccount.Code,
            AccountName = chartAccount.AccountName,

            AccountTypeId = chartAccount.AccountTypeId,
            AccountType = accountType,

            AccountGroup = chartAccount.AccountGroup,
            ForClients = chartAccount.ForClients,
            Archive = chartAccount.Archive
        };
    }

    // GET NEXT CODE
    public async Task<string> GetNextCodeAsync()
    {
        var lastChartAccount = await _chartAccounts
            .Find(_ => true)
            .SortByDescending(x => x.Code)
            .FirstOrDefaultAsync();

        if (lastChartAccount == null)
        {
            return "1/1";
        }

        var codeParts = lastChartAccount.Code.Split('/');

        if (codeParts.Length != 2 ||
            !int.TryParse(codeParts[1], out int lastNumber))
        {
            return "1/1";
        }

        return $"1/{lastNumber + 1}";
    }

    // CREATE
    public async Task CreateAsync(ChartAccount chartAccount)
    {
        chartAccount.Code = await GetNextCodeAsync();

        await _chartAccounts.InsertOneAsync(chartAccount);
    }

    // PARTIAL UPDATE
    public async Task<bool> UpdateAsync(
        string id,
        UpdateChartAccountRequest request)
    {
        var updates =
            new List<UpdateDefinition<ChartAccount>>();

        // Code is intentionally NOT updated.
        // Code is generated automatically.

        if (request.AccountName != null)
        {
            updates.Add(
                Builders<ChartAccount>.Update.Set(
                    x => x.AccountName,
                    request.AccountName));
        }

        if (request.AccountTypeId != null)
        {
            updates.Add(
                Builders<ChartAccount>.Update.Set(
                    x => x.AccountTypeId,
                    request.AccountTypeId));
        }

        if (request.AccountGroup != null)
        {
            updates.Add(
                Builders<ChartAccount>.Update.Set(
                    x => x.AccountGroup,
                    request.AccountGroup));
        }

        if (request.ForClients.HasValue)
        {
            updates.Add(
                Builders<ChartAccount>.Update.Set(
                    x => x.ForClients,
                    request.ForClients.Value));
        }

        if (request.Archive.HasValue)
        {
            updates.Add(
                Builders<ChartAccount>.Update.Set(
                    x => x.Archive,
                    request.Archive.Value));
        }

        if (updates.Count == 0)
        {
            return false;
        }

        var combinedUpdate =
            Builders<ChartAccount>.Update.Combine(updates);

        var result = await _chartAccounts.UpdateOneAsync(
            x => x.Id == id,
            combinedUpdate);

        return result.MatchedCount > 0;
    }

    // DELETE
    public async Task DeleteAsync(string id)
    {
        await _chartAccounts.DeleteOneAsync(
            x => x.Id == id);
    }
}
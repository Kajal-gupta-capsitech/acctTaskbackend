using BackendAcctTask.Models;
using BackendAcctTask.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BackendAcctTask.Services;

public class TrialBalanceService
{
	private readonly IMongoCollection<TrialBalance> _trialBalances;
	private readonly IMongoCollection<AccountingPeriod> _accountingPeriods;
	private readonly IMongoCollection<ChartAccount> _chartAccounts;

	public TrialBalanceService(
		IOptions<MongoDbSettings> mongoDbSettings)
	{
		var mongoClient = new MongoClient(
			mongoDbSettings.Value.ConnectionString);

		var mongoDatabase = mongoClient.GetDatabase(
			mongoDbSettings.Value.DatabaseName);

		_trialBalances =
			mongoDatabase.GetCollection<TrialBalance>(
				"TrialBalances");

		_accountingPeriods =
			mongoDatabase.GetCollection<AccountingPeriod>(
				"AccountingPeriods");

		_chartAccounts =
			mongoDatabase.GetCollection<ChartAccount>(
				"ChartAccounts");
	}

	// GET ALL
	public async Task<List<TrialBalanceResponse>> GetAsync()
	{
		var trialBalances = await _trialBalances
			.Find(_ => true)
			.ToListAsync();

		var result = new List<TrialBalanceResponse>();

		foreach (var trialBalance in trialBalances)
		{
			var accountingPeriod =
				await _accountingPeriods
					.Find(x => x.Id == trialBalance.AccountingPeriodId)
					.FirstOrDefaultAsync();

			var chartAccount =
				//await _chartAccounts
				await _chartAccounts
					.Find(x => x.Id == trialBalance.ChartAccountId)
					.FirstOrDefaultAsync();

			result.Add(new TrialBalanceResponse
			{
				Id = trialBalance.Id,
				RefNo = trialBalance.RefNo,
				TrialBalanceType = trialBalance.TrialBalanceType,

                JournalType = trialBalance.JournalType,

                JournalId = trialBalance.JournalId,

                AccountingPeriodId =
					trialBalance.AccountingPeriodId,

				AccountingPeriod = accountingPeriod,

				ChartAccountId =
					trialBalance.ChartAccountId,

				ChartAccount = chartAccount,

				Turnover = trialBalance.Turnover,
				Description = trialBalance.Description,
				AccountReports = trialBalance.AccountReports,
				ImportMode = trialBalance.ImportMode,
				ImportFormat = trialBalance.ImportFormat,
				FileName = trialBalance.FileName,
				Status = trialBalance.Status,
				CreatedAt = trialBalance.CreatedAt
			});
		}

		return result;
	}

	// GET BY ID
	public async Task<TrialBalanceResponse?> GetAsync(string id)
	{
		var trialBalance = await _trialBalances
			.Find(x => x.Id == id)
			.FirstOrDefaultAsync();

		if (trialBalance == null)
		{
			return null;
		}

		var accountingPeriod =
			await _accountingPeriods
				.Find(x => x.Id == trialBalance.AccountingPeriodId)
				.FirstOrDefaultAsync();

		var chartAccount =
			await _chartAccounts
				.Find(x => x.Id == trialBalance.ChartAccountId)
				.FirstOrDefaultAsync();

		return new TrialBalanceResponse
		{
			Id = trialBalance.Id,
			RefNo = trialBalance.RefNo,
			TrialBalanceType = trialBalance.TrialBalanceType,
			JournalType = trialBalance.JournalType,
			JournalId = trialBalance.JournalId,

			AccountingPeriodId =
				trialBalance.AccountingPeriodId,

			AccountingPeriod = accountingPeriod,

			ChartAccountId =
				trialBalance.ChartAccountId,

			ChartAccount = chartAccount,

			Turnover = trialBalance.Turnover,
			Description = trialBalance.Description,
			AccountReports = trialBalance.AccountReports,
			ImportMode = trialBalance.ImportMode,
			ImportFormat = trialBalance.ImportFormat,
			FileName = trialBalance.FileName,
			Status = trialBalance.Status,
			CreatedAt = trialBalance.CreatedAt
		};
	}

	// CREATE
	public async Task CreateAsync(TrialBalance trialBalance)
	{
        var last = await _trialBalances
    .Find(_ => true)
    .SortByDescending(x => x.RefNo)
    .FirstOrDefaultAsync();

        int nextNumber = 1;
      

        if (last != null && !string.IsNullOrEmpty(last.RefNo))
        {
            var numberPart = last.RefNo.Replace("TB-", "");

            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        trialBalance.RefNo = $"TB-{nextNumber:D2}";

        await _trialBalances.InsertOneAsync(trialBalance);
	}

	// PATCH
	public async Task<bool> UpdateAsync(
		string id,
		UpdateTrialBalanceRequest request)
	{
		var updates =
			new List<UpdateDefinition<TrialBalance>>();

		//if (request.RefNo != null)
		//{
		//	updates.Add(
		//		Builders<TrialBalance>.Update
		//			.Set(x => x.RefNo, request.RefNo));
		//}

		if (request.TrialBalanceType.HasValue)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.TrialBalanceType,
						request.TrialBalanceType.Value));
		}

		if (request.AccountingPeriodId != null)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.AccountingPeriodId,
						request.AccountingPeriodId));
		}

		if (request.ChartAccountId != null)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.ChartAccountId,
						request.ChartAccountId));
		}

        if (request.JournalType.HasValue)
        {
            updates.Add(
                Builders<TrialBalance>.Update
                    .Set(
                        x => x.JournalType,
                        request.JournalType.Value));
        }

        if (request.Turnover.HasValue)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.Turnover,
						request.Turnover.Value));
		}

		if (request.Description != null)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.Description,
						request.Description));
		}

		if (request.AccountReports != null)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.AccountReports,
						request.AccountReports));
		}

		if (request.ImportMode.HasValue)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.ImportMode,
						request.ImportMode.Value));
		}

		if (request.ImportFormat != null)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.ImportFormat,
						request.ImportFormat));
		}

		if (request.FileName != null)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.FileName,
						request.FileName));
		}

		if (request.Status.HasValue)
		{
			updates.Add(
				Builders<TrialBalance>.Update
					.Set(
						x => x.Status,
						request.Status.Value));
		}

		if (updates.Count == 0)
		{
			return false;
		}

		var combinedUpdate =
			Builders<TrialBalance>.Update.Combine(updates);

		var result = await _trialBalances.UpdateOneAsync(
			x => x.Id == id,
			combinedUpdate);

		return result.MatchedCount > 0;
	}

	// DELETE
	public async Task<bool> DeleteAsync(string id)
	{
		var result = await _trialBalances.DeleteOneAsync(
			x => x.Id == id);

		return result.DeletedCount > 0;
	}

    public async Task<bool> ExistsAsync(string id)
    {
        return await _trialBalances
            .Find(x => x.Id == id)
            .AnyAsync();
    }


    // Validate AccountingPeriod
    public async Task<bool> AccountingPeriodExistsAsync(
		string id)
	{
		return await _accountingPeriods
			.Find(x => x.Id == id)
			.AnyAsync();
	}

	// Validate ChartAccount
	public async Task<bool> ChartAccountExistsAsync(
		string id)
	{
		return await _chartAccounts
			.Find(x => x.Id == id)
			.AnyAsync();
	}
}
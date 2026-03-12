using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using PartnersPromoLambda.Models;

namespace PartnersPromoLambda.Services;

public class ExecutionLimitService : IExecutionLimitService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly int _dailyLimit;

    public ExecutionLimitService(IAmazonDynamoDB dynamoDbClient, string tableName, int dailyLimit)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = tableName;
        _dailyLimit = dailyLimit;
    }

    public async Task<bool> CanExecuteAsync()
    {
        var record = await GetExecutionRecordAsync();
        return record.Count < _dailyLimit;
    }

    public async Task IncrementExecutionCountAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "Date", new AttributeValue { S = today } }
            },
            UpdateExpression = "SET #count = if_not_exists(#count, :zero) + :inc, LastUpdated = :timestamp",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#count", "Count" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":zero", new AttributeValue { N = "0" } },
                { ":inc", new AttributeValue { N = "1" } },
                { ":timestamp", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
            }
        };

        await _dynamoDbClient.UpdateItemAsync(request);
    }

    public async Task<int> GetRemainingExecutionsAsync()
    {
        var record = await GetExecutionRecordAsync();
        return Math.Max(0, _dailyLimit - record.Count);
    }

    private async Task<ExecutionRecord> GetExecutionRecordAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "Date", new AttributeValue { S = today } }
            }
        };

        var response = await _dynamoDbClient.GetItemAsync(request);

        if (!response.IsItemSet || response.Item.Count == 0)
        {
            return new ExecutionRecord
            {
                Date = today,
                Count = 0,
                LastUpdated = DateTime.UtcNow
            };
        }

        return new ExecutionRecord
        {
            Date = response.Item["Date"].S,
            Count = int.Parse(response.Item.ContainsKey("Count") ? response.Item["Count"].N : "0"),
            LastUpdated = response.Item.ContainsKey("LastUpdated") 
                ? DateTime.Parse(response.Item["LastUpdated"].S) 
                : DateTime.UtcNow
        };
    }
}

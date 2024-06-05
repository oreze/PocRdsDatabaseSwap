using Amazon.RDS;
using Amazon.RDS.Model;
using Microsoft.AspNetCore.Mvc;
using PocRdsDatabaseSwap.API.Data;

namespace PocRdsDatabaseSwap.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AwsController(AppDbContext dbContext, ILogger<AwsController> logger, IAmazonRDS rdsClient) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentException(nameof(dbContext));
    private readonly ILogger<AwsController> _logger = logger ?? throw new ArgumentException(nameof(logger));
    private readonly IAmazonRDS _rdsClient = rdsClient ?? throw new ArgumentException(nameof(rdsClient));

    [HttpPost("snapshot-and-apply")]
    public async Task<IActionResult> SnapshotAndApply()
    {
        var prodDbIdentifier = "database-prod-1";
        var devDbIdentifier = "database-dev-1";
        var tempDevDbIdentifier = $"database-dev-temp-snapshot-{Guid.NewGuid()}";
        var snapshotIdentifier = $"{prodDbIdentifier}-snapshot-{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            // Create a snapshot of the production database.
            _logger.LogInformation($"Creating snapshot {snapshotIdentifier} of production database {prodDbIdentifier}.");
            var createSnapshotRequest = new CreateDBSnapshotRequest
            {
                DBInstanceIdentifier = prodDbIdentifier,
                DBSnapshotIdentifier = snapshotIdentifier
            };
            var createSnapshotResponse = await _rdsClient.CreateDBSnapshotAsync(createSnapshotRequest);
            _logger.LogInformation($"Snapshot {snapshotIdentifier} created successfully.");

            // Wait for the snapshot to be available.
            _logger.LogInformation($"Waiting for snapshot {snapshotIdentifier} to be available.");
            var describeSnapshotsRequest = new DescribeDBSnapshotsRequest
            {
                DBSnapshotIdentifier = snapshotIdentifier
            };
            DBSnapshot snapshot;
            do
            {
                var describeSnapshotsResponse = await _rdsClient.DescribeDBSnapshotsAsync(describeSnapshotsRequest);
                snapshot = describeSnapshotsResponse.DBSnapshots[0];
                await Task.Delay(TimeSpan.FromSeconds(10));
            } while (snapshot.Status != "available");

            _logger.LogInformation($"Snapshot {snapshotIdentifier} is available.");

            // Restore the snapshot to a new database instance with the temporary name.
            _logger.LogInformation($"Restoring snapshot {snapshotIdentifier} to new database instance {tempDevDbIdentifier}.");
            var restoreDbInstanceFromSnapshotRequest = new RestoreDBInstanceFromDBSnapshotRequest
            {
                DBInstanceIdentifier = tempDevDbIdentifier,
                DBSnapshotIdentifier = snapshotIdentifier
            };
            var restoreDbInstanceFromSnapshotResponse = await _rdsClient.RestoreDBInstanceFromDBSnapshotAsync(restoreDbInstanceFromSnapshotRequest);
            _logger.LogInformation($"Snapshot {snapshotIdentifier} restored successfully to new database instance {tempDevDbIdentifier}.");

            // Wait for the new database instance to be available.
            _logger.LogInformation($"Waiting for new database instance {tempDevDbIdentifier} to be available.");
            var describeTempDbInstanceRequest = new DescribeDBInstancesRequest
            {
                DBInstanceIdentifier = tempDevDbIdentifier
            };
            DBInstance tempInstance;
            do
            {
                var describeDbInstanceResponse = await _rdsClient.DescribeDBInstancesAsync(describeTempDbInstanceRequest);
                tempInstance = describeDbInstanceResponse.DBInstances[0];
                await Task.Delay(TimeSpan.FromSeconds(10));
            } while (tempInstance.DBInstanceStatus != "available");

            _logger.LogInformation($"New database instance {tempDevDbIdentifier} is available.");

            // Delete the existing development database instance.
            _logger.LogInformation($"Deleting existing development database instance {devDbIdentifier}.");
            var deleteDbInstanceRequest = new DeleteDBInstanceRequest
            {
                DBInstanceIdentifier = devDbIdentifier,
                SkipFinalSnapshot = true
            };
            var deleteDbInstanceResponse = await _rdsClient.DeleteDBInstanceAsync(deleteDbInstanceRequest);
            _logger.LogInformation($"Existing development database instance {devDbIdentifier} deleted successfully.");

            _logger.LogInformation($"Waiting for existing development database instance {devDbIdentifier} to be deleted.");
            var describeDevDbInstanceRequest = new DescribeDBInstancesRequest
            {
                DBInstanceIdentifier = devDbIdentifier
            };
            // Wait for the database instance to be deleted.
            DBInstance deletedInstance;
            try
            {
                do
                {
                    var describeDevDbInstanceResponse = await _rdsClient.DescribeDBInstancesAsync(describeDevDbInstanceRequest);
                    deletedInstance = describeDevDbInstanceResponse.DBInstances.FirstOrDefault();
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    _logger.LogInformation($"Waiting for existing development database instance {devDbIdentifier} to be deleted. Curret status is {deletedInstance.DBInstanceStatus}");
                } while (deletedInstance != null && deletedInstance.DBInstanceStatus != "deleted");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not find DB instace {devDbIdentifier}");
            }

            _logger.LogInformation($"Existing development database instance {devDbIdentifier} has been deleted.");

            // Rename the new database instance to the name of the development database instance.
            _logger.LogInformation($"Renaming new database instance {tempDevDbIdentifier} to {devDbIdentifier}.");
            var modifyDbInstanceRequest = new ModifyDBInstanceRequest
            {
                DBInstanceIdentifier = tempDevDbIdentifier,
                NewDBInstanceIdentifier = devDbIdentifier,
                ApplyImmediately = true
            };
            var modifyDbInstanceResponse = await _rdsClient.ModifyDBInstanceAsync(modifyDbInstanceRequest);
            _logger.LogInformation($"New database instance {tempDevDbIdentifier} renamed successfully to {devDbIdentifier}.");

            // Wait for the database instance to be renamed.
            _logger.LogInformation($"Waiting for new database instance {tempDevDbIdentifier} to be renamed to {devDbIdentifier}.");
            DBInstance renamedInstance;
            try
            {
                do
                {
                    var describeDbInstanceResponse = await _rdsClient.DescribeDBInstancesAsync(describeTempDbInstanceRequest);
                    renamedInstance = describeDbInstanceResponse.DBInstances[0];
                    await Task.Delay(TimeSpan.FromSeconds(10));
                } while (renamedInstance.DBInstanceIdentifier != devDbIdentifier);
            }
            catch (DBInstanceNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Could not find DB instace {tempDevDbIdentifier}");
                var describeDbInstanceResponse = await _rdsClient.DescribeDBInstancesAsync(describeDevDbInstanceRequest);
                _logger.LogInformation($"New database instance {tempDevDbIdentifier} has been renamed to {devDbIdentifier}.");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while replacing the development database with a production database snapshot.");
            return StatusCode(500, "An error occurred while replacing the development database with a production database snapshot.");
        }
    }
}

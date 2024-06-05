# RDS Database Swap POC

This is a proof-of-concept (POC) project that demonstrates how to use the Amazon RDS (Relational Database Service) SDK to manage database snapshots and instances. The project includes an API controller that handles a POST request to the `snapshot-and-apply` endpoint, which replaces dev database with prod database snapshot.

## Getting Started

To run this project, you will need to have the following prerequisites installed:

* .NET 8 SDK
* An AWS account with access to the RDS service and permissions to manage databases/snapshots
* A PostgreSQL database with a connection string configured in the `appsettings.json` file
* Two databases (database-dev-1 and database-prod-1) configured on AWS RDS.

Once you have the prerequisites installed, you can clone this repository and run the following commands to build and run the project:

```
dotnet build
dotnet run
```

The API will be available at `https://localhost:5086`.

## Configuration

The project can be configured using the `appsettings.json` file. The following settings are required:

* `ConnectionStrings:AppDb`: The connection string for the PostgreSQL database.
* `AWS:Region`: The AWS region for the RDS service.
* `AWS:AccessKey`: The AWS access key for the RDS service.
* `AWS:SecretKey`: The AWS secret key for the RDS service.

## Usage

To use the API, you can send a POST request to the `https://localhost:5086/aws/snapshot-and-apply` endpoint. This will trigger the `SnapshotAndApply` method in the `AwsController` class, which will perform the steps outlined above.

The `JobLogger` service will automatically start logging a GUID to the console and to the database every 99,999 seconds.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more information.

using Amazon.RDS;
using Microsoft.EntityFrameworkCore;
using PocRdsDatabaseSwap.API.Data;
using PocRdsDatabaseSwap.API.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("AppDb")
                       ?? throw new ArgumentNullException("AppDb connection string is null.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// var awsAccessKey = builder.Configuration["AWS:AccessKey"]
//                        ?? throw new ArgumentNullException("AWS access key connection string is null.");
// var awsSecretKey = builder.Configuration["AWS:SecretKey"]
//                    ?? throw new ArgumentNullException("AWS secret key connection string is null.");

var x = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonRDS>();

builder.Services.AddHostedService<JobLogger>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
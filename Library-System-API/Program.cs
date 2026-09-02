using LibrarySystem.API.Extensions;
using LibrarySystem.API.Middleware;
using LibrarySystem.Business.Hubs;
using LibrarySystem.DataAccess.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLibrarySystemServices(builder.Configuration);

var app = builder.Build();

await app.Services.SeedDevelopmentDataAsync(app.Environment);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Real-time notifications for admins and users.
app.MapHub<NotificationsHub>("/hubs/notifications");

app.Run();

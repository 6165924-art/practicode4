using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("ToDoDB"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB"))
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => new { message = "ToDoList API is running!" });

app.MapGet("/api/tasks", async (ToDoDbContext db) =>
{
    try
    {
        var tasks = await db.Items.ToListAsync();
        return Results.Ok(tasks);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error retrieving tasks: {ex.Message}");
    }
});

app.MapPost("/api/tasks", async (ToDoDbContext db, Item newTask) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(newTask.Name))
        {
            return Results.BadRequest("Task name is required");
        }

        db.Items.Add(newTask);
        await db.SaveChangesAsync();
        
        return Results.Created($"/api/tasks/{newTask.Id}", newTask);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error creating task: {ex.Message}");
    }
});

app.MapPut("/api/tasks/{id}", async (ToDoDbContext db, int id, Item updatedTask) =>
{
    try
    {
        var existingTask = await db.Items.FindAsync(id);
        
        if (existingTask == null)
        {
            return Results.NotFound($"Task with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(updatedTask.Name))
        {
            existingTask.Name = updatedTask.Name;
        }
        existingTask.IsComplete = updatedTask.IsComplete;

        db.Items.Update(existingTask);
        await db.SaveChangesAsync();
        
        return Results.Ok(existingTask);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error updating task: {ex.Message}");
    }
});

app.MapDelete("/api/tasks/{id}", async (ToDoDbContext db, int id) =>
{
    try
    {
        var task = await db.Items.FindAsync(id);
        
        if (task == null)
        {
            return Results.NotFound($"Task with ID {id} not found");
        }

        db.Items.Remove(task);
        await db.SaveChangesAsync();
        
        return Results.Ok("Task deleted successfully");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error deleting task: {ex.Message}");
    }
});

app.Run();
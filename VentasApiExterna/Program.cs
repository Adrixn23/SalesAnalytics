using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var comments = new[]
{
    new { CommentId = 1, CustomerEmail = "michaelcannon@yahoo.com", CommentText = "El producto llegó en excelentes condiciones.", CommentDate = DateTime.Now.AddDays(-5) },
    new { CommentId = 2, CustomerEmail = "ryansmith@yahoo.com", CommentText = "Muy buen servicio al cliente.", CommentDate = DateTime.Now.AddDays(-2) }
};

app.MapGet("/api/comments", () => comments);

app.Run();

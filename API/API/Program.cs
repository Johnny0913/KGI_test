var builder = WebApplication.CreateBuilder(args);

// 加入 CORS 政策
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy => policy.WithOrigins("http://localhost:5173") // Vue 開發網址
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// Add services to the container.

builder.Services.AddControllers();
//.AddJsonOptions(options =>
// {
//     // 關鍵：強制 JSON 序列化時保持屬性名稱原貌，不要自動轉 camelCase
//     options.JsonSerializerOptions.PropertyNamingPolicy = null;
// });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowVueApp"); // 啟用 CORS

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

using AutoMapper;
using Microsoft.AspNetCore.Mvc.Formatters;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5001;http://localhost:5000");
//builder.WebHost.UseEnvironment("Development");
builder.Services.AddControllers();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddControllers(options =>
    {
        // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
        options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
        // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
        options.ReturnHttpNotAcceptable = true;
        // Эта настройка приводит к игнорированию заголовка Accept, когда он содержит */*
        // Здесь она нужна, чтобы в этом случае ответ возвращался в формате JSON
        options.RespectBrowserAcceptHeader = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });


builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(c => c.Id))
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(c => $"{c.LastName} {c.FirstName}"))
        .ForMember(dest => dest.Login, opt => opt.MapFrom(c => c.Login))
        .ForMember(dest => dest.GamesPlayed, opt => opt.MapFrom(c => c.GamesPlayed))
        .ForMember(dest => dest.CurrentGameId, opt => opt.MapFrom(c => c.CurrentGameId));
});

var app = builder.Build();

app.MapControllers();

app.Run();

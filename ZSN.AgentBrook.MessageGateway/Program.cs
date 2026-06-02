using Scalar.AspNetCore;
using ZSN.AgentBrook.MessageGateway.Configuration;
using ZSN.AgentBrook.MessageGateway.Interfaces;
using ZSN.AgentBrook.MessageGateway.Services;
using ZSN.AgentBrook.MessageGateway.Providers.WeChatWork;
using ZSN.AgentBrook.MessageGateway.Providers.WhatsApp;
using ZSN.AgentBrook.MessageGateway.Providers.DingTalk;
using ZSN.AgentBrook.MessageGateway.Providers.Feishu;
using ZSN.AgentBrook.MessageGateway.Providers.Test;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection("MessageGateway"));

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IMessageProvider, WeChatWorkProvider>();
builder.Services.AddSingleton<IMessageProvider, WhatsAppProvider>();
builder.Services.AddSingleton<IMessageProvider, DingTalkProvider>();
builder.Services.AddSingleton<IMessageProvider, FeishuProvider>();
builder.Services.AddSingleton<IMessageProvider, TestProvider>();

builder.Services.AddSingleton<IMessageProviderFactory, MessageProviderFactory>();
builder.Services.AddScoped<IMessageSendService, MessageSendService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IMessageRouter, MessageRouter>();

builder.Services.AddHostedService<MessageSendQueueConsumer>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference("/doc");

app.MapControllers();

app.Run("http://0.0.0.0:5008");

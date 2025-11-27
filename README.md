# MailWarmup

Gmail (OAuth) ve Custom SMTP/IMAP hesaplarını ısıtmak için otomatik mail gönderme, cevaplama, spam kurtarma, okunmuş işaretleme ve arşivleme yapan bir arka plan servisi.

## Özellikler
- Günlük warmup işleri üretimi (send/reply/mark-important/move-to-inbox).
- Gönderilen warmup maillerini 10 sn’lik hızlı döngüyle yakalayıp spam’den çıkarma, okunmuş işaretleyip arşivleme. 5 dakikalık tam tarama fallback.
- Manuel `api/mail-accounts/{id}/fetch` ile anlık kurtarma.
- Gmail OAuth 2.0 token yenileme, token cache (`AutoWarm.GmailTokens`).
- Warmup profiliyle günlük hacim, zaman aralığı, reply oranı, süre kısıtı.
- Dashboard/veri için WarmupEmailLog kayıtları.

## Mimarinin Kısa Özeti
- **AutoWarm.Api**: REST API + hosted services (`DailyWarmupJobScheduler`, `WarmupJobExecutor`, `WarmupInboxMonitor`).
- **AutoWarm.Application**: İş mantığı (WarmupEngine, strateji, servisler) + DI kayıtları.
- **AutoWarm.Infrastructure**: EF Core SQL/Memory repo’lar, Gmail/SMTP sağlayıcıları.
- **AutoWarm.Domain**: Entity ve enum’lar.
- **WarmupInboxMonitor**: 10 sn’de bir kuyruklu hızlı tarama, 5 dakikada bir tam tarama; spam/okundu/arşiv temizliği.
- **WarmupEngine**: Günlük işler üretir, pending işleri icra eder ve gönderilen mailler için hızlı kurtarma kuyruğunu tetikler.

## Gereksinimler
- .NET 9 SDK (preview uyarıları görünebilir).
- SQL Server (veya yoksa in-memory DB dev için).
- Gmail OAuth istemci kimliği/sırrı ve frontend redirect (gerekirse).

## Hızlı Başlangıç
1) **Config**: `appsettings.Development.json` içine:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MailWarmup;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "SigningKey": "your-very-long-secret"
  },
  "GmailOAuth": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scopes": "https://www.googleapis.com/auth/gmail.modify",
    "FrontendRedirectUri": "http://localhost:5173"
  }
}
```
2) **DB**: `dotnet ef database update` (veya in-memory ile geçici çalış).
3) **Çalıştır**: `dotnet run --project AutoWarm.Api`.
4) **Swagger**: Dev ortamında açılır (`/swagger`).

## Önemli Akışlar
- **Gmail OAuth**:
  - `POST api/mail-accounts/gmail/start-auth` → auth URL/state.
  - Callback `GET api/mail-accounts/gmail/callback?code=...&state=...` → hesap oluşturur, token’ı cache’ler.
- **Warmup profili**: Hesaba özel günlük min/max, reply rate, zaman aralığı, süre kısıtı; oluşturunca bugünün işleri hemen planlanır.
- **Warmup işleri**:
  - `DailyWarmupJobScheduler` her gün (02:00 varsayılan) işler üretir.
  - `WarmupJobExecutor` pending işleri dakikada bir çalıştırır; send/reply sonrası hedef hesabı hızlı kurtarma kuyruğuna ekler.
- **Hızlı kurtarma**:
  - `WarmupInboxMonitor` 10 sn’de bir kuyruğu tüketir: SPAM → çıkar, UNREAD → kaldırır, INBOX → kaldırarak arşivler. `OpenedAt` loglanır.
  - 5 dakikada bir tam tarama fallback; manuel `GET api/mail-accounts/{id}/fetch` de aynı kurtarma mantığını kullanır.

## Operasyon Notları
- Gmail kotası için `MaxResults` küçük tutulur; daha ileri optimizasyon için `users.watch` + Pub/Sub ve `batchModify` değerlendirilebilir.
- Token cache (`AutoWarm.GmailTokens`) dosya tabanlıdır; çok instance’lı dağıtım için merkezi store önerilir.
- Rate limit / 429 durumlarında exponential backoff eklenebilir (şu an temel akış çalışır durumda).

## Geliştirme Komutları
- Build: `dotnet build AutoWarm.sln`
- Test (eklenirse): `dotnet test`
- API çalıştır: `dotnet run --project AutoWarm.Api`

## Güvenlik
- `Jwt.SigningKey` güçlü ve uzun olmalı; prod ortamda gizli yönetimi kullanın.
- Gmail OAuth client secret, refresh token gibi bilgileri gizli tutun.

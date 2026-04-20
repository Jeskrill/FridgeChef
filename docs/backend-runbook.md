# Backend Runbook

## CI

Основной workflow: `.github/workflows/backend-ci.yml`.

Он проверяет:
- `dotnet restore`
- `dotnet build`
- `dotnet test` для `tests/FridgeChef.Backend.Tests`
- синтаксис pricing sidecar через `node --check tools/scraper/server.js`

## Обязательные переменные окружения

- `Jwt__Secret`:
  Должен быть задан во всех non-dev средах.
  Минимум 32 байта.

- `ConnectionStrings__DefaultConnection`:
  Основная PostgreSQL connection string.

- `Pricing__PuppeteerScraper__BaseUrl`:
  URL sidecar-сервера, если pricing endpoints и sync используются не локально.

- `Pricing__Sync__BatchSize`:
  Размер батча для batch-capable scraper.

- `Pricing__Sync__MaxIngredientsPerRun`:
  Ограничитель для controlled run.
  `0` означает без лимита.
  Для smoke/preprod удобно ставить `3..20`.

- `Pricing__AutoSync`:
  По умолчанию `false`.
  Включать только после проверки sidecar и smoke-прогона.

## Локальный smoke-прогон

Скрипт:

```bash
./tools/smoke/run_local_backend_smoke.sh
```

Что делает скрипт:
- поднимает fake pricing sidecar
- поднимает локальный API
- регистрирует временного пользователя
- проверяет публичные, user и admin endpoints по HTTP
- для admin pricing endpoints временно повышает smoke-user до `admin`
- делает cleanup временного пользователя

Требования:
- локальная PostgreSQL `fridgechef`
- `curl`
- `jq`
- `node`
- `dotnet`
- `psql`

## Deploy Checklist

Перед выкладкой:
- прогнать `dotnet build`
- прогнать `dotnet test tests/FridgeChef.Backend.Tests/FridgeChef.Backend.Tests.csproj`
- прогнать `./tools/smoke/run_local_backend_smoke.sh`
- проверить `/health`
- проверить `/admin/pricing/status`
- убедиться, что `Jwt__Secret` приходит из secret store, а не из файла
- убедиться, что `Pricing__AutoSync=false`, если sidecar еще не валидирован на стенде

После выкладки:
- прогнать smoke against deployed API
- отдельно проверить `GET /admin/pricing/status`
- только потом включать autosync

## Pricing Sidecar

Поддерживаемые env vars:
- `PORT`
- `PUPPETEER_HEADLESS`
- `CHROME_EXECUTABLE_PATH`

Рекомендуемый порядок запуска:
1. Поднять sidecar
2. Проверить `/admin/pricing/status`
3. Прогнать `POST /admin/pricing/search-test`
4. Прогнать controlled sync с лимитом `Pricing__Sync__MaxIngredientsPerRun`
5. Только после этого включать `Pricing__AutoSync=true`

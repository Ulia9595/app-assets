# Сервер аутентификации и обучения Kotlin — ASP.NET Core

## Назначение
Backend-сервер для образовательной системы «Обучение KOTLIN», обеспечивающий:
- аутентификацию и авторизацию пользователей (JWT + Cookie);
- управление учебным контентом (темы, уровни, задания);
- автоматическую проверку практических заданий через Piston API;
- проведение PvP-турниров в реальном времени (SignalR);
- веб-панель администратора.

## Содержимое проекта
- ASP.NET Core Web API приложение (REST API + SignalR Hub)
- Веб-панель администратора (MVC, Razor)
- Интеграционные тесты
- Примеры конфигурационных файлов
- Статические файлы веб-интерфейса сброса пароля

## Технологический стек
- .NET 8 / ASP.NET Core
- Entity Framework Core (PostgreSQL)
- JWT аутентификация + Cookie-аутентификация для админ-панели
- SignalR (реальное время для PvP)
- BCrypt (хеширование паролей)
- MailKit (отправка email через Yandex SMTP)
- Docker + Piston API (изолированная компиляция кода)

## Структура
- **Controllers/** — контроллеры API (Auth, Profile, Learning, Task, Tournament, Admin)
- **Hubs/** — SignalR-компоненты (TournamentHub)
- **Services/** — бизнес-логика (AuthService, JwtService, TaskService, MatchmakingService и др.)
- **Models/** — сущности БД, DTO, ViewModels
- **Data/** — контекст базы данных (AppDbContext)
- **Filters/** — атрибуты ограничения запросов (RateLimit)
- **Views/** — Razor-представления административной панели
- **wwwroot/** — статические файлы (CSS, JS, страницы сброса пароля)

## Инструкция по установке

### Требования
- .NET 10 SDK
- PostgreSQL 16+
- Docker Desktop / Docker Engine (для запуска Piston API)
- Git


## Установка на локальное устройство (Windows 10/11) — подробная инструкция

### Требования
- **Visual Studio 2026** (с нагрузкой «ASP.NET и веб-разработка»)
- **PostgreSQL 17** (установщик Windows)
- **Docker Desktop** (для Piston API)
- **Git** (опционально, можно скачать ZIP-архив репозитория)

### Пошаговая инструкция:

#### 1. Установка Visual Studio 2026
- Скачать с официального сайта: https://visualstudio.microsoft.com/ru/downloads/
- При установке выбрать нагрузку **«ASP.NET и веб-разработка»** (включает .NET SDK, средства для работы с Entity Framework Core и т.д.)

#### 2. Установка PostgreSQL 17
- Скачать установщик: https://www.postgresql.org/download/windows/
- Запустить установку, запомнить пароль суперпользователя `postgres`
- Убедиться, что установлен компонент **pgAdmin** (графический интерфейс для работы с БД)
- Открыть pgAdmin → создать базу данных `kotlin_learning`

#### 3. Установка Docker Desktop
- Скачать: https://www.docker.com/products/docker-desktop/
- Установить с настройками по умолчанию
- После установки **перезагрузить компьютер**
- Запустить Docker Desktop (он должен работать в фоне)

#### 4. Клонирование репозитория
**Вариант А (через Git):**
```bash
git clone https://github.com/Ulia9595/app-assets.git
```
**Вариант Б (без Git):**
- Скачать ZIP-архив репозитория (зелёная кнопка «Code» → «Download ZIP»)
- Распаковать в удобную папку

#### 5. Открытие проекта в Visual Studio
- Запустить Visual Studio 2022
- Открыть папку с проектом (`Файл → Открыть → Проект/Решение`) — выбрать `.sln` файл, если есть, или открыть как папку

#### 6. Настройка `appsettings.json`
- В папке проекта найти файл `appsettings.Example.json`
- Скопировать его и переименовать копию в `appsettings.json`
- Отредактировать `appsettings.json` (в Visual Studio или любом текстовом редакторе):
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kotlin_learning;Username=postgres;Password=ВАШ_ПАРОЛЬ"
  },
  "JwtSettings": {
    "Key": "СГЕНЕРИРУЙТЕ_ДЛИННУЮ_СТРОКУ_НЕ_МЕНЕЕ_64_СИМВОЛОВ",
    "Issuer": "GameAuthServer",
    "Audience": "GameAuthApp"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.yandex.ru",
    "SmtpPort": 587,
    "SenderEmail": "ВАША_ПОЧТА@yandex.ru",
    "SenderPassword": "ПАРОЛЬ_ПРИЛОЖЕНИЯ",
    "EnableSsl": true
  },
  "PistonApi": {
    "BaseUrl": "http://localhost:2000"
  }
  ```

#### 7. Восстановление зависимостей и сборка
- В Visual Studio: **ПКМ по проекту → «Восстановить пакеты NuGet»** (или через меню `Сборка → Восстановить пакеты NuGet`)
- Нажать `Сборка → Собрать решение` (или `Ctrl+Shift+B`)

#### 8. Настройка и запуск Piston API
- **Запустить Docker Desktop** (он должен быть запущен)
- Открыть **командную строку** или **PowerShell** (администратор не требуется) и выполнить:
  ```bash
  docker run -d --name piston -p 2000:2000 ghcr.io/engineer-man/piston
  ```
- Проверить работоспособность: в браузере открыть `http://localhost:2000/api/v2/info` — должен вернуться JSON

#### 9. Выполнение миграций базы данных
- Открыть в Visual Studio **Консоль диспетчера пакетов** (`Инструменты → Диспетчер пакетов NuGet → Консоль диспетчера пакетов`)
- Выбрать проект, где находится `AppDbContext`
- Выполнить команду:
  ```powershell
  Update-Database
  ```
  *Если команда не найдена — установить инструменты EF Core:*
  ```powershell
  dotnet tool install --global dotnet-ef
  dotnet ef database update
  ```

#### 10. Запуск проекта
- В Visual Studio нажать `F5` (или зелёную кнопку «Запустить»)
- Сервер запустится по адресу: `http://localhost:5141`

## Альтернативный вариант (без Visual Studio, только через терминал)

```bash
# Установить .NET 10 SDK (https://dotnet.microsoft.com/download)
# Установить PostgreSQL 17
# Установить Docker Desktop
# Клонировать репозиторий
git clone https://github.com/Ulia9595/app-assets.git
cd app-assets

# Восстановить зависимости
dotnet restore

# Собрать проект
dotnet build --configuration Release

# Применить миграции
dotnet ef database update

# Запустить
dotnet run --project WebApplication1.csproj --urls "http://0.0.0.0:5141"
```

### Установка на удалённый сервер (Linux Ubuntu/Debian)

1. **Подключиться к серверу по SSH**
   ```bash
   ssh user@your-server-ip
   ```

2. **Обновить систему**
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

3. **Установить PostgreSQL 17**
   ```bash
   sudo apt install postgresql-17 -y
   sudo systemctl start postgresql
   sudo systemctl enable postgresql
   sudo -u postgres psql -c "CREATE DATABASE kotlin_learning;"
   sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'ваш_надежный_пароль';"
   ```

4. **Установить .NET 10 Runtime**
   ```bash
   sudo apt install dotnet-runtime-10.0 -y
   ```

5. **Установить Docker Engine**
   ```bash
   sudo apt install docker.io -y
   sudo systemctl start docker
   sudo systemctl enable docker
   sudo usermod -aG docker $USER
   ```

6. **Скопировать файлы проекта на сервер**
   ```bash
   git clone https://github.com/Ulia9595/app-assets.git
   cd app-assets
   ```

7. **Опубликовать приложение**
   ```bash
   dotnet publish WebApplication1.csproj -c Release -o ./publish
   ```

8. **Настроить `appsettings.json`**
   ```bash
   nano ./publish/appsettings.json
   ```
   Пример конфигурации:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=kotlin_learning;Username=postgres;Password=ваш_пароль"
     },
     "JwtSettings": {
       "Key": "ваш_секретный_ключ_не_менее_64_символов",
       "Issuer": "GameAuthServer",
       "Audience": "GameAuthApp"
     },
     "EmailSettings": {
       "SmtpServer": "smtp.yandex.ru",
       "SmtpPort": 587,
       "SenderEmail": "ваша_почта@yandex.ru",
       "SenderPassword": "пароль_приложения",
       "EnableSsl": true
     },
     "PistonApi": {
       "BaseUrl": "http://localhost:2000"
     }
   }
   ```

9. **Запустить Piston API (Docker)**
   ```bash
   docker run -d --name piston --restart always -p 2000:2000 ghcr.io/engineer-man/piston
   ```

10. **Выполнить миграции базы данных**
    ```bash
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:$HOME/.dotnet/tools"
    dotnet ef database update --project ./publish/WebApplication1.dll
    ```

11. **Запустить сервер**
    ```bash
    cd ./publish
    dotnet WebApplication1.dll --urls "http://0.0.0.0:5141"
    ```

12. **Настроить автозапуск (systemd-сервис)**
    ```bash
    sudo nano /etc/systemd/system/kotlin-server.service
    ```
    Содержимое файла:
    ```ini
    [Unit]
    Description=Kotlin Learning Server
    After=network.target postgresql.service docker.service

    [Service]
    WorkingDirectory=/home/user/app-assets/publish
    ExecStart=/usr/bin/dotnet WebApplication1.dll --urls "http://0.0.0.0:5141"
    Restart=always
    RestartSec=10
    User=user
    Environment=ASPNETCORE_ENVIRONMENT=Production

    [Install]
    WantedBy=multi-user.target
    ```
    Активировать и запустить:
    ```bash
    sudo systemctl enable kotlin-server
    sudo systemctl start kotlin-server
    ```

13. **Проверить работоспособность**
    ```bash
    curl http://localhost:5141/api/auth/public
    curl http://localhost:2000/api/v2/info
    ```

## Клиентская часть (Android)

Клиентское Android-приложение доступно в этом же репозитории в ветке `app`.

### Ссылка на APK-файл
[Скачать APK](https://github.com/Ulia9595/app-assets/raw/app/app-debug.apk) (ветка `app`)

## Ссылки
- Серверная часть (основная ветка): https://github.com/Ulia9595/app-assets
- Клиентская часть (ветка `app`): https://github.com/Ulia9595/app-assets/tree/app
- APK-файл: https://github.com/Ulia9595/app-assets/raw/app/app-debug.apk
- API документация: `/swagger` (в режиме разработки)
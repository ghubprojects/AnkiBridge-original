# AnkiBridge

AnkiBridge là ứng dụng học từ vựng self-hosted dành cho một người dùng. Hệ thống không có tài khoản hay màn hình đăng nhập và mặc định chỉ lắng nghe trên `127.0.0.1`.

## Self-host bằng Docker Compose

Máy người dùng chỉ cần:

- Docker có Docker Compose v2 (Docker Desktop trên Windows/macOS hoặc Docker Engine trên Linux).
- Anki và add-on AnkiConnect đang chạy trên cùng máy.

Sau khi clone repository, tạo cấu hình cục bộ rồi chạy tại thư mục gốc:

```shell
cp .env.example .env
# Sửa POSTGRES_PASSWORD trong .env trước khi tiếp tục.
docker compose up -d
```

Lần đầu Docker sẽ build ứng dụng, tạo database, chạy migration/seed rồi khởi động web. Kiểm tra trạng thái bằng:

```shell
docker compose ps
docker compose logs migrations
```

| Thành phần | Địa chỉ mặc định | Ghi chú |
| --- | --- | --- |
| AnkiBridge | http://localhost:8080 | Ứng dụng chính, không login |
| PostgreSQL | `localhost:5432` | Database `ankibridgedb` |
| Azurite Blob | `http://localhost:10000` | Chỉ chạy Blob service; không có Queue/Table |

`docker compose down` chỉ dừng và xóa container. Dữ liệu vẫn nằm trong các named volume:

- `ankibridge_postgres-data`: PostgreSQL.
- `ankibridge_azurite-data`: Blob data.
- `ankibridge_web-keys`: ASP.NET Core Data Protection keys.

Không chạy `docker compose down -v` nếu muốn giữ dữ liệu.

## Luồng lưu Learning Entry và media

Khi bấm **Create entry**:

1. Learning Entry và outbox message được ghi vào PostgreSQL trong cùng một transaction.
2. UI trả kết quả ngay, không chờ tải audio/image.
3. Background worker đọc outbox: với URL online, worker tải URL; với file upload local, worker mở trực tiếp absolute path của file tạm. Cả hai đều được lưu vào Azurite Blob rồi cập nhật trạng thái `Pending`, `Completed` hoặc `Failed`.
4. File local chỉ bị xóa sau khi Blob upload thành công, nên outbox vẫn retry được khi upload lỗi.

Outbox chỉ lưu metadata cùng URL nguồn hoặc absolute local path, không lưu byte/base64 của file trong PostgreSQL. Vì local path là file tạm của Web process, cần để ứng dụng chạy đến khi worker xử lý xong upload đó.

## Kết nối PostgreSQL bằng DBeaver

Tạo PostgreSQL connection với:

- Host: `localhost`
- Port: `5432`
- Database: `ankibridgedb`
- Username: `postgres`
- Password: giá trị `POSTGRES_PASSWORD` trong file `.env`
- SSL mode: `disable`

## Cấu hình

Dự án tách cấu hình thành ba lớp độc lập:

| Lớp | Mục đích | Vị trí | Commit vào Git |
| --- | --- | --- | --- |
| Cấu trúc và default | Giá trị không nhạy cảm, dùng chung | `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` | Có |
| Secret khi chạy F5 qua AppHost | Password PostgreSQL và API key trên máy dev | AppHost .NET User Secrets | Không |
| Secret khi chạy Docker | Password và API key của container | `.env`, được `compose.yaml` đưa vào bằng environment variables | Không |

ASP.NET Core áp dụng cấu hình theo thứ tự: `appsettings.json` → file theo environment → User Secrets khi `Development` → environment variables → command-line arguments. Giá trị phía sau ghi đè giá trị phía trước.

Repository không dùng `appsettings.Local.json` hay `appsettings.Staging.json`. Không file `appsettings*.json` nào chứa password hoặc connection string thật. Connection string `UseDevelopmentStorage=true` trong Development chỉ là shortcut công khai của Azurite local.

### Chạy development bằng AppHost

Toàn team dùng `AnkiBridge.AppHost` làm startup project khi F5. Không chạy trực tiếp `AnkiBridge.Web`: AppHost chịu trách nhiệm khởi tạo PostgreSQL, Azurite và MigrationService, sau đó truyền resource reference cùng API key vào Web.

Đặt ba parameter secret vào User Secrets của AppHost một lần trên mỗi máy dev:

```powershell
dotnet user-secrets set "Parameters:postgres-password" "<long-random-password>" --project aspire/AnkiBridge.AppHost
dotnet user-secrets set "Parameters:pixabay-api-key" "<your-pixabay-key>" --project aspire/AnkiBridge.AppHost
dotnet user-secrets set "Parameters:pexels-api-key" "<your-pexels-key>" --project aspire/AnkiBridge.AppHost
```

Nếu parameter chưa được cấu hình, Aspire Dashboard sẽ yêu cầu nhập khi khởi động; chọn lưu vào User Secrets để không phải nhập lại. AppHost truyền hai API key sang Web bằng `Images__Pixabay__ApiKey` và `Images__Pexels__ApiKey`, tương ứng với `Images:Pixabay:ApiKey` và `Images:Pexels:ApiKey` trong .NET configuration.

Chọn `AnkiBridge.AppHost` làm startup project rồi nhấn F5, hoặc chạy:

```powershell
dotnet run --project aspire/AnkiBridge.AppHost/AnkiBridge.AppHost.csproj
```

Nếu trước đây đã lưu API key trong User Secrets của Web, có thể xóa để tránh còn hai nguồn cấu hình development:

```powershell
dotnet user-secrets remove "Images:Pixabay:ApiKey" --project sources/AnkiBridge.Web
dotnet user-secrets remove "Images:Pexels:ApiKey" --project sources/AnkiBridge.Web
```

Runtime migration được AppHost chạy tự động. `ApplicationDbContextFactory` chỉ phục vụ EF tooling; khi cần tạo migration mới, đặt riêng connection string trỏ đến PostgreSQL endpoint đang hiển thị trong Aspire Dashboard vào User Secrets của Infrastructure:

```powershell
dotnet user-secrets set "ConnectionStrings:ankibridgedb" "Host=localhost;Port=<postgres-host-port>;Database=ankibridgedb;Username=postgres;Password=<apphost-postgres-password>" --project sources/AnkiBridge.Infrastructure
```

Tạo migration trực tiếp từ project Infrastructure:

```powershell
dotnet ef migrations add <MigrationName> `
  --project sources/AnkiBridge.Infrastructure `
  --startup-project sources/AnkiBridge.Infrastructure `
  --context ApplicationDbContext `
  --output-dir Persistence/Migrations
```

Nếu chạy lệnh ngay trong thư mục `sources/AnkiBridge.Infrastructure`, có thể bỏ cả `--project` và `--startup-project`.

### Chạy toàn bộ bằng Docker

File `.env` là bắt buộc vì PostgreSQL không có password mặc định được commit trong repository. Copy `.env.example` thành `.env`, đặt một password dài và ngẫu nhiên, rồi cấu hình port hoặc API key nếu cần. `.env` đã được Git ignore.

```shell
cp .env.example .env
docker compose up -d --build
```

Hai API key `PIXABAY_API_KEY` và `PEXELS_API_KEY` là tùy chọn. Provider không có key sẽ được bỏ qua; `Scrape online` vẫn hoạt động nhưng không có ảnh nếu cả hai key đều trống.

Nếu đổi `AZURITE_BLOB_PORT`, cần đổi `AZURITE_PUBLIC_ENDPOINT` tương ứng. Nếu đổi thông tin PostgreSQL sau khi volume đã được tạo, biến môi trường không tự đổi user/password đang tồn tại trong database.

`compose.yaml` đặt Web và MigrationService ở environment `Production`; connection string, password và API key chỉ được tạo từ `${...}` khi container khởi động. Dockerfile là multi-stage và dùng `dotnet publish`, vì vậy các file `appsettings.json`/`appsettings.Production.json` an toàn được copy vào image, còn `.env` không nằm trong build context của image.

### Kết nối Blob bằng Azure Storage Explorer

Dùng connection string Blob-only sau (đổi port nếu đã override):

```text
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```

Đây là account/key công khai mặc định của Azurite, không phải secret dùng cho production.

## AnkiConnect và Docker

Trên Docker Desktop (Windows/macOS), web container gọi AnkiConnect qua `host.docker.internal:8765`. Hãy mở Anki trước khi đồng bộ deck/note.

Trên Docker Engine native Linux, AnkiConnect mặc định chỉ bind `127.0.0.1`, trong khi container truy cập host qua Docker bridge. Mở **Tools → Add-ons → AnkiConnect → Config**, đặt `webBindAddress` thành `0.0.0.0`, sau đó restart Anki. Chỉ cho phép port `8765` từ Docker bridge trong firewall; không mở port này ra LAN/Internet.

## Cập nhật và xử lý sự cố

```shell
git pull
docker compose up -d --build
```

Xem log:

```shell
docker compose logs -f web
docker compose logs -f migrations
```

Nếu migration thất bại, `web` sẽ không khởi động. Sửa nguyên nhân rồi chạy lại `docker compose up -d`; migration và seed có thể chạy lặp lại an toàn.

## AppHost và Docker Compose

AppHost là workflow development thống nhất của team nhưng không đọc, sinh hay đồng bộ `compose.yaml`/`compose.infra.yaml`. Docker Compose vẫn là đường chạy độc lập cho self-hosted; người dùng self-hosted không cần .NET SDK hoặc Aspire.

AppHost để Aspire tự cấp host port động cho PostgreSQL và Azurite. Địa chỉ thực tế được inject tự động vào Web, nên không phụ thuộc port của Docker Compose.

## Giới hạn bảo mật

Thiết kế không-login chỉ phù hợp khi chạy cho một người trên máy cá nhân. Mặc định `BIND_ADDRESS=127.0.0.1` là một phần của ranh giới bảo mật. Không đổi thành `0.0.0.0` và không public ra Internet nếu chưa bổ sung authentication, HTTPS và reverse proxy.

# FiasXMLToCSV

ASP.NET Core API: скачать выгрузку ГАР/ФИАС, распаковать ZIP и потоково
переложить XML в CSV. Колонки берутся из XSD в `gar_schemas/`.

XmlReader + CsvHelper: файл целиком в память не грузится. Скачивание,
распаковка и конвертация — отдельные сервисы за интерфейсами.

.NET 8 · ASP.NET Core · CsvHelper · Swagger

```bash
dotnet restore
dotnet run --project FiasXMLToCSV.Server
```

Swagger в Development: `/swagger`.

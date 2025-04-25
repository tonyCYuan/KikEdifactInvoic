# KIK EDIFACT INVOIC Message Generator

This application generates EDIFACT INVOIC D01B messages for KIK from invoice data stored in a PostgreSQL database.

## Features

- Retrieves invoice data from PostgreSQL database
- Maps charge codes to EDIFACT codes
- Maps container sizes to standardized codes
- Generates proper EDIFACT INVOIC D01B messages
- Saves output files with custom naming convention: `INVOICE_KYBREC_{timestamp}_{invoice number}.edi`

## Configuration

Configuration settings are stored in `appsettings.json`:

- Database connection string
- EDIFACT sender and receiver information
- Output settings

## Usage

Run the application with the following command:

```
dotnet run
```

To enable debug mode:

```
dotnet run -- --debug
```

To process specific invoices:

```
dotnet run -- INVOICE1 INVOICE2
```

## Project Structure

- **Program.cs**: Main entry point and application logic
- **Models/**: Data models for invoices and mappings
- **Repositories/**: Database access code
- **Services/**: Business logic for invoice processing
- **Edifact/**: EDIFACT message building
- **Data/**: Reference data for mappings
- **Queries/**: SQL queries for data retrieval

## Dependencies

- .NET 7.0
- Npgsql
- Microsoft.Extensions libraries for configuration, DI, and logging
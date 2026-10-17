# Ecommerce Order & Inventory System 

A high-concurrency, production-grade .NET 8 Web API built using Clean Architecture principles, Entity Framework Core, and SQL Server. Designed to enforce strict server-side business rules, atomic stock reservations, optimistic concurrency, and idempotency.

---

##  Tech Stack & Architecture

* **Backend Framework:** .NET 8 Web API
* **Database & ORM:** SQL Server, Entity Framework Core (Code-First with Migrations)
* **Architecture:** Clean Architecture (`Domain`, `Application`, `Infrastructure`, `API`)
* **Key Packages & Features:** 
  * AutoMapper (Strict 1 Entity = 1 DTO mapping)
  * BCrypt.Net-Next (Secure password hashing)
  * JWT Authentication & Refresh Token Rotation
  * Built-in .NET Rate Limiter (Login protection: 5 req/min/IP)
  * `IMemoryCache` with ChangeToken Invalidation
  * Background Worker (`BackgroundService`) for automated order expiry and stock restoration

---

##  Getting Started & Prerequisites

To run this application locally on your machine, ensure you have the following installed:
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Visual Studio 2022 (or Visual Studio Code / JetBrains Rider)
* Microsoft SQL Server (LocalDB or SQL Server Express)

### 1. Configure Your Database Connection String
Before running migrations or seeding data, open **`src/Ecommerce.API/appsettings.json`** and update the `DefaultConnection` string to point to your local SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcommerceOrderDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
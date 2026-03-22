# Buyverse API

Production-ready e-commerce backend  
built with **ASP.NET Core 8** • **Clean Architecture** • **Domain-Driven Design (DDD)**

---

## ✨ Features

- 🛒 Product catalog (filtering, pagination, brands & types)
- 🧺 Shopping basket (Redis)
- 📦 Order creation & management
- 🔐 Customer authentication (JWT + ASP.NET Identity)
- 💳 Stripe Payment Intents + Webhook integration
- 🧱 Clean layered architecture with strict dependency rules
- ✅ Ready for testing & extension

---

## 🏗 Project Structure

Talabat.sln
├── Talabat.APIs → Presentation Layer
├── Talabat.Core → Domain Layer
├── Talabat.Repository → Infrastructure Layer
└── Talabat.Service → Application Layer


**Dependency Rule**
- `Talabat.Core` has **zero external dependencies**
- All outer layers depend **inward only**
- No circular references

---

## 🧩 Layers Overview

### 🔹 Talabat.APIs (Presentation Layer)
- Controllers
- DTOs
- Authentication & Authorization
- Swagger
- Global error handling
- Stripe Webhook endpoint

### 🔹 Talabat.Core (Domain Layer)
- Entities & Aggregates
- Value Objects
- Specifications
- Repository & Service interfaces  
🚫 No EF Core  
🚫 No external libraries

### 🔹 Talabat.Repository (Infrastructure Layer)
- Entity Framework Core
- DbContexts
- Repository implementations
- Migrations
- Database seeding

### 🔹 Talabat.Service (Application Layer)
- OrderService
- PaymentService
- TokenService
- Business logic orchestration

---

## 💳 Stripe Payment Integration

- Create Payment Intent
- Webhook endpoint:  

POST /api/payments/webhook

- Supported events:
- `payment_intent.succeeded`
- `payment_intent.payment_failed`
- Signature verification
- Automatic order status update

> ⚠️ Webhook secret is stored using **User Secrets** or **Environment Variables**  
> **Never committed to GitHub**

---

## 🔐 Security

- ASP.NET Identity
- JWT Bearer Authentication
- Secure password hashing
- Role-based authorization

---

## ⚙️ Configuration

### Files

- `appsettings.json`  
→ Shared non-sensitive config

- `appsettings.Development.json`  
→ Local secrets (**git ignored**)

- `appsettings.Example.json`  
→ Template for developers

### ❌ Never commit:
- JWT secret key
- Stripe secret / publishable keys
- Stripe webhook secret
- Production connection strings

---

## ▶️ How to Run Locally

```bash
git clone https://github.com/AhmedFarouk04/TalabatApi.git
cd TalabatApi

Option A — Copy example file

cp appsettings.Example.json appsettings.Development.json

Option B — Use User Secrets

dotnet user-secrets set "JWT:Key" "your-super-secret-key"
dotnet user-secrets set "StripeSettings:SecretKey" "sk_test_..."
dotnet user-secrets set "StripeSettings:WebhookSecret" "whsec_..."

Create & Seed Database

dotnet ef database update \
  --project Talabat.Repository \
  --startup-project Talabat.APIs

Run the API

dotnet run --project Talabat.APIs

Open Swagger:

https://localhost:<port>/swagger

🧠 Design Patterns Used

    Clean Architecture

    Domain-Driven Design (DDD)

    Specification Pattern

    Repository Pattern

    Unit of Work

    DTO + AutoMapper

➡️ Highly maintainable • Testable • Scalable
👤 Author

Ahmed Farouk
Backend Engineer (.NET)

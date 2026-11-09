جهزتلك README احترافي باللغة الإنجليزية بصيغة Markdown، وضفت فيه كل التفاصيل اللي طلبتها، بما فيها الـ Admin Demo Credentials، والـ Authorization لكل Endpoint، والـ Logging، والـ Docker، والـ Testing، وشرح دورة حياة الأوردر والـ Stripe Webhooks.

Writing

# 🛒 E-Commerce Backend System

A secure, scalable, and maintainable E-Commerce Backend API built with **ASP.NET Core 8**, following **Clean Architecture** principles. The system provides authentication and authorization, product and category management, shopping basket operations, order processing, Stripe payment integration, automated email notifications, and comprehensive testing.

---

## 🚀 Key Features

### 🏗️ Clean Architecture

The project follows Clean Architecture principles, separating responsibilities into independent layers:

- **Store.API** – Controllers, API endpoints, middleware, and application configuration.
- **Store.Application** – Business logic, DTOs, interfaces, and services.
- **Store.Domain** – Core entities and business rules.
- **Store.Infrastructure** – Database access, Entity Framework Core, email services, and Stripe integration.
- **Store.UnitTests** – Unit testing.
- **Store.IntegrationTest** – Integration testing.

### 🔐 Authentication & Authorization

- ASP.NET Core Identity for user management.
- Secure password hashing.
- JWT-based authentication.
- Role-based authorization.
- Admin and Client roles.
- User registration and login.
- Secure forgot-password and reset-password workflows.
- Protected endpoints based on user roles.

### 🛡️ Brute-Force Protection & Account Lockout

The system implements a progressive account-locking mechanism to protect against unauthorized login attempts:

1. **5 Failed Attempts:** Temporary account lockout for a predefined cooling period.
2. **10 Failed Attempts:** Extended or permanent lockout requiring administrative intervention or secure account recovery.
3. **Password Recovery:** Users can recover access through secure, time-sensitive email reset tokens.

### 📧 Email Service

An integrated Gmail SMTP email service handles automated notifications, including:

- Password reset emails.
- Order confirmation notifications.
- Transactional emails.
- Security-related notifications.

### 📝 Error Logging

The application includes a logging mechanism that saves application errors in a log file.

This helps with:

- Tracking runtime exceptions.
- Identifying unexpected application behavior.
- Debugging errors.
- Monitoring application failures.
- Maintaining a record of application issues for troubleshooting.

### 💳 Stripe Payment Integration

The system integrates with Stripe to process payments and synchronize payment statuses through webhooks.

Features include:

- Stripe Payment Intent creation.
- Payment Intent ID tracking.
- Stripe webhook integration.
- Payment status synchronization.
- Secure webhook signature verification.

**Order Payment Workflow:**

1. The client creates an order.
2. The order is initially created with a **Pending** payment status.
3. The application creates or processes the corresponding Stripe Payment Intent.
4. The Payment Intent ID is stored and associated with the order.
5. Once the payment is successfully completed, Stripe sends a `payment_intent.succeeded` webhook event.
6. The webhook handler verifies the event and matches its Payment Intent ID with the corresponding order.
7. The order payment status is updated to **Succeeded**.

The order remains Pending until the corresponding successful payment event is received and processed.

For local testing, Stripe CLI can be used to forward webhook events to the application:

bash

```bash
stripe listen --forward-to localhost:5000/api/Payment/webhook
```

You can also use Stripe CLI to generate test webhook events:

bash

```bash
stripe trigger payment_intent.succeeded
```

**Important:** The successful webhook must contain the Payment Intent ID associated with the actual order. A generated Stripe CLI fixture event may use a different Payment Intent ID, so use a matching test payment when verifying the complete order workflow.

### 🐳 Docker & Containerization

The project includes Docker and Docker Compose configurations.

- Dockerfile for building the API image.
- Docker Compose for managing application containers.
- Simplified local development environment.
- Consistent deployment configuration.

Run the entire environment using:

bash

```bash
docker-compose up --build
```

### 🧪 Automated Testing

The project includes two testing layers:

**Unit Tests**

- Testing individual business logic components.
- Testing services and application behavior.
- Validating basket calculations and business rules.

**Integration Tests**

- Testing API endpoints.
- Testing interactions between application layers.
- Verifying database and application workflows.
- Testing important application scenarios.

Run all tests:

bash

```bash
dotnet test
```

### ⚙️ CI/CD

GitHub Actions is configured to automatically:

- Restore dependencies.
- Build the solution.
- Run unit tests.
- Run integration tests.

The workflow is located at:

text

```text
.github/workflows/ci.yml
```

---

## 👤 Demo Admin Account

The project includes a predefined demo administrator account for testing the application from the Admin perspective.

| Credential | Value |
| --- | --- |
| Email | admin@store.com |
| Password | Admin#123 |
| Role | Admin |

**Important Security Notice:**

This is a demo account intended for development and testing purposes only. It allows reviewers to log in and explore administrative functionality.

Do not use these credentials in production. Change the password and configure secure credentials before deployment.

---

## 🔑 API Authentication & Authorization

The API uses role-based authorization to control access to endpoints.

There are two main roles:

- **Admin:** Responsible for administrative operations, managing products and categories, and accessing administrative data.
- **Client:** Responsible for shopping operations, managing their basket, creating orders, and accessing customer-related functionality.

### Account Endpoints

Base route: `/api/Account`

| Method | Endpoint | Access |
| --- | --- | --- |
| POST | `/api/Account/register` | Public |
| POST | `/api/Account/login` | Public |
| POST | `/api/Account/create-admin` | Admin |
| POST | `/api/Account/forgot-password` | Public |
| POST | `/api/Account/reset-password` | Public |
| GET | `/api/Account/currentuser` | Authenticated Users (Any Role) |

**Notes:**

- Registration and login are publicly accessible.
- Creating an administrator requires an existing Admin account.
- The Current User endpoint requires authentication, regardless of whether the authenticated user has the Admin or Client role.

### Product Endpoints

| Method | Endpoint | Access |
| --- | --- | --- |
| GET | `/api/Product` | Public |
| GET | `/api/Product/{id}` | Public |
| POST | `/api/Product` | Admin |
| PUT | `/api/Product/{id}` | Admin |
| DELETE | `/api/Product/{id}` | Admin |

**Authorization Rules:**

- All product GET endpoints are public.
- Creating, updating, and deleting products require the Admin role.

### Category Endpoints

| Method | Endpoint | Access |
| --- | --- | --- |
| GET | `/api/Category` | Public |
| POST | `/api/Category` | Admin |
| PUT | `/api/Category/{id}` | Admin |
| DELETE | `/api/Category/{id}` | Admin |

**Authorization Rules:**

- Retrieving categories is publicly accessible.
- All category modification operations require the Admin role.

### Basket Endpoints

Base route: `/api/Basket`

| Method | Endpoint | Access |
| --- | --- | --- |
| GET | `/api/Basket/{basketid}` | Client |
| POST | `/api/Basket/{basketid}` | Client |
| PUT | `/api/Basket` | Client |
| DELETE | `/api/Basket/{basketId}/items/{productId}` | Client |
| DELETE | `/api/Basket/{basketId}` | Client |

**Authorization Rules:**

All basket endpoints require an authenticated user with the Client role.

### Order Endpoints

Base route: `/api/Order`

| Method | Endpoint | Access |
| --- | --- | --- |
| POST | `/api/Order` | Client |
| GET | `/api/Order` | Admin |
| GET | `/api/Order/{id}` | Admin |
| GET | `/api/Order/delivery-methods` | Client |

**Authorization Rules:**

- Creating an order requires the Client role.
- Retrieving orders requires the Admin role.
- Retrieving available delivery methods requires the Client role.

### Payment Endpoints

Base route: `/api/Payment`

| Method | Endpoint | Access | |

# CSCI6844 – Distributed E‑Commerce Backend + Blazor Frontend

This repository is the **final deliverable** for CSCI6844 – Programming for the Internet.  
It extends the second deliverable by adding a **Blazor WebAssembly frontend** that communicates
with the containerized microservices backend through the API Gateway.

---

## Architecture Overview
Browser → BlazorFrontend (port 5005)
↓ HTTP
ApiGateway (port 5000) ←── single entry point (Ocelot)
↙ ↓ ↘
CustomerSvc OrderSvc ProductSvc (ports 5001–5003, SQLite each)
↓ RabbitMQ events (OrderCreated / OrderCancelled)
NotificationSvc (port 5004, logs only)


---

## What This Project Includes

### Backend (Second Deliverable)
- 4 microservices, each in its own container with its own SQLite database:
  - `CustomerService` – CRUD for customers
  - `ProductService` – CRUD for products + stock management
  - `OrderService` – Create / cancel orders, publishes RabbitMQ events
  - `NotificationService` – Consumes events, writes logs
- 1 **API Gateway** using Ocelot (single entry point + aggregation)
- **RabbitMQ** for async `OrderCreated` / `OrderCancelled` events
- Database‑per‑service architecture (no shared databases)

### Frontend (Final Deliverable)
- **Blazor WebAssembly** SPA running on port `5005`
- Pages: Customers, Products, Orders, Order Details (aggregated view)
- Communicates with backend exclusively through the API Gateway on port `5000`

---

## Services and Ports

| Service             | Port (host) | Database     | Role                                        |
|---------------------|------------:|--------------|---------------------------------------------|
| BlazorFrontend      | 5005        | —            | Blazor WASM UI                              |
| ApiGateway          | 5000        | —            | Single entry point, routing, aggregation    |
| CustomerService     | 5001        | customers.db | CRUD for customers                          |
| OrderService        | 5002        | orders.db    | Create / cancel orders, publish events      |
| ProductService      | 5003        | products.db  | CRUD for products, stock updates            |
| NotificationService | 5004        | —            | Consumes events, writes logs                |
| RabbitMQ UI         | 15672       | N/A          | RabbitMQ management console                 |

---

## How to Run (Docker)

Make sure Docker Desktop is running, then:

```bash
git clone <repo-url>
cd CSCI6844FINAL
docker compose up --build
```

- First build may take a few minutes (NuGet packages restore inside containers).
- Wait until all services print `Now listening on: http://[::]:8080`.
- Open the frontend at **http://localhost:5005**

To stop:

```bash
docker compose down
```

---

## API Gateway & Swagger

| URL | Description |
|-----|-------------|
| `http://localhost:5005` | Blazor frontend |
| `http://localhost:5000` | API Gateway entry point |
| `http://localhost:5001/swagger` | CustomerService Swagger |
| `http://localhost:5002/swagger` | OrderService Swagger |
| `http://localhost:5003/swagger` | ProductService Swagger |
| `http://localhost:15672` | RabbitMQ UI (guest / guest) |

### Aggregated Endpoint

```http
GET http://localhost:5000/api/aggregate/order-details/{orderId}
```

Returns order + product + customer data combined in a single JSON response.

---

## Basic Test Flow (Happy Path)

1. **Create a customer**
   ```http
   POST http://localhost:5000/customers
   { "name": "Davy Han", "email": "davy@example.com" }
   ```

2. **Create a product**
   ```http
   POST http://localhost:5000/products
   { "name": "MacBook Pro", "description": "Apple Laptop", "price": 2999.99, "stock": 100 }
   ```

3. **Place an order** → `OrderCreated` event fires, stock decreases
   ```http
   POST http://localhost:5000/orders
   { "customerId": 1, "productId": 1, "quantity": 2 }
   ```

4. **Verify stock** → should be `98`
   ```http
   GET http://localhost:5000/products/1
   ```

5. **Cancel the order** → `OrderCancelled` event fires, stock restored
   ```http
   DELETE http://localhost:5000/orders/1
   ```

6. **Verify stock** → should be back to `100`
   ```http
   GET http://localhost:5000/products/1
   ```

7. **Check NotificationService logs**
   ```bash
   docker logs notificationservice
   ```

---

## Project Structure

```text
CSCI6844FINAL/
├── BlazorFrontend/
│   └── BlazorFrontend/           # Blazor WASM pages and services
│
├── ApiGateway/
│   └── ApiGateway.Api/           # Ocelot config, aggregation controller
│
├── CustomerService/
│   └── CustomerService.Api/
│       ├── Controllers/
│       ├── Data/                 # CustomerDbContext
│       └── DTOs/
│
├── ProductService/
│   └── ProductService.Api/
│       ├── Controllers/
│       ├── Consumers/            # RabbitMQ consumers
│       ├── Data/                 # ProductDbContext
│       └── DTOs/
│
├── OrderService/
│   └── OrderService.Api/
│       ├── Controllers/
│       ├── Messaging/            # RabbitMQ publisher
│       ├── Data/                 # OrderDbContext
│       └── DTOs/
│
├── NotificationService/
│   └── NotificationService.Api/
│       └── Consumers/            # RabbitMQ consumers, logging only
│
├── data/                         # SQLite volume mounts
│   ├── customers/
│   ├── products/
│   └── orders/
│
└── docker-compose.yml
```

---

## Notes and Issues

- **Service discovery inside Docker** – must use container service names (e.g. `orderservice`, `productservice`) instead of `localhost`.
- **Database initialization** – created data directories in code before calling `Database.EnsureCreated()` to avoid first-run failures.
- **Async messaging debugging** – NotificationService logs confirmed `OrderCreated` / `OrderCancelled` events were correctly published and consumed.
- **Blazor CORS** – configured the API Gateway to allow cross-origin requests from the Blazor frontend container.

---

*Submitted as the **final deliverable** for CSCI6844 – Programming for the Internet.*
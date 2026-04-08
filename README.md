# CSCI6844 Second Deliverable – Distributed E‑Commerce Backend Extension

This repository contains the extended version of my CSCI6844 midterm project.  
It is a containerized microservices backend for a simple e‑commerce scenario, built with ASP.NET Core, EF Core, Docker Compose, RabbitMQ, and an API Gateway.

## What This Project Includes

- 4 core microservices, each in its own container and with its own database:
  - CustomerService
  - ProductService
  - OrderService
  - NotificationService
- 1 API Gateway service using Ocelot as a single entry point
- RabbitMQ for asynchronous messaging between services
- Database‑per‑service architecture (no shared databases)

Communication patterns:

- HTTP (synchronous) between the API Gateway and internal services  
- RabbitMQ (asynchronous) for `OrderCreated` and `OrderCancelled` events

## Services and Ports

| Service            | Port (host) | Database        | Role                                      |
|--------------------|------------:|-----------------|-------------------------------------------|
| ApiGateway         | 5000        | none            | Single entry point, routing, aggregation  |
| CustomerService    | 5001        | customers.db    | CRUD for customers                        |
| OrderService       | 5002        | orders.db       | Create / cancel orders, publish events    |
| ProductService     | 5003        | products.db     | CRUD for products, stock updates          |
| NotificationService| 5004        | none            | Consumes events, writes logs              |
| RabbitMQ UI        | 15672       | N/A             | RabbitMQ management console               |

All application services are reachable only through the API Gateway in a typical client scenario.

## How to Run (Docker)

Make sure Docker Desktop is running, then:

```bash
git clone <repo-url>
cd CSCI6844-DistributedDataAccessLab
docker compose up --build
```

- The first build may take a few minutes because NuGet packages are restored inside the containers.
- Wait until all services print a line similar to  
  `Now listening on: http://[::]:8080`.

To stop the system:

```bash
docker compose down
```

## API Gateway & Swagger

The API Gateway listens on port **5000**. Some useful URLs:

- API Gateway:
  - `http://localhost:5000` (public entry point)
- Swagger (internal service UIs, if exposed):
  - `http://localhost:5001/swagger` – CustomerService
  - `http://localhost:5002/swagger` – OrderService
  - `http://localhost:5003/swagger` – ProductService
- RabbitMQ UI:
  - `http://localhost:15672` (default user/pass: `guest` / `guest`)

### Aggregated Endpoint

The gateway provides an aggregated endpoint that returns order, product, and customer data in one call:

```http
GET http://localhost:5000/api/aggregate/order-details/{orderId}
```

The gateway internally calls the Order, Product, and Customer services and combines their DTO responses into a single JSON document.

## Basic Test Flow (Happy Path)

After the system is up:

1. **Create a customer**

   ```http
   POST http://localhost:5000/customers
   Content-Type: application/json

   {
     "name": "Davy Han",
     "email": "davy@example.com"
   }
   ```

2. **Create a product**

   ```http
   POST http://localhost:5000/products
   Content-Type: application/json

   {
     "name": "MacBook Pro",
     "description": "Apple Laptop",
     "price": 2999.99,
     "stock": 100
   }
   ```

3. **Place an order (OrderCreated event, stock decrease)**

   ```http
   POST http://localhost:5000/orders
   Content-Type: application/json

   {
     "customerId": 1,
     "productId": 1,
     "quantity": 2
   }
   ```

4. **Check product stock has decreased**

   ```http
   GET http://localhost:5000/products/1
   ```

   Stock should be `98` if everything worked.

5. **Cancel the order (OrderCancelled event, stock increase)**

   ```http
   DELETE http://localhost:5000/orders/1
   ```

6. **Check product stock is restored**

   ```http
   GET http://localhost:5000/products/1
   ```

   Stock should be back to `100`.

7. **Check NotificationService logs**

   ```bash
   docker logs notificationservice
   ```

   You should see log lines for both order creation and order cancellation.

## Project Structure (High Level)

```text
CSCI6844-DistributedDataAccessLab/
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
│       ├── Consumers/            # RabbitMQ consumers for events
│       ├── Data/                 # ProductDbContext
│       └── DTOs/
│
├── OrderService/
│   └── OrderService.Api/
│       ├── Controllers/
│       ├── Services/             # HTTP clients, event publisher
│       ├── Messaging/            # RabbitMQ publisher
│       ├── Data/                 # OrderDbContext
│       └── DTOs/
│
├── NotificationService/
│   └── NotificationService.Api/
│       └── Consumers/            # RabbitMQ consumers, logging only
│
├── data/                         # SQLite files (volume mounts)
│   ├── customers/
│   ├── products/
│   └── orders/
│
└── docker-compose.yml
```

## Notes and Issues I Hit

- **Service discovery inside Docker**  
  At first I used `localhost` between services.  
  Inside Docker, I had to use the service names like `orderservice` or `productservice` instead.

- **Database initialization**  
  Some containers failed on first run because the data directories did not exist.  
  I fixed this by creating directories in code before calling `Database.EnsureCreated()`.

- **Asynchronous messaging debugging**  
  It was not always clear if events were published or consumed correctly.  
  The NotificationService helped a lot, because its logs show when `OrderCreated` and `OrderCancelled` messages are handled.

---

This project is submitted as the **second deliverable** for CSCI6844 – Programming for the Internet.
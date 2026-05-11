# BorroDesk

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green)

BorroDesk is an internal IT ticket management system built with **ASP.NET Core**, **Angular**, and **SQL Server**.

The application allows employees to report IT issues, while support agents and administrators can manage, assign, comment on, and resolve tickets.

This project was built as a portfolio project to demonstrate full-stack development with .NET, Angular, SQL Server, authentication, authorization, testing, Docker, and deployment.

---

## Live Demo

Frontend: https://borrodesk-ui.onrender.com  
Backend: https://borrodesk.onrender.com

---

## Features

- User authentication with JWT
- Role-based authorization
  - Employee
  - Support
  - Admin
- Create, edit, view, and delete tickets
- Ticket statuses:
  - Open
  - In Progress
  - Resolved
- Ticket priorities:
  - Low
  - Medium
  - High
- Comments per ticket
- Screenshot/file uploads for tickets
- Search, filtering, and pagination
- Ticket assignment for support/admin users
- Admin user management
- Dashboard with ticket overview
- Backend integration tests
- Docker-based local development
- Render deployment

---

## Tech Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10.0.6
- ASP.NET Core Identity
- JWT Authentication
- SQL Server
- xUnit

### Frontend

- Angular 21
- TypeScript
- Angular Router
- Angular HTTP Client
- Reactive Forms

### DevOps

- Docker
- Docker Compose
- Render

---

## Architecture

```text
┌────────────────────────┐
│   Angular Frontend     │
│   borrodesk-ui         │
└───────────┬────────────┘
            │
            │ HTTP / REST
            ▼
┌────────────────────────┐
│   ASP.NET Core API     │
│   BorroDesk.Api        │
└───────────┬────────────┘
            │
            │ Entity Framework Core
            ▼
┌────────────────────────┐
│      SQL Server        │
│      BorroDesk DB      │
└────────────────────────┘
```

### Local Docker Architecture

```text
Browser
  │
  ├── http://localhost:8081
  │       │
  │       ▼
  │   Angular Frontend Container
  │
  └── http://localhost:8080
          │
          ▼
      ASP.NET Core API Container
          │
          ▼
      SQL Server Container
      localhost:1433
```

---

## Screenshots

### Dashboard

![Dashboard](docs/screenshots/dashboard.png)

### Ticket List

![Ticket List](docs/screenshots/ticket-list.png)

### Ticket Details

![Ticket Details](docs/screenshots/ticket-details.png)

### Admin User Management

![Admin User Management](docs/screenshots/admin-users.png)

---

## Project Structure

```text
BorroDesk/
├── backend/
│   ├── BorroDesk.Api/
│   └── BorroDesk.Api.Tests/
├── frontend/
│   └── borrodesk-ui/
├── docs/
│   └── screenshots/
├── docker-compose.yml
└── README.md
```

---

## Getting Started

### Prerequisites

Make sure the following tools are installed:

- .NET 10 SDK
- Node.js
- npm
- SQL Server
- Angular CLI
- Docker
- Docker Compose

---

## Run with Docker

From the repository root, run:

```bash
docker compose up
```

This starts the following services:

```text
Frontend: http://localhost:8081
Backend:  http://localhost:8080
SQL:      localhost:1433
```

To stop the containers:

```bash
docker compose down
```

To stop the containers and remove the database volume:

```bash
docker compose down -v
```

---

## Docker Services

The Docker setup contains three services:

```text
sqlserver
api
frontend
```

### SQL Server

```text
Container: borrodesk-sqlserver
Image:     mcr.microsoft.com/mssql/server:2022-latest
Port:      1433
Database:  BorroDesk
```

### API

```text
Container: borrodesk-api
Port:      8080
URL:       http://localhost:8080
```

### Frontend

```text
Container: borrodesk-frontend
Port:      8081
URL:       http://localhost:8081
```

---

## Backend Setup Without Docker

Go to the backend project:

```bash
cd backend/BorroDesk.Api
```

Restore dependencies:

```bash
dotnet restore
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the backend:

```bash
dotnet run
```

The API should be available at:

```text
https://localhost:7047
```

---

## Frontend Setup Without Docker

Go to the frontend project:

```bash
cd frontend/borrodesk-ui
```

Install dependencies:

```bash
npm install
```

Run the Angular application:

```bash
npm start
```

The frontend should be available at:

```text
http://localhost:4200
```

---

## Demo Accounts

The application can be tested with seeded demo users:

```text
Admin
Email: admin@borrodesk.local
Password: Admin123!

Support
Email: support@borrodesk.local
Password: Support123!

Employee
Email: user@borrodesk.local
Password: User123!
```

---

## Running Tests

Backend tests can be executed with:

```bash
dotnet test
```

The test project includes integration tests for:

- Authentication
- Authorization
- Ticket permissions
- Comments
- Uploads
- Admin functionality

---

## Main User Roles

### Employee

Employees can:

- Create tickets
- View their own tickets
- Comment on their own tickets
- Upload screenshots
- Delete allowed tickets

### Support

Support users can:

- View tickets assigned to them
- Update ticket statuses
- Add comments
- Assign tickets where allowed
- Work on support queues

### Admin

Admins can:

- View and manage all tickets
- Manage users
- Assign roles
- Activate or deactivate users
- Reset user passwords

---

## API Overview

Main backend areas:

```text
/api/auth
/api/tickets
/api/tickets/{id}/comments
/api/tickets/{id}/attachments
/api/admin/users
```

---

## Deployment

The project is deployed on Render.

Frontend: https://borrodesk-ui.onrender.com  
Backend: https://borrodesk.onrender.com

The frontend and backend are deployed as separate services.

---

## Environment Variables

The API uses environment variables for production configuration.

Important variables include:

```text
ASPNETCORE_ENVIRONMENT
ASPNETCORE_URLS
ConnectionStrings__DefaultConnection
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
Cors__AllowedOrigins__0
```

For local Docker development, these values are configured in `docker-compose.yml`.

---

## Security Notes

- Authentication is handled with JWT.
- Authorization is role-based.
- Demo accounts are intended for development and portfolio review only.
- Production secrets should be configured through environment variables.
- Local Docker passwords and JWT secrets should not be reused in production.

---

## Development Notes

This project focuses on realistic business application features.

Implemented concepts include:

- Clean separation between controllers, services, DTOs, and entities
- Entity Framework Core migrations
- Role-based business rules
- Server-side validation
- File upload validation
- Integration testing
- Angular routing and guards
- API communication through Angular services
- Docker-based local development
- Deployment preparation for Render

---

## License

This project is licensed under the MIT License.

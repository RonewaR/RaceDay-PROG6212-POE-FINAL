# RaceDay Application

## 1. Project Overview

RaceDay is a race event management application developed using C# and ASP.NET Core. It supports race organisers and participants through event management, registration, enrolments, results and profile management.

The project was developed across three parts, covering system planning, API development, automated testing and MVC web application development.

## 2. Part 1: System Planning and Database Design

Part 1 focused on planning the RaceDay system and preparing the database design.

Deliverables included:
- RaceDay Entity Relationship Diagram (ERD).
- SQL database creation and data script.
- API Endpoint Plan.
- Database structure and relationships.

## 3. Part 2: API Development and Testing

Part 2 extended the project with an ASP.NET Core Web API.

### Main Components
- User registration and login.
- JWT-based authentication and role-based authorisation.
- Database integration using Entity Framework Core.
- Event management.
- Category management.
- Participant enrolments.
- Race results and payment operations.
- Automated API tests.

### Technologies Used
- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Visual Studio 2022
- .NET 8
- GitHub and GitHub Actions

## 4. Part 3: MVC Web Application

Part 3 introduced an ASP.NET Core MVC website connected to the RaceDay API.

### Main Features

**Authentication and profiles**
- Participant and organiser login.
- Role-based dashboards.
- Profile viewing and updating.
- Participant profile picture uploads.

**Organiser features**
- Organiser dashboard with event and enrolment statistics.
- Create, view, edit and delete events.
- Upload and replace event banner images.
- Manage event categories.
- View and manage participant enrolments.
- Record race results.

**Participant features**
- Participant dashboard.
- Browse and search available events.
- Filter events by location.
- View event banners and categories.
- Enrol in race categories.
- View personal enrolments.
- View personal race results.
- View and update profile information.

### Azure Blob Storage

Azure Blob Storage is used to store profile pictures and event banner images. The application validates uploaded image types and file sizes before uploading.

## 5. Database

The application uses Microsoft SQL Server with Entity Framework Core.

The database is named `RaceDayDB` and contains tables for:
- Users
- Events
- Categories
- Enrolments
- Results
- Payments

The database connection is configured separately from the source code. Sensitive credentials and application secrets should be stored securely using Visual Studio User Secrets or environment variables.

## 6. Automated Testing

The automated test project is `RaceDay.API.Tests`.

The tests were executed using Visual Studio 2022. The successful local test run reported six passing tests.

## 7. GitHub Actions and CI/CD

GitHub Actions is configured to validate the RaceDay repository automatically when changes are pushed or pull requests are created.

The workflow:
1. Checks for required assignment files.
2. Sets up .NET 8.
3. Restores project dependencies.
4. Builds the solution.
5. Runs automated tests.
6. Reports successful validation when all steps pass.

The latest GitHub Actions workflow completed successfully.

## 8. Docker

A Dockerfile was created for the RaceDay API using the .NET 8 SDK and ASP.NET runtime images.

The Docker image was successfully built, and the container started and listened on port 8080.

**Current limitation:** Database-backed API operations inside the container still require SQL Server network connectivity to be configured. The container therefore starts successfully, but full database connectivity from Docker has not yet been verified.

## 9. Repository Structure

- `.github/workflows/` — GitHub Actions workflow.
- `RaceDay .API/` — API solution and source code.
- `RaceDay.API.Tests/` — Automated test project.
- `RaceDay.MVC/` — MVC web application.
- `Raceday.sql` — Database script.
- `Raceday erd.jpg` — Entity Relationship Diagram.
- `API endpoint plan.pdf` — API planning document.
- `Dockerfile` — Docker configuration for the API.

## 10. Conclusion

The RaceDay project demonstrates the development of a race event management system through database planning, API development, authentication, automated testing and an MVC web application.

The application provides separate functionality for organisers and participants, while GitHub Actions supports automated build and test validation. Docker was also used to build and run a containerised version of the API.

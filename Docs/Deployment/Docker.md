---
layout: default
title: Docker Deployment
---

# Docker Deployment

## Overview

The application includes Docker support for consistent development and deployment environments.

## Docker Compose Development

### Quick Start

```bash
# Build and run the application
docker-compose up --build

# Run in detached mode
docker-compose up -d --build

# Stop the application
docker-compose down
```

### Configuration

The `docker-compose.yml` file includes:

- ASP.NET Core application container
- SQLite database volume mounting
- Environment variable configuration
- Port mapping (5000:80)

### Environment Variables

Set the following environment variables in your `.env` file or docker-compose.yml:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - Authentication__Google__ClientId=your-client-id
  - Authentication__Google__ClientSecret=your-client-secret
```

## Production Docker Deployment

*Production deployment instructions will be added when the application is ready for production.*

## Troubleshooting

*Common Docker issues and solutions will be documented as they are encountered.*
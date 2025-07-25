# Secure Message Processor Solution

## 🏗️ Architecture Overview

This solution implements a secure message processing system with the following components:

- **Manager.Api**: Health check and management service
- **Dispatcher**: Message distribution and processor management
- **Processor**: Message processing service
- **Core**: Business logic, patterns, and utilities
- **Shared**: Common models and interfaces
- **Tests**: Unit and integration tests

## 🎯 Key Patterns Implemented

### 1. Proxy Pattern
- Service proxy with retry and circuit breaker logic
- Transparent handling of remote service calls
- Automatic failure handling and recovery

### 2. Circuit Breaker Pattern
- Prevents cascading failures
- Automatic state management (Closed, Open, Half-Open)
- Configurable failure thresholds and retry periods

### 3. Event-Driven Architecture
- gRPC bidirectional streaming for real-time communication
- Asynchronous message processing
- Decoupled service components

## 🚀 Getting Started

### Prerequisites
- .NET 6.0 SDK
- Visual Studio or VS Code

### Running the Solution

1. **Start Manager Service**
   ```bash
   cd SecureProcessor.Manager.Api
   dotnet run

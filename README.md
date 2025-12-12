# Wordle Game - Distributed Architecture

A distributed implementation of the popular Wordle word-guessing game, featuring a multi-tier service-oriented architecture using WCF and gRPC technologies.

Overview

This project implements a distributed Wordle game where players have 6 attempts to guess a daily 5-letter word. The system uses a three-tier architecture with separate services for word management, game logic, and client interaction.

## Architecture

The application consists of three main components:

```
Client (gRPC) ←→ WordleGameServer (gRPC) ←→ WordServer (WCF)
```

### WordServer (.NET Framework 4.8 - WCF Service)
- Provides daily word generation
- Validates player guesses against word list
- Manages word persistence
- Exposes WCF endpoints using SOAP/HTTP

### WordleGameServer (.NET Core 8.0 - gRPC Service)
- Implements game logic and rules
- Manages player sessions
- Tracks game statistics
- Provides bi-directional streaming for real-time gameplay
- Acts as WCF client to WordServer

### Client (.NET Core 8.0 - gRPC Client)
- Console-based user interface
- Communicates with WordleGameServer via gRPC
- Displays game results and statistics

## Technologies Used

### Backend
- **C#** - Primary programming language
- **.NET Framework 4.8** - WordServer platform
- **.NET Core 8.0** - WordleGameServer and Client platform
- **WCF (Windows Communication Foundation)** - Word service communication
- **gRPC** - Game server communication
- **Protocol Buffers** - Data serialization for gRPC

### Libraries & Frameworks
- **Newtonsoft.Json** - JSON serialization for data persistence
- **System.ServiceModel** - WCF client/server implementation
- **Grpc.AspNetCore** - gRPC server hosting
- **Grpc.Net.Client** - gRPC client implementation

### Concurrency & Persistence
- **Mutex** - Thread-safe statistics management
- **JSON Files** - Data persistence for words and statistics

## Installation

### Clone the Repository
```bash
git clone <repository-url>
cd Project2_Wordle
```

## Running the Application

**Important:** Start services in this exact order:

### Step 1: Start WordServer (WCF Service)
```bash
cd WordServer
# Run from Visual Studio or
dotnet run  # (if converted to .NET Core hosting)
```

Expected output:
```
The DailyWord service is ready at http://localhost:8080/DailyWordService
WSDL available at http://localhost:8080/DailyWordService?wsdl
Press <Enter> to stop the service.
```

### Step 2: Start WordleGameServer (gRPC Service)
```bash
cd WordleGameServer
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7245
```

### Step 3: Run Client
```bash
cd Project2_Wordle
dotnet run
```

## Project Structure

```
Project2_Wordle/
├── WordServer/                    # WCF Service (.NET Framework 4.8)
│   ├── Services/
│   │   ├── IDailyWordService.cs   # WCF Service Contract
│   │   └── DailyWordService.cs    # Service Implementation
│   ├── Program.cs                 # WCF Service Host
│   └── App.config                 # WCF Configuration
│
├── WordleGameServer/              # gRPC Service (.NET Core 8.0)
│   ├── Services/
│   │   └── DailyWordleService.cs  # Game Logic Implementation
│   ├── Protos/
│   │   └── dailywordle.proto      # gRPC Service Definition
│   └── Program.cs                 # gRPC Service Host
│
├── Project2_Wordle/               # Client Application (.NET Core 8.0)
│   └── Program.cs                 # Console Client
│
└── Data/                          # Shared Data Directory
    ├── wordle.json                # Valid word list
    ├── daily_word.json            # Current daily word
    └── user_stats.json            # Game statistics
```

## Game Rules

1. **Objective:** Guess the 5-letter word of the day in 6 attempts or less
2. **Valid Guesses:** Must be a valid 5-letter word from the word list
3. **Feedback System:**
   - `*` - Letter is correct and in the correct position
   - `?` - Letter is in the word but in the wrong position
   - `x` - Letter is not in the word
4. **Daily Word:** A new word is randomly selected each day
5. **Statistics:** Track total players, winners, and average guesses


### WordServer Endpoints
- **HTTP:** `http://localhost:8080/DailyWordService`
- **WSDL:** `http://localhost:8080/DailyWordService?wsdl`

### WordleGameServer Endpoints
- **gRPC:** `https://localhost:7245`

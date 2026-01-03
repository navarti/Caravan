# CaravanOnline

CaravanOnline is a web application project aimed at creating an online version of the Caravan card game inspired by the game from Fallout: New Vegas. This project is built using ASP.NET Core for the backend and Razor Pages for the frontend.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Running the Application](#running-the-application)
- [Gameplay](#gameplay)
- [Project Structure](#project-structure)

## Features
- Two-player online card game.
- Players can select and play cards sequentially.
- Game lanes display cards played by each player.
- Game state is managed using sessions to ensure continuity between moves.
- Evaluates the game and declares a winner based on the rules.

## Installation
To set up the project locally, follow these steps:

### Prerequisites
- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [Visual Studio 2019 or later](https://visualstudio.microsoft.com/) with ASP.NET and web development workload installed.

### Clone the Repository
```bash
Clone the project repository to your local machine using:
git clone https://github.com/yourusername/CaravanOnline.git
cd CaravanOnline
```

### Restore Dependencies
```bash
Restore the project dependencies using the .NET CLI:
dotnet restore
```

## Running the Application
To run the application locally, use the following steps:

### Using Visual Studio
```bash
1. Open the `CaravanOnline` solution in Visual Studio.
2. Set `CaravanOnline` as the startup project.
3. Press `F5` to run the application.
```

### Using the .NET CLI
```bash
1. Navigate to the project directory.
2. Run the application using the following command:
dotnet run
3. Open a web browser and navigate to `https://localhost:5001`.
```

## Gameplay
1. Each player is dealt a hand of cards.
2. Players take turns to play a card by selecting it from their hand and placing it on one of their lanes.
3. The game progresses with players alternating turns and placing cards until all lanes are filled.
4. The game evaluates the lanes and determines the winner based on predefined rules.

## Project Structure
The project follows a standard ASP.NET Core structure:
```bash
CaravanOnline/
│
├── Pages/
│ ├── Index.cshtml
│ ├── Index.cshtml.cs 
│ └── ...
│
├── Models/
│ ├── Card.cs # Card Object
│ └── ...
│
├── Services/
│ ├── CardManager.cs # Logic for managing cards
│ ├── LaneManager.cs # Logic for managing lanes
│ └── ...
│
├── wwwroot/ 
│
├── appsettings.json 
├── Program.cs 
└── CaravanOnline.csproj 
```

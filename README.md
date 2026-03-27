[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/hZIAsDPT)
# CSCI 1260 — Project

## Project Instructions
All project requirements, grading criteria, and submission details are provided on **D2L**.  
Refer to D2L as the *authoritative source* for this assignment.

This repository is intentionally minimal. You are responsible for:
- Creating the solution and projects
- Designing the class structure
- Implementing the required functionality

---

## Getting Started (CLI)

You may use **Visual Studio**, **VS Code**, or the **terminal**.

### Create a solution
```bash
dotnet new sln -n ProjectName
```

### Create a project (example: console app)
```bash
dotnet new console -n ProjectName.App
```

### Add the project to the solution
```bash
dotnet sln add ProjectName.App
```

### Build and run
```bash
dotnet build
dotnet run --project ProjectName.App
```

## Notes
- Commit early and commit often.
- Your repository history is part of your submission.
- Update this README with build/run instructions specific to your project.


The game supports 2–4 players, and the player count can be provided in two ways. 

1.
dotnet run -- 3

2.
Enter number of players (2–4):

When the game starts, you choose from the menu:

1.Automatic
2.Manual

After the game ends, the console asks if the user wants to play again.


This project was submitted through GitHub Classroom.
My repository link:
https://github.com/etsucs-scott/project-2-whitele3

The repository includes:

WarGame.Core
WarGame.Console
UML diagram (PDF)
README.md (this file)



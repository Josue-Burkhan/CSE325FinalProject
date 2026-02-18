# A Better Me - Skill Tracker

"A Better Me" is a comprehensive web application designed to help users track their skills, set goals, and monitor their progress. Built with .NET Blazor Server, it offers a rich, interactive user experience.

## ✨ Features

-   **Skill Management**: Create, track, and categorize skills with custom icons and colors.
-   **Goal Setting**: Break down skills into manageable goals and milestones.
-   **AI Integration**: Leverage Gemini AI to automatically generate personalized learning plans and goals.
-   **Progress Tracking**: Log your daily practice hours and view detailed analytics.
-   **Reports & Charts**: Visualize your weekly activity and overall mastery with interactive charts.
-   **Public Profiles**: Share your public portfolio of skills via a unique URL (`/u/{id}`).
-   **Profile Customization**: Edit your profile, upload an avatar, and choose between Light/Dark themes.

## 🛠️ Technology Stack

-   **Framework**: .NET 10 (Blazor Server)
-   **Database**: MySQL (Entity Framework Core)
-   **Styling**: Tailwind CSS + Custom CSS
-   **Authentication**: Custom Cookie-based Authentication
-   **AI**: Google Gemini API

## 🚀 Getting Started

### Prerequisites

-   .NET 8.0 SDK (or later)
-   MySQL Server
-   Gemini API Key

### Installation

1.  **Clone the repository**
    ```bash
    git clone https://github.com/your-username/a-better-me.git
    cd a-better-me
    ```

2.  **Configure Database**
    Update `appsettings.json` with your MySQL connection string and Gemini API Key:
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=skilltracker;User=root;Password=yourpassword;"
      },
      "Gemini": {
        "ApiKey": "###########"
      }
    }
    ```

3.  **Run Migrations (Optional)**
    The application includes a `DbInitializer` that runs on startup. Ensure your MySQL server is running.

4.  **Run the Application**
    ```bash
    dotnet run
    ```

5.  **Access the App**
    Open your browser and navigate to: `http://localhost:5180` (or the port shown in your terminal).

## 📖 Usage Guide

-   **Dashboard**: Overview of your active skills and recent progress.
-   **My Skills**: Manage your skills. Click "Add Skill" to start. Use the "AI Plan" button to let AI structure your learning.
-   **Reports**: View your "Weekly Activity" chart (hours logged per week).
-   **Profile**: View your stats. Toggle "Edit Profile" to update your bio or upload a photo.
-   **Public Profile**: Share your profile link with others to show off your public skills.

## 🤝 Contributing

1.  Fork the project
2.  Create your feature branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

## Testing
To run the automated tests, navigate to the project root and run:
```bash
dotnet test
```
The project includes unit tests for core services using xUnit and an in-memory database to ensure logic correctness without affecting the live database.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

This project is created for educational purposes. If you use this code in your own projects or for commercial purposes, please provide attribution to the original author.

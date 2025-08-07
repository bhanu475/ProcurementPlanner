
# Procurement Planner

## 📌 Overview

**Procurement Planner** is a full-stack enterprise-grade application built to optimize and automate procurement workflows within an organization. It supports demand forecasting, requisition approvals, purchase order management, and supplier coordination—ensuring efficient and cost-effective procurement.

---

## 🎯 Objectives

- Automate and streamline the entire procurement lifecycle
- Improve planning accuracy with historical data and forecasts
- Enhance visibility, control, and compliance in procurement operations
- Facilitate collaboration between departments and suppliers

---

## ✨ Key Features

- 🔮 **Demand Forecasting**: Uses consumption trends to predict future procurement needs  
- 📝 **Purchase Requisition Management**: Internal users can submit and track requests  
- 📦 **Automated Purchase Orders**: Create POs based on stock levels and requisitions  
- 🤝 **Supplier Management**: Maintain vendor records, pricing, and performance data  
- ✅ **Approval Workflow**: Customizable multi-level approvals for requisitions and POs  
- 🏷️ **Inventory Integration**: Real-time synchronization with inventory databases  
- 📊 **Analytics & Reporting**: Insights into spending, vendor performance, and KPIs  

---

## 🛠️ Tech Stack

| Layer         | Technology                          |
|---------------|--------------------------------------|
| Frontend      | React (with TypeScript or JavaScript) |
| Backend       | ASP.NET Core 8 (C#)                  |
| Database      | PostgreSQL / SQL Server             |
| API           | RESTful APIs                        |
| Authentication| ASP.NET Identity / JWT              |
| Deployment    | Docker / Azure / IIS                |

---

## 🚀 Benefits

- Increased procurement speed and accuracy  
- Reduced manual errors and costs  
- Greater visibility and traceability of purchases  
- Improved compliance with procurement policies  
- Scalable architecture for future enhancements  

---

## 📈 Use Cases

- 🏭 **Manufacturing**: Automate raw material procurement based on production demand  
- 🛒 **Retail**: Manage supplier orders and inventory restocking efficiently  
- 🏢 **Enterprises**: Standardize procurement across departments  
- 🏛️ **Public Sector**: Ensure transparency and control over purchasing processes  

---

## 📁 Project Structure

```

/procurement-planner/
├── frontend/               # React application
│   ├── src/
│   └── public/
├── backend/                # ASP.NET Core 8 Web API
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── Data/
├── database/               # SQL scripts / migrations
├── docs/                   # Diagrams and documentation
├── tests/                  # Unit and integration tests
└── README.md

````

---

## 🧪 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Node.js](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) or [SQL Server](https://www.microsoft.com/en-us/sql-server)
- Git
- Docker (optional)

### Clone the Repository

```bash
git clone https://github.com/bhanu475/procurement-planner.git
cd procurement-planner
````

---

## 🧰 Running Locally

### Backend (.NET 8)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

### Frontend (React)

```bash
cd frontend
npm install
npm start
```

---

## ⚙️ Configuration

* Update database connection string in `appsettings.json`
* Configure environment variables as needed for:

  * JWT authentication
  * SMTP (for email notifications)
  * Database provider (PostgreSQL or SQL Server)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🙌 Contributing

Contributions are welcome! Please fork the repo and submit a pull request.
For major changes, open an issue first to discuss what you would like to change.

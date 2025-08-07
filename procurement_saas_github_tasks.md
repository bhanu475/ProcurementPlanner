**GitHub Project: Procurement Planning SaaS**

**Tech Stack**: .NET Core (Backend), Angular (Frontend), SQL Server, JWT Auth

---

## **Project Board Structure**

### Columns:
1. Backlog
2. To Do
3. In Progress
4. Code Review
5. Testing
6. Done

---

## **EPIC: User Management & Roles**

### Task 1: Implement User Registration API
- **Input**: Username, Email, Password
- **Output**: JWT Token, Success Message
- **System Considerations**:
  - Password hashing (ASP.NET Identity)
  - Email uniqueness constraint
  - Send confirmation email (if SMTP configured)
- **Acceptance**: Valid users created in DB, JWT returned

### Task 2: Angular User Registration Page
- **Input**: User form fields
- **Output**: API call to backend, redirect to login
- **UI/UX**: Angular Material form, form validation

### Task 3: Implement JWT-based Login API
- **Input**: Email, Password
- **Output**: JWT Token
- **Considerations**:
  - Token expiry settings
  - Store user roles in claims

### Task 4: Angular Login Page + Token Storage
- **Input**: Login Form
- **Output**: Redirect on success, error toast on fail
- **Considerations**: Use Angular interceptor to add token to API requests

### Task 5: Role Management Endpoints
- **Input**: Role assignment API (userId, role)
- **Output**: 200 OK
- **System**: Admin only access

### Task 6: Role Guards on Angular Routes
- **Input**: Role from JWT
- **Output**: Redirect if role mismatch
- **Behavior**: Unauthorized users redirected to login/403

---

## **EPIC: Vendor Management**

### Task 1: Vendor CRUD API
- **Input**: Name, Address, Contact, Category
- **Output**: 200 OK or Validation Errors
- **System**: Soft delete, searchable endpoint

### Task 2: Angular Vendor Management Pages
- **Input**: Table View, Forms
- **Output**: Vendor table + edit modal
- **Components**: List, Create/Edit, View, Delete

---

## **EPIC: Product Catalog / Item Master**

### Task 1: Item CRUD API
- **Fields**: SKU, Name, UOM, LeadTime, MinQty, VendorList
- **Validation**: SKU uniqueness, vendor references

### Task 2: Angular Item Catalog
- **Behavior**: Select multiple vendors per item
- **UI**: Multi-select dropdowns, inline editing for lead time, MOQ

---

## **EPIC: Purchase Requisitions (PR)**

### Task 1: Create PR API
- **Input**: Item list, quantities, required date, justification
- **Output**: PR ID, status = Draft
- **Behavior**: Items validated for SKU and UOM, calculate total

### Task 2: Angular PR Form
- **Input**: Table with item dropdown, qty
- **Output**: Draft saved on form
- **Edge Case**: Duplicate item entries should be merged

### Task 3: PR Submission Endpoint
- **Input**: PR ID
- **Output**: PR status = Submitted
- **Behavior**: Trigger approval workflow

### Task 4: PR Approval Workflow Engine
- **Input**: PR ID
- **System**: Multi-step approval logic based on thresholds
- **Output**: PR status update, audit entry

### Task 5: Angular PR Approval Page
- **Behavior**: Approver view with comments, approve/reject buttons

---

## **EPIC: Purchase Orders (PO)**

### Task 1: Generate PO from Approved PR
- **Input**: PR ID
- **Output**: PO object with vendor info
- **Behavior**: Auto-assign vendor and copy item list

### Task 2: PO PDF Generation
- **Input**: PO ID
- **Output**: PDF download
- **System**: Use server-side PDF generator

### Task 3: Angular PO List View
- **View**: Status tags (Draft, Sent, Closed)
- **Actions**: Download PDF, Send to Vendor

---

## **EPIC: Approval Workflows**

### Task 1: Approval Rule Setup
- **Input**: Role levels, threshold config
- **System**: Store in settings table

### Task 2: Approve/Reject API
- **Input**: Entity (PR or PO), Action, Comments
- **Output**: Updated status, audit record

---

## **EPIC: Budgeting & Forecasting**

### Task 1: Budget Setup API
- **Input**: Dept, Item, Monthly limit
- **Output**: Budget table entries

### Task 2: Forecast Algorithm (Backend Job)
- **Input**: Last 6 months of consumption
- **Output**: Suggested Qty/Cost
- **System**: CRON job to update forecasts

---

## **EPIC: Notifications & Alerts**

### Task 1: Email Notification on PR Submission
- **Input**: PR ID
- **System**: SMTP/SendGrid

### Task 2: In-App Notification Bell
- **Angular**: Realtime updates via SignalR

---

## **EPIC: Reports & Analytics**

### Task 1: Spend Report API
- **Filter**: By vendor, month
- **Output**: JSON + export CSV

### Task 2: Angular Reports Dashboard
- **Chart.js**: Total spend, PO count, Vendor metrics

---

## **EPIC: Audit Logs**

### Task 1: Global Action Logger
- **System**: Middleware logs all updates with userId, before/after snapshot

### Task 2: Angular Audit Log Viewer
- **Access**: Admin only

---

## **EPIC: Settings & Configuration**

### Task 1: Master Data Management (UOM, Category)
- **CRUD**: Basic tables with references in Items

---

## **EPIC: API Integrations**

### Task 1: ERP Push PO API
- **Input**: PO JSON
- **Output**: Status from ERP
- **System**: OAuth-secured external call

### Task 2: Webhooks for PR/PO Events
- **System**: Event bus emits webhook to configured URLs

---

Let me know if you’d like this exported to GitHub Issues or formatted into a spreadsheet for importing into project management tools.


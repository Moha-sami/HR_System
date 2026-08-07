# Buy2 HRMS - Domain Context & Glossary

Canonical domain terminology and ubiquitous language governing the Buy2 HR Management System.

---

## Glossary

### 1. Identity & Access Management (IAM)
- **Role**: A named collection of CRUDS (Create, Read, Update, Delete, Suspend) permission toggles assigned to employees.
- **Bound Role**: A role actively assigned to one or more employees. Bound roles cannot be deleted.

### 2. Workforce & Job Management
- **Job Role**: A position definition (e.g., Cashier, Barista) containing required qualification prerequisites and performance metrics.
- **Qualification Prerequisite**: A mandatory capability (e.g., POS System, Management) required to perform a shift.

### 3. Site & Attendance Operations
- **Site**: A physical operational location (branch/campus) defined by geographic latitude/longitude coordinates and network MAC whitelists.
- **Attendance Profile**: Rules governing expected clock-in/out times, required hours, and mandatory break durations.

### 4. Advanced Scheduling Engine
- **Draft Shift**: An uncommitted shift assignment created on the scheduler canvas. Invisible to employees until published.
- **Published Shift**: A committed shift assignment visible to employees and active in the operational roster.
- **Pre-Flight Validation**: Automated engine checks executed before publishing:
  - **Qualification Hard Stop**: Blocks assignment if employee lacks job role prerequisites.
  - **Collision Hard Stop**: Blocks double-booking an employee across overlapping times.
  - **Overtime Risk Warning**: Flags shifts that push an employee past legal weekly hours limit.
- **Publish Justification**: Mandatory written audit log required from a manager to override overtime warnings during schedule publication.

### 5. Shift Market & Escrow
- **Market Shift**: An unassigned or dropped shift block made available on the internal organizational marketplace.
- **Standard Claim**: A shift claim that does not incur overtime. Auto-approved on a first-come, first-serve basis.
- **Escalated Claim**: A shift claim incurring overtime. Enters a `PendingApproval` state requiring branch manager authorization.

### 6. Gamification & Points Engine
- **Points Wallet**: A digital ledger tracking employee incentive points earned or lost.
- **Automation Rule**: Logic triggers evaluating attendance events (e.g., clock-in delay > 15m) to issue credits or debits automatically.
- **Disciplinary Violation**: A manually logged behavioral infraction attached to an employee record.

### 7. Rewards Inventory
- **Reward Item**: A digital voucher or gift card (e.g., Talabat, Noon) purchasable with wallet points.
- **Redemption Code**: A unique code imported via bulk Excel (.xlsx) file assigned to employees upon point redemption.

1. User Roles & Permissions 

For Developer: Implement Role-Based Access Control (RBAC) for the following. 

LFZ Context (Facility Owners): 

LFZ Admin: Can create LFZ Staff users. Can approve requests (if applicable). 

LFZ Staff: Can invite guests (Auto-approved). 

 Can approve walk-in guests specifically visiting them. 

Enterprise Context (Tenants): 

Enterprise Admin: Can create Enterprise Users. Approves all invites initiated by their users. Approves all walk-in guests for their enterprise. 

Enterprise Users: Can initiate guest invites (Requires Admin approval). 

Operational Context: 

Gate Admin: Can view approved lists. Can initiate walk-in requests. 

Shape 

2. Module: User Management (Admin Dashboard) 

Who uses this: LFZ Admin & Enterprise Admin. 

Designer Checklist (UI) 

[ ] Create User Button: Opens a modal or new page. 

[ ] User Form Fields: 

Name (Text) 

Official Email (Email/Text) 

Phone Number (Number) 

Department (Text/Dropdown) 

[ ] User List View: Table showing registered staff members. 

Developer Checklist (Logic) 

[ ] Constraint: Created users are "Standard Users" by default. They cannot be given approval rights via this form. 

[ ] Email Trigger: Send welcome email/password setup upon account creation. 

Shape 

3. Module: The Invitation Form (Web Portal) 

Who uses this: Enterprise Users & LFZ Staff. 

Designer Checklist (UI) 

[ ] Guest Details Section: 

Main Guest Name: Text field. 

Phone Number: Number field. 

Email: Email field (Label as Optional). 

[ ] Visit Details Section: 

Expected Arrival: Date & Time picker. 

Expected Departure: Date & Time picker. 

[ ] Proxy Toggle ("On Behalf Of"): 

Check box or Toggle switch: "Are you inviting on behalf of someone else?" 

Interaction: If Yes $\rightarrow$ Reveal Text Box for "Name of Person". 

[ ] Group & Vehicle Section: 

Number of Guests: Number counter. 

Number of Vehicles: Number counter. 

Vehicle Details: If vehicles > 0, show dynamic text fields for Plate Numbers. (e.g., If 2 vehicles selected, show 2 text boxes). 

Developer Checklist (Logic) 

[ ] Enterprise Logic: If User = Enterprise User $\rightarrow$ Status = PENDING_APPROVAL (Notifications sent to Enterprise Admin). 

[ ] LFZ Logic: If User = LFZ Staff $\rightarrow$ Status = APPROVED (Synced directly to Gate). 

[ ] Data Structure: Ensure the database links the "Main Guest" ID to multiple "Plate Numbers" and the "Additional Guest Count." 

Shape 

4. Module: Gate Operations (Tablet/Desktop View) 

Who uses this: Gate Admin. 

Image of mobile app gate pass interface 

Shutterstock 

Designer Checklist (UI) 

[ ] Search/Check-in View: Search bar to look up guest name or vehicle number, car model and color. 

State: Approved $\rightarrow$ Show "Allow Entry" button. 

 State: Not Found/Not Approved $\rightarrow$ Show "Create Walk-in Request" button. 

[ ] Walk-in Request Form: 

Name: Text field. 

Enterprise Visiting: Dropdown list (Must list all registered Enterprises). 

Who to Visit: Text box (Name of host). 

Phone Number: Number field. 

Email: Email field (Optional). 

Vehicle Number: Text field (if any). 

Number of Guests: Number field. 

Submit Button: Label "Request Approval". 

Developer Checklist (Logic) 

[ ] Routing Logic (Crucial): 

If Visitor selects Enterprise: The request is sent to the Enterprise Admin dashboard/email for approval. 

If Visitor selects LFZ Staff: The request is sent to the Specific LFZ Staff member (Host) for approval. 

[ ] Real-time Update: Gate dashboard must refresh status once approval is clicked by the Admin/Staff. 

Shape 

5. Approval Workflows (Backend) 

Developer Checklist 

[ ] Enterprise Invite Flow: 

User fills form. 

System notifies Enterprise Admin. 

Admin clicks "Approve". 

System updates status to "Approved". 

Gate sees Guest. 

[ ] LFZ Invite Flow: 

Staff fills form. 

System sets status to "Approved" immediately. 

Gate sees Guest. 

[ ] Walk-in Flow: 

Gate Admin fills form. 

System identifies target (Enterprise Admin OR Specific LFZ Staff). 
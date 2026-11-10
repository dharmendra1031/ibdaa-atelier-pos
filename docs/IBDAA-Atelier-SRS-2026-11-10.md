<!-- Source document: https://docs.google.com/document/d/1OWvb0xnisZzJTEOdGS1sY3PHn_WSn6XmysiM0PtCnok/edit -->
<!-- Snapshot prepared 2026-11-10. Keep the original Google Doc as the canonical source. -->

SOFTWARE REQUIREMENTS  
SPECIFICATION

Tailoring Shop Management  
& Point of Sale System

Reference Website: ibdaaatelier.com

Platform: Web Application  |  Languages: English & Arabic  |  Currency: KWD

Prepared: 10 November 2026

## Document Information

| Item | Details |

| --- | --- |

| Document | Software Requirements Specification (SRS) |

| Project | Tailoring Shop Management & Point of Sale System |

| Reference | https://ibdaaatelier.com/ |

| Business Domain | Men's clothing, Moroccan garments, dishdasha tailoring and alteration services |

| Primary Users | Administrator, POS / Salesperson |

| Interface Languages | English and Arabic (including RTL layout) |

| Default Currency | Kuwaiti Dinar (KWD / KD) |

| Status | Functional requirements baseline |

## 1. Project Overview

The proposed solution is a web-based application for managing a tailoring and clothing shop. It will provide an Admin Management Panel and a Point of Sale (POS) Panel. The product catalogue and product-detail experience will be based on the supplied IBDAA Atelier reference, adapted for fast in-store sales and order processing.

Products will be presented category-wise with images, descriptions, prices, available sizes and configurable measurement fields. At POS, staff will select products, capture customer details and measurements, add alteration services, set a delivery date, record payments and print an invoice.

## 2. Project Objectives

- Digitize shop sales, customer records, tailoring measurements and alteration services.

- Manage categories and products with multiple images, bilingual details and configurable prices.

- Capture measurements for each product/order item and preserve them with the transaction.

- Manage delivery dates, order statuses, payments, balances and daily cash collection.

- Provide sales, collection, customer, product, service and order reports.

- Control access through user roles and permissions.

## 3. Scope and User Roles

### 3.1 Administrator

- Full dashboard and reporting access.

- Manage categories, products, images, pricing, sizes and measurement definitions.

- Manage service categories and alteration services.

- Manage customers, users, roles, permissions and system settings.

- Review orders, payments, cash register sessions and daily collection.

### 3.2 POS User / Salesperson

- Browse active products by category and search products.

- Select sizes, enter measurements and tailoring instructions.

- Create or select customers and enter delivery dates.

- Add alteration services, apply permitted discounts and record payments.

- Print invoices and view permitted sales/collection information.

## 4. Admin Management Panel

### 4.1 Admin Dashboard

The dashboard shall provide an at-a-glance view of shop performance, with filters for Today, Yesterday, This Week, This Month and Custom Date Range.

- Total sales today and this month; order count; total customers.

- New, pending, processing, ready-for-delivery and delivered orders.

- Outstanding balances and advance payments.

- Cash, KNET/card, bank transfer and other collections.

- Daily/monthly sales charts, payment-method summary, category-wise sales and service-wise sales.

### 4.2 Category Management

Admin can create, edit, activate/deactivate and manage an unlimited number of product categories. Deletion must be restricted where historical or product references exist.

| Field | Requirement |

| --- | --- |

| Category ID | Auto-generated unique identifier |

| Category Name | English and Arabic |

| Category Image / Icon | Optional |

| Description | Optional; English and Arabic |

| Display Order | Controls POS display sequence |

| Status | Active / Inactive |

Suggested initial categories, all configurable: Traditional Moroccan, Casual Moroccan, Casual Trend, Dishdasha Tailoring, Jalabiya, Gandoura, Jabador, Caftan and Accessories.

### 4.3 Product Management

#### Product information

| Field | Requirement |

| --- | --- |

| Product ID / SKU | Auto-generated ID and unique product code/SKU |

| Product Name | English and Arabic |

| Category / Subcategory | Category required; subcategory optional |

| Description | English and Arabic |

| Pricing | Regular price and optional discounted price in KD |

| Product Type | Configurable product classification |

| Sizes | Product-specific size options |

| Status | Active / Inactive |

| Display Order | Controls product order in POS |

#### Product images

- Support at least four images per product, with additional images allowed.

- Set one image as the primary image and display the others in a gallery.

- Preview, replace and delete images before/after saving, subject to permissions.

- Support JPG, JPEG, PNG and WebP; optimize image size while preserving quality.

- Example gallery: front view, back view, side view and fabric/embroidery detail.

#### Pricing and availability

The system shall support regular price, discount price, automatic discount display and configurable product availability. Only active products may be selected for new POS orders. Inactive products must remain available in historical order records and reports.

### 4.4 Size and Size Chart Management

Admin can define product-specific sizes such as S, M, L, XL, XXL, XXXL and Custom Size. Size charts shall support centimetres by default and configurable values for each product or product type.

### 4.5 Product Measurement Configuration

Measurement fields must be configurable in Admin and assigned by category and/or product. Do not hardcode the measurement form in the POS.

| Default Measurement | Unit | Configuration |

| --- | --- | --- |

| Chest | cm | Mandatory / Optional |

| Shoulder | cm | Mandatory / Optional |

| Sleeve Length | cm | Mandatory / Optional |

| Total Length | cm | Mandatory / Optional |

| Custom Measurement | Configurable | Admin-defined name, unit and validation |

Each measurement definition shall support English and Arabic labels, unit, mandatory/optional flag, display order, applicable category/product and active/inactive status.

## 5. Service and Alteration Management

### 5.1 Service Categories

Admin can create service categories such as Alteration, Length Reduction, Sleeve Adjustment, Waist Adjustment, Size Adjustment, Repair and Other Tailoring Services.

### 5.2 Service Master

| Field | Requirement |

| --- | --- |

| Service ID | Auto-generated |

| Service Category | Linked to service category |

| Service Name | English and Arabic |

| Description | Optional, bilingual |

| Service Price | Configurable amount in KD |

| Estimated Completion Time | Optional |

| Status | Active / Inactive |

Example prices are illustrative only and must be configurable by Admin: Length Reduction KD 2.000; Sleeve Adjustment KD 3.000; Waist Adjustment KD 2.500; General Alteration KD 2.000.

## 6. Customer Management

### 6.1 Customer Master

| Field | Requirement |

| --- | --- |

| Customer ID | Auto-generated |

| Customer Name | English and Arabic stored separately |

| Mobile Number | Primary customer lookup field |

| Alternate Mobile | Optional |

| Address / Notes | Optional |

| Registration Date | System generated |

### 6.2 Bilingual Name Entry

- When English name is entered, the system may generate an Arabic name suggestion; when Arabic is entered, it may suggest English transliteration.

- Both language fields must remain editable and separately stored.

- Automatically generated text must not overwrite a manually corrected value.

- Name conversion should use a configured transliteration/translation service or approved local mechanism.

### 6.3 Customer Search and History

Search customers by mobile number, English name, Arabic name or Customer ID. Customer history shall show previous orders, total purchases, pending/delivered orders, saved measurements, payment history and outstanding balance.

## 7. Point of Sale (POS) Module

### 7.1 POS Main Screen

The POS screen should be optimized for desktop/tablet shop-counter use and divided into two working areas:

| Product Catalogue – Left | Current Order / Cart – Right |

| --- | --- |

| Category and subcategory list | Selected products |

| Product image grid and product search | Selected size and quantity |

| Product name, regular/discount price | Measurement values and tailoring notes |

| Active products only | Additional services, discounts and totals |

### 7.2 Product Selection Workflow

- Select a category; show active products in an image-based grid.

- Select a product to view its images, description, prices and available sizes.

- Select size and enter required measurements and tailoring instructions.

- Enter quantity and add the item to the cart.

- Repeat for multiple products; each cart item must retain its own size and measurements.

### 7.3 Measurement Entry

When a product is added, POS shall load the measurement form configured for that product/category. Mandatory fields must be validated. Measurements must be stored against the specific order item, remain editable before order confirmation, and be printed on the invoice. Different items in one order may have different measurements.

### 7.4 Customer Selection

POS users can search and select an existing customer by mobile/name, or create a new customer by entering English name, Arabic name suggestion and mobile number. Address is optional. Existing customer details and permitted history may be loaded automatically.

### 7.5 Order Details

- Auto-generated order number and date/time.

- Customer details and POS user.

- Products, product references/images, sizes, quantities and item prices.

- Measurements and tailoring instructions per order item.

- Additional services, discounts, charges and totals.

- Amount paid, balance due, payment method and delivery date.

- Delivery notes and order status.

### 7.6 Delivery Date

A delivery date is required before order confirmation. Delivery time and instructions may be optional. The system shall support customer pickup/collection and show upcoming and overdue deliveries on the Admin dashboard.

## 8. Order Management

### 8.1 Order Statuses

| Status | Meaning |

| --- | --- |

| New Order | Order created |

| Confirmed | Order accepted |

| In Processing | Work is in progress |

| Ready for Delivery | Order is ready for customer collection/delivery |

| Delivered | Order handed over to customer |

| Cancelled | Order cancelled with reason/authorization |

Admin may configure additional statuses. Every status change must record previous status, new status, user, date/time and remarks.

### 8.2 Search and Filters

Search by order number, customer name/mobile, product name, POS user or delivery date. Filter by order date, delivery date, status, payment status, customer and POS user.

### 8.3 Order Editing and Audit

Authorized users may edit customer details, measurements, delivery date, services and notes. Changes after confirmation must be logged. Price/discount changes, refunds and cancellations must be permission-controlled.

## 9. Payment and Daily Collection

### 9.1 Payment Methods

Default configurable methods: Cash, KNET/Card, Bank Transfer and Other. Support full payment, advance/partial payment, remaining balance and split payment across multiple methods.

Example: Order total KD 25.000; Cash KD 10.000; KNET KD 10.000; balance due KD 5.000. The balance must be calculated automatically.

### 9.2 POS Daily Collection

POS users shall see permitted daily figures: opening cash, cash sales/receipts, KNET/card, bank transfer, other collection, cash refunds, total collection, closing cash, order count and outstanding amount.

### 9.3 Admin Daily Collection

Admin shall see consolidated and POS-user-wise daily collection, including total sales, cash, KNET/card, bank transfer, other collection, refunds, net collection, opening/closing cash and cash differences.

### 9.4 Cash Register Session

- Open register and enter opening cash. Configure by admin yes or no

- Record payments and permission-controlled cash expenses.

- Close register and enter actual closing cash.

- Calculate expected cash and display cash difference.

- Submit closing report for Admin review/approval.

## 10. Invoice and Printing

### 10.1 Invoice Content

- Shop name, logo, address and contact information.

- Invoice number, order number and invoice date.

- Customer English/Arabic name and mobile number.

- Product name, SKU, optional image, size, quantity, unit price, discount and line total.

- All configured measurements and tailoring instructions for each item.

- Service name, service price and service notes.

- Subtotal, discount, additional charges, grand total, paid amount and balance due.

- Payment method, delivery date, terms and thank-you message.

### 10.2 Printing Requirements

Support invoice print, customer copy, shop copy, reprint of previous invoices, A4 and configurable 80mm thermal printer layouts. Arabic and English must render correctly. Measurements must be clearly visible on the printed invoice.

## 11. Reports and Analytics

| Report Group | Required Reports |

| --- | --- |

| Sales | Daily, weekly, monthly, custom date, POS-user-wise, category-wise, product-wise, service-wise, discount and cancelled orders |

| Collection | Daily collection, payment-method-wise, cash, KNET/card, bank transfer, POS-user-wise, outstanding, advance, refund and cash-register closing |

| Orders | New, pending, processing, ready, delivered, cancelled and overdue delivery orders |

| Customers | Customer list, new customers, purchase history, customer-wise sales, outstanding and repeat customers |

| Products | Product list, category-wise, active/inactive, top-selling and product-wise revenue |

Reports shall support date-range filters and PDF export, Excel export and printing. POS report visibility must follow user permissions.

## 12. User and Employee Management

Admin can create, edit, deactivate and manage users. Fields include Employee ID, name, mobile number, username, password, role, assigned permissions and account status.

Permissions should be configurable for module access, discounts, order editing, refunds, cancellations, payment visibility, collection access and report access. Record user activity for audit purposes.

## 13. System Settings

- Shop name, logo, address and contact details in English/Arabic.

- Currency (default KWD), measurement unit (default cm), date/time and language settings.

- Invoice/order number prefixes and invoice footer/terms.

- Default delivery settings and configurable payment methods/order statuses.

- Product image settings and other operational preferences.

## 14. Business Rules

- Only active products can be added to new POS orders.

- Inactive products remain available in historical transactions.

- A product may have multiple images and one primary image.

- Sizes and measurement fields may differ by product/category.

- Measurements are saved against the specific order item and printed on the invoice.

- Every order must be linked to a customer.

- English and Arabic customer names are stored separately.

- A delivery date is required before order confirmation.

- Additional services are included in the order total.

- Discounts, payments and outstanding balances are calculated automatically.

- Partial and split payments are supported.

- Daily collection is available to authorized POS users and Admin.

- Cancelled orders are excluded from net sales, while remaining in audit/history.

- Payment transactions must have audit records.

- Changes to product details/prices must not change historical invoices.

- Changes to confirmed orders must be recorded in the activity log.

- Refunds, discounts and cancellations require appropriate permissions.

## 15. Main Workflows and Examples

### 15.1 Admin Creates a Product

- Open Product Management and select/create a category.

- Enter English/Arabic product names and descriptions.

- Enter regular and discounted prices.

- Upload at least four images and choose the primary image.

- Configure sizes, size chart and measurement fields.

- Set status to Active and save.

Result: The product becomes available in the POS catalogue.

### 15.2 POS Creates a Tailoring Order

- Log in and select a product category.

- Select a product and review its images.

- Select size and enter measurements/instructions.

- Add the item to the cart; add more products if required.

- Select an existing customer or create a new customer.

- Set delivery date and review order totals.

- Record advance/full payment, confirm the order and print the invoice.

Result: Customer, product, size, measurements, services, payment and delivery details are saved with the order.

### 15.3 POS Adds an Alteration Service

- Select customer/order and open the Service section.

- Choose a service category and service.

- Enter quantity, price (if permitted) and notes.

- Add service to cart and recalculate total.

- Save the order and print the updated invoice.

### 15.4 Delivery and Collection

- Find the order and review customer/measurement details.

- Change status to Ready for Delivery.

- On handover, collect any remaining balance.

- Mark the order Delivered and retain delivery/payment history.

### 15.5 Daily Collection Closing

- POS user reviews payment-method collections.

- Enter actual closing cash and submit the session.

- System calculates expected cash and any difference.

- Admin reviews individual and consolidated closing reports.

## 16. Logical Database Entities

The database design should include, at minimum, the following logical entities. Final physical schema, keys, indexes and normalization will be defined during detailed technical design.

| No. | Logical Entity |

| --- | --- |

| 1 | Users |

| 2 | Roles |

| 3 | Permissions |

| 4 | Categories |

| 5 | Subcategories |

| 6 | Products |

| 7 | ProductImages |

| 8 | ProductPrices |

| 9 | ProductSizes |

| 10 | MeasurementDefinitions |

| 11 | ProductMeasurements |

| 12 | Customers |

| 13 | ServiceCategories |

| 14 | Services |

| 15 | Orders |

| 16 | OrderItems |

| 17 | OrderItemMeasurements |

| 18 | OrderItemServices |

| 19 | Payments |

| 20 | PaymentMethods |

| 21 | CashRegisterSessions |

| 22 | CashRegisterTransactions |

| 23 | OrderStatusHistory |

| 24 | InvoiceRecords |

| 25 | SystemSettings |

| 26 | AuditLogs |

## 17. Non-Functional Requirements

### 17.1 Performance

- Optimize product images and load catalogue images efficiently.

- Product search and order saving should be responsive under expected shop load.

- Invoice generation and printing must be reliable.

### 17.2 Security

- Secure authentication and password hashing.

- Role-based access control and server-side permission validation.

- Audit logs for sensitive actions and transaction changes.

- Secure database and file-storage access.

### 17.3 Usability and Localization

- Responsive desktop/tablet interface with a fast POS workflow.

- English and Arabic UI, including right-to-left layout.

- Clear measurement labels, large product images and easy data entry.

- Correct Arabic font rendering in screens and printed invoices.

### 17.4 Backup and Data Integrity

- Scheduled database and product-image backups.

- Documented restore procedure.

- Unique order and invoice numbers.

- Preserve historical transaction details and measurement snapshots.

- Validate payment totals and outstanding balances.

## 18. Acceptance Test Scenarios

| Test Scenario | Expected Result |

| --- | --- |

| Create category | Category is saved and available in POS. |

| Create product | Product details are saved successfully. |

| Upload four images | All images are linked to the correct product. |

| Set primary image | Selected image appears as the main product image. |

| Deactivate product | Product is hidden from new POS sales. |

| Select product and size | Selected product and size appear in the cart. |

| Enter measurements | Values are saved against the correct order item. |

| Add multiple products | Each item retains its own measurements. |

| Create/search customer | Customer can be saved and found by mobile/name. |

| Bilingual customer name | Both language values are saved and remain editable. |

| Add alteration | Service charge is included in order total. |

| Set delivery date | Date is saved and searchable. |

| Partial/split payment | Paid amount and balance are calculated correctly. |

| Print invoice | Customer, product, size and measurements print correctly. |

| Daily collection | Payment-method totals are calculated correctly. |

| Cash closing | Expected and actual cash and difference are displayed. |

| View sales report | Report matches selected date range and filters. |

| Change order status | Status history records user and timestamp. |

| Reprint invoice | Original transaction details are reproduced. |

## 19. Initial Website Catalogue Migration

The existing IBDAA Atelier website catalogue must be used as the baseline for initial product setup. The objective is to reproduce the current catalogue as closely as possible, rather than populate the system with sample data.

- All currently available product categories and subcategories.

- All existing products and their names.

- Existing English and Arabic descriptions, where available.

- All available product images/gallery images.

- Original regular and discounted prices, where displayed.

- Available sizes, size charts and measurement fields.

- Current product availability/active status, where ascertainable.

Migration note: The implementation team must verify the live website catalogue and obtain the original product content and image files before import. This SRS defines the migration requirement; extraction and import are implementation activities. Website content, images and prices may change, so the migrated dataset should be validated against the source at the time of migration.

## 20. Recommended Technical Architecture

| Layer | Recommended Technology |

| --- | --- |

| Backend | ASP.NET Core 10 |

| Web UI | ASP.NET Core MVC |

| Database | Microsoft SQL Server 2019 |

| UI Framework | Bootstrap 5 |

| Languages | English and Arabic with RTL support |

| Printing | Browser print with A4 and 80mm thermal layouts |

| Image Storage | Server-based image storage with optimized image delivery |

The final architecture may be adjusted after confirming hosting, deployment, printer models, expected concurrent users and integration requirements.

## 21. Future Enhancements

- WhatsApp/SMS order notifications and delivery reminders.

- Customer-facing order tracking.

- Online ordering website integration.

- Loyalty and reward points.

- Barcode-based product selection.

- Tailoring production workflow and work assignment.

- Multiple shop branches with centralized reporting.

- Reusable customer measurement profiles and templates.

## 22. Final Deliverables

- Admin Management Panel.

- Category and Product Management with multiple images.

- Product size, size-chart and dynamic measurement configuration.

- Customer Management with bilingual names.

- Service and Alteration Management.

- Point of Sale with image-based product catalogue.

- Customer-specific measurements and order instructions.

- Order, delivery and status management.

- Payment, daily collection and cash register management.

- Invoice generation and printing.

- Dashboard and business reports.

- User roles, permissions and audit logging.

- English/Arabic interface and backup capability.

## 23. Assumptions and Items to Confirm

The following points should be finalized before development sign-off:

- Exact shop branding, logo, address and invoice layout.

- Whether product stock/inventory quantities are required, or products are made to order.

- Whether one order can have multiple delivery dates or partial deliveries.

- Whether measurement profiles should be reusable across future orders.

- Whether Arabic name auto-fill should use transliteration only or a translation service.

- Required thermal printer make/model and paper width.

- Whether multiple branches, multiple tills or offline POS operation are required.

- Discount approval limits, refund rules and cash expense permissions.

END OF SOFTWARE REQUIREMENTS SPECIFICATION

# 🎬 CinéVerse - Modern Cinema Ticket Booking & Management System

> A modern online cinema ticket booking and theater management platform built on **ASP.NET MVC 5**, **Entity Framework 6**, and **Microsoft SQL Server**.

---

---

## 📖 Project Overview

**CinéVerse** is an end-to-end web application crafted to deliver an elevated movie-going experience for audiences and streamlined operational workflows for cinema operators. Featuring an immersive cinematic dark-mode UI, the platform provides seamless user journeys from movie discovery and seat reservation to checkout and digital ticketing.

---

## ✨ Key Features

### 🌟 Client Portal

* **Movie Discovery:** Explore Now Showing, Coming Soon, and Archived titles complete with trailers, cast & crew metadata, runtime, and age classification labels (P, T13, T16, T18).
* **Instant Search:** Real-time query matching across titles, genres, directors, and cast members.
* **Multi-Cinema Schedules:** Browse showtimes by release date, theater complex, and showtime slots.
* **Interactive Seat Matrix:**
* Visual seat map with live occupancy status (Available, Selected, Reserved).
* Dynamic seat tiers: **Standard**, **VIP**, and **Couple Seats**.
* Automated pricing calculations based on seat tier, weekend surcharges, and prime-time show slots.


* **Promotions & Vouchers:** Apply discount coupon codes with automated percentage deductions and max discount caps.
* **Checkout & Invoicing:** Customer detail confirmation, automated transaction processing, and instant e-ticket code generation.
* **Customer Account Management:** Authentication, secure sign-in, order history tracking, and digital e-ticket retrieval.

---

### 🛡 Admin Portal

* **Analytics Dashboard:** High-level metrics tracking total ticket sales, gross revenue, and trending movies.
* **Movie & Genre Catalog:** Create, edit, manage poster assets, toggling release statuses, and categorical grouping.
* **Cinema Complexes & Screening Halls:**
* Manage theater complexes and physical location data.
* Add screening rooms with automated grid-based seat layout generation (rows × columns).


* **Showtime Scheduling:**
* Filter halls by cinema location.
* Automated conflict-detection engine (prevents overlapping schedules across the same hall, date, and timeframe).


* **Seat Tiers & Pricing Control:** Configure base tariffs and tier multipliers across seat classes.
* **Voucher Management:** Configure discount rates, activation windows, and redemption quotas.
* **Orders & Ticket Auditing:** Query booking records and update transactional statuses.

---

## 🛠 Tech Stack

| Architecture Layer | Technologies & Libraries |
| --- | --- |
| **Backend** | C#, ASP.NET MVC 5, .NET Framework 4.7.2 |
| **ORM / Data Access** | Entity Framework 6 (Database First / EDMX) |
| **Database** | Microsoft SQL Server (Transact-SQL) |
| **Frontend UI** | Razor Views (`.cshtml`), HTML5, CSS3, JavaScript |
| **UI Framework & Icons** | Bootstrap 5, Bootstrap Icons, Google Fonts (DM Sans, Bebas Neue) |
| **Client Libraries** | jQuery 3.7, Newtonsoft.Json, WebGrease, Modernizr |

---

## 🗄 Database Schema

The core database `QL_VEPHIM` consists of the following primary tables:

* `RAPCHIEU` — Cinema branches (Branch ID, name, street address, hall count).
* `PHONGCHIEU` — Screening auditoriums mapped to cinemas (Row/column capacity).
* `LOAIGHE` — Seat tiers and base pricing models (Standard, VIP, Couple).
* `GHE` — Individual seat coordinates and layouts within auditoriums.
* `PHIM` & `THELOAI` — Movie listings, runtime metadata, and genre mapping.
* `KHUNGGIO` — Predefined daily screening time slots.
* `LICHCHIEU` — Scheduled screenings connecting Movie, Hall, Time Slot, and Date.
* `KHACHHANG` & `NHANVIEN` — Role-based access control for customers and administrative staff.
* `KHUYENMAI` — Promotional vouchers, discount percentages, and validity limits.
* `DONHANG` & `VE` — Transactions, purchased seats, and payment records.

---

## 🚀 Installation & Local Setup

### Prerequisites

* **Visual Studio 2019 / 2022** (with the *ASP.NET and web development* workload installed)
* **Microsoft SQL Server 2017+** & **SQL Server Management Studio (SSMS)**
* **.NET Framework 4.7.2 Developer Pack**

### Setup Steps

1. **Clone the repository:**
```bash
git clone https://github.com/YuluWusu/CineVerse.git

```


2. **Database Initialization:**
* Open SSMS and connect to your local SQL Server instance.
* Open the SQL script located at `DoAn_QuanLyVeXemPhimTrucTiep/QL_VEPHIM.sql`.
* Execute the script (**F5**) to provision the `QL_VEPHIM` database and seed default data.


3. **Configure Connection String:**
* Open the project solution (`.sln`) in Visual Studio.
* Locate and open `Web.config`, then update the `QL_VEPHIM2Entities` connection string:
```xml
<add name="QL_VEPHIM2Entities" 
     connectionString="metadata=res://*/Models.QL_VEPHIM_DuLieu.csdl|...;provider=System.Data.SqlClient;provider connection string=&quot;data source=YOUR_SERVER_NAME;initial catalog=QL_VEPHIM;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" 
     providerName="System.Data.EntityClient" />

```




*(Replace `YOUR_SERVER_NAME` with your local SQL Server instance name, e.g., `.`, `localhost`, or `DESKTOP-XXXXX\SQLEXPRESS`).*
4. **Build and Run:**
* Right-click the Solution in Visual Studio and select **Restore NuGet Packages**.
* Press **Ctrl + F5** (or click the **IIS Express** play button) to launch the web application.



---

## 👤 Demo Accounts

| Role | Username / Email | Default Password | Permissions |
| --- | --- | --- | --- |
| **Administrator** | `admin@cineverse.vn` | `123456` | Full administrative control |
| **Customer** | `nguyenvanan@gmail.com` | `123456` | Standard ticket booking & user dashboard |

---

## 📝 License & Attribution

Developed for academic coursework and study for the **Web Development** course at Ho Chi Minh City University of Industry and Trade (HUIT).

© 2026 **CinéVerse**. All rights reserved.

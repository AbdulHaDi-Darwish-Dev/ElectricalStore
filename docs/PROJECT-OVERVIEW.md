# Project overview

> **Purpose:** What this application is.  
> **Authority:** Product positioning for this host app (not Permixa itself).  
> **Update when:** Positioning, audience, or major capability scope changes.

## Summary

**ElectricalStore** is an ASP.NET Core application that uses **Permixa** as its Identity & Access Management framework via NuGet packages.

This repository is the **consumer application**. Permixa remains an external reusable framework.

Current business scope includes **Categories**, **Product + ProductVariant**, **Media/Images (Cloudinary)**, **Inventory**, **Shipping (fixed-fee Delivery Zones)**, **Checkout Preview**, **Orders** (guest + authenticated, COD, admin lifecycle with inventory reservation on Confirm), and **Access Management** (Back Office IAM Dashboard adapters over Permixa). Cart is frontend-only (not persisted).

## Stack

- .NET 8
- Clean Architecture (Domain / Application / Infrastructure / Api)
- SQL Server (one database; two DbContexts)
- Permixa.AspNetCore (+ optional Redis / Resend providers)

## Non-goals

- Not a fork of Permixa
- Not a multi-tenant product scaffold (unless you add it later)
- Not a deployment/IaC starter (CI only)

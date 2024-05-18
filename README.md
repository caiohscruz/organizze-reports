# Organizze Reports

## Description
Organizze Reports is a console application that generates reports based on financial transactions from the Organizze API. It provides insights and analysis of transactions grouped by categories.

## Features
- Generate category reports: The application generates spreadsheets that present transactions segregated by periods and amounts by category and period.
- Enriched category names: Categories are retrieved with enriched names, including the parent category name for nested categories.
- Filtering out irrelevant data: Transactions and categories that are not relevant for generating reports are filtered out.
- Future estimation: The application generates future estimation spreadsheets based on transactions from the next 12 months.

## Installation
1. Clone the repository: `git clone https://github.com/caiohscruz/organizze-reports.git`
2. Open the solution in Visual Studio.
3. Build the solution to restore NuGet packages.

## Configuration
1. Open the `appsettings.json` file.
2. Update the `OrganizzeApiSettings:ApiKey`, `OrganizzeApiSettings:UserName`, `OrganizzeApiSettings:Email`, and other necessary configuration settings.

## Components
- OrganizzeAPIAdapter: A class that interacts with the Organizze API to retrieve transactions, categories, accounts, and credit cards.
- ExcelService: A service that generates Excel spreadsheets based on the provided data.
- ReportService: A service that generates reports based on transactions and categories.


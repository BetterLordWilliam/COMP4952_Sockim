![no icon i guess...](./wwwroot/icons/message.png)

---

# Sockim

Browser based instant messaging application.

You can try the application [here](https://sockim.azurewebsites.net)!

## Required Software

**dotnet-ef tools**

> [!NOTE]
> This is a .NET 9 application, and using the latest version of `dotnet-ef` will not work.
> ```powershell
> dotnet tool install -g dotnet-ef --version 9.0.11 --allow-downgrade
> ```

**MySQL (used for the database)**

[lts version latest tested with](https://dev.mysql.com/downloads/mysql/)

> [!NOTE]
> The development environment is configured for a machine that has MySQL installed on it.

---

## Installation

1. Fork/Clone this repository
2. In root run

```powershell
dotnet restore
```

3. Next you will need execute the script to define the database \& service account
4. Copy the path to the [`dcl.sql` sql script](./data/dcl.sql), which defines these attributes
5. Login to MySQL in a shell and execute the script (using of course your path):

```sql
source C:\\path\\to\\project\\data\\dcl.sql
```

6. Now you should be able to apply the application's migrations & possess a development ready instance of `sockimdatabase`

```powershell
dotnet ef database update
```

7. To start the application, ensure you do so using an https endpoint

```powershell
dotnet build && dotnet run --urls 'https://localhost:7029'
```

> [!NOTE]
> You may need to add a trusted certificate for https
> ```powershell
> dotnet dev-certs https --trust
> ```
